#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for firewall alias cmdlets:
        Get-PveFirewallAlias, New-PveFirewallAlias, Set-PveFirewallAlias, Remove-PveFirewallAlias.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Get-PveFirewallAlias
# ---------------------------------------------------------------------------
Describe 'Get-PveFirewallAlias' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveFirewallAlias' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveFirewallAlias -Level Cluster -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveFirewallAlias
# ---------------------------------------------------------------------------
Describe 'New-PveFirewallAlias' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveFirewallAlias' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { New-PveFirewallAlias -Name 'testalias' -Cidr '10.0.0.0/24' -Level Cluster -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveFirewallAlias
# ---------------------------------------------------------------------------
Describe 'Set-PveFirewallAlias' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PveFirewallAlias' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Set-PveFirewallAlias -Name 'testalias' -Cidr '10.0.0.0/24' -Level Cluster -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveFirewallAlias
# ---------------------------------------------------------------------------
Describe 'Remove-PveFirewallAlias' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveFirewallAlias' }

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
            { Remove-PveFirewallAlias -Name 'testalias' -Confirm:$false -Level Cluster -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
