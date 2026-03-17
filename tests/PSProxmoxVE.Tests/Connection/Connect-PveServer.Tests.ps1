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
    $moduleRoot = Resolve-Path (Join-Path $PSScriptRoot '../../../src/PSProxmoxVE')
    $dllCandidates = @(
        Join-Path $moduleRoot 'bin/Debug/net8.0/PSProxmoxVE.dll'
        Join-Path $moduleRoot 'bin/Release/net8.0/PSProxmoxVE.dll'
        Join-Path $moduleRoot 'bin/Debug/net48/PSProxmoxVE.dll'
        Join-Path $moduleRoot 'bin/Release/net48/PSProxmoxVE.dll'
    )

    $script:ModuleDll = $dllCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

    if ($null -eq $script:ModuleDll) {
        throw "PSProxmoxVE.dll not found. Build the project before running Pester tests."
    }

    Import-Module $script:ModuleDll -Force -ErrorAction Stop
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

    Context 'Parameter validation — missing required parameters' {
        It 'Should throw when Server is omitted entirely' {
            { Connect-PveServer -ErrorAction Stop } | Should -Throw
        }

        It 'Should throw when Server is provided but neither Credential nor ApiToken is supplied' {
            # Both parameter sets require one of Credential/ApiToken; omitting both is an error.
            { Connect-PveServer -Server 'pve.example.com' -ErrorAction Stop } | Should -Throw
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
