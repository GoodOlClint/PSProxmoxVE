#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for container snapshot cmdlets:
        Get-PveContainerSnapshot, New-PveContainerSnapshot,
        Remove-PveContainerSnapshot, Restore-PveContainerSnapshot.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Get-PveContainerSnapshot
# ---------------------------------------------------------------------------
Describe 'Get-PveContainerSnapshot' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveContainerSnapshot' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveContainerSnapshot -Node 'pve-node1' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveContainerSnapshot
# ---------------------------------------------------------------------------
Describe 'New-PveContainerSnapshot' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveContainerSnapshot' }

    Context 'Parameter surface' {
        It 'Should NOT have IncludeVmState (LXC containers do not support RAM snapshots)' {
            $script:Cmd.Parameters.ContainsKey('IncludeVmState') | Should -BeFalse
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            { New-PveContainerSnapshot -Node 'pve-node1' -VmId 100 -Name 'snap1' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveContainerSnapshot
# ---------------------------------------------------------------------------
Describe 'Remove-PveContainerSnapshot' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveContainerSnapshot' }

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
            { Remove-PveContainerSnapshot -Node 'pve-node1' -VmId 100 -Name 'snap1' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Restore-PveContainerSnapshot
# ---------------------------------------------------------------------------
Describe 'Restore-PveContainerSnapshot' {
    BeforeAll { $script:Cmd = Get-Command 'Restore-PveContainerSnapshot' }

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
            { Restore-PveContainerSnapshot -Node 'pve-node1' -VmId 100 -Name 'snap1' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
