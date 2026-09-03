#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for firewall rule cmdlets:
        Get-PveFirewallRule, New-PveFirewallRule, Set-PveFirewallRule, Remove-PveFirewallRule.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Get-PveFirewallRule
# ---------------------------------------------------------------------------
Describe 'Get-PveFirewallRule' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveFirewallRule' }

    Context 'Parameter metadata' {
        It 'Should have mandatory Level parameter with ValidateSet' {
            $param = $script:Cmd.Parameters['Level']
            $param | Should -Not -BeNullOrEmpty
            $isMandatory = $param.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
            $validateSet = $param.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $validateSet | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveFirewallRule -Level 'cluster' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }

    Context 'Group level' {
        It 'Should include Group in the Level ValidateSet' {
            $param = $script:Cmd.Parameters['Level']
            $validateSet = $param.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $validateSet.ValidValues | Should -Contain 'Group'
        }

        It 'Should throw when Level is Group and Group is not specified' {
            { Get-PveFirewallRule -Level 'Group' -ErrorAction Stop } |
                Should -Throw '*Group is required when Level is Group*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveFirewallRule
# ---------------------------------------------------------------------------
Describe 'New-PveFirewallRule' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveFirewallRule' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Parameter metadata' {
        It 'Should have mandatory Level parameter with ValidateSet' {
            $param = $script:Cmd.Parameters['Level']
            $param | Should -Not -BeNullOrEmpty
            $isMandatory = $param.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
            $validateSet = $param.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $validateSet | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { New-PveFirewallRule -Level 'cluster' -Action 'ACCEPT' -Type 'in' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }

    Context 'Group level' {
        It 'Should include Group in the Level ValidateSet' {
            $param = $script:Cmd.Parameters['Level']
            $validateSet = $param.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $validateSet.ValidValues | Should -Contain 'Group'
        }

        It 'Should throw when Level is Group and Group is not specified' {
            { New-PveFirewallRule -Level 'Group' -Action 'ACCEPT' -Type 'in' -ErrorAction Stop } |
                Should -Throw '*Group is required when Level is Group*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveFirewallRule
# ---------------------------------------------------------------------------
Describe 'Set-PveFirewallRule' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PveFirewallRule' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Parameter metadata' {
        It 'Should have mandatory Level parameter with ValidateSet' {
            $param = $script:Cmd.Parameters['Level']
            $param | Should -Not -BeNullOrEmpty
            $isMandatory = $param.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
            $validateSet = $param.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $validateSet | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Set-PveFirewallRule -Level 'cluster' -Position 0 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }

    Context 'Group level' {
        It 'Should include Group in the Level ValidateSet' {
            $param = $script:Cmd.Parameters['Level']
            $validateSet = $param.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $validateSet.ValidValues | Should -Contain 'Group'
        }

        It 'Should throw when Level is Group and Group is not specified' {
            { Set-PveFirewallRule -Level 'Group' -Position 0 -ErrorAction Stop } |
                Should -Throw '*Group is required when Level is Group*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveFirewallRule
# ---------------------------------------------------------------------------
Describe 'Remove-PveFirewallRule' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveFirewallRule' }

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

    Context 'Parameter metadata' {
        It 'Should have mandatory Level parameter with ValidateSet' {
            $param = $script:Cmd.Parameters['Level']
            $param | Should -Not -BeNullOrEmpty
            $isMandatory = $param.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
            $validateSet = $param.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $validateSet | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Remove-PveFirewallRule -Level 'cluster' -Position 0 -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }

    Context 'Group level' {
        It 'Should include Group in the Level ValidateSet' {
            $param = $script:Cmd.Parameters['Level']
            $validateSet = $param.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $validateSet.ValidValues | Should -Contain 'Group'
        }

        It 'Should throw when Level is Group and Group is not specified' {
            { Remove-PveFirewallRule -Level 'Group' -Position 0 -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*Group is required when Level is Group*'
        }
    }
}
