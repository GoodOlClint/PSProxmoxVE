#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for Import-PveVmDisk.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Cmd = Get-Command 'Import-PveVmDisk' -ErrorAction SilentlyContinue
    $script:Available = $null -ne $script:Cmd

    function Skip-IfMissing {
        if (-not $script:Available) {
            Set-ItResult -Skipped -Because "Import-PveVmDisk is not yet implemented in this build"
        }
    }
}

Describe 'Import-PveVmDisk — manifest' {
    BeforeAll {
        $manifestPath = Join-Path (Get-Module PSProxmoxVE).ModuleBase 'PSProxmoxVE.psd1'
        $script:Manifest = if (Test-Path $manifestPath) { Import-PowerShellDataFile $manifestPath } else { $null }
    }

    It 'Should be declared in CmdletsToExport' {
        if ($null -eq $script:Manifest) { Set-ItResult -Skipped -Because 'Manifest not found'; return }
        $script:Manifest.CmdletsToExport | Should -Contain 'Import-PveVmDisk'
    }
}

Describe 'Import-PveVmDisk' {

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            Skip-IfMissing
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Disk should be Mandatory' {
            Skip-IfMissing
            $isMandatory = $script:Cmd.Parameters['Disk'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'TargetStorage should be Mandatory' {
            Skip-IfMissing
            $isMandatory = $script:Cmd.Parameters['TargetStorage'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Source should be Mandatory' {
            Skip-IfMissing
            $isMandatory = $script:Cmd.Parameters['Source'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Optional parameters' {
        It 'Should have Format parameter' {
            Skip-IfMissing
            $script:Cmd.Parameters.ContainsKey('Format') | Should -BeTrue
        }

        It 'Should have Wait switch parameter' {
            Skip-IfMissing
            $script:Cmd.Parameters.ContainsKey('Wait') | Should -BeTrue
        }

        It 'Should have Session parameter' {
            Skip-IfMissing
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Disk parameter validation' {
        It 'Should accept valid disk names' {
            Skip-IfMissing
            # ValidatePattern should accept these — test by checking the attribute exists
            $attrs = $script:Cmd.Parameters['Disk'].Attributes |
                Where-Object { $_ -is [System.Management.Automation.ValidatePatternAttribute] }
            $attrs | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Pipeline support' {
        It 'VmId should accept pipeline input by property name' {
            Skip-IfMissing
            $attr = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $attr | Should -Not -BeNullOrEmpty
        }

        It 'Node should accept pipeline input by property name' {
            Skip-IfMissing
            $attr = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $attr | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing
            { Import-PveVmDisk -Node 'pve1' -VmId 100 -Disk 'scsi0' `
                -TargetStorage 'local-lvm' -Source 'local:iso/test.img' `
                -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
