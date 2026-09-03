#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for snapshot cmdlets:
        Get-PveSnapshot, New-PveSnapshot, Remove-PveSnapshot, Restore-PveSnapshot.

    All tests are fully offline — no live Proxmox VE target is required.

    Snapshot cmdlets apply to both QEMU VMs and LXC containers; the
    parameter set distinction is captured in the 'Type' tests below.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Get-PveSnapshot
# ---------------------------------------------------------------------------
Describe 'Get-PveSnapshot' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveSnapshot' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveSnapshot -Node 'pve-node1' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveSnapshot
# ---------------------------------------------------------------------------
Describe 'New-PveSnapshot' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveSnapshot' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            { New-PveSnapshot -Node 'pve-node1' -VmId 100 -Name 'snap1' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveSnapshot
# ---------------------------------------------------------------------------
Describe 'Remove-PveSnapshot' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveSnapshot' }

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
            { Remove-PveSnapshot -Node 'pve-node1' -VmId 100 -Name 'snap1' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Restore-PveSnapshot
# ---------------------------------------------------------------------------
Describe 'Restore-PveSnapshot' {
    BeforeAll { $script:Cmd = Get-Command 'Restore-PveSnapshot' }

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
            { Restore-PveSnapshot -Node 'pve-node1' -VmId 100 -Name 'snap1' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
