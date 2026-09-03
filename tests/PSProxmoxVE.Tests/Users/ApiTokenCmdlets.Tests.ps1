#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for API token cmdlets:
        Get-PveApiToken, New-PveApiToken, Remove-PveApiToken.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $allNames = @('Get-PveApiToken', 'New-PveApiToken', 'Remove-PveApiToken')
}

# ---------------------------------------------------------------------------
# Get-PveApiToken
# ---------------------------------------------------------------------------
Describe 'Get-PveApiToken' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveApiToken' }

    Context 'Parameter metadata' {
        It 'UserId should accept pipeline input by property name' {
            $byPropName = $script:Cmd.Parameters['UserId'].ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $byPropName | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveApiToken -UserId 'admin@pam' -ErrorAction Stop } | Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveApiToken
# ---------------------------------------------------------------------------
Describe 'New-PveApiToken' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveApiToken' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'WhatIf should not throw even without a session' {
            { New-PveApiToken -UserId 'admin@pam' -TokenId 'test' -WhatIf } | Should -Not -Throw
        }
    }

    Context 'Required parameters' {
        It 'UserId should accept pipeline input by property name' {
            $byPropName = $script:Cmd.Parameters['UserId'].ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $byPropName | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { New-PveApiToken -UserId 'admin@pam' -TokenId 'test' -Confirm:$false -ErrorAction Stop } | Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveApiToken
# ---------------------------------------------------------------------------
Describe 'Remove-PveApiToken' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveApiToken' }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should declare ConfirmImpact High' {
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Required parameters' {
        It 'UserId should accept pipeline input by property name' {
            $byPropName = $script:Cmd.Parameters['UserId'].ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $byPropName | Should -Not -BeNullOrEmpty
        }

        It 'TokenId should accept pipeline input by property name' {
            $byPropName = $script:Cmd.Parameters['TokenId'].ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $byPropName | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Remove-PveApiToken -UserId 'admin@pam' -TokenId 'test' -Confirm:$false -ErrorAction Stop } | Should -Throw '*No active Proxmox VE session*'
        }
    }
}
