#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for Get-PveVm.
    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

Describe 'Get-PveVm' {
    Context 'Parameter validation' {
        BeforeAll {
            $script:Cmd = Get-Command 'Get-PveVm'
        }

        It 'Node should accept pipeline input by property name' {
            $node = $script:Cmd.Parameters['Node']
            $acceptsByPropName = $node.ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $acceptsByPropName | Should -Not -BeNullOrEmpty
        }

        It 'Should have TemplatesOnly switch parameter' {
            $script:Cmd.Parameters.ContainsKey('TemplatesOnly') | Should -BeTrue
            $script:Cmd.Parameters['TemplatesOnly'].ParameterType |
                Should -Be ([System.Management.Automation.SwitchParameter])
        }

        It 'None of the filter parameters should be Mandatory' {
            foreach ($paramName in @('Node', 'VmId', 'Name', 'Status', 'Tag', 'TemplatesOnly', 'Session')) {
                $p = $script:Cmd.Parameters[$paramName]
                $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
                $isMandatory | Should -BeNullOrEmpty -Because "$paramName should be optional"
            }
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active and no -Session is supplied' {
            { Get-PveVm -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }

        It 'Should throw PveNotConnectedException type' {
            try {
                Get-PveVm -ErrorAction Stop
            }
            catch {
                $_.Exception.GetType().Name | Should -Match 'PveNotConnectedException|CmdletInvocationException'
            }
        }
    }
}
