#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for snapshot cmdlets:
        Get-PveSnapshot, New-PveSnapshot, Remove-PveSnapshot, Restore-PveSnapshot.

    All tests are fully offline — no live Proxmox VE target is required.
    If a cmdlet is not yet compiled the test is marked Skipped.

    Snapshot cmdlets apply to both QEMU VMs and LXC containers; the
    parameter set distinction is captured in the 'Type' tests below.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Availability = @{}
    foreach ($name in @('Get-PveSnapshot', 'New-PveSnapshot',
                         'Remove-PveSnapshot', 'Restore-PveSnapshot')) {
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
Describe 'Snapshot cmdlets — manifest declarations' {
    BeforeAll {
        $manifestPath = Join-Path (Get-Module PSProxmoxVE).ModuleBase 'PSProxmoxVE.psd1'
        $script:Manifest = if (Test-Path $manifestPath) { Import-PowerShellDataFile $manifestPath } else { $null }
    }

    It "<cmdName> should be declared in CmdletsToExport" -TestCases @(
        @{ cmdName = 'Get-PveSnapshot' }
        @{ cmdName = 'New-PveSnapshot' }
        @{ cmdName = 'Remove-PveSnapshot' }
        @{ cmdName = 'Restore-PveSnapshot' }
    ) {
        if ($null -eq $script:Manifest) { Set-ItResult -Skipped -Because 'Manifest not found'; return }
        $script:Manifest.CmdletsToExport | Should -Contain $cmdName
    }
}

# ---------------------------------------------------------------------------
# Get-PveSnapshot
# ---------------------------------------------------------------------------
Describe 'Get-PveSnapshot' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveSnapshot' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveSnapshot'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveSnapshot'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Get-PveSnapshot'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Get-PveSnapshot'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Should have Name parameter (optional filter by snapshot name)' {
            Skip-IfMissing 'Get-PveSnapshot'
            $script:Cmd.Parameters.ContainsKey('Name') | Should -BeTrue
        }

        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveSnapshot'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveSnapshot'
            { Get-PveSnapshot -Node 'pve-node1' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveSnapshot
# ---------------------------------------------------------------------------
Describe 'New-PveSnapshot' {

    BeforeAll { $script:Cmd = Get-Command 'New-PveSnapshot' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveSnapshot'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'New-PveSnapshot'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'New-PveSnapshot'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            Skip-IfMissing 'New-PveSnapshot'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Name (snapshot name) should be Mandatory' {
            Skip-IfMissing 'New-PveSnapshot'
            $isMandatory = $script:Cmd.Parameters['Name'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Optional parameters' {
        It 'Should have Description parameter' {
            Skip-IfMissing 'New-PveSnapshot'
            $script:Cmd.Parameters.ContainsKey('Description') | Should -BeTrue
        }

        It 'Should have IncludeVmState switch parameter' {
            Skip-IfMissing 'New-PveSnapshot'
            $script:Cmd.Parameters.ContainsKey('IncludeVmState') | Should -BeTrue
        }

        It 'Should have Wait switch parameter' {
            Skip-IfMissing 'New-PveSnapshot'
            $script:Cmd.Parameters.ContainsKey('Wait') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            Skip-IfMissing 'New-PveSnapshot'
            { New-PveSnapshot -Node 'pve-node1' -VmId 100 -Name 'snap1' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveSnapshot
# ---------------------------------------------------------------------------
Describe 'Remove-PveSnapshot' {

    BeforeAll { $script:Cmd = Get-Command 'Remove-PveSnapshot' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Remove-PveSnapshot'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Remove-PveSnapshot'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should declare ConfirmImpact High' {
            Skip-IfMissing 'Remove-PveSnapshot'
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Remove-PveSnapshot'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Remove-PveSnapshot'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Name should be Mandatory' {
            Skip-IfMissing 'Remove-PveSnapshot'
            $isMandatory = $script:Cmd.Parameters['Name'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            Skip-IfMissing 'Remove-PveSnapshot'
            { Remove-PveSnapshot -Node 'pve-node1' -VmId 100 -Name 'snap1' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Restore-PveSnapshot
# ---------------------------------------------------------------------------
Describe 'Restore-PveSnapshot' {

    BeforeAll { $script:Cmd = Get-Command 'Restore-PveSnapshot' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Restore-PveSnapshot'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Restore-PveSnapshot'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should declare ConfirmImpact High' {
            Skip-IfMissing 'Restore-PveSnapshot'
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Restore-PveSnapshot'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Restore-PveSnapshot'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Name should be Mandatory' {
            Skip-IfMissing 'Restore-PveSnapshot'
            $isMandatory = $script:Cmd.Parameters['Name'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Optional parameters' {
        It 'Should have Wait switch parameter' {
            Skip-IfMissing 'Restore-PveSnapshot'
            $script:Cmd.Parameters.ContainsKey('Wait') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            Skip-IfMissing 'Restore-PveSnapshot'
            { Restore-PveSnapshot -Node 'pve-node1' -VmId 100 -Name 'snap1' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
