#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for SDN subnet cmdlets:
        Get-PveSdnSubnet, New-PveSdnSubnet, Remove-PveSdnSubnet.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Get-PveSdnSubnet
# ---------------------------------------------------------------------------
Describe 'Get-PveSdnSubnet' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveSdnSubnet' }

    Context 'Parameter metadata' {
        It 'Vnet should accept pipeline input by property name' {
            $attr = $script:Cmd.Parameters['Vnet'].ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $attr | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveSdnSubnet -Vnet 'myvnet' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveSdnSubnet
# ---------------------------------------------------------------------------
Describe 'New-PveSdnSubnet' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveSdnSubnet' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            { New-PveSdnSubnet -Vnet 'myvnet' -Subnet '10.0.0.0/24' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveSdnSubnet
# ---------------------------------------------------------------------------
Describe 'Remove-PveSdnSubnet' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveSdnSubnet' }

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
        It 'Should throw when no session is active (without -WhatIf)' {
            { Remove-PveSdnSubnet -Vnet 'myvnet' -Subnet '10.0.0.0/24' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
