#Requires -Module Pester

BeforeAll {
    . $PSScriptRoot/_IntegrationHelper.ps1
    Connect-TestPve
}

AfterAll {
    if (-not $script:SkipReason) {
        # Clean up users created during this file
        foreach ($userId in @('pester-user@pam', 'pester-tokenuser@pam', 'pester-permuser@pam')) {
            try { Remove-PveUser -UserId $userId -Confirm:$false -ErrorAction SilentlyContinue } catch { }
        }
        # Clean up roles
        try { Remove-PveRole -RoleId 'PesterTestRole' -Confirm:$false -ErrorAction SilentlyContinue } catch { }
    }
    Disconnect-TestPve
}

Describe 'Users — Integration' -Tag 'Integration' {
    Context 'User CRUD' {
        It 'Should create a new user' {
            if (Skip-IfNoTarget) { return }

            { New-PveUser -UserId 'pester-user@pam' -ErrorAction Stop } |
                Should -Not -Throw
        }

        It 'Should list users and find the new user' {
            if (Skip-IfNoTarget) { return }

            $users = Get-PveUser
            $users | Should -Not -BeNullOrEmpty
            $users | Where-Object { $_.UserId -eq 'pester-user@pam' } |
                Should -Not -BeNullOrEmpty
        }

        It 'Should update user properties' {
            if (Skip-IfNoTarget) { return }

            { Set-PveUser -UserId 'pester-user@pam' `
                -Comment 'Updated by Pester integration test' `
                -ErrorAction Stop } | Should -Not -Throw

            $user = Get-PveUser | Where-Object { $_.UserId -eq 'pester-user@pam' }
            $user.Comment | Should -Be 'Updated by Pester integration test'
        }

        It 'Should remove the user' {
            if (Skip-IfNoTarget) { return }

            { Remove-PveUser -UserId 'pester-user@pam' -Confirm:$false -ErrorAction Stop } |
                Should -Not -Throw

            $users = Get-PveUser
            $users | Where-Object { $_.UserId -eq 'pester-user@pam' } |
                Should -BeNullOrEmpty
        }
    }

    Context 'Role CRUD' {
        It 'Should create a new role' {
            if (Skip-IfNoTarget) { return }

            { New-PveRole -RoleId 'PesterTestRole' `
                -Privileges 'VM.Audit,VM.Console' `
                -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should list roles and find the new role' {
            if (Skip-IfNoTarget) { return }

            $roles = Get-PveRole
            $roles | Should -Not -BeNullOrEmpty
            $roles | Where-Object { $_.RoleId -eq 'PesterTestRole' } |
                Should -Not -BeNullOrEmpty
        }

        It 'Should remove the role' {
            if (Skip-IfNoTarget) { return }

            { Remove-PveRole -RoleId 'PesterTestRole' -Confirm:$false -ErrorAction Stop } |
                Should -Not -Throw
        }
    }

    Context 'API Token CRUD' {
        BeforeAll {
            if ($null -eq $script:SkipReason) {
                # Idempotent setup: remove existing token/user first if they exist
                try { Remove-PveApiToken -UserId 'pester-tokenuser@pam' -TokenId 'pester-token' -Confirm:$false -ErrorAction SilentlyContinue } catch { }
                try { Remove-PveUser -UserId 'pester-tokenuser@pam' -Confirm:$false -ErrorAction SilentlyContinue } catch { }

                New-PveUser -UserId 'pester-tokenuser@pam' -ErrorAction SilentlyContinue
            }
        }

        It 'Should create an API token' {
            if (Skip-IfNoTarget) { return }

            $result = New-PveApiToken `
                -UserId  'pester-tokenuser@pam' `
                -TokenId 'pester-token' `
                -ErrorAction Stop

            $result | Should -Not -BeNullOrEmpty
        }

        It 'Should list API tokens for the user' {
            if (Skip-IfNoTarget) { return }

            $tokens = Get-PveApiToken -UserId 'pester-tokenuser@pam'
            $tokens | Should -Not -BeNullOrEmpty
            $tokens | Where-Object { $_.TokenId -eq 'pester-token' } |
                Should -Not -BeNullOrEmpty
        }

        It 'Should remove the API token' {
            if (Skip-IfNoTarget) { return }

            { Remove-PveApiToken `
                -UserId  'pester-tokenuser@pam' `
                -TokenId 'pester-token' `
                -Confirm:$false `
                -ErrorAction Stop } | Should -Not -Throw
        }
    }

    Context 'Permissions' {
        BeforeAll {
            if ($null -eq $script:SkipReason) {
                # Idempotent setup
                try { Remove-PveUser -UserId 'pester-permuser@pam' -Confirm:$false -ErrorAction SilentlyContinue } catch { }
                New-PveUser -UserId 'pester-permuser@pam' -ErrorAction SilentlyContinue
            }
        }

        It 'Should list permissions' {
            if (Skip-IfNoTarget) { return }

            { Get-PvePermission -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should set a permission' {
            if (Skip-IfNoTarget) { return }

            { Set-PvePermission `
                -Path  '/' `
                -Role  'PVEAuditor' `
                -UgId  'pester-permuser@pam' `
                -Type  'user' `
                -ErrorAction Stop } | Should -Not -Throw
        }
    }
}
