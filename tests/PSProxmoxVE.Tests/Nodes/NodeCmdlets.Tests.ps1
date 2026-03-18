#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for Get-PveNode and Get-PveNodeStatus.
    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Get-PveNode
# ---------------------------------------------------------------------------
Describe 'Get-PveNode' {

    Context 'Command existence' {
        It 'Should be available after module import' {
            Get-Command 'Get-PveNode' -ErrorAction SilentlyContinue |
                Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            (Get-Command 'Get-PveNode').CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter validation' {
        BeforeAll {
            $script:Cmd = Get-Command 'Get-PveNode'
        }

        It 'Should have Name parameter' {
            $script:Cmd.Parameters.ContainsKey('Name') | Should -BeTrue
        }

        It 'Name should not be Mandatory' {
            $p = $script:Cmd.Parameters['Name']
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }

        It 'Name should be at Position 0' {
            $p = $script:Cmd.Parameters['Name']
            $pos = $p.ParameterSets.Values | ForEach-Object { $_.Position }
            $pos | Should -Contain 0
        }

        It 'Name should be of type String' {
            $script:Cmd.Parameters['Name'].ParameterType | Should -Be ([string])
        }

        It 'Should have Session parameter (inherited from PveCmdletBase)' {
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }

        It 'Session should not be Mandatory' {
            $p = $script:Cmd.Parameters['Session']
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }

        It 'Should declare PveNode as OutputType' {
            $outputTypes = $script:Cmd.OutputType.Type
            $outputTypes.Name | Should -Contain 'PveNode'
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active and no -Session is supplied' {
            { Get-PveNode -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }

        It 'Should throw PveNotConnectedException type' {
            try {
                Get-PveNode -ErrorAction Stop
            }
            catch {
                $_.Exception.GetType().Name | Should -Match 'PveNotConnectedException|CmdletInvocationException'
            }
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveNodeStatus
# ---------------------------------------------------------------------------
Describe 'Get-PveNodeStatus' {

    Context 'Command existence' {
        It 'Should be available after module import' {
            Get-Command 'Get-PveNodeStatus' -ErrorAction SilentlyContinue |
                Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            (Get-Command 'Get-PveNodeStatus').CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter validation' {
        BeforeAll {
            $script:Cmd = Get-Command 'Get-PveNodeStatus'
        }

        It 'Should have Node parameter' {
            $script:Cmd.Parameters.ContainsKey('Node') | Should -BeTrue
        }

        It 'Node should be Mandatory' {
            $p = $script:Cmd.Parameters['Node']
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Node should be at Position 0' {
            $p = $script:Cmd.Parameters['Node']
            $pos = $p.ParameterSets.Values | ForEach-Object { $_.Position }
            $pos | Should -Contain 0
        }

        It 'Node should be of type String' {
            $script:Cmd.Parameters['Node'].ParameterType | Should -Be ([string])
        }

        It 'Node should accept pipeline input by property name' {
            $p = $script:Cmd.Parameters['Node']
            $acceptsByPropName = $p.ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $acceptsByPropName | Should -Not -BeNullOrEmpty
        }

        It 'Should have Session parameter (inherited from PveCmdletBase)' {
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }

        It 'Session should not be Mandatory' {
            $p = $script:Cmd.Parameters['Session']
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }

        It 'Should declare PveNodeStatus as OutputType' {
            $outputTypes = $script:Cmd.OutputType.Type
            $outputTypes.Name | Should -Contain 'PveNodeStatus'
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active and no -Session is supplied' {
            { Get-PveNodeStatus -Node 'pve1' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }

        It 'Should throw PveNotConnectedException type' {
            try {
                Get-PveNodeStatus -Node 'pve1' -ErrorAction Stop
            }
            catch {
                $_.Exception.GetType().Name | Should -Match 'PveNotConnectedException|CmdletInvocationException'
            }
        }

        It 'Should require the Node parameter' {
            { Get-PveNodeStatus -ErrorAction Stop } |
                Should -Throw '*Node*'
        }
    }
}
