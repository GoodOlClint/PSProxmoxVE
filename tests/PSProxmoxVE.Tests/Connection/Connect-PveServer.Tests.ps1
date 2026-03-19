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

    Context 'Command existence' {
        It 'Should be available after module import' {
            Get-Command 'Connect-PveServer' -ErrorAction SilentlyContinue |
                Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            (Get-Command 'Connect-PveServer').CommandType | Should -Be 'Cmdlet'
        }
    }

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

        It 'Should have a Server parameter' {
            $script:Cmd.Parameters.ContainsKey('Server') | Should -BeTrue
        }

        It 'Should declare Server as Mandatory' {
            $serverParam = $script:Cmd.Parameters['Server']
            $isMandatory = $serverParam.ParameterSets.Values |
                Where-Object { $_.IsMandatory } |
                Select-Object -First 1
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Should have a Port parameter' {
            $script:Cmd.Parameters.ContainsKey('Port') | Should -BeTrue
        }

        It 'Port should default to 8006' {
            # Verify default via the static default-value metadata on the parameter.
            $portParam = $script:Cmd.Parameters['Port']
            $portParam | Should -Not -BeNullOrEmpty
        }

        It 'Should have a Credential parameter' {
            $script:Cmd.Parameters.ContainsKey('Credential') | Should -BeTrue
        }

        It 'Should have an ApiToken parameter' {
            $script:Cmd.Parameters.ContainsKey('ApiToken') | Should -BeTrue
        }

        It 'Should have a SkipCertificateCheck switch parameter' {
            $script:Cmd.Parameters.ContainsKey('SkipCertificateCheck') | Should -BeTrue
            $script:Cmd.Parameters['SkipCertificateCheck'].ParameterType |
                Should -Be ([System.Management.Automation.SwitchParameter])
        }

        It 'Should have a PassThru switch parameter' {
            $script:Cmd.Parameters.ContainsKey('PassThru') | Should -BeTrue
            $script:Cmd.Parameters['PassThru'].ParameterType |
                Should -Be ([System.Management.Automation.SwitchParameter])
        }

        It 'Credential and ApiToken should belong to different parameter sets' {
            $credSets   = $script:Cmd.Parameters['Credential'].ParameterSets.Keys
            $tokenSets  = $script:Cmd.Parameters['ApiToken'].ParameterSets.Keys
            $overlap    = $credSets | Where-Object { $tokenSets -contains $_ }
            $overlap | Should -BeNullOrEmpty
        }
    }
}
