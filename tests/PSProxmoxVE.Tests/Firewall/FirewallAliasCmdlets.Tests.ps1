#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for firewall alias cmdlets:
        Get-PveFirewallAlias, New-PveFirewallAlias, Set-PveFirewallAlias, Remove-PveFirewallAlias.

    All tests are fully offline — no live Proxmox VE target is required.
    If a cmdlet is not yet compiled the test is marked Skipped.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Availability = @{}
    foreach ($name in @('Get-PveFirewallAlias', 'New-PveFirewallAlias', 'Set-PveFirewallAlias', 'Remove-PveFirewallAlias')) {
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
Describe 'Firewall alias cmdlets — manifest declarations' {
    BeforeAll {
        $manifestPath = Join-Path (Get-Module PSProxmoxVE).ModuleBase 'PSProxmoxVE.psd1'
        $script:Manifest = if (Test-Path $manifestPath) { Import-PowerShellDataFile $manifestPath } else { $null }
    }

    It "<cmdName> should be declared in CmdletsToExport" -TestCases @(
        @{ cmdName = 'Get-PveFirewallAlias' }
        @{ cmdName = 'New-PveFirewallAlias' }
        @{ cmdName = 'Set-PveFirewallAlias' }
        @{ cmdName = 'Remove-PveFirewallAlias' }
    ) {
        if ($null -eq $script:Manifest) { Set-ItResult -Skipped -Because 'Manifest not found'; return }
        $script:Manifest.CmdletsToExport | Should -Contain $cmdName
    }
}

# ---------------------------------------------------------------------------
# Get-PveFirewallAlias
# ---------------------------------------------------------------------------
Describe 'Get-PveFirewallAlias' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveFirewallAlias' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveFirewallAlias'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveFirewallAlias'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have optional Name parameter' {
            Skip-IfMissing 'Get-PveFirewallAlias'
            $script:Cmd.Parameters.ContainsKey('Name') | Should -BeTrue
        }

        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveFirewallAlias'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveFirewallAlias'
            { Get-PveFirewallAlias -Level Cluster -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveFirewallAlias
# ---------------------------------------------------------------------------
Describe 'New-PveFirewallAlias' {

    BeforeAll { $script:Cmd = Get-Command 'New-PveFirewallAlias' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveFirewallAlias'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'New-PveFirewallAlias'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Name should be Mandatory' {
            Skip-IfMissing 'New-PveFirewallAlias'
            $isMandatory = $script:Cmd.Parameters['Name'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Cidr should be Mandatory' {
            Skip-IfMissing 'New-PveFirewallAlias'
            $isMandatory = $script:Cmd.Parameters['Cidr'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Optional parameters' {
        It 'Should have Comment parameter' {
            Skip-IfMissing 'New-PveFirewallAlias'
            $script:Cmd.Parameters.ContainsKey('Comment') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'New-PveFirewallAlias'
            { New-PveFirewallAlias -Name 'testalias' -Cidr '10.0.0.0/24' -Level Cluster -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveFirewallAlias
# ---------------------------------------------------------------------------
Describe 'Set-PveFirewallAlias' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PveFirewallAlias' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PveFirewallAlias'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Set-PveFirewallAlias'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Name should be Mandatory' {
            Skip-IfMissing 'Set-PveFirewallAlias'
            $isMandatory = $script:Cmd.Parameters['Name'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Should have optional Cidr parameter' {
            Skip-IfMissing 'Set-PveFirewallAlias'
            $script:Cmd.Parameters.ContainsKey('Cidr') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Set-PveFirewallAlias'
            { Set-PveFirewallAlias -Name 'testalias' -Cidr '10.0.0.0/24' -Level Cluster -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveFirewallAlias
# ---------------------------------------------------------------------------
Describe 'Remove-PveFirewallAlias' {

    BeforeAll { $script:Cmd = Get-Command 'Remove-PveFirewallAlias' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Remove-PveFirewallAlias'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Remove-PveFirewallAlias'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should declare ConfirmImpact High' {
            Skip-IfMissing 'Remove-PveFirewallAlias'
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Required parameters' {
        It 'Name should be Mandatory' {
            Skip-IfMissing 'Remove-PveFirewallAlias'
            $isMandatory = $script:Cmd.Parameters['Name'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Remove-PveFirewallAlias'
            { Remove-PveFirewallAlias -Name 'testalias' -Confirm:$false -Level Cluster -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
