#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for Get-PveVm.
    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    $moduleRoot = Resolve-Path (Join-Path $PSScriptRoot '../../../src/PSProxmoxVE')
    $dllCandidates = @(
        Join-Path $moduleRoot 'bin/Debug/net9.0/PSProxmoxVE.dll'
        Join-Path $moduleRoot 'bin/Release/net9.0/PSProxmoxVE.dll'
        Join-Path $moduleRoot 'bin/Debug/net48/PSProxmoxVE.dll'
        Join-Path $moduleRoot 'bin/Release/net48/PSProxmoxVE.dll'
    )

    $script:ModuleDll = $dllCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

    if ($null -eq $script:ModuleDll) {
        throw "PSProxmoxVE.dll not found. Build the project before running Pester tests."
    }

    Import-Module $script:ModuleDll -Force -ErrorAction Stop
}

Describe 'Get-PveVm' {

    Context 'Command existence' {
        It 'Should be available after module import' {
            Get-Command 'Get-PveVm' -ErrorAction SilentlyContinue |
                Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            (Get-Command 'Get-PveVm').CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter validation' {
        BeforeAll {
            $script:Cmd = Get-Command 'Get-PveVm'
        }

        It 'Should have Node parameter' {
            $script:Cmd.Parameters.ContainsKey('Node') | Should -BeTrue
        }

        It 'Node should not be Mandatory (all-nodes query when omitted)' {
            $node = $script:Cmd.Parameters['Node']
            $isMandatory = $node.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }

        It 'Node should accept pipeline input by property name' {
            $node = $script:Cmd.Parameters['Node']
            $acceptsByPropName = $node.ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $acceptsByPropName | Should -Not -BeNullOrEmpty
        }

        It 'Should have VmId parameter' {
            $script:Cmd.Parameters.ContainsKey('VmId') | Should -BeTrue
        }

        It 'Should have Name parameter' {
            $script:Cmd.Parameters.ContainsKey('Name') | Should -BeTrue
        }

        It 'Should have Status parameter' {
            $script:Cmd.Parameters.ContainsKey('Status') | Should -BeTrue
        }

        It 'Should have Tag parameter' {
            $script:Cmd.Parameters.ContainsKey('Tag') | Should -BeTrue
        }

        It 'Should have TemplatesOnly switch parameter' {
            $script:Cmd.Parameters.ContainsKey('TemplatesOnly') | Should -BeTrue
            $script:Cmd.Parameters['TemplatesOnly'].ParameterType |
                Should -Be ([System.Management.Automation.SwitchParameter])
        }

        It 'Should have Session parameter (inherited from PveCmdletBase)' {
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
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
