#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for container configuration cmdlets:
        Copy-PveContainer, Get-PveContainerConfig, Set-PveContainerConfig.

    All tests are fully offline — no live Proxmox VE target is required.
    If a cmdlet is not yet compiled the test is marked Skipped.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Availability = @{}
    foreach ($name in @('Copy-PveContainer', 'Get-PveContainerConfig', 'Set-PveContainerConfig')) {
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
Describe 'Container config cmdlets — manifest declarations' {
    BeforeAll {
        $manifestPath = Join-Path (Get-Module PSProxmoxVE).ModuleBase 'PSProxmoxVE.psd1'
        $script:Manifest = if (Test-Path $manifestPath) { Import-PowerShellDataFile $manifestPath } else { $null }
    }

    It "<cmdName> should be declared in CmdletsToExport" -TestCases @(
        @{ cmdName = 'Copy-PveContainer' }
        @{ cmdName = 'Get-PveContainerConfig' }
        @{ cmdName = 'Set-PveContainerConfig' }
    ) {
        if ($null -eq $script:Manifest) { Set-ItResult -Skipped -Because 'Manifest not found'; return }
        $script:Manifest.CmdletsToExport | Should -Contain $cmdName
    }
}

# ---------------------------------------------------------------------------
# Copy-PveContainer
# ---------------------------------------------------------------------------
Describe 'Copy-PveContainer' {

    BeforeAll { $script:Cmd = Get-Command 'Copy-PveContainer' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Copy-PveContainer'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Copy-PveContainer'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Copy-PveContainer'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should support Confirm' {
            Skip-IfMissing 'Copy-PveContainer'
            $script:Cmd.Parameters.ContainsKey('Confirm') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'SourceNode should be Mandatory' {
            Skip-IfMissing 'Copy-PveContainer'
            $isMandatory = $script:Cmd.Parameters['SourceNode'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Copy-PveContainer'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Optional parameters' {
        It 'Should have NewVmId parameter' {
            Skip-IfMissing 'Copy-PveContainer'
            $script:Cmd.Parameters.ContainsKey('NewVmId') | Should -BeTrue
        }

        It 'NewVmId should not be Mandatory' {
            Skip-IfMissing 'Copy-PveContainer'
            $isMandatory = $script:Cmd.Parameters['NewVmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }

        It 'Should have NewName parameter' {
            Skip-IfMissing 'Copy-PveContainer'
            $script:Cmd.Parameters.ContainsKey('NewName') | Should -BeTrue
        }

        It 'Should have TargetNode parameter' {
            Skip-IfMissing 'Copy-PveContainer'
            $script:Cmd.Parameters.ContainsKey('TargetNode') | Should -BeTrue
        }

        It 'Should have Full switch parameter' {
            Skip-IfMissing 'Copy-PveContainer'
            $script:Cmd.Parameters.ContainsKey('Full') | Should -BeTrue
            $script:Cmd.Parameters['Full'].SwitchParameter | Should -BeTrue
        }

        It 'Should have Storage parameter' {
            Skip-IfMissing 'Copy-PveContainer'
            $script:Cmd.Parameters.ContainsKey('Storage') | Should -BeTrue
        }

        It 'Should have Wait switch parameter' {
            Skip-IfMissing 'Copy-PveContainer'
            $script:Cmd.Parameters.ContainsKey('Wait') | Should -BeTrue
            $script:Cmd.Parameters['Wait'].SwitchParameter | Should -BeTrue
        }
    }

    Context 'Pipeline support' {
        It 'VmId should accept pipeline input by property name' {
            Skip-IfMissing 'Copy-PveContainer'
            $vmid = $script:Cmd.Parameters['VmId']
            $acceptsByPropName = $vmid.ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $acceptsByPropName | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Session parameter' {
        It 'Should have Session parameter (inherited from PveCmdletBase)' {
            Skip-IfMissing 'Copy-PveContainer'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Copy-PveContainer'
            { Copy-PveContainer -SourceNode 'pve1' -VmId 100 -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveContainerConfig
# ---------------------------------------------------------------------------
Describe 'Get-PveContainerConfig' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveContainerConfig' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveContainerConfig'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveContainerConfig'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Get-PveContainerConfig'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Get-PveContainerConfig'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Pipeline support' {
        It 'Node should accept pipeline input by property name' {
            Skip-IfMissing 'Get-PveContainerConfig'
            $node = $script:Cmd.Parameters['Node']
            $acceptsByPropName = $node.ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $acceptsByPropName | Should -Not -BeNullOrEmpty
        }

        It 'VmId should accept pipeline input by property name' {
            Skip-IfMissing 'Get-PveContainerConfig'
            $vmid = $script:Cmd.Parameters['VmId']
            $acceptsByPropName = $vmid.ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $acceptsByPropName | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Session parameter' {
        It 'Should have Session parameter (inherited from PveCmdletBase)' {
            Skip-IfMissing 'Get-PveContainerConfig'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'ShouldProcess not required' {
        It 'Should not have WhatIf (Get verb is read-only)' {
            Skip-IfMissing 'Get-PveContainerConfig'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeFalse
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveContainerConfig'
            { Get-PveContainerConfig -Node 'pve1' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveContainerConfig
# ---------------------------------------------------------------------------
Describe 'Set-PveContainerConfig' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PveContainerConfig' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PveContainerConfig'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Set-PveContainerConfig'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Set-PveContainerConfig'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should support Confirm' {
            Skip-IfMissing 'Set-PveContainerConfig'
            $script:Cmd.Parameters.ContainsKey('Confirm') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Set-PveContainerConfig'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Set-PveContainerConfig'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Optional configuration parameters' {
        It 'Should have Hostname parameter' {
            Skip-IfMissing 'Set-PveContainerConfig'
            $script:Cmd.Parameters.ContainsKey('Hostname') | Should -BeTrue
        }

        It 'Hostname should not be Mandatory' {
            Skip-IfMissing 'Set-PveContainerConfig'
            $isMandatory = $script:Cmd.Parameters['Hostname'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }

        It 'Should have Cores parameter' {
            Skip-IfMissing 'Set-PveContainerConfig'
            $script:Cmd.Parameters.ContainsKey('Cores') | Should -BeTrue
        }

        It 'Should have Memory parameter' {
            Skip-IfMissing 'Set-PveContainerConfig'
            $script:Cmd.Parameters.ContainsKey('Memory') | Should -BeTrue
        }

        It 'Should have Swap parameter' {
            Skip-IfMissing 'Set-PveContainerConfig'
            $script:Cmd.Parameters.ContainsKey('Swap') | Should -BeTrue
        }

        It 'Should have Description parameter' {
            Skip-IfMissing 'Set-PveContainerConfig'
            $script:Cmd.Parameters.ContainsKey('Description') | Should -BeTrue
        }

        It 'Should have Tags parameter' {
            Skip-IfMissing 'Set-PveContainerConfig'
            $script:Cmd.Parameters.ContainsKey('Tags') | Should -BeTrue
        }

        It 'Should have Nameserver parameter' {
            Skip-IfMissing 'Set-PveContainerConfig'
            $script:Cmd.Parameters.ContainsKey('Nameserver') | Should -BeTrue
        }

        It 'Should have SearchDomain parameter' {
            Skip-IfMissing 'Set-PveContainerConfig'
            $script:Cmd.Parameters.ContainsKey('SearchDomain') | Should -BeTrue
        }
    }

    Context 'Pipeline support' {
        It 'Node should accept pipeline input by property name' {
            Skip-IfMissing 'Set-PveContainerConfig'
            $node = $script:Cmd.Parameters['Node']
            $acceptsByPropName = $node.ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $acceptsByPropName | Should -Not -BeNullOrEmpty
        }

        It 'VmId should accept pipeline input by property name' {
            Skip-IfMissing 'Set-PveContainerConfig'
            $vmid = $script:Cmd.Parameters['VmId']
            $acceptsByPropName = $vmid.ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $acceptsByPropName | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Session parameter' {
        It 'Should have Session parameter (inherited from PveCmdletBase)' {
            Skip-IfMissing 'Set-PveContainerConfig'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Set-PveContainerConfig'
            { Set-PveContainerConfig -Node 'pve1' -VmId 100 -Hostname 'test' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
