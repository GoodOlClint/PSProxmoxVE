#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for New-PveVm.
    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

Describe 'New-PveVm' {

    Context 'Command existence' {
        It 'Should be available after module import' {
            Get-Command 'New-PveVm' -ErrorAction SilentlyContinue |
                Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            (Get-Command 'New-PveVm').CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        BeforeAll {
            $script:Cmd = Get-Command 'New-PveVm'
        }

        It 'Should support ShouldProcess (WhatIf parameter present)' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should support ShouldProcess (Confirm parameter present)' {
            $script:Cmd.Parameters.ContainsKey('Confirm') | Should -BeTrue
        }

        It 'Should accept -WhatIf without throwing even when no session exists' {
            # WhatIf must short-circuit before any network call or session check.
            { New-PveVm -Node 'pve-node1' -WhatIf -ErrorAction Stop } | Should -Not -Throw
        }
    }

    Context 'Required parameter — Node' {
        It 'Node should be Mandatory' {
            $nodeParam = (Get-Command 'New-PveVm').Parameters['Node']
            $isMandatory = $nodeParam.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Optional parameter metadata' {
        BeforeAll {
            $script:Cmd = Get-Command 'New-PveVm'
        }

        It 'Should have VmId parameter' {
            $script:Cmd.Parameters.ContainsKey('VmId') | Should -BeTrue
        }

        It 'Should have Name parameter' {
            $script:Cmd.Parameters.ContainsKey('Name') | Should -BeTrue
        }

        It 'Should have Memory parameter' {
            $script:Cmd.Parameters.ContainsKey('Memory') | Should -BeTrue
        }

        It 'Should have Cores parameter' {
            $script:Cmd.Parameters.ContainsKey('Cores') | Should -BeTrue
        }

        It 'Should have Sockets parameter' {
            $script:Cmd.Parameters.ContainsKey('Sockets') | Should -BeTrue
        }

        It 'Should have CpuType parameter' {
            $script:Cmd.Parameters.ContainsKey('CpuType') | Should -BeTrue
        }

        It 'Should have Bios parameter' {
            $script:Cmd.Parameters.ContainsKey('Bios') | Should -BeTrue
        }

        It 'Should have Machine parameter' {
            $script:Cmd.Parameters.ContainsKey('Machine') | Should -BeTrue
        }

        It 'Should have DiskSize parameter' {
            $script:Cmd.Parameters.ContainsKey('DiskSize') | Should -BeTrue
        }

        It 'Should have DiskStorage parameter' {
            $script:Cmd.Parameters.ContainsKey('DiskStorage') | Should -BeTrue
        }

        It 'Should have DiskFormat parameter' {
            $script:Cmd.Parameters.ContainsKey('DiskFormat') | Should -BeTrue
        }

        It 'Should have Network parameter' {
            $script:Cmd.Parameters.ContainsKey('Network') | Should -BeTrue
        }

        It 'Should have Bridge parameter' {
            $script:Cmd.Parameters.ContainsKey('Bridge') | Should -BeTrue
        }

        It 'Should have OsType parameter' {
            $script:Cmd.Parameters.ContainsKey('OsType') | Should -BeTrue
        }

        It 'Should have Start switch parameter' {
            $script:Cmd.Parameters.ContainsKey('Start') | Should -BeTrue
            $script:Cmd.Parameters['Start'].ParameterType |
                Should -Be ([System.Management.Automation.SwitchParameter])
        }

        It 'Should have Wait switch parameter' {
            $script:Cmd.Parameters.ContainsKey('Wait') | Should -BeTrue
            $script:Cmd.Parameters['Wait'].ParameterType |
                Should -Be ([System.Management.Automation.SwitchParameter])
        }

        It 'Should have Session parameter (inherited from PveCmdletBase)' {
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            { New-PveVm -Node 'pve-node1' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }

    Context 'DiskSize validation' {
        # Validation runs before ShouldProcess so -WhatIf is enough to exercise it
        # without an active session.

        It 'Should reject sub-GB units (e.g. 512M)' {
            { New-PveVm -Node 'pve-node1' -DiskStorage 'local-lvm' -DiskSize '512M' -WhatIf -ErrorAction Stop } |
                Should -Throw '*unsupported unit*'
        }

        It 'Should reject sub-GB units even when -DiskStorage is omitted' {
            { New-PveVm -Node 'pve-node1' -DiskSize '512M' -WhatIf -ErrorAction Stop } |
                Should -Throw '*unsupported unit*'
        }

        It 'Should reject malformed input (e.g. 32.5G)' {
            { New-PveVm -Node 'pve-node1' -DiskStorage 'local-lvm' -DiskSize '32.5G' -WhatIf -ErrorAction Stop } |
                Should -Throw '*not a valid size*'
        }

        It 'Should accept a bare integer with -WhatIf' {
            { New-PveVm -Node 'pve-node1' -DiskStorage 'local-lvm' -DiskSize '32' -WhatIf -ErrorAction Stop } |
                Should -Not -Throw
        }

        It 'Should accept "32G" with -WhatIf' {
            { New-PveVm -Node 'pve-node1' -DiskStorage 'local-lvm' -DiskSize '32G' -WhatIf -ErrorAction Stop } |
                Should -Not -Throw
        }

        It 'Should accept "1T" with -WhatIf' {
            { New-PveVm -Node 'pve-node1' -DiskStorage 'local-lvm' -DiskSize '1T' -WhatIf -ErrorAction Stop } |
                Should -Not -Throw
        }
    }

    Context 'Disk controller / IO parameter metadata' {
        BeforeAll { $script:Cmd = Get-Command 'New-PveVm' }

        It 'Should have <_> parameter' -ForEach @(
            'DiskBus', 'ScsiHardware', 'DiskIoThread', 'DiskAio', 'DiskSsd', 'DiskDiscard', 'DiskCache'
        ) {
            $script:Cmd.Parameters.ContainsKey($_) | Should -BeTrue
        }

        It 'DiskBus should have a ValidateSet of virtio, scsi, sata, ide' {
            $vs = $script:Cmd.Parameters['DiskBus'].Attributes |
                Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] } |
                Select-Object -First 1
            $vs.ValidValues | Should -Contain 'virtio'
            $vs.ValidValues | Should -Contain 'scsi'
            $vs.ValidValues | Should -Contain 'sata'
            $vs.ValidValues | Should -Contain 'ide'
        }

        It 'DiskIoThread and DiskSsd should be switch parameters' {
            $script:Cmd.Parameters['DiskIoThread'].ParameterType | Should -Be ([System.Management.Automation.SwitchParameter])
            $script:Cmd.Parameters['DiskSsd'].ParameterType | Should -Be ([System.Management.Automation.SwitchParameter])
        }
    }

    Context 'Disk option validation' {
        # Validation runs before ShouldProcess, so -WhatIf exercises it offline.

        It 'Should reject -DiskSsd on the virtio bus' {
            { New-PveVm -Node 'pve-node1' -DiskStorage 'local-lvm' -DiskSize '32' -DiskSsd -WhatIf -ErrorAction Stop } |
                Should -Throw '*virtio bus*'
        }

        It 'Should reject -DiskSsd on the default (virtio) bus when bus omitted' {
            { New-PveVm -Node 'pve-node1' -DiskStorage 'local-lvm' -DiskSize '32' -DiskSsd -WhatIf -ErrorAction Stop } |
                Should -Throw '*virtio bus*'
        }

        It 'Should accept -DiskSsd on the scsi bus' {
            { New-PveVm -Node 'pve-node1' -DiskStorage 'local-lvm' -DiskSize '32' -DiskBus scsi -DiskSsd -WhatIf -ErrorAction Stop } |
                Should -Not -Throw
        }

        It 'Should reject -DiskIoThread on the sata bus' {
            { New-PveVm -Node 'pve-node1' -DiskStorage 'local-lvm' -DiskSize '32' -DiskBus sata -DiskIoThread -WhatIf -ErrorAction Stop } |
                Should -Throw '*requires -DiskBus virtio or scsi*'
        }

        It 'Should reject -DiskIoThread on scsi without -ScsiHardware virtio-scsi-single' {
            { New-PveVm -Node 'pve-node1' -DiskStorage 'local-lvm' -DiskSize '32' -DiskBus scsi -DiskIoThread -WhatIf -ErrorAction Stop } |
                Should -Throw '*virtio-scsi-single*'
        }

        It 'Should reject -DiskIoThread on scsi with a wrong -ScsiHardware (virtio-scsi-pci)' {
            { New-PveVm -Node 'pve-node1' -DiskStorage 'local-lvm' -DiskSize '32' -DiskBus scsi -ScsiHardware 'virtio-scsi-pci' -DiskIoThread -WhatIf -ErrorAction Stop } |
                Should -Throw '*virtio-scsi-single*'
        }

        It 'Should accept -DiskIoThread on scsi with -ScsiHardware virtio-scsi-single' {
            { New-PveVm -Node 'pve-node1' -DiskStorage 'local-lvm' -DiskSize '32' -DiskBus scsi -ScsiHardware 'virtio-scsi-single' -DiskIoThread -WhatIf -ErrorAction Stop } |
                Should -Not -Throw
        }

        It 'Should accept -DiskIoThread on the default virtio bus' {
            { New-PveVm -Node 'pve-node1' -DiskStorage 'local-lvm' -DiskSize '32' -DiskIoThread -WhatIf -ErrorAction Stop } |
                Should -Not -Throw
        }

        It 'Should accept a fully tuned scsi disk spec' {
            { New-PveVm -Node 'pve-node1' -DiskStorage 'local-lvm' -DiskSize '60' -DiskBus scsi `
                -ScsiHardware 'virtio-scsi-single' -DiskIoThread -DiskAio native -DiskSsd -DiskDiscard `
                -DiskCache none -WhatIf -ErrorAction Stop } |
                Should -Not -Throw
        }
    }
}
