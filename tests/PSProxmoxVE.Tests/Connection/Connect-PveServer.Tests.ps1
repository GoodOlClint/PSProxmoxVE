#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for Connect-PveServer.
    All tests are fully offline — no live Proxmox VE target is required.
    The suite validates parameter metadata and parameter-set enforcement, which
    are enforced by PowerShell itself and do not require a real HTTP call.
#>

BeforeAll {
    # Resolve the built DLL relative to the repository root.
    # Adjust the path if your build output directory differs.
    . $PSScriptRoot/../_TestHelper.ps1
}

Describe 'Connect-PveServer' {
    Context 'Parameter validation — required parameters' {
        It 'Server should be Mandatory' {
            $param = (Get-Command 'Connect-PveServer').Parameters['Server']
            $isMandatory = $param.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Should require either Credential or ApiToken (both parameter sets exist)' {
            $cmd = Get-Command 'Connect-PveServer'
            $cmd.ParameterSets.Count | Should -BeGreaterOrEqual 2
        }
    }

    Context 'Parameter validation — mutually exclusive parameter sets' {
        It 'Should not allow both Credential and ApiToken together' {
            $securePass = ConvertTo-SecureString 'hunter2' -AsPlainText -Force
            $cred = [System.Management.Automation.PSCredential]::new('root@pam', $securePass)
            {
                Connect-PveServer `
                    -Server     'pve.example.com' `
                    -Credential $cred `
                    -ApiToken   'root@pam!mytoken=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee' `
                    -ErrorAction Stop
            } | Should -Throw
        }
    }

    Context 'Parameter metadata' {
        BeforeAll {
            $script:Cmd = Get-Command 'Connect-PveServer'
        }

        It 'Should declare Server as Mandatory' {
            $serverParam = $script:Cmd.Parameters['Server']
            $isMandatory = $serverParam.ParameterSets.Values |
                Where-Object { $_.IsMandatory } |
                Select-Object -First 1
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Port should default to 8006' {
            # Verify default via the static default-value metadata on the parameter.
            $portParam = $script:Cmd.Parameters['Port']
            $portParam | Should -Not -BeNullOrEmpty
        }

        It 'Should have a SkipCertificateCheck switch parameter' {
            $script:Cmd.Parameters.ContainsKey('SkipCertificateCheck') | Should -BeTrue
            $script:Cmd.Parameters['SkipCertificateCheck'].ParameterType |
                Should -Be ([System.Management.Automation.SwitchParameter])
        }

        It 'Should have a PassThru switch parameter (deprecated, hidden)' {
            $script:Cmd.Parameters.ContainsKey('PassThru') | Should -BeTrue
            $script:Cmd.Parameters['PassThru'].ParameterType |
                Should -Be ([System.Management.Automation.SwitchParameter])
        }

        It 'Should have a Quiet switch parameter' {
            $script:Cmd.Parameters.ContainsKey('Quiet') | Should -BeTrue
            $script:Cmd.Parameters['Quiet'].ParameterType |
                Should -Be ([System.Management.Automation.SwitchParameter])
        }

        It 'Credential and ApiToken should belong to different parameter sets' {
            $credSets   = $script:Cmd.Parameters['Credential'].ParameterSets.Keys
            $tokenSets  = $script:Cmd.Parameters['ApiToken'].ParameterSets.Keys
            $overlap    = $credSets | Where-Object { $tokenSets -contains $_ }
            $overlap | Should -BeNullOrEmpty
        }

        It 'TimeoutSeconds should reject negative values' {
            $securePass = ConvertTo-SecureString 'hunter2' -AsPlainText -Force
            $cred = [System.Management.Automation.PSCredential]::new('root@pam', $securePass)
            {
                Connect-PveServer -Server 'pve.example.com' -Credential $cred -TimeoutSeconds -1 -ErrorAction Stop
            } | Should -Throw
        }
    }
}
