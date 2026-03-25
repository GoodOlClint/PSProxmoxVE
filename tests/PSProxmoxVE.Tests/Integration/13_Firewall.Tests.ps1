#Requires -Module Pester

BeforeAll {
    . $PSScriptRoot/_IntegrationHelper.ps1
    Connect-TestPve

    $script:FirewallTestRulePos = $null
}

AfterAll {
    if ($null -eq $script:SkipReason) {
        # Cleanup: firewall test artifacts
        try {
            $rules = Get-PveFirewallRule -Level Cluster -ErrorAction SilentlyContinue
            $testRules = $rules | Where-Object { $_.Comment -like 'pester-test*' }
            foreach ($r in $testRules) {
                Remove-PveFirewallRule -Level Cluster -Position $r.Pos -Confirm:$false -ErrorAction SilentlyContinue
            }
        } catch { }

        # Cleanup: firewall aliases
        try {
            Remove-PveFirewallAlias -Level Cluster -Name 'pester-alias' -Confirm:$false -ErrorAction SilentlyContinue
        } catch { }

        # Cleanup: firewall IP sets
        try {
            Remove-PveFirewallIpSet -Level Cluster -Name 'pester-ipset' -Confirm:$false -ErrorAction SilentlyContinue
        } catch { }
    }
    Disconnect-TestPve
}

Describe 'Firewall — Integration' -Tag 'Integration' {

    Context 'Firewall — Cluster Rules' {
        It 'Should create a firewall rule' {
            Skip-IfNoTarget
            { New-PveFirewallRule -Level Cluster -Type in -Action ACCEPT -Proto tcp -Dport '8080' -Comment 'pester-test-rule' -Enable -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should list firewall rules and find the test rule' {
            Skip-IfNoTarget
            $rules = Get-PveFirewallRule -Level Cluster
            $rules | Should -Not -BeNullOrEmpty
            $testRule = $rules | Where-Object { $_.Comment -eq 'pester-test-rule' }
            $testRule | Should -Not -BeNullOrEmpty
            $script:FirewallTestRulePos = $testRule.Pos
        }

        It 'Should update the firewall rule' {
            Skip-IfNoTarget
            if ($null -eq $script:FirewallTestRulePos) {
                Set-ItResult -Skipped -Because 'No test rule was created'
            }
            { Set-PveFirewallRule -Level Cluster -Position $script:FirewallTestRulePos -Comment 'pester-test-updated' -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should remove the firewall rule' {
            Skip-IfNoTarget
            if ($null -eq $script:FirewallTestRulePos) {
                Set-ItResult -Skipped -Because 'No test rule was created'
            }
            { Remove-PveFirewallRule -Level Cluster -Position $script:FirewallTestRulePos -Confirm:$false -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should get firewall options' {
            Skip-IfNoTarget
            $opts = Get-PveFirewallOptions -Level Cluster
            $opts | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Firewall — Aliases and IP Sets' {
        It 'Should create a firewall alias' {
            Skip-IfNoTarget
            { New-PveFirewallAlias -Level Cluster -Name 'pester-alias' -Cidr '192.168.99.0/24' -Comment 'test alias' -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should list and find the alias' {
            Skip-IfNoTarget
            $aliases = Get-PveFirewallAlias -Level Cluster
            $aliases | Where-Object { $_.Name -eq 'pester-alias' } | Should -Not -BeNullOrEmpty
        }

        It 'Should remove the alias' {
            Skip-IfNoTarget
            { Remove-PveFirewallAlias -Level Cluster -Name 'pester-alias' -Confirm:$false -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should create an IP set' {
            Skip-IfNoTarget
            { New-PveFirewallIpSet -Level Cluster -Name 'pester-ipset' -Comment 'test ipset' -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should add an entry to the IP set' {
            Skip-IfNoTarget
            { New-PveFirewallIpSetEntry -Level Cluster -Name 'pester-ipset' -Cidr '10.99.0.0/16' -Comment 'test entry' -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should list IP set entries' {
            Skip-IfNoTarget
            $entries = Get-PveFirewallIpSetEntry -Level Cluster -Name 'pester-ipset'
            $entries | Should -Not -BeNullOrEmpty
            ($entries | Where-Object { $_.Cidr -like '10.99.0.0*' }) | Should -Not -BeNullOrEmpty
        }

        It 'Should remove the IP set entry' {
            Skip-IfNoTarget
            { Remove-PveFirewallIpSetEntry -Level Cluster -Name 'pester-ipset' -Cidr '10.99.0.0/16' -Confirm:$false -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should remove the IP set' {
            Skip-IfNoTarget
            { Remove-PveFirewallIpSet -Level Cluster -Name 'pester-ipset' -Confirm:$false -ErrorAction Stop } | Should -Not -Throw
        }
    }
}
