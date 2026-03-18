#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for VM configuration cmdlets:
        Get-PveVmConfig, Set-PveVmConfig, Resize-PveVmDisk.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Get-PveVmConfig
# ---------------------------------------------------------------------------
Describe 'Get-PveVmConfig' {

    Context 'Command existence' {
        It 'Should be available after module import' {
            Get-Command 'Get-PveVmConfig' -ErrorAction SilentlyContinue |
                Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            (Get-Command 'Get-PveVmConfig').CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        BeforeAll {
            $script:Cmd = Get-Command 'Get-PveVmConfig'
        }

        It 'Should have Node parameter' {
            $script:Cmd.Parameters.ContainsKey('Node') | Should -BeTrue
        }

        It 'Node should be Mandatory' {
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Node should be of type String' {
            $script:Cmd.Parameters['Node'].ParameterType | Should -Be ([string])
        }

        It 'Node should accept pipeline input by property name' {
            $acceptsByPropName = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $acceptsByPropName | Should -Not -BeNullOrEmpty
        }

        It 'Should have VmId parameter' {
            $script:Cmd.Parameters.ContainsKey('VmId') | Should -BeTrue
        }

        It 'VmId should be Mandatory' {
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be of type Int32' {
            $script:Cmd.Parameters['VmId'].ParameterType | Should -Be ([int])
        }

        It 'VmId should accept pipeline input by property name' {
            $acceptsByPropName = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $acceptsByPropName | Should -Not -BeNullOrEmpty
        }

        It 'Should have Session parameter (inherited from PveCmdletBase)' {
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }

        It 'Should not support ShouldProcess (read-only cmdlet)' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeFalse
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active and no -Session is supplied' {
            { Get-PveVmConfig -Node 'pve-node1' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }

        It 'Should throw PveNotConnectedException type' {
            try {
                Get-PveVmConfig -Node 'pve-node1' -VmId 100 -ErrorAction Stop
            }
            catch {
                $_.Exception.GetType().Name | Should -Match 'PveNotConnectedException|CmdletInvocationException'
            }
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveVmConfig
# ---------------------------------------------------------------------------
Describe 'Set-PveVmConfig' {

    Context 'Command existence' {
        It 'Should be available after module import' {
            Get-Command 'Set-PveVmConfig' -ErrorAction SilentlyContinue |
                Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            (Get-Command 'Set-PveVmConfig').CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        BeforeAll {
            $script:Cmd = Get-Command 'Set-PveVmConfig'
        }

        It 'Should have Node parameter' {
            $script:Cmd.Parameters.ContainsKey('Node') | Should -BeTrue
        }

        It 'Node should be Mandatory' {
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Node should accept pipeline input by property name' {
            $acceptsByPropName = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $acceptsByPropName | Should -Not -BeNullOrEmpty
        }

        It 'Should have VmId parameter' {
            $script:Cmd.Parameters.ContainsKey('VmId') | Should -BeTrue
        }

        It 'VmId should be Mandatory' {
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should accept pipeline input by property name' {
            $acceptsByPropName = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $acceptsByPropName | Should -Not -BeNullOrEmpty
        }

        # Configuration parameters — all optional
        It 'Should have Cores parameter (optional, Int32)' {
            $script:Cmd.Parameters.ContainsKey('Cores') | Should -BeTrue
            $script:Cmd.Parameters['Cores'].ParameterType |
                Should -Be ([System.Nullable[int]])
            $isMandatory = $script:Cmd.Parameters['Cores'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }

        It 'Should have Sockets parameter (optional, Int32)' {
            $script:Cmd.Parameters.ContainsKey('Sockets') | Should -BeTrue
            $script:Cmd.Parameters['Sockets'].ParameterType |
                Should -Be ([System.Nullable[int]])
            $isMandatory = $script:Cmd.Parameters['Sockets'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }

        It 'Should have Memory parameter (optional, Int32)' {
            $script:Cmd.Parameters.ContainsKey('Memory') | Should -BeTrue
            $script:Cmd.Parameters['Memory'].ParameterType |
                Should -Be ([System.Nullable[int]])
            $isMandatory = $script:Cmd.Parameters['Memory'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }

        It 'Should have CpuType parameter (optional, String)' {
            $script:Cmd.Parameters.ContainsKey('CpuType') | Should -BeTrue
            $script:Cmd.Parameters['CpuType'].ParameterType | Should -Be ([string])
            $isMandatory = $script:Cmd.Parameters['CpuType'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }

        It 'Should have Description parameter (optional, String)' {
            $script:Cmd.Parameters.ContainsKey('Description') | Should -BeTrue
            $script:Cmd.Parameters['Description'].ParameterType | Should -Be ([string])
        }

        It 'Should have Tags parameter (optional, String)' {
            $script:Cmd.Parameters.ContainsKey('Tags') | Should -BeTrue
            $script:Cmd.Parameters['Tags'].ParameterType | Should -Be ([string])
        }

        It 'Should have Bios parameter (optional, String)' {
            $script:Cmd.Parameters.ContainsKey('Bios') | Should -BeTrue
            $script:Cmd.Parameters['Bios'].ParameterType | Should -Be ([string])
        }

        It 'Should have Machine parameter (optional, String)' {
            $script:Cmd.Parameters.ContainsKey('Machine') | Should -BeTrue
            $script:Cmd.Parameters['Machine'].ParameterType | Should -Be ([string])
        }

        It 'Should have OsType parameter (optional, String)' {
            $script:Cmd.Parameters.ContainsKey('OsType') | Should -BeTrue
            $script:Cmd.Parameters['OsType'].ParameterType | Should -Be ([string])
        }

        It 'Should have Session parameter (inherited from PveCmdletBase)' {
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support ShouldProcess (WhatIf parameter exists)' {
            $cmd = Get-Command 'Set-PveVmConfig'
            $cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should support Confirm parameter' {
            $cmd = Get-Command 'Set-PveVmConfig'
            $cmd.Parameters.ContainsKey('Confirm') | Should -BeTrue
        }

        It 'Should not throw with -WhatIf (no session required)' {
            { Set-PveVmConfig -Node 'pve-node1' -VmId 100 -Memory 4096 -WhatIf -ErrorAction Stop } |
                Should -Not -Throw
        }

        It 'Should not throw with -WhatIf and multiple config params' {
            { Set-PveVmConfig -Node 'pve-node1' -VmId 100 -Cores 4 -Sockets 2 -Memory 8192 -WhatIf -ErrorAction Stop } |
                Should -Not -Throw
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            { Set-PveVmConfig -Node 'pve-node1' -VmId 100 -Memory 4096 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }

        It 'Should throw PveNotConnectedException type' {
            try {
                Set-PveVmConfig -Node 'pve-node1' -VmId 100 -Cores 2 -ErrorAction Stop
            }
            catch {
                $_.Exception.GetType().Name | Should -Match 'PveNotConnectedException|CmdletInvocationException'
            }
        }
    }
}

# ---------------------------------------------------------------------------
# Resize-PveVmDisk
# ---------------------------------------------------------------------------
Describe 'Resize-PveVmDisk' {

    Context 'Command existence' {
        It 'Should be available after module import' {
            Get-Command 'Resize-PveVmDisk' -ErrorAction SilentlyContinue |
                Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            (Get-Command 'Resize-PveVmDisk').CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        BeforeAll {
            $script:Cmd = Get-Command 'Resize-PveVmDisk'
        }

        It 'Should have Node parameter' {
            $script:Cmd.Parameters.ContainsKey('Node') | Should -BeTrue
        }

        It 'Node should be Mandatory' {
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Node should be of type String' {
            $script:Cmd.Parameters['Node'].ParameterType | Should -Be ([string])
        }

        It 'Node should accept pipeline input by property name' {
            $acceptsByPropName = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $acceptsByPropName | Should -Not -BeNullOrEmpty
        }

        It 'Should have VmId parameter' {
            $script:Cmd.Parameters.ContainsKey('VmId') | Should -BeTrue
        }

        It 'VmId should be Mandatory' {
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be of type Int32' {
            $script:Cmd.Parameters['VmId'].ParameterType | Should -Be ([int])
        }

        It 'VmId should accept pipeline input by property name' {
            $acceptsByPropName = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $acceptsByPropName | Should -Not -BeNullOrEmpty
        }

        It 'Should have Disk parameter' {
            $script:Cmd.Parameters.ContainsKey('Disk') | Should -BeTrue
        }

        It 'Disk should be Mandatory' {
            $isMandatory = $script:Cmd.Parameters['Disk'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Disk should be of type String' {
            $script:Cmd.Parameters['Disk'].ParameterType | Should -Be ([string])
        }

        It 'Should have Size parameter' {
            $script:Cmd.Parameters.ContainsKey('Size') | Should -BeTrue
        }

        It 'Size should be Mandatory' {
            $isMandatory = $script:Cmd.Parameters['Size'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Size should be of type String' {
            $script:Cmd.Parameters['Size'].ParameterType | Should -Be ([string])
        }

        It 'Should have Wait switch parameter' {
            $script:Cmd.Parameters.ContainsKey('Wait') | Should -BeTrue
            $script:Cmd.Parameters['Wait'].ParameterType |
                Should -Be ([System.Management.Automation.SwitchParameter])
        }

        It 'Wait should not be Mandatory' {
            $isMandatory = $script:Cmd.Parameters['Wait'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }

        It 'Should have Session parameter (inherited from PveCmdletBase)' {
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support ShouldProcess (WhatIf parameter exists)' {
            $cmd = Get-Command 'Resize-PveVmDisk'
            $cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should support Confirm parameter' {
            $cmd = Get-Command 'Resize-PveVmDisk'
            $cmd.Parameters.ContainsKey('Confirm') | Should -BeTrue
        }

        It 'Should not throw with -WhatIf (no session required)' {
            { Resize-PveVmDisk -Node 'pve-node1' -VmId 100 -Disk 'scsi0' -Size '+10G' -WhatIf -ErrorAction Stop } |
                Should -Not -Throw
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            { Resize-PveVmDisk -Node 'pve-node1' -VmId 100 -Disk 'scsi0' -Size '50G' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }

        It 'Should throw PveNotConnectedException type' {
            try {
                Resize-PveVmDisk -Node 'pve-node1' -VmId 100 -Disk 'virtio0' -Size '+5G' -ErrorAction Stop
            }
            catch {
                $_.Exception.GetType().Name | Should -Match 'PveNotConnectedException|CmdletInvocationException'
            }
        }
    }
}
