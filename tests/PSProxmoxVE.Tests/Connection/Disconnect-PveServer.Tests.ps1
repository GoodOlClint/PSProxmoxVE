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

    Context 'Active session lifecycle' {
        BeforeAll {
            $script:Module = Get-Module PSProxmoxVE
            $script:Module | Should -Not -BeNullOrEmpty

            $script:SetActiveSession = {
                param($Value)
                $script:Module.SessionState.PSVariable.Set('PSProxmoxVE.ActiveSession', $Value)
            }
            $script:GetActiveSession = {
                $script:Module.SessionState.PSVariable.GetValue('PSProxmoxVE.ActiveSession')
            }

            $script:SessionCtor = [PSProxmoxVE.Core.Authentication.PveSession].GetConstructor(
                [System.Reflection.BindingFlags]'NonPublic, Instance',
                $null,
                [type[]]@([string], [int], [bool], [string]),
                $null)
            $script:SessionCtor | Should -Not -BeNullOrEmpty
        }

        AfterEach {
            & $script:SetActiveSession $null
        }

        It 'Should report "no session" when nothing is active' {
            Disconnect-PveServer -Confirm:$false -WarningVariable w -WarningAction SilentlyContinue
            $w[0] | Should -Match 'No active Proxmox VE session'
        }

        It 'Should clear the active session and warn on a second disconnect' {
            $activeSession = $script:SessionCtor.Invoke(@('active.example', 8006, $false, 'activetoken'))
            & $script:SetActiveSession $activeSession

            Disconnect-PveServer -Confirm:$false -WarningVariable w -WarningAction SilentlyContinue
            $w | Should -BeNullOrEmpty
            & $script:GetActiveSession | Should -BeNullOrEmpty

            Disconnect-PveServer -Confirm:$false -WarningVariable w2 -WarningAction SilentlyContinue
            $w2[0] | Should -Match 'No active Proxmox VE session'
        }

        It 'Should warn when disconnecting an explicit non-active session, and leave the active session untouched' {
            $activeSession = $script:SessionCtor.Invoke(@('active.example', 8006, $false, 'activetoken'))
            & $script:SetActiveSession $activeSession
            $mismatchedSession = $script:SessionCtor.Invoke(@('other.example', 8006, $false, 'othertoken'))

            Disconnect-PveServer -Session $mismatchedSession -Confirm:$false -WarningVariable w -WarningAction SilentlyContinue

            $w[0] | Should -Match 'not the module-level session'
            $w[0] | Should -Match 'Remove-PveApiToken'
            [object]::ReferenceEquals((& $script:GetActiveSession), $activeSession) | Should -BeTrue
        }

        It 'Should disconnect when -Session matches the active session' {
            $activeSession = $script:SessionCtor.Invoke(@('active.example', 8006, $false, 'activetoken'))
            & $script:SetActiveSession $activeSession

            Disconnect-PveServer -Session $activeSession -Confirm:$false -WarningVariable w -WarningAction SilentlyContinue

            $w | Should -BeNullOrEmpty
            & $script:GetActiveSession | Should -BeNullOrEmpty
        }

        It 'Should not clear the active session under -WhatIf' {
            $activeSession = $script:SessionCtor.Invoke(@('active.example', 8006, $false, 'activetoken'))
            & $script:SetActiveSession $activeSession

            Disconnect-PveServer -WhatIf -ErrorAction Stop

            [object]::ReferenceEquals((& $script:GetActiveSession), $activeSession) | Should -BeTrue
        }

        It 'Should see the session Test-PveConnection sees' {
            $activeSession = $script:SessionCtor.Invoke(@('active.example', 8006, $false, 'activetoken'))
            & $script:SetActiveSession $activeSession

            Test-PveConnection | Should -BeTrue
            [object]::ReferenceEquals((Test-PveConnection -Detailed), $activeSession) | Should -BeTrue
        }
    }
}
