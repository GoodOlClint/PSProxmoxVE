#Requires -Module Pester

BeforeAll {
    . $PSScriptRoot/_IntegrationHelper.ps1
    Connect-TestPve
}

AfterAll {
    Disconnect-TestPve
}

Describe 'Connection — Integration' -Tag 'Integration' {
    Context 'Connection' {
        It 'Should connect to PVE server' {
            if (Skip-IfNoTarget) { return }

            $session = Connect-PveServer `
                -Server  $script:Host_ `
                -Port    $script:Port `
                -Credential (New-Object System.Management.Automation.PSCredential(
                    'root@pam',
                    (ConvertTo-SecureString $script:Password -AsPlainText -Force)
                )) `
                -SkipCertificateCheck `
                -PassThru

            $session            | Should -Not -BeNullOrEmpty
            $session.Hostname   | Should -Be $script:Host_
            $session.AuthMode.ToString() | Should -Be 'Ticket'

            Test-PveConnection   | Should -BeTrue
        }

        It 'Should detect server version' {
            if (Skip-IfNoTarget) { return }

            $detail = Test-PveConnection -Detailed
            $detail                         | Should -Not -BeNullOrEmpty
            $detail.ServerVersion           | Should -Not -BeNullOrEmpty
            $detail.ServerVersion.Major     | Should -BeIn @(8, 9)

            # If we know the expected version, verify it matches
            if ($script:PveVersion) {
                $detail.ServerVersion.Major | Should -Be ([int]$script:PveVersion)
            }
        }
    }
}
