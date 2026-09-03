#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for container lifecycle cmdlets:
        New-PveContainer, Remove-PveContainer,
        Start-PveContainer, Stop-PveContainer, Restart-PveContainer.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    # Pre-calculate availability for each cmdlet.
}

# ---------------------------------------------------------------------------
# New-PveContainer
# ---------------------------------------------------------------------------
Describe 'New-PveContainer' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveContainer' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            { New-PveContainer -Node 'pve-node1' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveContainer
# ---------------------------------------------------------------------------
Describe 'Remove-PveContainer' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveContainer' }

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
            { Remove-PveContainer -Node 'pve-node1' -VmId 200 -Confirm:$false -ErrorAction Stop } | Should -Throw '*No active Proxmox VE session*'
        }

        It 'Should not throw with -WhatIf' {
            { Remove-PveContainer -Node 'pve-node1' -VmId 200 -WhatIf -ErrorAction Stop } |
                Should -Not -Throw
        }
    }
}

# ---------------------------------------------------------------------------
# Start-PveContainer
# ---------------------------------------------------------------------------
Describe 'Start-PveContainer' {
    BeforeAll { $script:Cmd = Get-Command 'Start-PveContainer' }

    Context 'Parameter metadata' {
        It 'Should support ShouldProcess' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Start-PveContainer -Node 'pve-node1' -VmId 200 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Stop-PveContainer
# ---------------------------------------------------------------------------
Describe 'Stop-PveContainer' {
    BeforeAll { $script:Cmd = Get-Command 'Stop-PveContainer' }

    Context 'Parameter metadata' {
        It 'Should support ShouldProcess' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Stop-PveContainer -Node 'pve-node1' -VmId 200 -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Restart-PveContainer
# ---------------------------------------------------------------------------
Describe 'Restart-PveContainer' {
    BeforeAll { $script:Cmd = Get-Command 'Restart-PveContainer' }

    Context 'Parameter metadata' {
        It 'Should support ShouldProcess' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Restart-PveContainer -Node 'pve-node1' -VmId 200 -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Move-PveContainer
# ---------------------------------------------------------------------------
Describe 'Move-PveContainer' {
    BeforeAll { $script:Cmd = Get-Command 'Move-PveContainer' }

    Context 'Parameter metadata' {
        It 'Should support ShouldProcess' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Move-PveContainer -Node 'pve-node1' -VmId 200 -TargetNode 'pve-node2' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
