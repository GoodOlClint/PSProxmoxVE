#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for firewall rule cmdlets:
        Get-PveFirewallRule, New-PveFirewallRule, Set-PveFirewallRule, Remove-PveFirewallRule.

    All tests are fully offline — no live Proxmox VE target is required.
    If a cmdlet is not yet compiled the test is marked Skipped.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Availability = @{}
    foreach ($name in @('Get-PveFirewallRule', 'New-PveFirewallRule', 'Set-PveFirewallRule', 'Remove-PveFirewallRule')) {
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
Describe 'Firewall rule cmdlets — manifest declarations' {
    BeforeAll {
        $manifestPath = Join-Path (Get-Module PSProxmoxVE).ModuleBase 'PSProxmoxVE.psd1'
        $script:Manifest = if (Test-Path $manifestPath) { Import-PowerShellDataFile $manifestPath } else { $null }
    }

    It "<cmdName> should be declared in CmdletsToExport" -TestCases @(
        @{ cmdName = 'Get-PveFirewallRule' }
        @{ cmdName = 'New-PveFirewallRule' }
        @{ cmdName = 'Set-PveFirewallRule' }
        @{ cmdName = 'Remove-PveFirewallRule' }
    ) {
        if ($null -eq $script:Manifest) { Set-ItResult -Skipped -Because 'Manifest not found'; return }
        $script:Manifest.CmdletsToExport | Should -Contain $cmdName
    }
}

# ---------------------------------------------------------------------------
# Get-PveFirewallRule
# ---------------------------------------------------------------------------
Describe 'Get-PveFirewallRule' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveFirewallRule' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveFirewallRule'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveFirewallRule'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have mandatory Level parameter with ValidateSet' {
            Skip-IfMissing 'Get-PveFirewallRule'
            $param = $script:Cmd.Parameters['Level']
            $param | Should -Not -BeNullOrEmpty
            $isMandatory = $param.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
            $validateSet = $param.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $validateSet | Should -Not -BeNullOrEmpty
        }

        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveFirewallRule'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveFirewallRule'
            { Get-PveFirewallRule -Level 'cluster' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveFirewallRule
# ---------------------------------------------------------------------------
Describe 'New-PveFirewallRule' {

    BeforeAll { $script:Cmd = Get-Command 'New-PveFirewallRule' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveFirewallRule'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'New-PveFirewallRule'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Parameter metadata' {
        It 'Should have mandatory Level parameter with ValidateSet' {
            Skip-IfMissing 'New-PveFirewallRule'
            $param = $script:Cmd.Parameters['Level']
            $param | Should -Not -BeNullOrEmpty
            $isMandatory = $param.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
            $validateSet = $param.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $validateSet | Should -Not -BeNullOrEmpty
        }

        It 'Should have Action parameter' {
            Skip-IfMissing 'New-PveFirewallRule'
            $script:Cmd.Parameters.ContainsKey('Action') | Should -BeTrue
        }

        It 'Should have Type parameter' {
            Skip-IfMissing 'New-PveFirewallRule'
            $script:Cmd.Parameters.ContainsKey('Type') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'New-PveFirewallRule'
            { New-PveFirewallRule -Level 'cluster' -Action 'ACCEPT' -Type 'in' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveFirewallRule
# ---------------------------------------------------------------------------
Describe 'Set-PveFirewallRule' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PveFirewallRule' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PveFirewallRule'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Set-PveFirewallRule'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Parameter metadata' {
        It 'Should have mandatory Level parameter with ValidateSet' {
            Skip-IfMissing 'Set-PveFirewallRule'
            $param = $script:Cmd.Parameters['Level']
            $param | Should -Not -BeNullOrEmpty
            $isMandatory = $param.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
            $validateSet = $param.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $validateSet | Should -Not -BeNullOrEmpty
        }

        It 'Should have Position parameter' {
            Skip-IfMissing 'Set-PveFirewallRule'
            $script:Cmd.Parameters.ContainsKey('Position') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Set-PveFirewallRule'
            { Set-PveFirewallRule -Level 'cluster' -Position 0 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveFirewallRule
# ---------------------------------------------------------------------------
Describe 'Remove-PveFirewallRule' {

    BeforeAll { $script:Cmd = Get-Command 'Remove-PveFirewallRule' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Remove-PveFirewallRule'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Remove-PveFirewallRule'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should declare ConfirmImpact High' {
            Skip-IfMissing 'Remove-PveFirewallRule'
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Parameter metadata' {
        It 'Should have mandatory Level parameter with ValidateSet' {
            Skip-IfMissing 'Remove-PveFirewallRule'
            $param = $script:Cmd.Parameters['Level']
            $param | Should -Not -BeNullOrEmpty
            $isMandatory = $param.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
            $validateSet = $param.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $validateSet | Should -Not -BeNullOrEmpty
        }

        It 'Should have Position parameter' {
            Skip-IfMissing 'Remove-PveFirewallRule'
            $script:Cmd.Parameters.ContainsKey('Position') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Remove-PveFirewallRule'
            { Remove-PveFirewallRule -Level 'cluster' -Position 0 -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
