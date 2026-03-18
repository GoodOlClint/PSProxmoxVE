#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for storage cmdlets:
        Get-PveStorage, Get-PveStorageContent, New-PveStorage, Remove-PveStorage.

    All tests are fully offline — no live Proxmox VE target is required.
    If a cmdlet is not yet compiled the test is marked Skipped.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Availability = @{}
    foreach ($name in @('Get-PveStorage', 'Get-PveStorageContent',
                         'New-PveStorage', 'Remove-PveStorage')) {
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
Describe 'Storage cmdlets — manifest declarations' {
    It 'Get-PveStorage should be declared in CmdletsToExport' {
        $manifestPath = Join-Path (Get-Module PSProxmoxVE).ModuleBase 'PSProxmoxVE.psd1'
        if (-not (Test-Path $manifestPath)) { Set-ItResult -Skipped -Because 'Manifest not found'; return }
        $manifest = Import-PowerShellDataFile $manifestPath
        $manifest.CmdletsToExport | Should -Contain 'Get-PveStorage'
    }

    It 'Get-PveStorageContent should be declared in CmdletsToExport' {
        $manifestPath = Join-Path (Get-Module PSProxmoxVE).ModuleBase 'PSProxmoxVE.psd1'
        if (-not (Test-Path $manifestPath)) { Set-ItResult -Skipped -Because 'Manifest not found'; return }
        $manifest = Import-PowerShellDataFile $manifestPath
        $manifest.CmdletsToExport | Should -Contain 'Get-PveStorageContent'
    }
}

# ---------------------------------------------------------------------------
# Get-PveStorage
# ---------------------------------------------------------------------------
Describe 'Get-PveStorage' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveStorage' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveStorage'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveStorage'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have Node parameter' {
            Skip-IfMissing 'Get-PveStorage'
            $script:Cmd.Parameters.ContainsKey('Node') | Should -BeTrue
        }

        It 'Node should not be Mandatory (cluster-wide query when omitted)' {
            Skip-IfMissing 'Get-PveStorage'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }

        It 'Should have Storage parameter (filter by name)' {
            Skip-IfMissing 'Get-PveStorage'
            $script:Cmd.Parameters.ContainsKey('Storage') | Should -BeTrue
        }

        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveStorage'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveStorage'
            { Get-PveStorage -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveStorageContent
# ---------------------------------------------------------------------------
Describe 'Get-PveStorageContent' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveStorageContent' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveStorageContent'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Parameter metadata' {
        It 'Node should be present' {
            Skip-IfMissing 'Get-PveStorageContent'
            $script:Cmd.Parameters.ContainsKey('Node') | Should -BeTrue
        }

        It 'Storage should be present' {
            Skip-IfMissing 'Get-PveStorageContent'
            $script:Cmd.Parameters.ContainsKey('Storage') | Should -BeTrue
        }

        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveStorageContent'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveStorageContent'
            { Get-PveStorageContent -Node 'pve-node1' -Storage 'local' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveStorage
# ---------------------------------------------------------------------------
Describe 'New-PveStorage' {

    BeforeAll { $script:Cmd = Get-Command 'New-PveStorage' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveStorage'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'New-PveStorage'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Storage (name) should be Mandatory' {
            Skip-IfMissing 'New-PveStorage'
            $isMandatory = $script:Cmd.Parameters['Storage'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Type should be Mandatory' {
            Skip-IfMissing 'New-PveStorage'
            $isMandatory = $script:Cmd.Parameters['Type'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveStorage
# ---------------------------------------------------------------------------
Describe 'Remove-PveStorage' {

    BeforeAll { $script:Cmd = Get-Command 'Remove-PveStorage' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Remove-PveStorage'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Remove-PveStorage'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should declare ConfirmImpact High' {
            Skip-IfMissing 'Remove-PveStorage'
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Required parameters' {
        It 'Storage should be Mandatory' {
            Skip-IfMissing 'Remove-PveStorage'
            $isMandatory = $script:Cmd.Parameters['Storage'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }
}
