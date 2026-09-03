#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for VM disk operation cmdlets:
        Move-PveVmDisk, Remove-PveVmDisk.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Move-PveVmDisk
# ---------------------------------------------------------------------------
Describe 'Move-PveVmDisk' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Move-PveVmDisk' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Move-PveVmDisk -Node 'pve' -VmId 100 -Disk 'scsi0' -Storage 'local-lvm' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveVmDisk
# ---------------------------------------------------------------------------
Describe 'Remove-PveVmDisk' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveVmDisk' }

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
            { Remove-PveVmDisk -Node 'pve' -VmId 100 -IdList 'scsi0' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
