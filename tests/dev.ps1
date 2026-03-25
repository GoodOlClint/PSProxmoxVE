#Requires -Version 5.1
<#
.SYNOPSIS
    Helper script for the dev/test containers.

.DESCRIPTION
    Manages Docker-based dev containers for building, testing, and running
    integration tests for PSProxmoxVE. Works on Windows, macOS, and Linux.

    Use switches to compose actions: -Provision -Integration -Cleanup can
    be combined in a single invocation.

    For x86-only commands (integration, provision, cleanup), use -DockerHost
    to run containers on a remote Docker host via SSH. The script syncs the
    repo to the remote host automatically.

.PARAMETER Shell
    Open an interactive pwsh shell in the dev container.

.PARAMETER Build
    Build the module inside the container.

.PARAMETER Test
    Run unit tests (Pester, excluding Integration tag).

.PARAMETER Provision
    Provision nested PVE VMs (x86 only). Required before -Integration
    unless VMs are already running.

.PARAMETER Integration
    Run integration tests against provisioned PVE VMs.

.PARAMETER Cleanup
    Destroy provisioned VMs (x86 only).

.PARAMETER Stop
    Stop all containers.

.PARAMETER Rebuild
    Rebuild container images from scratch.

.PARAMETER Reprovision
    When used with -Provision, taints the PVE VMs in Terraform state
    before applying, forcing them to be destroyed and recreated.
    Useful when the VMs are in a bad state (e.g. clustered, broken).

.PARAMETER Force
    When used with -Cleanup, bypasses Terraform and destroys VMs
    directly via the PVE API. Useful when Terraform state is corrupted
    (e.g. after an interrupted provision). Also removes Terraform
    state files so the next provision starts clean.

.PARAMETER Tests
    Filter integration tests by area name. Comma-separated list of test
    area names that match the numbered file prefixes. Examples:
        -Tests Connection,Nodes     # runs 00_Connection + 01_Nodes
        -Tests VMs,Snapshots        # runs 06_VMs + 07_Snapshots
        -Tests Cluster              # runs 16_Cluster
    When omitted, all integration test files are run.

.PARAMETER Version
    PVE version to test against (8, 9, or all). Default: all.

.PARAMETER DockerHost
    SSH destination for a remote Docker host (e.g. 172.16.40.113).

.PARAMETER NoCleanup
    When used with -Integration, skips cleanup after tests complete.

.EXAMPLE
    ./tests/dev.ps1 -Shell
    # Opens a pwsh shell in the dev container

.EXAMPLE
    ./tests/dev.ps1 -Build -Test
    # Builds the module and runs unit tests

.EXAMPLE
    ./tests/dev.ps1 -Provision -Integration -Cleanup -DockerHost 172.16.40.113
    # Full lifecycle: provision, test, cleanup on remote host

.EXAMPLE
    ./tests/dev.ps1 -Integration -Tests Connection,VMs -Version 9 -DockerHost 172.16.40.113
    # Run only Connection and VMs integration tests for PVE 9

.EXAMPLE
    ./tests/dev.ps1 -Provision -Integration -Tests Cluster,HA -Version 9 -Cleanup -DockerHost 172.16.40.113
    # Provision, run cluster+HA tests for PVE 9, cleanup
#>
[CmdletBinding()]
param(
    [switch] $Shell,
    [switch] $Build,
    [switch] $Test,
    [switch] $Provision,
    [switch] $Integration,
    [switch] $Cleanup,
    [switch] $Stop,
    [switch] $Rebuild,
    [switch] $Reprovision,
    [switch] $Force,

    [string[]] $Tests,

    [Alias('PveVersion')]
    [string] $Version = 'all',

    [string] $DockerHost,

    [Alias('k')]
    [switch] $NoCleanup
)

$ErrorActionPreference = 'Stop'

# If no switches specified, default to -Shell
$anySwitchSet = $Shell -or $Build -or $Test -or $Provision -or $Integration -or $Cleanup -or $Stop -or $Rebuild
if (-not $anySwitchSet) {
    $Shell = $true
}

# Resolve repo root (parent of tests/)
$RepoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $RepoRoot
try {

# ── Remote Docker host support ────────────────────────────────────────
$RemoteRepoPath = $null

if ($DockerHost) {
    $RemoteRepoPath = "/tmp/psproxmoxve-dev"
    $env:DOCKER_HOST = "ssh://$DockerHost"

    Write-Host "Syncing repo to ${DockerHost}:${RemoteRepoPath}..."
    ssh $DockerHost "mkdir -p $RemoteRepoPath"
    if ($LASTEXITCODE -ne 0) { throw "Failed to create remote directory" }

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

$ComposeFile = 'tests/docker-compose.test.yml'
$ComposeArgs = @('-f', $ComposeFile)
$OverrideFile = $null

if ($RemoteRepoPath) {
    $OverrideFile = Join-Path ([System.IO.Path]::GetTempPath()) 'docker-compose.remote-override.yml'
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

# Build test filter argument for run-integration.sh
$TestFilter = ''
if ($Tests) {
    $TestFilter = ($Tests -join ',')
}

# ── Execute actions in order ──────────────────────────────────────────

if ($Stop) {
    docker compose @ComposeArgs --profile infra down
}

if ($Rebuild) {
    docker compose @ComposeArgs --profile infra down
    docker compose @ComposeArgs build --no-cache dev
    docker compose @ComposeArgs --profile infra build --no-cache dev-infra
    docker compose @ComposeArgs up -d dev
}

if ($Shell) {
    if ($DockerHost) {
        Write-Warning "Interactive shell over remote Docker is not supported. Use: ssh $DockerHost 'docker exec -it $InfraContainer pwsh -NoProfile'"
        return
    }
    Start-DevContainer
    docker exec -it $DevContainer pwsh -NoProfile
}

if ($Build) {
    Start-DevContainer
    Invoke-BuildModule $DevContainer
}

if ($Test) {
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

if ($Provision) {
    Start-InfraContainer
    if ($Reprovision) {
        docker exec $InfraContainer bash $RunIntegration taint $Version
        if ($LASTEXITCODE -ne 0) { throw "Taint failed (exit code $LASTEXITCODE)" }
    }
    docker exec $InfraContainer bash $RunIntegration provision $Version
    if ($LASTEXITCODE -ne 0) { throw "Provisioning failed (exit code $LASTEXITCODE)" }
}

if ($Integration) {
    Start-InfraContainer

    # Verify environment is ready (config file exists from provisioning)
    $configCheck = docker exec $InfraContainer bash -c 'test -f "${CONFIG_FILE:-/tmp/pve-integration/config.json}" && echo OK || echo MISSING'
    if ($configCheck.Trim() -eq 'MISSING' -and -not $Provision) {
        throw "Integration environment not ready. Run with -Provision first, or use -Provision -Integration together."
    }

    docker exec $InfraContainer bash $RunIntegration test $Version $TestFilter
    if ($LASTEXITCODE -ne 0) { throw "Integration tests failed (exit code $LASTEXITCODE)" }
}

if ($Cleanup) {
    Start-InfraContainer
    if ($Force) {
        if ($Version -ne 'all') {
            throw "-Force cannot be combined with -Version. Force cleanup destroys all resources and wipes Terraform state."
        }
        docker exec $InfraContainer bash $RunIntegration force-cleanup
    } else {
        docker exec $InfraContainer bash $RunIntegration cleanup $Version
    }
    if ($LASTEXITCODE -ne 0) { throw "Cleanup failed (exit code $LASTEXITCODE)" }
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
