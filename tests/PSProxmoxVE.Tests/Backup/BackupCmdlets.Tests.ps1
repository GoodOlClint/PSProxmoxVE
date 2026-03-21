#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for backup cmdlets:
        New-PveBackup, Get-PveBackupJob, New-PveBackupJob, Set-PveBackupJob, Remove-PveBackupJob.

    All tests are fully offline — no live Proxmox VE target is required.
    If a cmdlet is not yet compiled the test is marked Skipped.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Availability = @{}
    foreach ($name in @('New-PveBackup', 'Get-PveBackupJob', 'New-PveBackupJob', 'Set-PveBackupJob', 'Remove-PveBackupJob')) {
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
Describe 'Backup cmdlets — manifest declarations' {
    BeforeAll {
        $manifestPath = Join-Path (Get-Module PSProxmoxVE).ModuleBase 'PSProxmoxVE.psd1'
        $script:Manifest = if (Test-Path $manifestPath) { Import-PowerShellDataFile $manifestPath } else { $null }
    }

    It "<cmdName> should be declared in CmdletsToExport" -TestCases @(
        @{ cmdName = 'New-PveBackup' }
        @{ cmdName = 'Get-PveBackupJob' }
        @{ cmdName = 'New-PveBackupJob' }
        @{ cmdName = 'Set-PveBackupJob' }
        @{ cmdName = 'Remove-PveBackupJob' }
    ) {
        if ($null -eq $script:Manifest) { Set-ItResult -Skipped -Because 'Manifest not found'; return }
        $script:Manifest.CmdletsToExport | Should -Contain $cmdName
    }
}

# ---------------------------------------------------------------------------
# New-PveBackup
# ---------------------------------------------------------------------------
Describe 'New-PveBackup' {

    BeforeAll { $script:Cmd = Get-Command 'New-PveBackup' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveBackup'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'New-PveBackup'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'New-PveBackup'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Parameter metadata' {
        It 'Should have VmId parameter' {
            Skip-IfMissing 'New-PveBackup'
            $script:Cmd.Parameters.ContainsKey('VmId') | Should -BeTrue
        }

        It 'Should have Storage parameter' {
            Skip-IfMissing 'New-PveBackup'
            $script:Cmd.Parameters.ContainsKey('Storage') | Should -BeTrue
        }

        It 'Should have Node parameter' {
            Skip-IfMissing 'New-PveBackup'
            $script:Cmd.Parameters.ContainsKey('Node') | Should -BeTrue
        }

        It 'Should have Session parameter' {
            Skip-IfMissing 'New-PveBackup'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'New-PveBackup'
            { New-PveBackup -VmId 100 -Storage 'local' -Node 'pve' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveBackupJob
# ---------------------------------------------------------------------------
Describe 'Get-PveBackupJob' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveBackupJob' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveBackupJob'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveBackupJob'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have optional Id parameter' {
            Skip-IfMissing 'Get-PveBackupJob'
            $script:Cmd.Parameters.ContainsKey('Id') | Should -BeTrue
        }

        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveBackupJob'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveBackupJob'
            { Get-PveBackupJob -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveBackupJob
# ---------------------------------------------------------------------------
Describe 'New-PveBackupJob' {

    BeforeAll { $script:Cmd = Get-Command 'New-PveBackupJob' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveBackupJob'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'New-PveBackupJob'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Storage should be Mandatory' {
            Skip-IfMissing 'New-PveBackupJob'
            $isMandatory = $script:Cmd.Parameters['Storage'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Schedule should be Mandatory' {
            Skip-IfMissing 'New-PveBackupJob'
            $isMandatory = $script:Cmd.Parameters['Schedule'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Optional parameters' {
        It 'Should have VmId parameter' {
            Skip-IfMissing 'New-PveBackupJob'
            $script:Cmd.Parameters.ContainsKey('VmId') | Should -BeTrue
        }

        It 'Should have Mode parameter' {
            Skip-IfMissing 'New-PveBackupJob'
            $script:Cmd.Parameters.ContainsKey('Mode') | Should -BeTrue
        }

        It 'Should have Compress parameter' {
            Skip-IfMissing 'New-PveBackupJob'
            $script:Cmd.Parameters.ContainsKey('Compress') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'New-PveBackupJob'
            { New-PveBackupJob -Storage 'local' -Schedule 'daily' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveBackupJob
# ---------------------------------------------------------------------------
Describe 'Set-PveBackupJob' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PveBackupJob' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PveBackupJob'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Set-PveBackupJob'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Id should be Mandatory' {
            Skip-IfMissing 'Set-PveBackupJob'
            $isMandatory = $script:Cmd.Parameters['Id'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Set-PveBackupJob'
            { Set-PveBackupJob -Id 'backup-test' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveBackupJob
# ---------------------------------------------------------------------------
Describe 'Remove-PveBackupJob' {

    BeforeAll { $script:Cmd = Get-Command 'Remove-PveBackupJob' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Remove-PveBackupJob'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Remove-PveBackupJob'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should declare ConfirmImpact High' {
            Skip-IfMissing 'Remove-PveBackupJob'
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Required parameters' {
        It 'Id should be Mandatory' {
            Skip-IfMissing 'Remove-PveBackupJob'
            $isMandatory = $script:Cmd.Parameters['Id'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Remove-PveBackupJob'
            { Remove-PveBackupJob -Id 'backup-test' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
