#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for VM lifecycle cmdlets:
        Start-PveVm, Stop-PveVm, Suspend-PveVm, Resume-PveVm,
        Reset-PveVm, Restart-PveVm.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Start-PveVm
# ---------------------------------------------------------------------------
Describe 'Start-PveVm' {
    Context 'Parameter metadata' {
        BeforeAll { $script:Cmd = Get-Command 'Start-PveVm' }

        It 'Should have Wait switch parameter' {
            $script:Cmd.Parameters.ContainsKey('Wait') | Should -BeTrue
            $script:Cmd.Parameters['Wait'].ParameterType |
                Should -Be ([System.Management.Automation.SwitchParameter])
        }

        It 'Should support ShouldProcess' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            { Start-PveVm -Node 'pve-node1' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }

        It 'Should not throw with -WhatIf' {
            { Start-PveVm -Node 'pve-node1' -VmId 100 -WhatIf -ErrorAction Stop } |
                Should -Not -Throw
        }
    }
}

# ---------------------------------------------------------------------------
# Stop-PveVm
# ---------------------------------------------------------------------------
Describe 'Stop-PveVm' {
    Context 'Parameter metadata' {
        BeforeAll { $script:Cmd = Get-Command 'Stop-PveVm' }

        It 'Should support ShouldProcess' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            { Stop-PveVm -Node 'pve-node1' -VmId 100 -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }

        It 'Should not throw with -WhatIf' {
            { Stop-PveVm -Node 'pve-node1' -VmId 100 -WhatIf -ErrorAction Stop } |
                Should -Not -Throw
        }
    }
}

# ---------------------------------------------------------------------------
# Suspend-PveVm
# ---------------------------------------------------------------------------
Describe 'Suspend-PveVm' {
    Context 'Parameter metadata' {
        BeforeAll { $script:Cmd = Get-Command 'Suspend-PveVm' }

        It 'Should support ShouldProcess' {
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
            { Suspend-PveVm -Node 'pve-node1' -VmId 100 -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Resume-PveVm
# ---------------------------------------------------------------------------
Describe 'Resume-PveVm' {
    Context 'Parameter metadata' {
        BeforeAll { $script:Cmd = Get-Command 'Resume-PveVm' }

        It 'Should support ShouldProcess' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            { Resume-PveVm -Node 'pve-node1' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Reset-PveVm
# ---------------------------------------------------------------------------
Describe 'Reset-PveVm' {
    Context 'Parameter metadata' {
        BeforeAll {
            $script:Cmd = Get-Command 'Reset-PveVm'
        }

        It 'Should support ShouldProcess' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }
}

# ---------------------------------------------------------------------------
# Restart-PveVm
# ---------------------------------------------------------------------------
Describe 'Restart-PveVm' {
    Context 'Parameter metadata' {
        BeforeAll { $script:Cmd = Get-Command 'Restart-PveVm' }

        It 'Should have Wait switch parameter' {
            $script:Cmd.Parameters.ContainsKey('Wait') | Should -BeTrue
            $script:Cmd.Parameters['Wait'].ParameterType |
                Should -Be ([System.Management.Automation.SwitchParameter])
        }

        It 'Should support ShouldProcess' {
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
            { Restart-PveVm -Node 'pve-node1' -VmId 100 -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }

        It 'Should not throw with -WhatIf' {
            { Restart-PveVm -Node 'pve-node1' -VmId 100 -WhatIf -ErrorAction Stop } |
                Should -Not -Throw
        }
    }
}
