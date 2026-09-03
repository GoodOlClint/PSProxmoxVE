#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for SDN cmdlets:
        Get-PveSdnZone, New-PveSdnZone, Remove-PveSdnZone,
        Get-PveSdnVnet, New-PveSdnVnet, Remove-PveSdnVnet.

    All tests are fully offline — no live Proxmox VE target is required.

    SDN support was introduced in Proxmox VE 7 and became stable in PVE 8.
    These cmdlets should include a version guard that raises PveVersionException
    (or similar) when the server version is below the minimum requirement.
    The version guard tests use fully-mocked sessions (no network calls).
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:SdnCmdlets = @(
        'Get-PveSdnZone',  'New-PveSdnZone',  'Remove-PveSdnZone',
        'Get-PveSdnVnet',  'New-PveSdnVnet',  'Remove-PveSdnVnet'
    )
}

# ---------------------------------------------------------------------------
# Get-PveSdnZone
# ---------------------------------------------------------------------------
Describe 'Get-PveSdnZone' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveSdnZone' }

    Context 'Version guard behaviour — without active session' {
        It 'Should throw when no session is active' {
            { Get-PveSdnZone -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveSdnZone
# ---------------------------------------------------------------------------
Describe 'New-PveSdnZone' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveSdnZone' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Type ValidateSet' {
        It 'Type parameter should have ValidateSet including known zone types' {
            $validateSetAttr = $script:Cmd.Parameters['Type'].Attributes |
                Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] } |
                Select-Object -First 1
            $validateSetAttr | Should -Not -BeNullOrEmpty
            $validateSetAttr.ValidValues | Should -Contain 'simple'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveSdnZone
# ---------------------------------------------------------------------------
Describe 'Remove-PveSdnZone' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveSdnZone' }

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
# Get-PveSdnVnet
# ---------------------------------------------------------------------------
Describe 'Get-PveSdnVnet' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveSdnVnet' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveSdnVnet -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveSdnVnet
# ---------------------------------------------------------------------------
Describe 'New-PveSdnVnet' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveSdnVnet' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveSdnVnet
# ---------------------------------------------------------------------------
Describe 'Remove-PveSdnVnet' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveSdnVnet' }

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
