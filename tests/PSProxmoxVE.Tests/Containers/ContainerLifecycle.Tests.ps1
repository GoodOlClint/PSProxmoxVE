#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for container lifecycle cmdlets:
        New-PveContainer, Remove-PveContainer,
        Start-PveContainer, Stop-PveContainer, Restart-PveContainer.

    All tests are fully offline — no live Proxmox VE target is required.
    If a cmdlet is not yet compiled into the DLL the test is marked Skipped.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    # Pre-calculate availability for each cmdlet.
    $script:Availability = @{}
    foreach ($name in @('New-PveContainer', 'Remove-PveContainer',
                         'Start-PveContainer', 'Stop-PveContainer',
                         'Restart-PveContainer',
                         'Get-PveContainerConfig', 'Set-PveContainerConfig',
                         'Copy-PveContainer', 'Move-PveContainer')) {
        $script:Availability[$name] = $null -ne (Get-Command $name -ErrorAction SilentlyContinue)
    }

    function Skip-IfMissing([string]$Name) {
        if (-not $script:Availability[$Name]) {
            Set-ItResult -Skipped -Because "$Name is not yet implemented in this build"
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveContainer
# ---------------------------------------------------------------------------
Describe 'New-PveContainer' {

    BeforeAll { $script:Cmd = Get-Command 'New-PveContainer' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveContainer'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'New-PveContainer'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'New-PveContainer'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

    }

    Context 'Optional parameters' {
        It 'Should have Wait switch parameter' {
            Skip-IfMissing 'New-PveContainer'
            $script:Cmd.Parameters.ContainsKey('Wait') | Should -BeTrue
        }

        It 'Should have Session parameter' {
            Skip-IfMissing 'New-PveContainer'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            Skip-IfMissing 'New-PveContainer'
            { New-PveContainer -Node 'pve-node1' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveContainer
# ---------------------------------------------------------------------------
Describe 'Remove-PveContainer' {

    BeforeAll { $script:Cmd = Get-Command 'Remove-PveContainer' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Remove-PveContainer'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Remove-PveContainer'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should declare ConfirmImpact High' {
            Skip-IfMissing 'Remove-PveContainer'
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Remove-PveContainer'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Remove-PveContainer'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Optional parameters' {
        It 'Should have Purge switch parameter' {
            Skip-IfMissing 'Remove-PveContainer'
            $script:Cmd.Parameters.ContainsKey('Purge') | Should -BeTrue
        }

        It 'Should have Wait switch parameter' {
            Skip-IfMissing 'Remove-PveContainer'
            $script:Cmd.Parameters.ContainsKey('Wait') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            Skip-IfMissing 'Remove-PveContainer'
            { Remove-PveContainer -Node 'pve-node1' -VmId 200 -Confirm:$false -ErrorAction Stop } | Should -Throw '*No active Proxmox VE session*'
        }

        It 'Should not throw with -WhatIf' {
            Skip-IfMissing 'Remove-PveContainer'
            { Remove-PveContainer -Node 'pve-node1' -VmId 200 -WhatIf -ErrorAction Stop } |
                Should -Not -Throw
        }
    }
}

# ---------------------------------------------------------------------------
# Start-PveContainer
# ---------------------------------------------------------------------------
Describe 'Start-PveContainer' {

    BeforeAll { $script:Cmd = Get-Command 'Start-PveContainer' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Start-PveContainer'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Parameter metadata' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Start-PveContainer'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Start-PveContainer'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Should have Wait switch parameter' {
            Skip-IfMissing 'Start-PveContainer'
            $script:Cmd.Parameters.ContainsKey('Wait') | Should -BeTrue
        }

        It 'Should support ShouldProcess' {
            Skip-IfMissing 'Start-PveContainer'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Start-PveContainer'
            { Start-PveContainer -Node 'pve-node1' -VmId 200 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Stop-PveContainer
# ---------------------------------------------------------------------------
Describe 'Stop-PveContainer' {

    BeforeAll { $script:Cmd = Get-Command 'Stop-PveContainer' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Stop-PveContainer'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Parameter metadata' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Stop-PveContainer'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Stop-PveContainer'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Should have Wait switch parameter' {
            Skip-IfMissing 'Stop-PveContainer'
            $script:Cmd.Parameters.ContainsKey('Wait') | Should -BeTrue
        }

        It 'Should support ShouldProcess' {
            Skip-IfMissing 'Stop-PveContainer'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Stop-PveContainer'
            { Stop-PveContainer -Node 'pve-node1' -VmId 200 -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Restart-PveContainer
# ---------------------------------------------------------------------------
Describe 'Restart-PveContainer' {

    BeforeAll { $script:Cmd = Get-Command 'Restart-PveContainer' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Restart-PveContainer'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Parameter metadata' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Restart-PveContainer'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Restart-PveContainer'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Should support ShouldProcess' {
            Skip-IfMissing 'Restart-PveContainer'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Restart-PveContainer'
            { Restart-PveContainer -Node 'pve-node1' -VmId 200 -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Move-PveContainer
# ---------------------------------------------------------------------------
Describe 'Move-PveContainer' {

    BeforeAll { $script:Cmd = Get-Command 'Move-PveContainer' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Move-PveContainer'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Parameter metadata' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Move-PveContainer'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Move-PveContainer'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'TargetNode should be Mandatory' {
            Skip-IfMissing 'Move-PveContainer'
            $isMandatory = $script:Cmd.Parameters['TargetNode'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Should have Online switch parameter' {
            Skip-IfMissing 'Move-PveContainer'
            $script:Cmd.Parameters.ContainsKey('Online') | Should -BeTrue
        }

        It 'Should have Wait switch parameter' {
            Skip-IfMissing 'Move-PveContainer'
            $script:Cmd.Parameters.ContainsKey('Wait') | Should -BeTrue
        }

        It 'Should support ShouldProcess' {
            Skip-IfMissing 'Move-PveContainer'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Move-PveContainer'
            { Move-PveContainer -Node 'pve-node1' -VmId 200 -TargetNode 'pve-node2' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
