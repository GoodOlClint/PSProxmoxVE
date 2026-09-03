#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for network cmdlets:
        Get-PveNetwork, New-PveNetwork, Set-PveNetwork,
        Remove-PveNetwork, Invoke-PveNetworkApply.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Get-PveNetwork
# ---------------------------------------------------------------------------
Describe 'Get-PveNetwork' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveNetwork' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveNetwork -Node 'pve-node1' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveNetwork
# ---------------------------------------------------------------------------
Describe 'New-PveNetwork' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveNetwork' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'VLAN-aware bridge' {
        It 'Should expose BridgeVlanAware as a switch' {
            $script:Cmd.Parameters.ContainsKey('BridgeVlanAware') | Should -BeTrue
            $script:Cmd.Parameters['BridgeVlanAware'].ParameterType |
                Should -Be ([System.Management.Automation.SwitchParameter])
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveNetwork
# ---------------------------------------------------------------------------
Describe 'Set-PveNetwork' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PveNetwork' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'VLAN-aware bridge' {
        It 'Should expose BridgeVlanAware as a switch' {
            $script:Cmd.Parameters.ContainsKey('BridgeVlanAware') | Should -BeTrue
            $script:Cmd.Parameters['BridgeVlanAware'].ParameterType |
                Should -Be ([System.Management.Automation.SwitchParameter])
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveNetwork
# ---------------------------------------------------------------------------
Describe 'Remove-PveNetwork' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveNetwork' }

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
}

# ---------------------------------------------------------------------------
# Invoke-PveNetworkApply
# ---------------------------------------------------------------------------
Describe 'Invoke-PveNetworkApply' {
    BeforeAll { $script:Cmd = Get-Command 'Invoke-PveNetworkApply' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }
}
