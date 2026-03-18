#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for role cmdlets:
        New-PveRole, Remove-PveRole.

    All tests are fully offline — no live Proxmox VE target is required.
    If a cmdlet is not yet compiled the test is marked Skipped.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Availability = @{}
    foreach ($name in @('New-PveRole', 'Remove-PveRole')) {
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
Describe 'Role cmdlets — manifest declarations' {
    BeforeAll {
        $manifestPath = Join-Path (Get-Module PSProxmoxVE).ModuleBase 'PSProxmoxVE.psd1'
        $script:Manifest = if (Test-Path $manifestPath) { Import-PowerShellDataFile $manifestPath } else { $null }
    }

    It "<cmdName> should be declared in CmdletsToExport" -TestCases @(
        @{ cmdName = 'New-PveRole' }
        @{ cmdName = 'Remove-PveRole' }
    ) {
        if ($null -eq $script:Manifest) { Set-ItResult -Skipped -Because 'Manifest not found'; return }
        $script:Manifest.CmdletsToExport | Should -Contain $cmdName
    }
}

# ---------------------------------------------------------------------------
# New-PveRole
# ---------------------------------------------------------------------------
Describe 'New-PveRole' {

    BeforeAll { $script:Cmd = Get-Command 'New-PveRole' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveRole'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'New-PveRole'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'New-PveRole'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should support Confirm' {
            Skip-IfMissing 'New-PveRole'
            $script:Cmd.Parameters.ContainsKey('Confirm') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'RoleId should be Mandatory' {
            Skip-IfMissing 'New-PveRole'
            $isMandatory = $script:Cmd.Parameters['RoleId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'RoleId should be at Position 0' {
            Skip-IfMissing 'New-PveRole'
            $pos = $script:Cmd.Parameters['RoleId'].ParameterSets.Values |
                ForEach-Object { $_.Position }
            $pos | Should -Contain 0
        }
    }

    Context 'Optional parameters' {
        It 'Should have Privileges parameter' {
            Skip-IfMissing 'New-PveRole'
            $script:Cmd.Parameters.ContainsKey('Privileges') | Should -BeTrue
        }

        It 'Privileges should not be Mandatory' {
            Skip-IfMissing 'New-PveRole'
            $isMandatory = $script:Cmd.Parameters['Privileges'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }

        It 'Privileges should be at Position 1' {
            Skip-IfMissing 'New-PveRole'
            $pos = $script:Cmd.Parameters['Privileges'].ParameterSets.Values |
                ForEach-Object { $_.Position }
            $pos | Should -Contain 1
        }
    }

    Context 'Session parameter' {
        It 'Should have Session parameter (inherited from PveCmdletBase)' {
            Skip-IfMissing 'New-PveRole'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'New-PveRole'
            { New-PveRole -RoleId 'TestRole' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveRole
# ---------------------------------------------------------------------------
Describe 'Remove-PveRole' {

    BeforeAll { $script:Cmd = Get-Command 'Remove-PveRole' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Remove-PveRole'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Remove-PveRole'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Remove-PveRole'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should support Confirm' {
            Skip-IfMissing 'Remove-PveRole'
            $script:Cmd.Parameters.ContainsKey('Confirm') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'RoleId should be Mandatory' {
            Skip-IfMissing 'Remove-PveRole'
            $isMandatory = $script:Cmd.Parameters['RoleId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'RoleId should be at Position 0' {
            Skip-IfMissing 'Remove-PveRole'
            $pos = $script:Cmd.Parameters['RoleId'].ParameterSets.Values |
                ForEach-Object { $_.Position }
            $pos | Should -Contain 0
        }
    }

    Context 'Pipeline support' {
        It 'RoleId should accept pipeline input by property name' {
            Skip-IfMissing 'Remove-PveRole'
            $roleId = $script:Cmd.Parameters['RoleId']
            $acceptsByPropName = $roleId.ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $acceptsByPropName | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Session parameter' {
        It 'Should have Session parameter (inherited from PveCmdletBase)' {
            Skip-IfMissing 'Remove-PveRole'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Remove-PveRole'
            { Remove-PveRole -RoleId 'TestRole' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
