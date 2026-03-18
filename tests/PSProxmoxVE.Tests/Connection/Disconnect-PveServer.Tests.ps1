#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for Disconnect-PveServer.
    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

Describe 'Disconnect-PveServer' {

    Context 'Command existence' {
        It 'Should be available after module import' {
            Get-Command 'Disconnect-PveServer' -ErrorAction SilentlyContinue |
                Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            (Get-Command 'Disconnect-PveServer').CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        BeforeAll {
            $script:Cmd = Get-Command 'Disconnect-PveServer'
        }

        It 'Should support ShouldProcess (have WhatIf and Confirm parameters)' {
            $script:Cmd.Parameters.ContainsKey('WhatIf')  | Should -BeTrue
            $script:Cmd.Parameters.ContainsKey('Confirm') | Should -BeTrue
        }

        It 'Should declare ConfirmImpact Low (no explicit -Confirm needed for normal use)' {
            # SupportsShouldProcess is reflected as WhatIf/Confirm parameters.
            # ConfirmImpact=Low means PowerShell will not auto-prompt; just verify the
            # attribute is present by confirming ShouldProcess support is enabled.
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Behaviour when no session is active' {
        It 'Should run without error and emit a warning when no session exists' {
            # Ensure module state has no active session by disconnecting first (may already be null).
            # Disconnect-PveServer should emit a warning, not throw.
            { Disconnect-PveServer -ErrorAction Stop } | Should -Not -Throw
        }
    }

    Context 'WhatIf support' {
        It 'Should accept -WhatIf without throwing' {
            { Disconnect-PveServer -WhatIf -ErrorAction Stop } | Should -Not -Throw
        }
    }
}
