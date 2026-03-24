#Requires -Version 5.1
<#
.SYNOPSIS
    Helper script for the dev/test containers.

.DESCRIPTION
    Manages Docker-based dev containers for building, testing, and running
    integration tests for PSProxmoxVE. Works on Windows, macOS, and Linux.

    For x86-only commands (integration, provision, cleanup), use -DockerHost
    to run containers on a remote Docker host via SSH. The script syncs the
    repo to the remote host automatically.

.PARAMETER Command
    The action to perform:
      shell        - Open pwsh in the dev container (default)
      build        - Build the module inside the container
      test         - Run unit tests (ARM + x86)
      integration  - Provision nested PVE VMs, run integration tests, cleanup (x86 only)
      provision    - Provision nested PVE VMs only, no tests (x86 only)
      cleanup      - Destroy provisioned VMs (x86 only)
      stop         - Stop all containers
      rebuild      - Rebuild container image(s)

.PARAMETER PveVersion
    PVE version to test against (8, 9, or all). Default: all.
    Only used with 'integration' and 'provision' commands.

.PARAMETER NoCleanup
    When used with 'integration', skips cleanup after tests complete.
    Nested PVE VMs are left running for inspection or re-testing.
    Run './tests/dev.ps1 cleanup' to destroy them later.

.PARAMETER DockerHost
    SSH destination for a remote Docker host (e.g. user@runner-vm).
    When set, the repo is rsynced to the remote host and all Docker
    commands run against the remote daemon. Useful for running x86
    containers from an ARM Mac.

    Prerequisites on the remote host:
      - Docker installed
      - SSH user in the 'docker' group (sudo usermod -aG docker $USER)
      - SSH key-based auth from the local machine

.EXAMPLE
    ./tests/dev.ps1
    # Opens a pwsh shell in the dev container

.EXAMPLE
    ./tests/dev.ps1 test
    # Builds the module and runs unit tests

.EXAMPLE
    ./tests/dev.ps1 integration -DockerHost user@runner-vm
    # Syncs repo to runner-vm, provisions nested PVE, runs tests, cleans up

.EXAMPLE
    ./tests/dev.ps1 integration 9 -DockerHost user@runner-vm
    # Same but only for PVE 9
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('shell', 'build', 'test', 'integration', 'provision', 'cleanup', 'stop', 'rebuild')]
    [string] $Command = 'shell',

    [Parameter(Position = 1)]
    [string] $PveVersion = 'all',

    [Parameter()]
    [string] $DockerHost,

    [Parameter()]
    [Alias('k')]
    [switch] $NoCleanup
)

$ErrorActionPreference = 'Stop'

