#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for role cmdlets:
        New-PveRole, Remove-PveRole.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# New-PveRole
# ---------------------------------------------------------------------------
Describe 'New-PveRole' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveRole' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should support Confirm' {
            $script:Cmd.Parameters.ContainsKey('Confirm') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'RoleId should be at Position 0' {
            $pos = $script:Cmd.Parameters['RoleId'].ParameterSets.Values |
                ForEach-Object { $_.Position }
            $pos | Should -Contain 0
        }
    }

    Context 'Optional parameters' {
        It 'Privileges should be at Position 1' {
            $pos = $script:Cmd.Parameters['Privileges'].ParameterSets.Values |
                ForEach-Object { $_.Position }
            $pos | Should -Contain 1
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { New-PveRole -RoleId 'TestRole' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveRole
# ---------------------------------------------------------------------------
Describe 'Remove-PveRole' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveRole' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should support Confirm' {
            $script:Cmd.Parameters.ContainsKey('Confirm') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'RoleId should be at Position 0' {
            $pos = $script:Cmd.Parameters['RoleId'].ParameterSets.Values |
                ForEach-Object { $_.Position }
            $pos | Should -Contain 0
        }
    }

    Context 'Pipeline support' {
        It 'RoleId should accept pipeline input by property name' {
            $roleId = $script:Cmd.Parameters['RoleId']
            $acceptsByPropName = $roleId.ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $acceptsByPropName | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Remove-PveRole -RoleId 'TestRole' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
