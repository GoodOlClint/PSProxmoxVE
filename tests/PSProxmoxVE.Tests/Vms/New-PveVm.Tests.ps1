#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for New-PveVm.
    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
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

Describe 'New-PveVm' {

    Context 'Command existence' {
        It 'Should be available after module import' {
            Get-Command 'New-PveVm' -ErrorAction SilentlyContinue |
                Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            (Get-Command 'New-PveVm').CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        BeforeAll {
            $script:Cmd = Get-Command 'New-PveVm'
        }

        It 'Should support ShouldProcess (WhatIf parameter present)' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should support ShouldProcess (Confirm parameter present)' {
            $script:Cmd.Parameters.ContainsKey('Confirm') | Should -BeTrue
        }

        It 'Should accept -WhatIf without throwing even when no session exists' {
            # WhatIf must short-circuit before any network call or session check.
            { New-PveVm -Node 'pve-node1' -WhatIf -ErrorAction Stop } | Should -Not -Throw
        }
    }

    Context 'Required parameter — Node' {
        It 'Should throw when Node is omitted' {
            { New-PveVm -ErrorAction Stop } | Should -Throw
        }

        It 'Node should be Mandatory' {
            $nodeParam = (Get-Command 'New-PveVm').Parameters['Node']
            $isMandatory = $nodeParam.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Optional parameter metadata' {
        BeforeAll {
            $script:Cmd = Get-Command 'New-PveVm'
        }

        It 'Should have VmId parameter' {
            $script:Cmd.Parameters.ContainsKey('VmId') | Should -BeTrue
        }

        It 'Should have Name parameter' {
            $script:Cmd.Parameters.ContainsKey('Name') | Should -BeTrue
        }

        It 'Should have Memory parameter' {
            $script:Cmd.Parameters.ContainsKey('Memory') | Should -BeTrue
        }

        It 'Should have Cores parameter' {
            $script:Cmd.Parameters.ContainsKey('Cores') | Should -BeTrue
        }

        It 'Should have Sockets parameter' {
            $script:Cmd.Parameters.ContainsKey('Sockets') | Should -BeTrue
        }

        It 'Should have CpuType parameter' {
            $script:Cmd.Parameters.ContainsKey('CpuType') | Should -BeTrue
        }

        It 'Should have Bios parameter' {
            $script:Cmd.Parameters.ContainsKey('Bios') | Should -BeTrue
        }

        It 'Should have Machine parameter' {
            $script:Cmd.Parameters.ContainsKey('Machine') | Should -BeTrue
        }

        It 'Should have DiskSize parameter' {
            $script:Cmd.Parameters.ContainsKey('DiskSize') | Should -BeTrue
        }

        It 'Should have DiskStorage parameter' {
            $script:Cmd.Parameters.ContainsKey('DiskStorage') | Should -BeTrue
        }

        It 'Should have DiskFormat parameter' {
            $script:Cmd.Parameters.ContainsKey('DiskFormat') | Should -BeTrue
        }

        It 'Should have Network parameter' {
            $script:Cmd.Parameters.ContainsKey('Network') | Should -BeTrue
        }

        It 'Should have Bridge parameter' {
            $script:Cmd.Parameters.ContainsKey('Bridge') | Should -BeTrue
        }

        It 'Should have OsType parameter' {
            $script:Cmd.Parameters.ContainsKey('OsType') | Should -BeTrue
        }

        It 'Should have Start switch parameter' {
            $script:Cmd.Parameters.ContainsKey('Start') | Should -BeTrue
            $script:Cmd.Parameters['Start'].ParameterType |
                Should -Be ([System.Management.Automation.SwitchParameter])
        }

        It 'Should have Wait switch parameter' {
            $script:Cmd.Parameters.ContainsKey('Wait') | Should -BeTrue
            $script:Cmd.Parameters['Wait'].ParameterType |
                Should -Be ([System.Management.Automation.SwitchParameter])
        }

        It 'Should have Session parameter (inherited from PveCmdletBase)' {
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            { New-PveVm -Node 'pve-node1' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
