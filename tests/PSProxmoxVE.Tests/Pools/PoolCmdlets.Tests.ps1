#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for pool cmdlets:
        Get-PvePool, New-PvePool, Set-PvePool, Remove-PvePool.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Get-PvePool
# ---------------------------------------------------------------------------
Describe 'Get-PvePool' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PvePool' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PvePool -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PvePool
# ---------------------------------------------------------------------------
Describe 'New-PvePool' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'New-PvePool' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { New-PvePool -PoolId 'testpool' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PvePool
# ---------------------------------------------------------------------------
Describe 'Set-PvePool' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PvePool' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Set-PvePool -PoolId 'testpool' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PvePool
# ---------------------------------------------------------------------------
Describe 'Remove-PvePool' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PvePool' }

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

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Remove-PvePool -PoolId 'testpool' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
