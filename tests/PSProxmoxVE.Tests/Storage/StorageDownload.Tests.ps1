#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for Invoke-PveStorageDownload.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Invoke-PveStorageDownload
# ---------------------------------------------------------------------------
Describe 'Invoke-PveStorageDownload' {
    BeforeAll { $script:Cmd = Get-Command 'Invoke-PveStorageDownload' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should support Confirm' {
            $script:Cmd.Parameters.ContainsKey('Confirm') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Node should be at Position 0' {
            $pos = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                ForEach-Object { $_.Position }
            $pos | Should -Contain 0
        }

        It 'Storage should be at Position 1' {
            $pos = $script:Cmd.Parameters['Storage'].ParameterSets.Values |
                ForEach-Object { $_.Position }
            $pos | Should -Contain 1
        }

        It 'Url should be at Position 2' {
            $pos = $script:Cmd.Parameters['Url'].ParameterSets.Values |
                ForEach-Object { $_.Position }
            $pos | Should -Contain 2
        }

        It 'Filename should be at Position 3' {
            $pos = $script:Cmd.Parameters['Filename'].ParameterSets.Values |
                ForEach-Object { $_.Position }
            $pos | Should -Contain 3
        }
    }

    Context 'Optional parameters' {
        It 'ContentType should have a ValidateSet of iso, vztmpl, backup, import' {
            $validateSet = $script:Cmd.Parameters['ContentType'].Attributes |
                Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $validateSet | Should -Not -BeNullOrEmpty
            $validValues = $validateSet.ValidValues
            $validValues | Should -Contain 'iso'
            $validValues | Should -Contain 'vztmpl'
            $validValues | Should -Contain 'backup'
            $validValues | Should -Contain 'import'
        }

        It 'Should have Wait switch parameter' {
            $script:Cmd.Parameters.ContainsKey('Wait') | Should -BeTrue
            $script:Cmd.Parameters['Wait'].SwitchParameter | Should -BeTrue
        }

        It 'TimeoutSeconds should reject negative values' {
            { Invoke-PveStorageDownload -Node 'pve1' -Storage 'local' -Url 'https://example.com/test.iso' -Filename 'test.iso' -TimeoutSeconds -1 -Confirm:$false -ErrorAction Stop } |
                Should -Throw
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Invoke-PveStorageDownload -Node 'pve1' -Storage 'local' -Url 'https://example.com/test.iso' -Filename 'test.iso' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
