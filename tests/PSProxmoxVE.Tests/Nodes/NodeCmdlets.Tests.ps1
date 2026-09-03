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
    Context 'Parameter validation' {
        BeforeAll {
            $script:Cmd = Get-Command 'Get-PveNode'
        }

        It 'Name should be at Position 0' {
            $p = $script:Cmd.Parameters['Name']
            $pos = $p.ParameterSets.Values | ForEach-Object { $_.Position }
            $pos | Should -Contain 0
        }

        It 'Name should be of type String' {
            $script:Cmd.Parameters['Name'].ParameterType | Should -Be ([string])
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
    Context 'Parameter validation' {
        BeforeAll {
            $script:Cmd = Get-Command 'Get-PveNodeStatus'
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
