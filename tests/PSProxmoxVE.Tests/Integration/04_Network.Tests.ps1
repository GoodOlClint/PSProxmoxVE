#Requires -Module Pester

BeforeAll {
    . $PSScriptRoot/_IntegrationHelper.ps1
    Connect-TestPve
}

AfterAll {
    if (-not $script:SkipReason) {
        # Clean up pester network bridge if it still exists
        try { Remove-PveNetwork -Node $script:Node -Iface 'vmbr99' -Confirm:$false -ErrorAction SilentlyContinue } catch { }
        try { Invoke-PveNetworkApply -Node $script:Node -Wait -ErrorAction SilentlyContinue } catch { }
    }
    Disconnect-TestPve
}

Describe 'Network — Integration' -Tag 'Integration' {
    Context 'Network — Read' {
        It 'Should list networks' {
            if (Skip-IfNoTarget) { return }

            $networks = Get-PveNetwork -Node $script:Node
            $networks | Should -Not -BeNullOrEmpty
        }

        It 'Should filter by interface type' {
            if (Skip-IfNoTarget) { return }

            # Filter for bridges — every PVE node has at least vmbr0
            $bridges = Get-PveNetwork -Node $script:Node -Type 'bridge'
            $bridges | Should -Not -BeNullOrEmpty
            $bridges | ForEach-Object { $_.Type | Should -Be 'bridge' }
        }
    }

    Context 'Network — CRUD' {
        It 'Should create a Linux bridge' {
            if (Skip-IfNoTarget) { return }

            { New-PveNetwork `
                -Node    $script:Node `
                -Iface   'vmbr99' `
                -Type    'bridge' `
                -Autostart `
                -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should find the new bridge in pending changes' {
            if (Skip-IfNoTarget) { return }

            $networks = Get-PveNetwork -Node $script:Node
            $networks | Where-Object { $_.Iface -eq 'vmbr99' } |
                Should -Not -BeNullOrEmpty
        }

        It 'Should update bridge comments' {
            if (Skip-IfNoTarget) { return }

            { Set-PveNetwork `
                -Node    $script:Node `
                -Iface   'vmbr99' `
                -Type    'bridge' `
                -Comments 'Created by Pester integration test' `
                -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should remove the bridge' {
            if (Skip-IfNoTarget) { return }

            { Remove-PveNetwork `
                -Node    $script:Node `
                -Iface   'vmbr99' `
                -Confirm:$false `
                -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should revert pending changes (apply to baseline)' {
            if (Skip-IfNoTarget) { return }

            # Apply reverts any pending changes back to the running config
            { Invoke-PveNetworkApply `
                -Node    $script:Node `
                -Wait `
                -ErrorAction Stop } | Should -Not -Throw
        }
    }
}
