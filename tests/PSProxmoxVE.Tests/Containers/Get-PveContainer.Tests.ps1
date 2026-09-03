#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for Get-PveContainer.
    All tests are fully offline — no live Proxmox VE target is required.

    Get-PveContainer mirrors the design of Get-PveVm for LXC containers.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

Describe 'Get-PveContainer' {
    Context 'Parameter metadata' {
        BeforeAll {
            $script:Cmd = Get-Command 'Get-PveContainer'
        }

        It 'Node should accept pipeline input by property name' {
            $node = $script:Cmd.Parameters['Node']
            $acceptsByPropName = $node.ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $acceptsByPropName | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveContainer -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
