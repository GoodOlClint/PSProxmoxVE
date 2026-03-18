#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for VM advanced operation cmdlets:
        Copy-PveVm, Move-PveVm.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Copy-PveVm
# ---------------------------------------------------------------------------
Describe 'Copy-PveVm' {

    Context 'Command existence' {
        It 'Should be available after module import' {
            Get-Command 'Copy-PveVm' -ErrorAction SilentlyContinue |
                Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            (Get-Command 'Copy-PveVm').CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        BeforeAll {
            $script:Cmd = Get-Command 'Copy-PveVm'
        }

        # --- SourceNode ---
        It 'Should have SourceNode parameter' {
            $script:Cmd.Parameters.ContainsKey('SourceNode') | Should -BeTrue
        }

        It 'SourceNode should be Mandatory' {
            $isMandatory = $script:Cmd.Parameters['SourceNode'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'SourceNode should be of type String' {
            $script:Cmd.Parameters['SourceNode'].ParameterType | Should -Be ([string])
        }

        # --- VmId ---
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

        # --- NewVmId (optional) ---
        It 'Should have NewVmId parameter' {
            $script:Cmd.Parameters.ContainsKey('NewVmId') | Should -BeTrue
        }

        It 'NewVmId should not be Mandatory' {
            $isMandatory = $script:Cmd.Parameters['NewVmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }

        It 'NewVmId should be of nullable Int32 type' {
            $script:Cmd.Parameters['NewVmId'].ParameterType |
                Should -Be ([System.Nullable[int]])
        }

        # --- NewName (optional) ---
        It 'Should have NewName parameter' {
            $script:Cmd.Parameters.ContainsKey('NewName') | Should -BeTrue
        }

        It 'NewName should not be Mandatory' {
            $isMandatory = $script:Cmd.Parameters['NewName'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }

        It 'NewName should be of type String' {
            $script:Cmd.Parameters['NewName'].ParameterType | Should -Be ([string])
        }

        # --- TargetNode (optional) ---
        It 'Should have TargetNode parameter' {
            $script:Cmd.Parameters.ContainsKey('TargetNode') | Should -BeTrue
        }

        It 'TargetNode should not be Mandatory' {
            $isMandatory = $script:Cmd.Parameters['TargetNode'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }

        It 'TargetNode should be of type String' {
            $script:Cmd.Parameters['TargetNode'].ParameterType | Should -Be ([string])
        }

        # --- Full (switch, optional) ---
        It 'Should have Full switch parameter' {
            $script:Cmd.Parameters.ContainsKey('Full') | Should -BeTrue
            $script:Cmd.Parameters['Full'].ParameterType |
                Should -Be ([System.Management.Automation.SwitchParameter])
        }

        It 'Full should not be Mandatory' {
            $isMandatory = $script:Cmd.Parameters['Full'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }

        # --- Storage (optional) ---
        It 'Should have Storage parameter' {
            $script:Cmd.Parameters.ContainsKey('Storage') | Should -BeTrue
        }

        It 'Storage should not be Mandatory' {
            $isMandatory = $script:Cmd.Parameters['Storage'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }

        It 'Storage should be of type String' {
            $script:Cmd.Parameters['Storage'].ParameterType | Should -Be ([string])
        }

        # --- Wait (switch, optional) ---
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

        # --- Session (inherited) ---
        It 'Should have Session parameter (inherited from PveCmdletBase)' {
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support ShouldProcess (WhatIf parameter exists)' {
            $cmd = Get-Command 'Copy-PveVm'
            $cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should support Confirm parameter' {
            $cmd = Get-Command 'Copy-PveVm'
            $cmd.Parameters.ContainsKey('Confirm') | Should -BeTrue
        }

        It 'Should not throw with -WhatIf (no session required)' {
            { Copy-PveVm -SourceNode 'pve-node1' -VmId 100 -WhatIf -ErrorAction Stop } |
                Should -Not -Throw
        }

        It 'Should not throw with -WhatIf and optional params' {
            { Copy-PveVm -SourceNode 'pve-node1' -VmId 100 -NewVmId 200 -NewName 'clone-test' -TargetNode 'pve-node2' -Full -WhatIf -ErrorAction Stop } |
                Should -Not -Throw
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            { Copy-PveVm -SourceNode 'pve-node1' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }

        It 'Should throw PveNotConnectedException type' {
            try {
                Copy-PveVm -SourceNode 'pve-node1' -VmId 100 -ErrorAction Stop
            }
            catch {
                $_.Exception.GetType().Name | Should -Match 'PveNotConnectedException|CmdletInvocationException'
            }
        }
    }
}

# ---------------------------------------------------------------------------
# Move-PveVm
# ---------------------------------------------------------------------------
Describe 'Move-PveVm' {

    Context 'Command existence' {
        It 'Should be available after module import' {
            Get-Command 'Move-PveVm' -ErrorAction SilentlyContinue |
                Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            (Get-Command 'Move-PveVm').CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        BeforeAll {
            $script:Cmd = Get-Command 'Move-PveVm'
        }

        # --- Node ---
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

        # --- VmId ---
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

        # --- TargetNode ---
        It 'Should have TargetNode parameter' {
            $script:Cmd.Parameters.ContainsKey('TargetNode') | Should -BeTrue
        }

        It 'TargetNode should be Mandatory' {
            $isMandatory = $script:Cmd.Parameters['TargetNode'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'TargetNode should be of type String' {
            $script:Cmd.Parameters['TargetNode'].ParameterType | Should -Be ([string])
        }

        # --- Online (switch, optional) ---
        It 'Should have Online switch parameter' {
            $script:Cmd.Parameters.ContainsKey('Online') | Should -BeTrue
            $script:Cmd.Parameters['Online'].ParameterType |
                Should -Be ([System.Management.Automation.SwitchParameter])
        }

        It 'Online should not be Mandatory' {
            $isMandatory = $script:Cmd.Parameters['Online'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }

        # --- Wait (switch, optional) ---
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

        # --- Session (inherited) ---
        It 'Should have Session parameter (inherited from PveCmdletBase)' {
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support ShouldProcess (WhatIf parameter exists)' {
            $cmd = Get-Command 'Move-PveVm'
            $cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should support Confirm parameter' {
            $cmd = Get-Command 'Move-PveVm'
            $cmd.Parameters.ContainsKey('Confirm') | Should -BeTrue
        }

        It 'Should not throw with -WhatIf (no session required)' {
            { Move-PveVm -Node 'pve-node1' -VmId 100 -TargetNode 'pve-node2' -WhatIf -ErrorAction Stop } |
                Should -Not -Throw
        }

        It 'Should not throw with -WhatIf and -Online flag' {
            { Move-PveVm -Node 'pve-node1' -VmId 100 -TargetNode 'pve-node2' -Online -WhatIf -ErrorAction Stop } |
                Should -Not -Throw
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            { Move-PveVm -Node 'pve-node1' -VmId 100 -TargetNode 'pve-node2' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }

        It 'Should throw PveNotConnectedException type' {
            try {
                Move-PveVm -Node 'pve-node1' -VmId 100 -TargetNode 'pve-node2' -ErrorAction Stop
            }
            catch {
                $_.Exception.GetType().Name | Should -Match 'PveNotConnectedException|CmdletInvocationException'
            }
        }
    }
}
