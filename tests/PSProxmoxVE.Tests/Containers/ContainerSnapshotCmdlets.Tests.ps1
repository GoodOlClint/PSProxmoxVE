#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for container snapshot cmdlets:
        Get-PveContainerSnapshot, New-PveContainerSnapshot,
        Remove-PveContainerSnapshot, Restore-PveContainerSnapshot.

    All tests are fully offline — no live Proxmox VE target is required.
    If a cmdlet is not yet compiled the test is marked Skipped.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Availability = @{}
    foreach ($name in @('Get-PveContainerSnapshot', 'New-PveContainerSnapshot',
                         'Remove-PveContainerSnapshot', 'Restore-PveContainerSnapshot')) {
        $script:Availability[$name] = $null -ne (Get-Command $name -ErrorAction SilentlyContinue)
    }

    function Skip-IfMissing([string]$Name) {
        if (-not $script:Availability[$Name]) {
            Set-ItResult -Skipped -Because "$Name is not yet implemented in this build"
        }
    }
}

# ---------------------------------------------------------------------------
# Manifest contract
# ---------------------------------------------------------------------------
Describe 'Container snapshot cmdlets — manifest declarations' {
    BeforeAll {
        $manifestPath = Join-Path (Get-Module PSProxmoxVE).ModuleBase 'PSProxmoxVE.psd1'
        $script:Manifest = if (Test-Path $manifestPath) { Import-PowerShellDataFile $manifestPath } else { $null }
    }

    It "<cmdName> should be declared in CmdletsToExport" -TestCases @(
        @{ cmdName = 'Get-PveContainerSnapshot' }
        @{ cmdName = 'New-PveContainerSnapshot' }
        @{ cmdName = 'Remove-PveContainerSnapshot' }
        @{ cmdName = 'Restore-PveContainerSnapshot' }
    ) {
        if ($null -eq $script:Manifest) { Set-ItResult -Skipped -Because 'Manifest not found'; return }
        $script:Manifest.CmdletsToExport | Should -Contain $cmdName
    }
}

# ---------------------------------------------------------------------------
# Get-PveContainerSnapshot
# ---------------------------------------------------------------------------
Describe 'Get-PveContainerSnapshot' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveContainerSnapshot' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveContainerSnapshot'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveContainerSnapshot'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Get-PveContainerSnapshot'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Get-PveContainerSnapshot'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Should have Name parameter (optional filter by snapshot name)' {
            Skip-IfMissing 'Get-PveContainerSnapshot'
            $script:Cmd.Parameters.ContainsKey('Name') | Should -BeTrue
        }

        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveContainerSnapshot'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveContainerSnapshot'
            { Get-PveContainerSnapshot -Node 'pve-node1' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveContainerSnapshot
# ---------------------------------------------------------------------------
Describe 'New-PveContainerSnapshot' {

    BeforeAll { $script:Cmd = Get-Command 'New-PveContainerSnapshot' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveContainerSnapshot'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'New-PveContainerSnapshot'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'New-PveContainerSnapshot'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            Skip-IfMissing 'New-PveContainerSnapshot'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Name (snapshot name) should be Mandatory' {
            Skip-IfMissing 'New-PveContainerSnapshot'
            $isMandatory = $script:Cmd.Parameters['Name'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Optional parameters' {
        It 'Should have Description parameter' {
            Skip-IfMissing 'New-PveContainerSnapshot'
            $script:Cmd.Parameters.ContainsKey('Description') | Should -BeTrue
        }

        It 'Should have Wait switch parameter' {
            Skip-IfMissing 'New-PveContainerSnapshot'
            $script:Cmd.Parameters.ContainsKey('Wait') | Should -BeTrue
        }

        It 'Should NOT have IncludeVmState (LXC containers do not support RAM snapshots)' {
            Skip-IfMissing 'New-PveContainerSnapshot'
            $script:Cmd.Parameters.ContainsKey('IncludeVmState') | Should -BeFalse
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            Skip-IfMissing 'New-PveContainerSnapshot'
            { New-PveContainerSnapshot -Node 'pve-node1' -VmId 100 -Name 'snap1' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveContainerSnapshot
# ---------------------------------------------------------------------------
Describe 'Remove-PveContainerSnapshot' {

    BeforeAll { $script:Cmd = Get-Command 'Remove-PveContainerSnapshot' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Remove-PveContainerSnapshot'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Remove-PveContainerSnapshot'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should declare ConfirmImpact High' {
            Skip-IfMissing 'Remove-PveContainerSnapshot'
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Remove-PveContainerSnapshot'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Remove-PveContainerSnapshot'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Name should be Mandatory' {
            Skip-IfMissing 'Remove-PveContainerSnapshot'
            $isMandatory = $script:Cmd.Parameters['Name'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            Skip-IfMissing 'Remove-PveContainerSnapshot'
            { Remove-PveContainerSnapshot -Node 'pve-node1' -VmId 100 -Name 'snap1' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Restore-PveContainerSnapshot
# ---------------------------------------------------------------------------
Describe 'Restore-PveContainerSnapshot' {

    BeforeAll { $script:Cmd = Get-Command 'Restore-PveContainerSnapshot' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Restore-PveContainerSnapshot'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Restore-PveContainerSnapshot'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should declare ConfirmImpact High' {
            Skip-IfMissing 'Restore-PveContainerSnapshot'
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Restore-PveContainerSnapshot'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Restore-PveContainerSnapshot'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Name should be Mandatory' {
            Skip-IfMissing 'Restore-PveContainerSnapshot'
            $isMandatory = $script:Cmd.Parameters['Name'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Optional parameters' {
        It 'Should have Wait switch parameter' {
            Skip-IfMissing 'Restore-PveContainerSnapshot'
            $script:Cmd.Parameters.ContainsKey('Wait') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            Skip-IfMissing 'Restore-PveContainerSnapshot'
            { Restore-PveContainerSnapshot -Node 'pve-node1' -VmId 100 -Name 'snap1' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
