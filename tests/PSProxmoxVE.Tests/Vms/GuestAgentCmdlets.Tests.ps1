#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for QEMU guest agent cmdlets:
        Test-PveVmGuestAgent, Get-PveVmGuestNetwork, Invoke-PveVmGuestExec.

    All tests are fully offline — no live Proxmox VE target is required.
    If a cmdlet is not yet compiled the test is marked Skipped.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Availability = @{}
    foreach ($name in @('Test-PveVmGuestAgent', 'Get-PveVmGuestNetwork', 'Invoke-PveVmGuestExec')) {
        $script:Availability[$name] = $null -ne (Get-Command $name -ErrorAction SilentlyContinue)
    }

    function Skip-IfMissing([string]$Name) {
        if (-not $script:Availability[$Name]) {
            Set-ItResult -Skipped -Because "$Name is not yet implemented in this build"
        }
    }
}

# ---------------------------------------------------------------------------
# Manifest contract
# ---------------------------------------------------------------------------
Describe 'Guest agent cmdlets — manifest declarations' {
    BeforeAll {
        $manifestPath = Join-Path (Get-Module PSProxmoxVE).ModuleBase 'PSProxmoxVE.psd1'
        $script:Manifest = if (Test-Path $manifestPath) { Import-PowerShellDataFile $manifestPath } else { $null }
    }

    It "<cmdName> should be declared in CmdletsToExport" -TestCases @(
        @{ cmdName = 'Test-PveVmGuestAgent' }
        @{ cmdName = 'Get-PveVmGuestNetwork' }
        @{ cmdName = 'Invoke-PveVmGuestExec' }
    ) {
        if ($null -eq $script:Manifest) { Set-ItResult -Skipped -Because 'Manifest not found'; return }
        $script:Manifest.CmdletsToExport | Should -Contain $cmdName
    }
}

# ---------------------------------------------------------------------------
# Test-PveVmGuestAgent
# ---------------------------------------------------------------------------
Describe 'Test-PveVmGuestAgent' {

    BeforeAll { $script:Cmd = Get-Command 'Test-PveVmGuestAgent' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Test-PveVmGuestAgent'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Test-PveVmGuestAgent'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Test-PveVmGuestAgent'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Test-PveVmGuestAgent'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should accept pipeline input by property name' {
            Skip-IfMissing 'Test-PveVmGuestAgent'
            $attr = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $attr | Should -Not -BeNullOrEmpty
        }

        It 'Should output bool' {
            Skip-IfMissing 'Test-PveVmGuestAgent'
            $outputType = $script:Cmd.OutputType | Select-Object -First 1
            $outputType.Type | Should -Be ([bool])
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Test-PveVmGuestAgent'
            { Test-PveVmGuestAgent -Node 'pve1' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveVmGuestNetwork
# ---------------------------------------------------------------------------
Describe 'Get-PveVmGuestNetwork' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveVmGuestNetwork' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveVmGuestNetwork'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveVmGuestNetwork'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Get-PveVmGuestNetwork'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Get-PveVmGuestNetwork'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should accept pipeline input by property name' {
            Skip-IfMissing 'Get-PveVmGuestNetwork'
            $attr = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $attr | Should -Not -BeNullOrEmpty
        }

        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveVmGuestNetwork'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveVmGuestNetwork'
            { Get-PveVmGuestNetwork -Node 'pve1' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Invoke-PveVmGuestExec
# ---------------------------------------------------------------------------
Describe 'Invoke-PveVmGuestExec' {

    BeforeAll { $script:Cmd = Get-Command 'Invoke-PveVmGuestExec' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Invoke-PveVmGuestExec'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Invoke-PveVmGuestExec'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Invoke-PveVmGuestExec'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Invoke-PveVmGuestExec'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Invoke-PveVmGuestExec'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Command should be Mandatory' {
            Skip-IfMissing 'Invoke-PveVmGuestExec'
            $isMandatory = $script:Cmd.Parameters['Command'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Invoke-PveVmGuestExec'
            { Invoke-PveVmGuestExec -Node 'pve1' -VmId 100 -Command 'hostname' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
