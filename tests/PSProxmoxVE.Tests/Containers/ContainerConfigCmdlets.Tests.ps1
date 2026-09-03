#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for container configuration cmdlets:
        Copy-PveContainer, Get-PveContainerConfig, Set-PveContainerConfig.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Copy-PveContainer
# ---------------------------------------------------------------------------
Describe 'Copy-PveContainer' {
    BeforeAll { $script:Cmd = Get-Command 'Copy-PveContainer' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should support Confirm' {
            $script:Cmd.Parameters.ContainsKey('Confirm') | Should -BeTrue
        }
    }

    Context 'Optional parameters' {
        It 'Should have Full switch parameter' {
            $script:Cmd.Parameters.ContainsKey('Full') | Should -BeTrue
            $script:Cmd.Parameters['Full'].SwitchParameter | Should -BeTrue
        }

        It 'Should have Wait switch parameter' {
            $script:Cmd.Parameters.ContainsKey('Wait') | Should -BeTrue
            $script:Cmd.Parameters['Wait'].SwitchParameter | Should -BeTrue
        }
    }

    Context 'Pipeline support' {
        It 'VmId should accept pipeline input by property name' {
            $vmid = $script:Cmd.Parameters['VmId']
            $acceptsByPropName = $vmid.ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $acceptsByPropName | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Copy-PveContainer -SourceNode 'pve1' -VmId 100 -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveContainerConfig
# ---------------------------------------------------------------------------
Describe 'Get-PveContainerConfig' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveContainerConfig' }

    Context 'Pipeline support' {
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

    Context 'ShouldProcess not required' {
        It 'Should not have WhatIf (Get verb is read-only)' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeFalse
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveContainerConfig -Node 'pve1' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveContainerConfig
# ---------------------------------------------------------------------------
Describe 'Set-PveContainerConfig' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PveContainerConfig' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should support Confirm' {
            $script:Cmd.Parameters.ContainsKey('Confirm') | Should -BeTrue
        }
    }

    Context 'Pipeline support' {
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
        It 'Should throw when no session is active' {
            { Set-PveContainerConfig -Node 'pve1' -VmId 100 -Hostname 'test' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
