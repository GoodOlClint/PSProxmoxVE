#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for user, role, and permission cmdlets:
        Get-PveUser, New-PveUser, Remove-PveUser, Set-PveUser,
        Get-PveRole, New-PveRole, Remove-PveRole,
        Get-PvePermission, Set-PvePermission.

    All tests are fully offline — no live Proxmox VE target is required.
    If a cmdlet is not yet compiled the test is marked Skipped.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $allNames = @(
        'Get-PveUser', 'New-PveUser', 'Remove-PveUser', 'Set-PveUser',
        'Get-PveRole', 'New-PveRole', 'Remove-PveRole',
        'Get-PvePermission', 'Set-PvePermission'
    )

    $script:Availability = @{}
    foreach ($name in $allNames) {
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
Describe 'User / Role / Permission cmdlets — manifest declarations' {
    BeforeAll {
        $manifestPath = Join-Path (Get-Module PSProxmoxVE).ModuleBase 'PSProxmoxVE.psd1'
        $script:Manifest = if (Test-Path $manifestPath) { Import-PowerShellDataFile $manifestPath } else { $null }
    }

    It "<cmdName> should be declared in CmdletsToExport" -TestCases @(
        @{ cmdName = 'Get-PveUser' }
        @{ cmdName = 'New-PveUser' }
        @{ cmdName = 'Remove-PveUser' }
        @{ cmdName = 'Set-PveUser' }
        @{ cmdName = 'Get-PveRole' }
        @{ cmdName = 'New-PveRole' }
        @{ cmdName = 'Remove-PveRole' }
        @{ cmdName = 'Get-PvePermission' }
        @{ cmdName = 'Set-PvePermission' }
    ) {
        if ($null -eq $script:Manifest) { Set-ItResult -Skipped -Because 'Manifest not found'; return }
        $script:Manifest.CmdletsToExport | Should -Contain $cmdName
    }
}

# ---------------------------------------------------------------------------
# Get-PveUser
# ---------------------------------------------------------------------------
Describe 'Get-PveUser' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveUser' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveUser'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveUser'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have UserId parameter (optional filter)' {
            Skip-IfMissing 'Get-PveUser'
            $script:Cmd.Parameters.ContainsKey('UserId') | Should -BeTrue
        }

        It 'UserId should not be Mandatory' {
            Skip-IfMissing 'Get-PveUser'
            $isMandatory = $script:Cmd.Parameters['UserId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }

        It 'Should have Enabled switch or parameter to filter by enabled state' {
            Skip-IfMissing 'Get-PveUser'
            # Either an 'Enabled' switch or a filter parameter is expected.
            $hasEnabled = $script:Cmd.Parameters.ContainsKey('Enabled') -or
                          $script:Cmd.Parameters.ContainsKey('EnabledOnly')
            $hasEnabled | Should -BeTrue
        }

        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveUser'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveUser'
            { Get-PveUser -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveUser
# ---------------------------------------------------------------------------
Describe 'New-PveUser' {

    BeforeAll { $script:Cmd = Get-Command 'New-PveUser' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveUser'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'New-PveUser'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'UserId should be Mandatory (must include realm, e.g. user@pam)' {
            Skip-IfMissing 'New-PveUser'
            $isMandatory = $script:Cmd.Parameters['UserId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Optional parameters' {
        It 'Should have Password parameter' {
            Skip-IfMissing 'New-PveUser'
            $hasPassword = $script:Cmd.Parameters.ContainsKey('Password') -or
                           $script:Cmd.Parameters.ContainsKey('Credential')
            $hasPassword | Should -BeTrue
        }

        It 'Should have Email parameter' {
            Skip-IfMissing 'New-PveUser'
            $script:Cmd.Parameters.ContainsKey('Email') | Should -BeTrue
        }

        It 'Should have Groups parameter' {
            Skip-IfMissing 'New-PveUser'
            $script:Cmd.Parameters.ContainsKey('Groups') | Should -BeTrue
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveUser
# ---------------------------------------------------------------------------
Describe 'Remove-PveUser' {

    BeforeAll { $script:Cmd = Get-Command 'Remove-PveUser' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Remove-PveUser'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Remove-PveUser'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should declare ConfirmImpact High' {
            Skip-IfMissing 'Remove-PveUser'
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Required parameters' {
        It 'UserId should be Mandatory' {
            Skip-IfMissing 'Remove-PveUser'
            $isMandatory = $script:Cmd.Parameters['UserId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveUser
# ---------------------------------------------------------------------------
Describe 'Set-PveUser' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PveUser' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PveUser'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Set-PveUser'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'UserId should be Mandatory' {
            Skip-IfMissing 'Set-PveUser'
            $isMandatory = $script:Cmd.Parameters['UserId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveRole
# ---------------------------------------------------------------------------
Describe 'Get-PveRole' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveRole' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveRole'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Parameter metadata' {
        It 'Should have RoleId parameter (optional filter)' {
            Skip-IfMissing 'Get-PveRole'
            $script:Cmd.Parameters.ContainsKey('RoleId') | Should -BeTrue
        }

        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveRole'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveRole'
            { Get-PveRole -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PvePermission
# ---------------------------------------------------------------------------
Describe 'Get-PvePermission' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PvePermission' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PvePermission'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Parameter metadata' {
        It 'Should have UserId parameter' {
            Skip-IfMissing 'Get-PvePermission'
            $script:Cmd.Parameters.ContainsKey('UserId') | Should -BeTrue
        }

        It 'Should have Path parameter (ACL path, optional)' {
            Skip-IfMissing 'Get-PvePermission'
            $script:Cmd.Parameters.ContainsKey('Path') | Should -BeTrue
        }

        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PvePermission'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PvePermission'
            { Get-PvePermission -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PvePermission
# ---------------------------------------------------------------------------
Describe 'Set-PvePermission' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PvePermission' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PvePermission'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Set-PvePermission'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Path should be Mandatory (the ACL path, e.g. / or /vms/100)' {
            Skip-IfMissing 'Set-PvePermission'
            $isMandatory = $script:Cmd.Parameters['Path'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Role should be Mandatory' {
            Skip-IfMissing 'Set-PvePermission'
            $isMandatory = $script:Cmd.Parameters['Role'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }
}
