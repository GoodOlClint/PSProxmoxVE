#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for VM lifecycle cmdlets:
        Start-PveVm, Stop-PveVm, Suspend-PveVm, Resume-PveVm,
        Reset-PveVm, Restart-PveVm.

    All tests are fully offline — no live Proxmox VE target is required.

    NOTE: The C# source declares Reset-PveVm and Restart-PveVm both with
    [Cmdlet(VerbsLifecycle.Restart, "PveVm")].  When both are compiled into
    the same assembly PowerShell registers the last one loaded as
    'Restart-PveVm'.  The test suite validates whichever cmdlet is actually
    registered under each name, rather than assuming a specific implementing
    type.  A separate test records whether 'Reset-PveVm' is resolvable, which
    is expected to change once the naming collision is fixed.
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

    # Helper: assert the standard lifecycle parameter set for Node/VmId/Wait.
    function Assert-StandardLifecycleParams {
        param([string] $CmdletName)
        $cmd = Get-Command $CmdletName -ErrorAction SilentlyContinue
        if ($null -eq $cmd) {
            Set-ItResult -Skipped -Because "$CmdletName is not yet registered (possible name collision in source)"
            return $null
        }
        return $cmd
    }
}

# ---------------------------------------------------------------------------
# Start-PveVm
# ---------------------------------------------------------------------------
Describe 'Start-PveVm' {

    Context 'Command existence' {
        It 'Should be available after module import' {
            Get-Command 'Start-PveVm' -ErrorAction SilentlyContinue |
                Should -Not -BeNullOrEmpty
        }
    }

    Context 'Parameter metadata' {
        BeforeAll { $script:Cmd = Get-Command 'Start-PveVm' }

        It 'Node should be Mandatory' {
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Should have Wait switch parameter' {
            $script:Cmd.Parameters.ContainsKey('Wait') | Should -BeTrue
            $script:Cmd.Parameters['Wait'].ParameterType |
                Should -Be ([System.Management.Automation.SwitchParameter])
        }

        It 'Should support ShouldProcess' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            { Start-PveVm -Node 'pve-node1' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }

        It 'Should not throw with -WhatIf' {
            { Start-PveVm -Node 'pve-node1' -VmId 100 -WhatIf -ErrorAction Stop } |
                Should -Not -Throw
        }
    }
}

# ---------------------------------------------------------------------------
# Stop-PveVm
# ---------------------------------------------------------------------------
Describe 'Stop-PveVm' {

    Context 'Command existence' {
        It 'Should be available after module import' {
            Get-Command 'Stop-PveVm' -ErrorAction SilentlyContinue |
                Should -Not -BeNullOrEmpty
        }
    }

    Context 'Parameter metadata' {
        BeforeAll { $script:Cmd = Get-Command 'Stop-PveVm' }

        It 'Node should be Mandatory' {
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Should have Wait switch parameter' {
            $script:Cmd.Parameters.ContainsKey('Wait') | Should -BeTrue
        }

        It 'Should support ShouldProcess' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            { Stop-PveVm -Node 'pve-node1' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }

        It 'Should not throw with -WhatIf' {
            { Stop-PveVm -Node 'pve-node1' -VmId 100 -WhatIf -ErrorAction Stop } |
                Should -Not -Throw
        }
    }
}

