#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for backup info cmdlets:
        Get-PveBackupInfo.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Get-PveBackupInfo
# ---------------------------------------------------------------------------
Describe 'Get-PveBackupInfo' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveBackupInfo' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveBackupInfo -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
