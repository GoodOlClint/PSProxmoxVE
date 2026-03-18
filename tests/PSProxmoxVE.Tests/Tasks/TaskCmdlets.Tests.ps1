#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for Get-PveTask and Wait-PveTask.
    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Get-PveTask
# ---------------------------------------------------------------------------
Describe 'Get-PveTask' {

    Context 'Command existence' {
        It 'Should be available after module import' {
            Get-Command 'Get-PveTask' -ErrorAction SilentlyContinue |
                Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            (Get-Command 'Get-PveTask').CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter validation' {
        BeforeAll {
            $script:Cmd = Get-Command 'Get-PveTask'
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

        It 'Should have Upid parameter' {
            $script:Cmd.Parameters.ContainsKey('Upid') | Should -BeTrue
        }

        It 'Upid should be Mandatory' {
            $p = $script:Cmd.Parameters['Upid']
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Upid should be at Position 1' {
            $p = $script:Cmd.Parameters['Upid']
            $pos = $p.ParameterSets.Values | ForEach-Object { $_.Position }
            $pos | Should -Contain 1
        }

        It 'Upid should be of type String' {
            $script:Cmd.Parameters['Upid'].ParameterType | Should -Be ([string])
        }

        It 'Upid should accept pipeline input by property name' {
            $p = $script:Cmd.Parameters['Upid']
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

        It 'Should declare PveTask as OutputType' {
            $outputTypes = $script:Cmd.OutputType.Type
            $outputTypes.Name | Should -Contain 'PveTask'
        }

        It 'Both Node and Upid should be required together' {
            $mandatoryParams = @('Node', 'Upid')
            foreach ($paramName in $mandatoryParams) {
                $p = $script:Cmd.Parameters[$paramName]
                $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
                $isMandatory | Should -Not -BeNullOrEmpty -Because "$paramName should be mandatory"
            }
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active and no -Session is supplied' {
            { Get-PveTask -Node 'pve1' -Upid 'UPID:pve1:00001234:0A1B2C3D:12345678:qmcreate:100:user@pam:' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }

        It 'Should throw PveNotConnectedException type' {
            try {
                Get-PveTask -Node 'pve1' -Upid 'UPID:pve1:00001234:0A1B2C3D:12345678:qmcreate:100:user@pam:' -ErrorAction Stop
            }
            catch {
                $_.Exception.GetType().Name | Should -Match 'PveNotConnectedException|CmdletInvocationException'
            }
        }

        It 'Should require the Node parameter' {
            { Get-PveTask -Upid 'UPID:pve1:00001234:0A1B2C3D:12345678:qmcreate:100:user@pam:' -ErrorAction Stop } |
                Should -Throw '*Node*'
        }

        It 'Should require the Upid parameter' {
            { Get-PveTask -Node 'pve1' -ErrorAction Stop } |
                Should -Throw '*Upid*'
        }
    }
}

# ---------------------------------------------------------------------------
# Wait-PveTask
# ---------------------------------------------------------------------------
Describe 'Wait-PveTask' {

    Context 'Command existence' {
        It 'Should be available after module import' {
            Get-Command 'Wait-PveTask' -ErrorAction SilentlyContinue |
                Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            (Get-Command 'Wait-PveTask').CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter validation' {
        BeforeAll {
            $script:Cmd = Get-Command 'Wait-PveTask'
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

        It 'Should have Upid parameter' {
            $script:Cmd.Parameters.ContainsKey('Upid') | Should -BeTrue
        }

        It 'Upid should be Mandatory' {
            $p = $script:Cmd.Parameters['Upid']
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Upid should be at Position 1' {
            $p = $script:Cmd.Parameters['Upid']
            $pos = $p.ParameterSets.Values | ForEach-Object { $_.Position }
            $pos | Should -Contain 1
        }

        It 'Upid should be of type String' {
            $script:Cmd.Parameters['Upid'].ParameterType | Should -Be ([string])
        }

        It 'Upid should accept pipeline input by property name' {
            $p = $script:Cmd.Parameters['Upid']
            $acceptsByPropName = $p.ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $acceptsByPropName | Should -Not -BeNullOrEmpty
        }

        It 'Should have Timeout parameter' {
            $script:Cmd.Parameters.ContainsKey('Timeout') | Should -BeTrue
        }

        It 'Timeout should not be Mandatory' {
            $p = $script:Cmd.Parameters['Timeout']
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }

        It 'Timeout should be of type Nullable[TimeSpan]' {
            $script:Cmd.Parameters['Timeout'].ParameterType |
                Should -Be ([System.Nullable[System.TimeSpan]])
        }

        It 'Should have PollInterval parameter' {
            $script:Cmd.Parameters.ContainsKey('PollInterval') | Should -BeTrue
        }

        It 'PollInterval should not be Mandatory' {
            $p = $script:Cmd.Parameters['PollInterval']
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }

        It 'PollInterval should be of type Nullable[TimeSpan]' {
            $script:Cmd.Parameters['PollInterval'].ParameterType |
                Should -Be ([System.Nullable[System.TimeSpan]])
        }

        It 'Should have Session parameter (inherited from PveCmdletBase)' {
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }

        It 'Session should not be Mandatory' {
            $p = $script:Cmd.Parameters['Session']
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }

        It 'Should declare PveTask as OutputType' {
            $outputTypes = $script:Cmd.OutputType.Type
            $outputTypes.Name | Should -Contain 'PveTask'
        }

        It 'Both Node and Upid should be required, Timeout and PollInterval optional' {
            $mandatoryParams = @('Node', 'Upid')
            foreach ($paramName in $mandatoryParams) {
                $p = $script:Cmd.Parameters[$paramName]
                $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
                $isMandatory | Should -Not -BeNullOrEmpty -Because "$paramName should be mandatory"
            }

            $optionalParams = @('Timeout', 'PollInterval', 'Session')
            foreach ($paramName in $optionalParams) {
                $p = $script:Cmd.Parameters[$paramName]
                $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
                $isMandatory | Should -BeNullOrEmpty -Because "$paramName should be optional"
            }
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active and no -Session is supplied' {
            { Wait-PveTask -Node 'pve1' -Upid 'UPID:pve1:00001234:0A1B2C3D:12345678:qmcreate:100:user@pam:' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }

        It 'Should throw PveNotConnectedException type' {
            try {
                Wait-PveTask -Node 'pve1' -Upid 'UPID:pve1:00001234:0A1B2C3D:12345678:qmcreate:100:user@pam:' -ErrorAction Stop
            }
            catch {
                $_.Exception.GetType().Name | Should -Match 'PveNotConnectedException|CmdletInvocationException'
            }
        }

        It 'Should require the Node parameter' {
            { Wait-PveTask -Upid 'UPID:pve1:00001234:0A1B2C3D:12345678:qmcreate:100:user@pam:' -ErrorAction Stop } |
                Should -Throw '*Node*'
        }

        It 'Should require the Upid parameter' {
            { Wait-PveTask -Node 'pve1' -ErrorAction Stop } |
                Should -Throw '*Upid*'
        }
    }
}
