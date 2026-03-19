<#
.SYNOPSIS
    Shared module-loading helper for Pester tests.
    Dot-source this file inside BeforeAll to import PSProxmoxVE reliably
    in both local-dev and CI environments.
#>

# If the module is already loaded, nothing to do.
if (Get-Module -Name PSProxmoxVE) { return }

# 1. Try importing by module name (works in CI where the module is
#    installed to a PSModulePath location via dotnet publish + copy).
try {
    Import-Module PSProxmoxVE -Force -ErrorAction Stop
    return
}
catch {
    # Module not on PSModulePath — fall through to local-path discovery.
}

# 2. Discover the built DLL from the local source tree.
#    Prefer the framework that matches the running PowerShell edition.
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '../..')
$moduleRoot = Join-Path $repoRoot 'src/PSProxmoxVE'

if ($PSVersionTable.PSEdition -eq 'Core') {
    $frameworks = @('net9.0', 'net48')
}
else {
    $frameworks = @('net48', 'net9.0')
}

$searchPaths = foreach ($fw in $frameworks) {
    # Publish output (has all dependencies co-located)
    Join-Path $repoRoot "publish/$fw/PSProxmoxVE.dll"
    # Build output
    Join-Path $moduleRoot "bin/Debug/$fw/PSProxmoxVE.dll"
    Join-Path $moduleRoot "bin/Release/$fw/PSProxmoxVE.dll"
}

$moduleDll = $searchPaths | Where-Object { Test-Path $_ } | Select-Object -First 1

if ($null -eq $moduleDll) {
    throw "PSProxmoxVE.dll not found. Build the project before running Pester tests."
}

# Remove deps.json if present — it causes assembly resolution conflicts
# when loading a binary module inside PowerShell's own .NET runtime.
$depsJson = Join-Path (Split-Path $moduleDll) 'PSProxmoxVE.deps.json'
if (Test-Path $depsJson) { Remove-Item $depsJson -Force }

Import-Module $moduleDll -Force -ErrorAction Stop
