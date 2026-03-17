#Requires -Version 5.1
<#
.SYNOPSIS
    Test runner for the PSProxmoxVE project.

.DESCRIPTION
    Runs unit and/or integration tests for PSProxmoxVE. Unit tests include both
    xUnit (.NET) and Pester (PowerShell) suites. Integration tests require a live
    or mocked PVE endpoint and are executed via Pester with the Integration tag.

.PARAMETER Tier
    Which tier of tests to run. Valid values: Unit, Integration, All. Default: Unit.

.PARAMETER Framework
    Dotnet target framework moniker to pass to 'dotnet test' (e.g. net8.0, net9.0).
    When omitted the framework is auto-detected from the first *.csproj found under
    tests/PSProxmoxVE.Core.Tests.

.EXAMPLE
    ./tools/Invoke-Tests.ps1

.EXAMPLE
    ./tools/Invoke-Tests.ps1 -Tier All

.EXAMPLE
    ./tools/Invoke-Tests.ps1 -Tier Integration

.EXAMPLE
    ./tools/Invoke-Tests.ps1 -Tier Unit -Framework net9.0
#>
[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Unit', 'Integration', 'All')]
    [string] $Tier = 'Unit',

    [Parameter()]
    [string] $Framework
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

function Write-Header {
    param([string] $Text)
    $line = '-' * 70
    Write-Host ''
    Write-Host $line -ForegroundColor Cyan
    Write-Host "  $Text" -ForegroundColor Cyan
    Write-Host $line -ForegroundColor Cyan
}

function Assert-Command {
    param([string] $Name)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found on PATH."
    }
}

# ---------------------------------------------------------------------------
# Path resolution
# ---------------------------------------------------------------------------

$repoRoot = Split-Path -Parent $PSScriptRoot

$coreTestProject = Join-Path $repoRoot 'tests' 'PSProxmoxVE.Core.Tests'
$pesterTestDir   = Join-Path $repoRoot 'tests' 'PSProxmoxVE.Tests'
$integrationDir  = Join-Path $pesterTestDir 'Integration'

# ---------------------------------------------------------------------------
# Auto-detect framework when not supplied
# ---------------------------------------------------------------------------

if (-not $Framework) {
    $csproj = Get-ChildItem -Path $coreTestProject -Filter '*.csproj' -ErrorAction SilentlyContinue |
              Select-Object -First 1

    if ($csproj) {
        [xml] $proj = Get-Content $csproj.FullName -Raw
        $tfm = $proj.Project.PropertyGroup |
               Where-Object { $_.TargetFramework } |
               Select-Object -First 1 -ExpandProperty TargetFramework

        if ($tfm) {
            $Framework = $tfm.Trim()
            Write-Verbose "Auto-detected target framework: $Framework"
        }
    }
}

# ---------------------------------------------------------------------------
# Result tracking
# ---------------------------------------------------------------------------

$results = [ordered] @{}

# ---------------------------------------------------------------------------
# xUnit tests
# ---------------------------------------------------------------------------

function Invoke-XUnitTests {
    Write-Header 'xUnit Tests  (PSProxmoxVE.Core.Tests)'

    Assert-Command 'dotnet'

    if (-not (Test-Path $coreTestProject)) {
        Write-Warning "xUnit test project not found at: $coreTestProject  -- skipping."
        $script:results['xUnit'] = 'Skipped'
        return
    }

    $dotnetArgs = @(
        'test'
        $coreTestProject
        '--logger'
        'console;verbosity=normal'
        '--nologo'
    )

    if ($Framework) {
        $dotnetArgs += '--framework'
        $dotnetArgs += $Framework
    }

    Write-Host "Running: dotnet $($dotnetArgs -join ' ')" -ForegroundColor DarkGray

    & dotnet @dotnetArgs
    $exitCode = $LASTEXITCODE

    if ($exitCode -eq 0) {
        Write-Host 'xUnit tests PASSED.' -ForegroundColor Green
        $script:results['xUnit'] = 'Passed'
    }
    else {
        Write-Host "xUnit tests FAILED (exit code $exitCode)." -ForegroundColor Red
        $script:results['xUnit'] = "Failed (exit $exitCode)"
    }
}

# ---------------------------------------------------------------------------
# Pester unit tests
# ---------------------------------------------------------------------------

