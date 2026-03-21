#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for firewall options/ref cmdlets:
        Get-PveFirewallOptions, Set-PveFirewallOptions, Get-PveFirewallRef.

    All tests are fully offline — no live Proxmox VE target is required.
    If a cmdlet is not yet compiled the test is marked Skipped.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Availability = @{}
    foreach ($name in @('Get-PveFirewallOptions', 'Set-PveFirewallOptions', 'Get-PveFirewallRef')) {
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
Describe 'Firewall options/ref cmdlets — manifest declarations' {
    BeforeAll {
        $manifestPath = Join-Path (Get-Module PSProxmoxVE).ModuleBase 'PSProxmoxVE.psd1'
        $script:Manifest = if (Test-Path $manifestPath) { Import-PowerShellDataFile $manifestPath } else { $null }
    }

    It "<cmdName> should be declared in CmdletsToExport" -TestCases @(
        @{ cmdName = 'Get-PveFirewallOptions' }
        @{ cmdName = 'Set-PveFirewallOptions' }
        @{ cmdName = 'Get-PveFirewallRef' }
    ) {
        if ($null -eq $script:Manifest) { Set-ItResult -Skipped -Because 'Manifest not found'; return }
        $script:Manifest.CmdletsToExport | Should -Contain $cmdName
    }
}

# ---------------------------------------------------------------------------
# Get-PveFirewallOptions
# ---------------------------------------------------------------------------
Describe 'Get-PveFirewallOptions' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveFirewallOptions' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveFirewallOptions'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveFirewallOptions'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have mandatory Level parameter with ValidateSet' {
            Skip-IfMissing 'Get-PveFirewallOptions'
            $param = $script:Cmd.Parameters['Level']
            $param | Should -Not -BeNullOrEmpty
            $isMandatory = $param.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
            $validateSet = $param.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $validateSet | Should -Not -BeNullOrEmpty
        }

        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveFirewallOptions'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveFirewallOptions'
            { Get-PveFirewallOptions -Level 'cluster' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveFirewallOptions
# ---------------------------------------------------------------------------
Describe 'Set-PveFirewallOptions' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PveFirewallOptions' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PveFirewallOptions'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Set-PveFirewallOptions'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Parameter metadata' {
        It 'Should have mandatory Level parameter with ValidateSet' {
            Skip-IfMissing 'Set-PveFirewallOptions'
            $param = $script:Cmd.Parameters['Level']
            $param | Should -Not -BeNullOrEmpty
            $isMandatory = $param.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
            $validateSet = $param.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $validateSet | Should -Not -BeNullOrEmpty
        }

        It 'Should have Enable parameter' {
            Skip-IfMissing 'Set-PveFirewallOptions'
            $script:Cmd.Parameters.ContainsKey('Enable') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Set-PveFirewallOptions'
            { Set-PveFirewallOptions -Level 'cluster' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveFirewallRef
# ---------------------------------------------------------------------------
Describe 'Get-PveFirewallRef' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveFirewallRef' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveFirewallRef'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveFirewallRef'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveFirewallRef'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }

        It 'Should have optional Type parameter' {
            Skip-IfMissing 'Get-PveFirewallRef'
            $script:Cmd.Parameters.ContainsKey('Type') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveFirewallRef'
            { Get-PveFirewallRef -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
