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

.PARAMETER FromTerraform
    Read PVE connection details from 'terraform output -json' in
    tests/infrastructure/. Requires Terraform to be installed and
    'terraform apply' to have been run. Implies -Tier Integration.

.PARAMETER PveHost
    Hostname or IP of the PVE test node. Sets PVETEST_HOST.

.PARAMETER PvePort
    API port. Default 8006. Sets PVETEST_PORT.

.PARAMETER PveApiToken
    API token in USER@REALM!TOKENID=UUID format. Sets PVETEST_APITOKEN.

.PARAMETER PveNode
    PVE node name (e.g. pve). Sets PVETEST_NODE.

.PARAMETER PveStorage
    Storage pool for disk/ISO operations (e.g. local). Sets PVETEST_STORAGE.

.PARAMETER PveIsoPath
    Local filesystem path to a small .iso for upload tests. Sets PVETEST_ISO_PATH.

.EXAMPLE
    ./tools/Invoke-Tests.ps1

.EXAMPLE
    ./tools/Invoke-Tests.ps1 -Tier All

.EXAMPLE
    ./tools/Invoke-Tests.ps1 -FromTerraform

.EXAMPLE
    ./tools/Invoke-Tests.ps1 -Tier Integration `
        -PveHost 192.168.1.200 -PveApiToken "root@pam!integration=abc123..." `
        -PveNode pve -PveStorage local -PveIsoPath /tmp/test.iso

.EXAMPLE
    ./tools/Invoke-Tests.ps1 -Tier Unit -Framework net9.0
#>
[CmdletBinding(DefaultParameterSetName = 'Explicit')]
param(
    [Parameter()]
    [ValidateSet('Unit', 'Integration', 'All')]
    [string] $Tier = 'Unit',

    [Parameter()]
    [string] $Framework,

    # --- Terraform-sourced connection ---
    [Parameter(ParameterSetName = 'Terraform')]
    [switch] $FromTerraform,

    # --- Explicit connection params ---
    [Parameter(ParameterSetName = 'Explicit')]
    [string] $PveHost,

    [Parameter(ParameterSetName = 'Explicit')]
    [int] $PvePort = 8006,

    [Parameter(ParameterSetName = 'Explicit')]
    [string] $PveApiToken,

    [Parameter(ParameterSetName = 'Explicit')]
    [string] $PveNode,

    [Parameter(ParameterSetName = 'Explicit')]
    [string] $PveStorage,

    [Parameter(ParameterSetName = 'Explicit')]
    [string] $PveIsoPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# Integration connection setup
# ---------------------------------------------------------------------------

if ($FromTerraform) {
    $Tier = 'Integration'
    $infraDir = Join-Path $PSScriptRoot '../tests/infrastructure'

    if (-not (Get-Command terraform -ErrorAction SilentlyContinue)) {
        throw 'terraform not found on PATH. Install Terraform >= 1.5 first.'
    }
    if (-not (Test-Path $infraDir)) {
        throw "Infrastructure directory not found: $infraDir"
    }

    Write-Host 'Reading connection details from terraform output...' -ForegroundColor Cyan
    $tfOutputJson = terraform -chdir:$infraDir output -json 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "terraform output failed. Have you run 'terraform apply' in $infraDir?`n$tfOutputJson"
    }

    $tfOutput = $tfOutputJson | ConvertFrom-Json
    $env:PVETEST_HOST     = $tfOutput.pve_test_host.value
    $env:PVETEST_PORT     = $tfOutput.pve_test_port.value
    $env:PVETEST_NODE     = $tfOutput.pve_test_node_name.value
    $env:PVETEST_APITOKEN = terraform -chdir:$infraDir output -raw pve_test_api_token 2>&1

    # Storage and ISO path are not provisioned by Terraform; require env vars or defaults
    if (-not $env:PVETEST_STORAGE)  { $env:PVETEST_STORAGE  = 'local' }
    if (-not $env:PVETEST_ISO_PATH) {
        Write-Warning 'PVETEST_ISO_PATH not set — ISO upload tests will be skipped.'
    }

    Write-Host "  PVE host : $($env:PVETEST_HOST):$($env:PVETEST_PORT)" -ForegroundColor DarkGray
    Write-Host "  PVE node : $($env:PVETEST_NODE)" -ForegroundColor DarkGray
    Write-Host "  Storage  : $($env:PVETEST_STORAGE)" -ForegroundColor DarkGray
}
elseif ($PSBoundParameters.ContainsKey('PveHost') -or $PSBoundParameters.ContainsKey('PveApiToken')) {
    # Explicit params override env vars
    if ($PveHost)     { $env:PVETEST_HOST     = $PveHost }
    if ($PvePort)     { $env:PVETEST_PORT     = $PvePort }
    if ($PveApiToken) { $env:PVETEST_APITOKEN = $PveApiToken }
    if ($PveNode)     { $env:PVETEST_NODE     = $PveNode }
    if ($PveStorage)  { $env:PVETEST_STORAGE  = $PveStorage }
    if ($PveIsoPath)  { $env:PVETEST_ISO_PATH = $PveIsoPath }

    if ($Tier -eq 'Unit') { $Tier = 'Integration' }
}

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
        # Try singular TargetFramework first, then first entry from TargetFrameworks
        $tfmNode = Select-Xml -Path $csproj.FullName -XPath '//*[local-name()="TargetFramework" or local-name()="TargetFrameworks"]' |
                   Select-Object -First 1

        if ($tfmNode) {
            # TargetFrameworks may be semicolon-separated; pick the first .NET Core/5+ TFM
            # On non-Windows, skip net48 since .NET Framework is not available
            $allTfms = $tfmNode.Node.InnerText.Trim() -split ';' | ForEach-Object { $_.Trim() }
            $onWindows = $PSVersionTable.Platform -eq 'Win32NT' -or $PSVersionTable.PSEdition -eq 'Desktop'
            $Framework = $allTfms | Where-Object { $_ -notmatch 'net4' -or $onWindows } | Select-Object -First 1
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

        Import-Module -Name Pester -RequiredVersion $pesterModule.Version -Force

        $config = New-PesterConfiguration
        $config.Run.Path          = $pesterTestDir
        $config.Filter.ExcludeTag = @('Integration', 'MockIntegration')
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

        Import-Module -Name Pester -RequiredVersion $pesterModule.Version -Force

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

    Write-Host ('  {0,-30} {1}' -f "${suite}:", $status) -ForegroundColor $color
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