function Invoke-PesterUnitTests {
    Write-Header 'Pester Unit Tests  (PSProxmoxVE.Tests)'

    if (-not (Test-Path $pesterTestDir)) {
        Write-Warning "Pester test directory not found at: $pesterTestDir  -- skipping."
        $script:results['Pester-Unit'] = 'Skipped'
        return
    }

    try {
        $pesterModule = Get-Module -Name Pester -ListAvailable |
                        Sort-Object Version -Descending |
                        Select-Object -First 1

        if (-not $pesterModule) {
            throw 'Pester module is not installed. Run: Install-Module Pester -Force'
        }

        Import-Module $pesterModule.ModuleBase -Force

        $config = New-PesterConfiguration
        $config.Run.Path          = $pesterTestDir
        $config.Filter.ExcludeTag = @('Integration')
        $config.Output.Verbosity  = 'Detailed'
        $config.Run.PassThru      = $true

        $pesterResult = Invoke-Pester -Configuration $config

        if ($pesterResult.FailedCount -gt 0) {
            Write-Host "Pester unit tests FAILED ($($pesterResult.FailedCount) failed, $($pesterResult.PassedCount) passed)." -ForegroundColor Red
            $script:results['Pester-Unit'] = "Failed ($($pesterResult.FailedCount) failed)"
        }
        else {
            Write-Host "Pester unit tests PASSED ($($pesterResult.PassedCount) passed)." -ForegroundColor Green
            $script:results['Pester-Unit'] = "Passed ($($pesterResult.PassedCount) passed)"
        }
    }
    catch {
        Write-Host "Pester unit tests ERROR: $_" -ForegroundColor Red
        $script:results['Pester-Unit'] = "Error: $_"
    }
}

# ---------------------------------------------------------------------------
# Pester integration tests
# ---------------------------------------------------------------------------

function Invoke-PesterIntegrationTests {
    Write-Header 'Pester Integration Tests  (PSProxmoxVE.Tests/Integration)'

    if (-not (Test-Path $integrationDir)) {
        Write-Warning "Integration test directory not found at: $integrationDir  -- skipping."
        $script:results['Pester-Integration'] = 'Skipped'
        return
    }

    try {
        $pesterModule = Get-Module -Name Pester -ListAvailable |
                        Sort-Object Version -Descending |
                        Select-Object -First 1

        if (-not $pesterModule) {
            throw 'Pester module is not installed. Run: Install-Module Pester -Force'
        }

        Import-Module $pesterModule.ModuleBase -Force

        $config = New-PesterConfiguration
        $config.Run.Path        = $integrationDir
        $config.Filter.Tag      = @('Integration')
        $config.Output.Verbosity = 'Detailed'
        $config.Run.PassThru    = $true

        $pesterResult = Invoke-Pester -Configuration $config

        if ($pesterResult.FailedCount -gt 0) {
            Write-Host "Integration tests FAILED ($($pesterResult.FailedCount) failed, $($pesterResult.PassedCount) passed)." -ForegroundColor Red
            $script:results['Pester-Integration'] = "Failed ($($pesterResult.FailedCount) failed)"
        }
        else {
            Write-Host "Integration tests PASSED ($($pesterResult.PassedCount) passed)." -ForegroundColor Green
            $script:results['Pester-Integration'] = "Passed ($($pesterResult.PassedCount) passed)"
        }
    }
    catch {
        Write-Host "Integration tests ERROR: $_" -ForegroundColor Red
        $script:results['Pester-Integration'] = "Error: $_"
    }
}

# ---------------------------------------------------------------------------
# Dispatch
# ---------------------------------------------------------------------------

switch ($Tier) {
    'Unit' {
        Invoke-XUnitTests
        Invoke-PesterUnitTests
    }
    'Integration' {
        Invoke-PesterIntegrationTests
    }
    'All' {
        Invoke-XUnitTests
        Invoke-PesterUnitTests
        Invoke-PesterIntegrationTests
    }
}

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

Write-Header 'Test Run Summary'

$anyFailure = $false

foreach ($suite in $results.Keys) {
    $status = $results[$suite]
    $color  = switch -Wildcard ($status) {
        'Passed*'  { 'Green'   }
        'Skipped'  { 'Yellow'  }
        default    { 'Red'     }
    }

    if ($color -eq 'Red') { $anyFailure = $true }

    Write-Host ('  {0,-30} {1}' -f "$suite:", $status) -ForegroundColor $color
}

Write-Host ''

if ($anyFailure) {
    Write-Host 'One or more test suites FAILED.' -ForegroundColor Red
    exit 1
}
else {
    Write-Host 'All test suites completed successfully.' -ForegroundColor Green
    exit 0
}
