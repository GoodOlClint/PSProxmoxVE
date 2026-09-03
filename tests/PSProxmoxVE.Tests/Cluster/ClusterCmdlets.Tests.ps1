#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for cluster cmdlets:
        Get-PveClusterResource.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Get-PveClusterResource
# ---------------------------------------------------------------------------
Describe 'Get-PveClusterResource' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveClusterResource' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveClusterResource -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
