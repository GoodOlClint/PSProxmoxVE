#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for firewall options/ref cmdlets:
        Get-PveFirewallOptions, Set-PveFirewallOptions, Get-PveFirewallRef.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Get-PveFirewallOptions
# ---------------------------------------------------------------------------
Describe 'Get-PveFirewallOptions' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveFirewallOptions' }

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
            { Get-PveFirewallOptions -Level 'cluster' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveFirewallOptions
# ---------------------------------------------------------------------------
Describe 'Set-PveFirewallOptions' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PveFirewallOptions' }

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
            { Set-PveFirewallOptions -Level 'cluster' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveFirewallRef
# ---------------------------------------------------------------------------
Describe 'Get-PveFirewallRef' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveFirewallRef' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveFirewallRef -Level Cluster -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
