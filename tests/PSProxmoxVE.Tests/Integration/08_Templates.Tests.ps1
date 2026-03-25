#Requires -Module Pester

BeforeAll {
    . $PSScriptRoot/_IntegrationHelper.ps1
    Connect-TestPve
}

AfterAll {
    Disconnect-TestPve
}

Describe 'Templates — Integration' -Tag 'Integration' {
    Context 'Templates' {
        It 'Should list templates' {
            if (Skip-IfNoTarget) { return }

            # Zero templates is acceptable; just verify the cmdlet does not throw.
            { Get-PveTemplate -Node $script:Node -ErrorAction Stop } | Should -Not -Throw
        }
    }
}
