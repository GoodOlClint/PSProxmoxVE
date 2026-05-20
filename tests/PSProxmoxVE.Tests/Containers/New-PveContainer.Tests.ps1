#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for New-PveContainer.
    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

Describe 'New-PveContainer' {

    Context 'Command existence' {
        It 'Should be available after module import' {
            Get-Command 'New-PveContainer' -ErrorAction SilentlyContinue |
                Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            (Get-Command 'New-PveContainer').CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        BeforeAll {
            $script:Cmd = Get-Command 'New-PveContainer'
        }

        It 'Should support ShouldProcess (WhatIf parameter present)' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should support ShouldProcess (Confirm parameter present)' {
            $script:Cmd.Parameters.ContainsKey('Confirm') | Should -BeTrue
        }
    }

    Context 'RootFsSize validation' {
        # Validation runs before ShouldProcess so -WhatIf is enough to exercise it
        # without an active session.

        It 'Should reject sub-GB units (e.g. 512M)' {
            { New-PveContainer -Node 'pve-node1' -RootFsStorage 'local-lvm' -RootFsSize '512M' -WhatIf -ErrorAction Stop } |
                Should -Throw '*unsupported unit*'
        }

        It 'Should reject sub-GB units even when -RootFsStorage is omitted' {
            { New-PveContainer -Node 'pve-node1' -RootFsSize '512M' -WhatIf -ErrorAction Stop } |
                Should -Throw '*unsupported unit*'
        }

        It 'Should reject malformed input (e.g. 8.5G)' {
            { New-PveContainer -Node 'pve-node1' -RootFsStorage 'local-lvm' -RootFsSize '8.5G' -WhatIf -ErrorAction Stop } |
                Should -Throw '*not a valid size*'
        }

        It 'Should accept a bare integer with -WhatIf' {
            { New-PveContainer -Node 'pve-node1' -RootFsStorage 'local-lvm' -RootFsSize '8' -WhatIf -ErrorAction Stop } |
                Should -Not -Throw
        }

        It 'Should accept "8G" with -WhatIf' {
            { New-PveContainer -Node 'pve-node1' -RootFsStorage 'local-lvm' -RootFsSize '8G' -WhatIf -ErrorAction Stop } |
                Should -Not -Throw
        }
    }
}
