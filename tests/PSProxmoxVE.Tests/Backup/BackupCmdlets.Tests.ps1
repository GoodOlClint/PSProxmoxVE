#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for backup cmdlets:
        New-PveBackup, Get-PveBackupJob, New-PveBackupJob, Set-PveBackupJob, Remove-PveBackupJob.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# New-PveBackup
# ---------------------------------------------------------------------------
Describe 'New-PveBackup' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveBackup' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { New-PveBackup -VmId 100 -Storage 'local' -Node 'pve' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveBackupJob
# ---------------------------------------------------------------------------
Describe 'Get-PveBackupJob' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveBackupJob' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveBackupJob -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveBackupJob
# ---------------------------------------------------------------------------
Describe 'New-PveBackupJob' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveBackupJob' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { New-PveBackupJob -Storage 'local' -Schedule 'daily' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveBackupJob
# ---------------------------------------------------------------------------
Describe 'Set-PveBackupJob' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PveBackupJob' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Set-PveBackupJob -Id 'backup-test' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveBackupJob
# ---------------------------------------------------------------------------
Describe 'Remove-PveBackupJob' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveBackupJob' }

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
            { Remove-PveBackupJob -Id 'backup-test' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