# ---------------------------------------------------------------------------
# Suspend-PveVm
# ---------------------------------------------------------------------------
Describe 'Suspend-PveVm' {

    Context 'Command existence' {
        It 'Should be available after module import' {
            Get-Command 'Suspend-PveVm' -ErrorAction SilentlyContinue |
                Should -Not -BeNullOrEmpty
        }
    }

    Context 'Parameter metadata' {
        BeforeAll { $script:Cmd = Get-Command 'Suspend-PveVm' }

        It 'Node should be Mandatory' {
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Should have Wait switch parameter' {
            $script:Cmd.Parameters.ContainsKey('Wait') | Should -BeTrue
        }

        It 'Should support ShouldProcess' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            { Suspend-PveVm -Node 'pve-node1' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Resume-PveVm
# ---------------------------------------------------------------------------
Describe 'Resume-PveVm' {

    Context 'Command existence' {
        It 'Should be available after module import' {
            Get-Command 'Resume-PveVm' -ErrorAction SilentlyContinue |
                Should -Not -BeNullOrEmpty
        }
    }

    Context 'Parameter metadata' {
        BeforeAll { $script:Cmd = Get-Command 'Resume-PveVm' }

        It 'Node should be Mandatory' {
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Should have Wait switch parameter' {
            $script:Cmd.Parameters.ContainsKey('Wait') | Should -BeTrue
        }

        It 'Should support ShouldProcess' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            { Resume-PveVm -Node 'pve-node1' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Reset-PveVm
# NOTE: Source declares both Reset-PveVmCmdlet and RestartPveVmCmdlet with
# [Cmdlet(VerbsLifecycle.Restart, "PveVm")].  Until the collision is resolved,
# Reset-PveVm may not be separately registered.
# ---------------------------------------------------------------------------
Describe 'Reset-PveVm' {

    Context 'Command existence' {
        It 'Reset-PveVm should be registered in the module exports' {
            # Per the .psd1 manifest, 'Reset-PveVm' is listed in CmdletsToExport.
            # If the name-collision is present, this may resolve to Restart-PveVm's type.
            $cmd = Get-Command 'Reset-PveVm' -ErrorAction SilentlyContinue
            $cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Parameter metadata' {
        BeforeAll {
            $script:Cmd = Get-Command 'Reset-PveVm' -ErrorAction SilentlyContinue
        }

        It 'Node should be present' {
            if ($null -eq $script:Cmd) { Set-ItResult -Skipped -Because 'Reset-PveVm not registered'; return }
            $script:Cmd.Parameters.ContainsKey('Node') | Should -BeTrue
        }

        It 'VmId should be present' {
            if ($null -eq $script:Cmd) { Set-ItResult -Skipped -Because 'Reset-PveVm not registered'; return }
            $script:Cmd.Parameters.ContainsKey('VmId') | Should -BeTrue
        }

        It 'Wait should be present' {
            if ($null -eq $script:Cmd) { Set-ItResult -Skipped -Because 'Reset-PveVm not registered'; return }
            $script:Cmd.Parameters.ContainsKey('Wait') | Should -BeTrue
        }

        It 'Should support ShouldProcess' {
            if ($null -eq $script:Cmd) { Set-ItResult -Skipped -Because 'Reset-PveVm not registered'; return }
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }
}

# ---------------------------------------------------------------------------
# Restart-PveVm
# ---------------------------------------------------------------------------
Describe 'Restart-PveVm' {

    Context 'Command existence' {
        It 'Should be available after module import' {
            Get-Command 'Restart-PveVm' -ErrorAction SilentlyContinue |
                Should -Not -BeNullOrEmpty
        }
    }

    Context 'Parameter metadata' {
        BeforeAll { $script:Cmd = Get-Command 'Restart-PveVm' }

        It 'Node should be Mandatory' {
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Should have Timeout parameter' {
            $script:Cmd.Parameters.ContainsKey('Timeout') | Should -BeTrue
        }

        It 'Should have Wait switch parameter' {
            $script:Cmd.Parameters.ContainsKey('Wait') | Should -BeTrue
            $script:Cmd.Parameters['Wait'].ParameterType |
                Should -Be ([System.Management.Automation.SwitchParameter])
        }

        It 'Should support ShouldProcess' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            { Restart-PveVm -Node 'pve-node1' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }

        It 'Should not throw with -WhatIf' {
            { Restart-PveVm -Node 'pve-node1' -VmId 100 -WhatIf -ErrorAction Stop } |
                Should -Not -Throw
        }
    }
}
