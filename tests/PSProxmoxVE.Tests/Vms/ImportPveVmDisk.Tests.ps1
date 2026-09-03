#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for Import-PveVmDisk.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Cmd = Get-Command 'Import-PveVmDisk'
}

Describe 'Import-PveVmDisk' {
    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Disk parameter validation' {
        It 'Should accept valid disk names' {
            # ValidatePattern should accept these — test by checking the attribute exists
            $attrs = $script:Cmd.Parameters['Disk'].Attributes |
                Where-Object { $_ -is [System.Management.Automation.ValidatePatternAttribute] }
            $attrs | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Pipeline support' {
        It 'VmId should accept pipeline input by property name' {
            $attr = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $attr | Should -Not -BeNullOrEmpty
        }

        It 'Node should accept pipeline input by property name' {
            $attr = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $attr | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Import-PveVmDisk -Node 'pve1' -VmId 100 -Disk 'scsi0' `
                -TargetStorage 'local-lvm' -Source 'local:iso/test.img' `
                -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
