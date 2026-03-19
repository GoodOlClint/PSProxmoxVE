<#
.SYNOPSIS
    Shared module-loading helper for Pester tests.
    Dot-source this file inside BeforeAll to import PSProxmoxVE reliably
    in both local-dev and CI environments.
#>

# If the module is already loaded, nothing to do.
if (Get-Module -Name PSProxmoxVE) { return }

# Ensure DOTNET_ROOT doesn't override PS's bundled runtime (CI sets this
# via setup-dotnet and it can break binary module assembly resolution).
if ($env:DOTNET_ROOT) {
    $env:DOTNET_ROOT = $null
    $env:DOTNET_MULTILEVEL_LOOKUP = $null
}

# 1. Try importing by module name (works in CI where the module is
#    installed to a PSModulePath location via dotnet publish + copy).
$available = Get-Module PSProxmoxVE -ListAvailable -ErrorAction SilentlyContinue
if ($available) {
    Import-Module PSProxmoxVE -Force -ErrorAction Stop
    return
}

# 2. Discover the module manifest (.psd1) from the local source tree.
#    Loading via manifest ensures PS handles assembly resolution correctly.
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
    Join-Path $repoRoot "publish/$fw/PSProxmoxVE.psd1"
    # Build output
    Join-Path $moduleRoot "bin/Debug/$fw/PSProxmoxVE.psd1"
    Join-Path $moduleRoot "bin/Release/$fw/PSProxmoxVE.psd1"
}

$moduleManifest = $searchPaths | Where-Object { Test-Path $_ } | Select-Object -First 1

if ($null -eq $moduleManifest) {
    throw "PSProxmoxVE module not found. Build the project before running Pester tests."
}

# Remove files that cause .NET assembly resolution conflicts when loading
# a binary module inside PowerShell's own runtime.
$moduleDir = Split-Path $moduleManifest
Get-ChildItem $moduleDir -Filter '*.deps.json' -ErrorAction SilentlyContinue | Remove-Item -Force
Get-ChildItem $moduleDir -Filter '*.runtimeconfig.json' -ErrorAction SilentlyContinue | Remove-Item -Force

Import-Module $moduleManifest -Force -ErrorAction Stop
