#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for user, role, and permission cmdlets:
        Get-PveUser, New-PveUser, Remove-PveUser, Set-PveUser,
        Get-PveRole, New-PveRole, Remove-PveRole,
        Get-PvePermission, Set-PvePermission.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $allNames = @(
        'Get-PveUser', 'New-PveUser', 'Remove-PveUser', 'Set-PveUser',
        'Get-PveRole', 'New-PveRole', 'Remove-PveRole',
        'Get-PvePermission', 'Set-PvePermission'
    )
}

# ---------------------------------------------------------------------------
# Get-PveUser
# ---------------------------------------------------------------------------
Describe 'Get-PveUser' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveUser' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveUser -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveUser
# ---------------------------------------------------------------------------
Describe 'New-PveUser' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveUser' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveUser
# ---------------------------------------------------------------------------
Describe 'Remove-PveUser' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveUser' }

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
}

# ---------------------------------------------------------------------------
# Set-PveUser
# ---------------------------------------------------------------------------
Describe 'Set-PveUser' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PveUser' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveRole
# ---------------------------------------------------------------------------
Describe 'Get-PveRole' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveRole' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveRole -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PvePermission
# ---------------------------------------------------------------------------
Describe 'Get-PvePermission' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PvePermission' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PvePermission -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PvePermission
# ---------------------------------------------------------------------------
Describe 'Set-PvePermission' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PvePermission' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }
}
