#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for QEMU guest agent cmdlets:
        Test-PveVmGuestAgent, Get-PveVmGuestNetwork, Invoke-PveVmGuestExec.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Test-PveVmGuestAgent
# ---------------------------------------------------------------------------
Describe 'Test-PveVmGuestAgent' {
    BeforeAll { $script:Cmd = Get-Command 'Test-PveVmGuestAgent' }

    Context 'Parameter metadata' {
        It 'VmId should accept pipeline input by property name' {
            $attr = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $attr | Should -Not -BeNullOrEmpty
        }

        It 'Should output bool' {
            $outputType = $script:Cmd.OutputType | Select-Object -First 1
            $outputType.Type | Should -Be ([bool])
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Test-PveVmGuestAgent -Node 'pve1' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveVmGuestNetwork
# ---------------------------------------------------------------------------
Describe 'Get-PveVmGuestNetwork' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveVmGuestNetwork' }

    Context 'Parameter metadata' {
        It 'VmId should accept pipeline input by property name' {
            $attr = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $attr | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveVmGuestNetwork -Node 'pve1' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Invoke-PveVmGuestExec
# ---------------------------------------------------------------------------
Describe 'Invoke-PveVmGuestExec' {
    BeforeAll { $script:Cmd = Get-Command 'Invoke-PveVmGuestExec' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Should have Timeout parameter of type int' {
            $script:Cmd.Parameters.ContainsKey('Timeout') | Should -BeTrue
            $script:Cmd.Parameters['Timeout'].ParameterType | Should -Be ([int])
        }

        It 'Timeout should have ValidateRange(1,3600)' {
            $rangeAttr = $script:Cmd.Parameters['Timeout'].Attributes |
                Where-Object { $_ -is [System.Management.Automation.ValidateRangeAttribute] }
            $rangeAttr | Should -Not -BeNullOrEmpty
            $rangeAttr.MinRange | Should -Be 1
            $rangeAttr.MaxRange | Should -Be 3600
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Invoke-PveVmGuestExec -Node 'pve1' -VmId 100 -Command 'hostname' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
