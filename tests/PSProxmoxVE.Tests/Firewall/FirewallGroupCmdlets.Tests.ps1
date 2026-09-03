#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for firewall group cmdlets:
        Get-PveFirewallGroup, New-PveFirewallGroup, Remove-PveFirewallGroup.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Get-PveFirewallGroup
# ---------------------------------------------------------------------------
Describe 'Get-PveFirewallGroup' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveFirewallGroup' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveFirewallGroup -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveFirewallGroup
# ---------------------------------------------------------------------------
Describe 'New-PveFirewallGroup' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveFirewallGroup' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { New-PveFirewallGroup -Group 'testgroup' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveFirewallGroup
# ---------------------------------------------------------------------------
Describe 'Remove-PveFirewallGroup' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveFirewallGroup' }

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
            { Remove-PveFirewallGroup -Group 'testgroup' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
