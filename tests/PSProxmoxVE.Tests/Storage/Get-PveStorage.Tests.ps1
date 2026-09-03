#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for storage cmdlets:
        Get-PveStorage, Get-PveStorageContent, New-PveStorage, Remove-PveStorage.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Get-PveStorage
# ---------------------------------------------------------------------------
Describe 'Get-PveStorage' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveStorage' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveStorage -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveStorageContent
# ---------------------------------------------------------------------------
Describe 'Get-PveStorageContent' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveStorageContent' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveStorageContent -Node 'pve-node1' -Storage 'local' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveStorage
# ---------------------------------------------------------------------------
Describe 'New-PveStorage' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveStorage' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'iSCSI/NFS parameters' {
        It 'Should have Shared switch parameter' {
            $script:Cmd.Parameters.ContainsKey('Shared') | Should -BeTrue
            $script:Cmd.Parameters['Shared'].ParameterType | Should -Be ([System.Management.Automation.SwitchParameter])
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveStorage
# ---------------------------------------------------------------------------
Describe 'Remove-PveStorage' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveStorage' }

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
