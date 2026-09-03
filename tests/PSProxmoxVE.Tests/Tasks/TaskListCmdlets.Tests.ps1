#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for task list cmdlets:
        Get-PveTaskList, Stop-PveTask.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Get-PveTaskList
# ---------------------------------------------------------------------------
Describe 'Get-PveTaskList' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveTaskList' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveTaskList -Node 'pve' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Stop-PveTask
# ---------------------------------------------------------------------------
Describe 'Stop-PveTask' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Stop-PveTask' }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
        It 'Should declare ConfirmImpact High' {
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Stop-PveTask -Node 'pve' -Upid 'UPID:pve:00000001:00000000:00000000:test::root@pam:' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
