#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for Test-PveConnection.
    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    $moduleRoot = Resolve-Path (Join-Path $PSScriptRoot '../../../src/PSProxmoxVE')
    $dllCandidates = @(
        Join-Path $moduleRoot 'bin/Debug/net9.0/PSProxmoxVE.dll'
        Join-Path $moduleRoot 'bin/Release/net9.0/PSProxmoxVE.dll'
        Join-Path $moduleRoot 'bin/Debug/net48/PSProxmoxVE.dll'
        Join-Path $moduleRoot 'bin/Release/net48/PSProxmoxVE.dll'
    )

    $script:ModuleDll = $dllCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

    if ($null -eq $script:ModuleDll) {
        throw "PSProxmoxVE.dll not found. Build the project before running Pester tests."
    }

    Import-Module $script:ModuleDll -Force -ErrorAction Stop
}

Describe 'Test-PveConnection' {

    Context 'Command existence' {
        It 'Should be available after module import' {
            Get-Command 'Test-PveConnection' -ErrorAction SilentlyContinue |
                Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            (Get-Command 'Test-PveConnection').CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        BeforeAll {
            $script:Cmd = Get-Command 'Test-PveConnection'
        }

        It 'Should have a Detailed switch parameter' {
            $script:Cmd.Parameters.ContainsKey('Detailed') | Should -BeTrue
            $script:Cmd.Parameters['Detailed'].ParameterType |
                Should -Be ([System.Management.Automation.SwitchParameter])
        }

        It 'Detailed should not be Mandatory' {
            $detailed = $script:Cmd.Parameters['Detailed']
            $isMandatory = $detailed.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }
    }

    Context 'Behaviour without an active session' {
        It 'Should return $false when no session is active (default mode)' {
            $result = Test-PveConnection
            $result | Should -BeFalse
        }

        It 'Should return nothing when -Detailed is specified and no session is active' {
            $result = Test-PveConnection -Detailed
            $result | Should -BeNullOrEmpty
        }

        It 'Should not throw when called with no arguments and no session' {
            { Test-PveConnection -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should not throw when called with -Detailed and no session' {
            { Test-PveConnection -Detailed -ErrorAction Stop } | Should -Not -Throw
        }
    }

    Context 'Output type contract' {
        It 'Default output is a boolean' {
            $result = Test-PveConnection
            $result | Should -BeOfType [bool]
        }
    }
}
