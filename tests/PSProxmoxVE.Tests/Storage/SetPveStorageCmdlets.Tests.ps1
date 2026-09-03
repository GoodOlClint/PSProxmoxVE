#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for storage management cmdlets:
        Set-PveStorage, Get-PveStorageStatus, Remove-PveStorageContent,
        Set-PveStorageContent, New-PveStorageDisk.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Set-PveStorage
# ---------------------------------------------------------------------------
Describe 'Set-PveStorage' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PveStorage' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Set-PveStorage -Storage 'local' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveStorageStatus
# ---------------------------------------------------------------------------
Describe 'Get-PveStorageStatus' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveStorageStatus' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveStorageStatus -Node 'pve' -Storage 'local' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveStorageContent
# ---------------------------------------------------------------------------
Describe 'Remove-PveStorageContent' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveStorageContent' }

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
            { Remove-PveStorageContent -Node 'pve' -Storage 'local' -Volume 'local:iso/test.iso' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveStorageContent
# ---------------------------------------------------------------------------
Describe 'Set-PveStorageContent' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PveStorageContent' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Set-PveStorageContent -Node 'pve' -Storage 'local' -Volume 'local:iso/test.iso' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveStorageDisk
# ---------------------------------------------------------------------------
Describe 'New-PveStorageDisk' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveStorageDisk' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { New-PveStorageDisk -Node 'pve' -Storage 'local' -Filename 'vm-100-disk-1' -Size '32G' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
