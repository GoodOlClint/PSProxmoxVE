#Requires -Module Pester

BeforeAll {
    . $PSScriptRoot/_IntegrationHelper.ps1
    Connect-TestPve
}

AfterAll {
    Disconnect-TestPve
}

Describe 'Nodes — Integration' -Tag 'Integration' {
    Context 'Nodes' {
        It 'Should list nodes' {
            if (Skip-IfNoTarget) { return }

            $nodes = Get-PveNode
            $nodes | Should -Not -BeNullOrEmpty
        }

        It 'Should get node status' {
            if (Skip-IfNoTarget) { return }

            $status = Get-PveNodeStatus -Node $script:Node
            $status            | Should -Not -BeNullOrEmpty
            $status.Node       | Should -Be $script:Node
        }
    }
}
