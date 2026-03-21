#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for task list cmdlets:
        Get-PveTaskList, Stop-PveTask.

    All tests are fully offline — no live Proxmox VE target is required.
    If a cmdlet is not yet compiled the test is marked Skipped.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Availability = @{}
    foreach ($name in @('Get-PveTaskList', 'Stop-PveTask')) {
        $script:Availability[$name] = $null -ne (Get-Command $name -ErrorAction SilentlyContinue)
    }

    function Skip-IfMissing([string]$Name) {
        if (-not $script:Availability[$Name]) {
            Set-ItResult -Skipped -Because "$Name is not yet implemented in this build"
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveTaskList
# ---------------------------------------------------------------------------
Describe 'Get-PveTaskList' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveTaskList' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveTaskList'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveTaskList'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Get-PveTaskList'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Parameter metadata' {
        It 'Should have optional VmId parameter' {
            Skip-IfMissing 'Get-PveTaskList'
            $script:Cmd.Parameters.ContainsKey('VmId') | Should -BeTrue
        }
        It 'Should have optional Limit parameter' {
            Skip-IfMissing 'Get-PveTaskList'
            $script:Cmd.Parameters.ContainsKey('Limit') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveTaskList'
            { Get-PveTaskList -Node 'pve' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Stop-PveTask
# ---------------------------------------------------------------------------
Describe 'Stop-PveTask' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Stop-PveTask' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Stop-PveTask'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Stop-PveTask'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Stop-PveTask'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
        It 'Should declare ConfirmImpact High' {
            Skip-IfMissing 'Stop-PveTask'
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Stop-PveTask'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Upid should be Mandatory' {
            Skip-IfMissing 'Stop-PveTask'
            $isMandatory = $script:Cmd.Parameters['Upid'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Stop-PveTask'
            { Stop-PveTask -Node 'pve' -Upid 'UPID:pve:00000001:00000000:00000000:test::root@pam:' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
