#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for Remove-PveVm.
    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

Describe 'Remove-PveVm' {

    Context 'Command existence' {
        It 'Should be available after module import' {
            Get-Command 'Remove-PveVm' -ErrorAction SilentlyContinue |
                Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            (Get-Command 'Remove-PveVm').CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        BeforeAll {
            $script:Cmd = Get-Command 'Remove-PveVm'
        }

        It 'Should support ShouldProcess (WhatIf present)' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should support ShouldProcess (Confirm present)' {
            $script:Cmd.Parameters.ContainsKey('Confirm') | Should -BeTrue
        }

        It 'Should declare ConfirmImpact High (verified via CmdletAttribute)' {
            # Retrieve the CmdletAttribute from the underlying type to confirm ConfirmImpact.
            $cmdletType = $script:Cmd.ImplementingType
            $attr = $cmdletType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }

        It 'Should accept -WhatIf without throwing even when no session exists' {
            { Remove-PveVm -Node 'pve-node1' -VmId 100 -WhatIf -ErrorAction Stop } |
                Should -Not -Throw
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            $nodeParam = (Get-Command 'Remove-PveVm').Parameters['Node']
            $isMandatory = $nodeParam.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            $vmidParam = (Get-Command 'Remove-PveVm').Parameters['VmId']
            $isMandatory = $vmidParam.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Optional switch parameters' {
        BeforeAll {
            $script:Cmd = Get-Command 'Remove-PveVm'
        }

        It 'Should have Purge switch parameter' {
            $script:Cmd.Parameters.ContainsKey('Purge') | Should -BeTrue
            $script:Cmd.Parameters['Purge'].ParameterType |
                Should -Be ([System.Management.Automation.SwitchParameter])
        }

        It 'Should have Force switch parameter' {
            $script:Cmd.Parameters.ContainsKey('Force') | Should -BeTrue
            $script:Cmd.Parameters['Force'].ParameterType |
                Should -Be ([System.Management.Automation.SwitchParameter])
        }

        It 'Should have Wait switch parameter' {
            $script:Cmd.Parameters.ContainsKey('Wait') | Should -BeTrue
            $script:Cmd.Parameters['Wait'].ParameterType |
                Should -Be ([System.Management.Automation.SwitchParameter])
        }
    }

    Context 'Pipeline input support' {
        BeforeAll {
            $script:Cmd = Get-Command 'Remove-PveVm'
        }

        It 'Node should accept pipeline input by property name' {
            $node = $script:Cmd.Parameters['Node']
            $acceptsByPropName = $node.ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $acceptsByPropName | Should -Not -BeNullOrEmpty
        }

        It 'VmId should accept pipeline input by property name' {
            $vmid = $script:Cmd.Parameters['VmId']
            $acceptsByPropName = $vmid.ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $acceptsByPropName | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            { Remove-PveVm -Node 'pve-node1' -VmId 100 -Confirm:$false -ErrorAction Stop } | Should -Throw '*No active Proxmox VE session*'
        }
    }
}
