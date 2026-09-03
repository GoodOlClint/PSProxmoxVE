#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for container gap cmdlets:
        Suspend-PveContainer, Resume-PveContainer, Resize-PveContainerDisk,
        New-PveContainerTemplate, Move-PveContainerVolume, Get-PveContainerInterface.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Suspend-PveContainer
# ---------------------------------------------------------------------------
Describe 'Suspend-PveContainer' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Suspend-PveContainer' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Suspend-PveContainer -Node 'pve' -VmId 100 -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Resume-PveContainer
# ---------------------------------------------------------------------------
Describe 'Resume-PveContainer' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Resume-PveContainer' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Resume-PveContainer -Node 'pve' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Resize-PveContainerDisk
# ---------------------------------------------------------------------------
Describe 'Resize-PveContainerDisk' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Resize-PveContainerDisk' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Resize-PveContainerDisk -Node 'pve' -VmId 100 -Disk 'rootfs' -Size '+5G' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveContainerTemplate
# ---------------------------------------------------------------------------
Describe 'New-PveContainerTemplate' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveContainerTemplate' }

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
            { New-PveContainerTemplate -Node 'pve' -VmId 100 -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Move-PveContainerVolume
# ---------------------------------------------------------------------------
Describe 'Move-PveContainerVolume' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Move-PveContainerVolume' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Move-PveContainerVolume -Node 'pve' -VmId 100 -Volume 'rootfs' -Storage 'local-lvm' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveContainerInterface
# ---------------------------------------------------------------------------
Describe 'Get-PveContainerInterface' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveContainerInterface' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveContainerInterface -Node 'pve' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
