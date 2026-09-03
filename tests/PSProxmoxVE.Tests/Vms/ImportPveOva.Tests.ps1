#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for Import-PveOva.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Cmd = Get-Command 'Import-PveOva'
}

Describe 'Import-PveOva' {
    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should support Confirm' {
            $script:Cmd.Parameters.ContainsKey('Confirm') | Should -BeTrue
        }
    }

    Context 'Optional parameters' {
        It 'VmId should not be Mandatory' {
            $allMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $allMandatory | Should -BeNullOrEmpty
        }
    }

    Context 'Output type' {
        It 'Should declare PveVm as output type' {
            $outputTypes = $script:Cmd.OutputType | ForEach-Object { $_.Type.Name }
            $outputTypes | Should -Contain 'PveVm'
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active and file does not exist' {
            { Import-PveOva -Node 'pve1' -Storage 'local' -Path '/nonexistent/file.ova' `
                -TargetStorage 'local-lvm' -Confirm:$false -ErrorAction Stop } |
                Should -Throw
        }
    }
}
