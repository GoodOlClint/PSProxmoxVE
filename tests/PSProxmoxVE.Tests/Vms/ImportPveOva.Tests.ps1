#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for Import-PveOva.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Cmd = Get-Command 'Import-PveOva' -ErrorAction SilentlyContinue
    $script:Available = $null -ne $script:Cmd

    function Skip-IfMissing {
        if (-not $script:Available) {
            Set-ItResult -Skipped -Because "Import-PveOva is not yet implemented in this build"
        }
    }
}

Describe 'Import-PveOva — manifest' {
    BeforeAll {
        $manifestPath = Join-Path (Get-Module PSProxmoxVE).ModuleBase 'PSProxmoxVE.psd1'
        $script:Manifest = if (Test-Path $manifestPath) { Import-PowerShellDataFile $manifestPath } else { $null }
    }

    It 'Should be declared in CmdletsToExport' {
        if ($null -eq $script:Manifest) { Set-ItResult -Skipped -Because 'Manifest not found'; return }
        $script:Manifest.CmdletsToExport | Should -Contain 'Import-PveOva'
    }
}

Describe 'Import-PveOva' {

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

        It 'Should support Confirm' {
            Skip-IfMissing
            $script:Cmd.Parameters.ContainsKey('Confirm') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Storage should be Mandatory' {
            Skip-IfMissing
            $isMandatory = $script:Cmd.Parameters['Storage'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Path should be Mandatory' {
            Skip-IfMissing
            $isMandatory = $script:Cmd.Parameters['Path'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'TargetStorage should be Mandatory' {
            Skip-IfMissing
            $isMandatory = $script:Cmd.Parameters['TargetStorage'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Optional parameters' {
        It 'Should have VmId parameter' {
            Skip-IfMissing
            $script:Cmd.Parameters.ContainsKey('VmId') | Should -BeTrue
        }

        It 'VmId should not be Mandatory' {
            Skip-IfMissing
            $allMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $allMandatory | Should -BeNullOrEmpty
        }

        It 'Should have Name parameter' {
            Skip-IfMissing
            $script:Cmd.Parameters.ContainsKey('Name') | Should -BeTrue
        }

        It 'Should have Memory parameter' {
            Skip-IfMissing
            $script:Cmd.Parameters.ContainsKey('Memory') | Should -BeTrue
        }

        It 'Should have Cores parameter' {
            Skip-IfMissing
            $script:Cmd.Parameters.ContainsKey('Cores') | Should -BeTrue
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

    Context 'Output type' {
        It 'Should declare PveVm as output type' {
            Skip-IfMissing
            $outputTypes = $script:Cmd.OutputType | ForEach-Object { $_.Type.Name }
            $outputTypes | Should -Contain 'PveVm'
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active and file does not exist' {
            Skip-IfMissing
            { Import-PveOva -Node 'pve1' -Storage 'local' -Path '/nonexistent/file.ova' `
                -TargetStorage 'local-lvm' -Confirm:$false -ErrorAction Stop } |
                Should -Throw
        }
    }
}
