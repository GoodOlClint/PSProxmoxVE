#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for backup info cmdlets:
        Get-PveBackupInfo.

    All tests are fully offline — no live Proxmox VE target is required.
    If a cmdlet is not yet compiled the test is marked Skipped.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Availability = @{}
    foreach ($name in @('Get-PveBackupInfo')) {
        $script:Availability[$name] = $null -ne (Get-Command $name -ErrorAction SilentlyContinue)
    }

    function Skip-IfMissing([string]$Name) {
        if (-not $script:Availability[$Name]) {
            Set-ItResult -Skipped -Because "$Name is not yet implemented in this build"
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveBackupInfo
# ---------------------------------------------------------------------------
Describe 'Get-PveBackupInfo' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveBackupInfo' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveBackupInfo'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveBackupInfo'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveBackupInfo'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveBackupInfo'
            { Get-PveBackupInfo -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
