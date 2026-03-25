<#
.SYNOPSIS
    Shared setup helper for integration tests.
    Dot-source this in each integration test file's BeforeAll block.

.DESCRIPTION
    Provides:
    - Module loading via _TestHelper.ps1
    - Environment variable reading (PVETEST_*)
    - Skip helper functions
    - Connect-TestPve: establishes a credential-based connection if needed
    - Resource discovery helpers for cross-file test dependencies
#>

# Load the module
. $PSScriptRoot/../_TestHelper.ps1

# ── Environment variables ────────────────────────────────────────────────
$script:Host_           = $env:PVETEST_HOST
$script:Port            = if ($env:PVETEST_PORT) { [int]$env:PVETEST_PORT } else { 8006 }
$script:Node            = $env:PVETEST_NODE
$script:Password        = $env:PVETEST_PASSWORD
$script:Storage         = $env:PVETEST_STORAGE
$script:IsoPath         = $env:PVETEST_ISO_PATH
$script:PveVersion      = $env:PVETEST_PVE_VERSION
$script:CloudImagePath  = $env:PVETEST_CLOUD_IMAGE_PATH
$script:OvaPath         = $env:PVETEST_OVA_PATH

# Multi-node (cluster tests)
$script:HostB           = $env:PVETEST_HOST_B
$script:PasswordB       = $env:PVETEST_PASSWORD_B  # Falls back to Password if not set
if (-not $script:PasswordB) { $script:PasswordB = $script:Password }

# ── Skip reason ──────────────────────────────────────────────────────────
$script:SkipReason = $null

$requiredVars = @('PVETEST_HOST', 'PVETEST_NODE', 'PVETEST_PASSWORD')
foreach ($var in $requiredVars) {
    if (-not [System.Environment]::GetEnvironmentVariable($var)) {
        $script:SkipReason = "Missing required env var: $var (need: $($requiredVars -join ', '))"
        break
    }
}

# ── Skip helpers ─────────────────────────────────────────────────────────

function script:Skip-IfNoTarget {
    if ($script:SkipReason) {
        Set-ItResult -Skipped -Because $script:SkipReason
        return $true
    }
    return $false
}

function script:Skip-IfNoStorage {
    if (Skip-IfNoTarget) { return $true }
    if (-not $script:Storage -or -not $script:IsoPath) {
        Set-ItResult -Skipped -Because 'PVETEST_STORAGE and PVETEST_ISO_PATH required'
        return $true
    }
    return $false
}

function script:Skip-IfNoPassword {
    if (Skip-IfNoTarget) { return $true }
    if (-not $script:Password -or -not $script:CloudImagePath) {
        Set-ItResult -Skipped -Because 'PVETEST_PASSWORD and PVETEST_CLOUD_IMAGE_PATH required'
        return $true
    }
    return $false
}

function script:Skip-IfNoOva {
    if (Skip-IfNoTarget) { return $true }
    if (-not $script:OvaPath) {
        Set-ItResult -Skipped -Because 'PVETEST_OVA_PATH required'
        return $true
    }
    return $false
}

function script:Skip-IfNoNodeB {
    if (Skip-IfNoTarget) { return $true }
    if (-not $script:HostB -or -not $script:Password) {
        Set-ItResult -Skipped -Because 'Multi-node env vars not set (PVETEST_HOST_B, PVETEST_PASSWORD)'
        return $true
    }
    return $false
}

function script:Skip-IfNoPve9 {
    if (Skip-IfNoTarget) { return $true }
    if ($script:PveVersion -and [int]$script:PveVersion -lt 9) {
        Set-ItResult -Skipped -Because 'Requires PVE 9.0+'
        return $true
    }
    # If version not set, try to detect
    if (-not $script:PveVersion) {
        try {
            $detail = Test-PveConnection -Detailed -ErrorAction Stop
            if ($detail.ServerVersion.Major -lt 9) {
                Set-ItResult -Skipped -Because "PVE version $($detail.ServerVersion) < 9.0"
                return $true
            }
        } catch {
            Set-ItResult -Skipped -Because 'Cannot determine PVE version'
            return $true
        }
    }
    return $false
}

# ── Connection helper ────────────────────────────────────────────────────

function script:Connect-TestPve {
    <#
    .SYNOPSIS
        Connects to the test PVE server using root@pam credentials.
        Reuses existing connection if already connected to the same host.
    #>
    if ($script:SkipReason) { return }

    # Check if we're already connected to the right host
    try {
        if (Test-PveConnection) {
            $detail = Test-PveConnection -Detailed
            if ($detail.Hostname -eq $script:Host_) { return }
        }
    } catch { }

    # Connect with credentials (not API token) — all cluster/privileged ops work
    $secPw = ConvertTo-SecureString $script:Password -AsPlainText -Force
    $cred = New-Object System.Management.Automation.PSCredential('root@pam', $secPw)
    Connect-PveServer `
        -Server $script:Host_ `
        -Port $script:Port `
        -Credential $cred `
        -SkipCertificateCheck
}

function script:Disconnect-TestPve {
    <#
    .SYNOPSIS
        Disconnects the test PVE session if one was established by this file.
    #>
    try { Disconnect-PveServer -ErrorAction SilentlyContinue } catch { }
}

# ── Resource discovery helpers ───────────────────────────────────────────
# These allow later test files to find resources created by earlier files
# without sharing script-scoped variables. If the resource doesn't exist,
# the caller gets $null and should skip.

function script:Find-TestVm {
    <#
    .SYNOPSIS
        Finds the pester-test-vm by checking env state or querying PVE.
    #>
    param([string]$Name = 'pester-test-vm')

    # Check env var first (set by the file that created it)
    $envKey = "PVETEST_STATE_$($Name.Replace('-','_').ToUpper())"
    $vmId = [System.Environment]::GetEnvironmentVariable($envKey)
    if ($vmId) { return [int]$vmId }

    # Fall back to querying PVE
    try {
        $vms = Get-PveVm -Node $script:Node -ErrorAction Stop
        $match = $vms | Where-Object { $_.Name -eq $Name } | Select-Object -First 1
        if ($match) { return [int]$match.VmId }
    } catch { }

    return $null
}

function script:Register-TestResource {
    <#
    .SYNOPSIS
        Registers a resource VMID in env for cross-file discovery.
    #>
    param(
        [string]$Name,
        [int]$VmId
    )
    $envKey = "PVETEST_STATE_$($Name.Replace('-','_').ToUpper())"
    [System.Environment]::SetEnvironmentVariable($envKey, $VmId.ToString())
}
