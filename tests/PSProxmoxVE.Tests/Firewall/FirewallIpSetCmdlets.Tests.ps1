#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for firewall IP set cmdlets:
        Get-PveFirewallIpSet, New-PveFirewallIpSet, Remove-PveFirewallIpSet,
        Get-PveFirewallIpSetEntry, New-PveFirewallIpSetEntry, Set-PveFirewallIpSetEntry,
        Remove-PveFirewallIpSetEntry.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Get-PveFirewallIpSet
# ---------------------------------------------------------------------------
Describe 'Get-PveFirewallIpSet' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveFirewallIpSet' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveFirewallIpSet -Level Cluster -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveFirewallIpSet
# ---------------------------------------------------------------------------
Describe 'New-PveFirewallIpSet' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveFirewallIpSet' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { New-PveFirewallIpSet -Name 'testipset' -Level Cluster -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveFirewallIpSet
# ---------------------------------------------------------------------------
Describe 'Remove-PveFirewallIpSet' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveFirewallIpSet' }

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
            { Remove-PveFirewallIpSet -Name 'testipset' -Confirm:$false -Level Cluster -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveFirewallIpSetEntry
# ---------------------------------------------------------------------------
Describe 'Get-PveFirewallIpSetEntry' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveFirewallIpSetEntry' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveFirewallIpSetEntry -Name 'testipset' -Level Cluster -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveFirewallIpSetEntry
# ---------------------------------------------------------------------------
Describe 'New-PveFirewallIpSetEntry' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveFirewallIpSetEntry' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { New-PveFirewallIpSetEntry -Name 'testipset' -Cidr '10.0.0.1' -Level Cluster -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveFirewallIpSetEntry
# ---------------------------------------------------------------------------
Describe 'Set-PveFirewallIpSetEntry' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PveFirewallIpSetEntry' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Set-PveFirewallIpSetEntry -Name 'testipset' -Cidr '10.0.0.1' -Level Cluster -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveFirewallIpSetEntry
# ---------------------------------------------------------------------------
Describe 'Remove-PveFirewallIpSetEntry' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveFirewallIpSetEntry' }

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
            { Remove-PveFirewallIpSetEntry -Name 'testipset' -Cidr '10.0.0.1' -Confirm:$false -Level Cluster -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
