#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for the cmdlet-level PVE error record mapping.
    Fully offline — the failure under test is raised before any request is sent.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

Describe 'PVE error record mapping' {
    Context 'Without an active session' {
        It 'Reports the missing session as ConnectionError' {
            $record = $null
            try { Get-PveNode -ErrorAction Stop } catch { $record = $_ }

            $record | Should -Not -BeNullOrEmpty
            $record.CategoryInfo.Category | Should -Be 'ConnectionError'
        }

        It 'Reports the missing session with the PveNotConnected error id' {
            $record = $null
            try { Get-PveVm -ErrorAction Stop } catch { $record = $_ }

            $record | Should -Not -BeNullOrEmpty
            $record.FullyQualifiedErrorId | Should -BeLike 'PveNotConnected,*'
        }
    }

    Context 'Error kind vocabulary' {
        It 'Every PveErrorKind name is also an ErrorCategory name' {
            $kinds = [enum]::GetNames([PSProxmoxVE.Core.Errors.PveErrorKind])
            $categories = [enum]::GetNames([System.Management.Automation.ErrorCategory])

            $kinds | Should -Not -BeNullOrEmpty
            foreach ($kind in $kinds) {
                $categories | Should -Contain $kind
            }
        }
    }
}