# Resolve repo root (parent of tests/)
$RepoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $RepoRoot
try {

# ── Remote Docker host support ────────────────────────────────────────
# When -DockerHost is specified, we rsync the repo (including .env.test)
# to the remote host and set DOCKER_HOST so all docker/compose commands
# execute on the remote daemon. The compose file's volume mount (..:/repo)
# and build context reference the remote copy.

$RemoteRepoPath = $null

if ($DockerHost) {
    $RemoteRepoPath = "/tmp/psproxmoxve-dev"
    $env:DOCKER_HOST = "ssh://$DockerHost"

    Write-Host "Syncing repo to ${DockerHost}:${RemoteRepoPath}..."
    # Create remote directory
    ssh $DockerHost "mkdir -p $RemoteRepoPath"
    if ($LASTEXITCODE -ne 0) { throw "Failed to create remote directory" }

    # Rsync repo to remote host, excluding build artifacts
    rsync -az --delete `
        --exclude 'bin/' `
        --exclude 'obj/' `
        --exclude 'publish/' `
        --exclude 'TestResults/' `
        --exclude '.terraform/' `
        --exclude 'terraform.tfstate*' `
        --include '.env.test' `
        ./ "${DockerHost}:${RemoteRepoPath}/"
    if ($LASTEXITCODE -ne 0) { throw "Failed to sync repo to remote host" }

    Write-Host "Using remote Docker host: $DockerHost"
}

# When running remotely, docker compose reads the compose file locally but
# volume mounts resolve on the remote host. We generate a temporary override
# file that remaps volume mounts to the rsynced remote path.
$ComposeFile = 'tests/docker-compose.test.yml'
$ComposeArgs = @('-f', $ComposeFile)
$OverrideFile = $null

if ($RemoteRepoPath) {
    $OverrideFile = Join-Path ([System.IO.Path]::GetTempPath()) 'docker-compose.remote-override.yml'

    # Override volume mounts to point at the rsynced remote repo path.
    # env_file is NOT overridden — compose reads it locally and injects
    # the values as container env vars, which is what we want.
    @"
services:
  dev:
    volumes:
      - ${RemoteRepoPath}:/repo
  dev-infra:
    volumes:
      - ${RemoteRepoPath}:/repo
      - /opt/pve-isos:/opt/pve-isos
"@ | Set-Content -Path $OverrideFile -Encoding utf8

    $ComposeArgs = @('-f', $ComposeFile, '-f', $OverrideFile)
}

$DevContainer = 'psproxmoxve-dev'
$InfraContainer = 'psproxmoxve-dev-infra'
$RunIntegration = 'tests/infrastructure/scripts/run-integration.sh'

function Start-DevContainer {
    # 'up -d' is idempotent: starts if stopped, recreates if config/env changed, no-ops if current
    docker compose @ComposeArgs up -d dev
    if ($LASTEXITCODE -ne 0) { throw 'Failed to start dev container' }
}

function Start-InfraContainer {
    docker compose @ComposeArgs --profile infra up -d dev-infra
    if ($LASTEXITCODE -ne 0) { throw 'Failed to start infra container (x86 only)' }
}

function Invoke-BuildModule {
    param([string] $Container)
    docker exec $Container bash -c @"
dotnet publish src/PSProxmoxVE/PSProxmoxVE.csproj -c Release -f netstandard2.0 -o /tmp/publish 2>&1 | tail -1 && \
cp -r /tmp/publish/* /usr/local/share/powershell/Modules/PSProxmoxVE/ && \
echo 'Module installed to /usr/local/share/powershell/Modules/PSProxmoxVE'
"@
    if ($LASTEXITCODE -ne 0) { throw "Module build failed (exit code $LASTEXITCODE)" }
}

switch ($Command) {
    'shell' {
        if ($DockerHost) {
            Write-Warning "Interactive shell over remote Docker is not supported. Use: ssh $DockerHost 'docker exec -it $DevContainer pwsh -NoProfile'"
            return
        }
        Start-DevContainer
        docker exec -it $DevContainer pwsh -NoProfile
    }

    'build' {
        Start-DevContainer
        Invoke-BuildModule $DevContainer
    }

    'test' {
        Start-DevContainer
        Invoke-BuildModule $DevContainer
        docker exec $DevContainer pwsh -NoProfile -Command @'
            $config = New-PesterConfiguration
            $config.Run.Path = 'tests/PSProxmoxVE.Tests'
            $config.Run.Exit = $true
            $config.Filter.ExcludeTag = @('Integration')
            $config.Output.Verbosity = 'Detailed'
            Invoke-Pester -Configuration $config
'@
        if ($LASTEXITCODE -ne 0) { throw "Unit tests failed (exit code $LASTEXITCODE)" }
    }

    'integration' {
        # Full lifecycle: provision nested PVE VMs -> test -> cleanup (x86 only)
        # Requires PVE_ENDPOINT, PVE_API_TOKEN, PVE_TARGET_NODE, PVE_PASSWORD in .env.test
        # run-integration.sh handles module build if no pre-built artifact exists
        Start-InfraContainer
        if ($NoCleanup) {
            # Provision and test separately — leave VMs running for inspection
            docker exec $InfraContainer bash $RunIntegration provision
            if ($LASTEXITCODE -ne 0) { throw "Provisioning failed (exit code $LASTEXITCODE)" }
            docker exec $InfraContainer bash $RunIntegration test $PveVersion
            if ($LASTEXITCODE -ne 0) { throw "Integration tests failed (exit code $LASTEXITCODE)" }
            Write-Host 'VMs left running (-NoCleanup). Run ./tests/dev.ps1 cleanup to destroy them.'
        } else {
            docker exec $InfraContainer bash $RunIntegration all $PveVersion
            if ($LASTEXITCODE -ne 0) { throw "Integration tests failed (exit code $LASTEXITCODE)" }
        }
    }

    'provision' {
        # Provision nested PVE VMs only, without running tests (x86 only)
        # Useful for iterating: provision once, then shell in and run tests manually
        Start-InfraContainer
        docker exec $InfraContainer bash $RunIntegration provision
        if ($LASTEXITCODE -ne 0) { throw "Provisioning failed (exit code $LASTEXITCODE)" }
    }

    'cleanup' {
        # Destroy provisioned VMs
        Start-InfraContainer
        docker exec $InfraContainer bash $RunIntegration cleanup
        if ($LASTEXITCODE -ne 0) { throw "Cleanup failed (exit code $LASTEXITCODE)" }
    }

    'stop' {
        docker compose @ComposeArgs --profile infra down
    }

    'rebuild' {
        docker compose @ComposeArgs --profile infra down
        docker compose @ComposeArgs build --no-cache dev
        docker compose @ComposeArgs --profile infra build --no-cache dev-infra
        docker compose @ComposeArgs up -d dev
    }
}

} finally {
    if ($OverrideFile -and (Test-Path $OverrideFile)) {
        Remove-Item $OverrideFile -Force -ErrorAction SilentlyContinue
    }
    if ($DockerHost) {
        Remove-Item Env:\DOCKER_HOST -ErrorAction SilentlyContinue
    }
    Pop-Location
}
