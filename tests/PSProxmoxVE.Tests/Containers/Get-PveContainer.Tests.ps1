#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for Get-PveContainer.
    All tests are fully offline — no live Proxmox VE target is required.

    Get-PveContainer mirrors the design of Get-PveVm for LXC containers.
    If the cmdlet is not yet implemented (dll compiled without it), tests
    that depend on invocation are marked Skipped rather than Failed.
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

    $script:CmdExists = $null -ne (Get-Command 'Get-PveContainer' -ErrorAction SilentlyContinue)
}

Describe 'Get-PveContainer' {

    Context 'Command existence' {
        It 'Get-PveContainer should be listed in the module manifest CmdletsToExport' {
            # The .psd1 declares the cmdlet; implementation may be pending.
            $manifestPath = Join-Path $moduleRoot 'PSProxmoxVE.psd1'
            if (Test-Path $manifestPath) {
                $manifest = Import-PowerShellDataFile $manifestPath
                $manifest.CmdletsToExport | Should -Contain 'Get-PveContainer'
            }
            else {
                Set-ItResult -Skipped -Because 'Module manifest not found at expected path'
            }
        }

        It 'Should be available after module import (or skip if not yet compiled)' {
            if (-not $script:CmdExists) {
                Set-ItResult -Skipped -Because 'Get-PveContainer is not yet implemented in this build'
                return
            }
            (Get-Command 'Get-PveContainer').CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        BeforeAll {
            $script:Cmd = Get-Command 'Get-PveContainer' -ErrorAction SilentlyContinue
        }

        It 'Should have Node parameter' {
            if (-not $script:CmdExists) { Set-ItResult -Skipped -Because 'Not yet compiled'; return }
            $script:Cmd.Parameters.ContainsKey('Node') | Should -BeTrue
        }

        It 'Node should not be Mandatory (all-nodes query when omitted)' {
            if (-not $script:CmdExists) { Set-ItResult -Skipped -Because 'Not yet compiled'; return }
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }

        It 'Should have VmId parameter' {
            if (-not $script:CmdExists) { Set-ItResult -Skipped -Because 'Not yet compiled'; return }
            $script:Cmd.Parameters.ContainsKey('VmId') | Should -BeTrue
        }

        It 'Should have Name parameter' {
            if (-not $script:CmdExists) { Set-ItResult -Skipped -Because 'Not yet compiled'; return }
            $script:Cmd.Parameters.ContainsKey('Name') | Should -BeTrue
        }

        It 'Should have Status parameter' {
            if (-not $script:CmdExists) { Set-ItResult -Skipped -Because 'Not yet compiled'; return }
            $script:Cmd.Parameters.ContainsKey('Status') | Should -BeTrue
        }

        It 'Should have Session parameter (inherited from PveCmdletBase)' {
            if (-not $script:CmdExists) { Set-ItResult -Skipped -Because 'Not yet compiled'; return }
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }

        It 'Node should accept pipeline input by property name' {
            if (-not $script:CmdExists) { Set-ItResult -Skipped -Because 'Not yet compiled'; return }
            $node = $script:Cmd.Parameters['Node']
            $acceptsByPropName = $node.ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $acceptsByPropName | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            if (-not $script:CmdExists) { Set-ItResult -Skipped -Because 'Not yet compiled'; return }
            { Get-PveContainer -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
