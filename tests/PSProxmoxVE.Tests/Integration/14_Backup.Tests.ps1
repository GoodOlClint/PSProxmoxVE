#Requires -Module Pester

BeforeAll {
    . $PSScriptRoot/_IntegrationHelper.ps1
    Connect-TestPve

    $script:BackupTestJobId = $null
}

AfterAll {
    if ($null -eq $script:SkipReason) {
        try {
            if ($script:BackupTestJobId) {
                Remove-PveBackupJob -Id $script:BackupTestJobId -Confirm:$false -ErrorAction SilentlyContinue
            }
        } catch { }
    }
    Disconnect-TestPve
}

Describe 'Backup Jobs — Integration' -Tag 'Integration' {

    Context 'Backup Jobs' {
        It 'Should create a backup job' {
            Skip-IfNoTarget
            { New-PveBackupJob -Schedule 'sat 03:00' -Storage $script:Storage -Mode snapshot -All -Comment 'pester-test-backup' -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should list backup jobs and find the test job' {
            Skip-IfNoTarget
            $jobs = Get-PveBackupJob
            $testJob = $jobs | Where-Object { $_.Comment -eq 'pester-test-backup' }
            $testJob | Should -Not -BeNullOrEmpty
            $script:BackupTestJobId = $testJob.Id
        }

        It 'Should update the backup job' {
            Skip-IfNoTarget
            if (-not $script:BackupTestJobId) {
                Set-ItResult -Skipped -Because 'No test backup job was created'
            }
            { Set-PveBackupJob -Id $script:BackupTestJobId -Comment 'pester-test-updated' -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should remove the backup job' {
            Skip-IfNoTarget
            if (-not $script:BackupTestJobId) {
                Set-ItResult -Skipped -Because 'No test backup job was created'
            }
            { Remove-PveBackupJob -Id $script:BackupTestJobId -Confirm:$false -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should verify the backup job was removed' {
            Skip-IfNoTarget
            if (-not $script:BackupTestJobId) {
                Set-ItResult -Skipped -Because 'No test backup job was created'
            }
            $jobs = Get-PveBackupJob
            $testJob = $jobs | Where-Object { $_.Id -eq $script:BackupTestJobId }
            $testJob | Should -BeNullOrEmpty
        }
    }
}
