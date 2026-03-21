#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for firewall IP set cmdlets:
        Get-PveFirewallIpSet, New-PveFirewallIpSet, Remove-PveFirewallIpSet,
        Get-PveFirewallIpSetEntry, New-PveFirewallIpSetEntry, Set-PveFirewallIpSetEntry,
        Remove-PveFirewallIpSetEntry.

    All tests are fully offline — no live Proxmox VE target is required.
    If a cmdlet is not yet compiled the test is marked Skipped.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Availability = @{}
    foreach ($name in @(
        'Get-PveFirewallIpSet', 'New-PveFirewallIpSet', 'Remove-PveFirewallIpSet',
        'Get-PveFirewallIpSetEntry', 'New-PveFirewallIpSetEntry', 'Set-PveFirewallIpSetEntry',
        'Remove-PveFirewallIpSetEntry'
    )) {
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
Describe 'Firewall IP set cmdlets — manifest declarations' {
    BeforeAll {
        $manifestPath = Join-Path (Get-Module PSProxmoxVE).ModuleBase 'PSProxmoxVE.psd1'
        $script:Manifest = if (Test-Path $manifestPath) { Import-PowerShellDataFile $manifestPath } else { $null }
    }

    It "<cmdName> should be declared in CmdletsToExport" -TestCases @(
        @{ cmdName = 'Get-PveFirewallIpSet' }
        @{ cmdName = 'New-PveFirewallIpSet' }
        @{ cmdName = 'Remove-PveFirewallIpSet' }
        @{ cmdName = 'Get-PveFirewallIpSetEntry' }
        @{ cmdName = 'New-PveFirewallIpSetEntry' }
        @{ cmdName = 'Set-PveFirewallIpSetEntry' }
        @{ cmdName = 'Remove-PveFirewallIpSetEntry' }
    ) {
        if ($null -eq $script:Manifest) { Set-ItResult -Skipped -Because 'Manifest not found'; return }
        $script:Manifest.CmdletsToExport | Should -Contain $cmdName
    }
}

# ---------------------------------------------------------------------------
# Get-PveFirewallIpSet
# ---------------------------------------------------------------------------
Describe 'Get-PveFirewallIpSet' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveFirewallIpSet' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveFirewallIpSet'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveFirewallIpSet'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have optional Name parameter' {
            Skip-IfMissing 'Get-PveFirewallIpSet'
            $script:Cmd.Parameters.ContainsKey('Name') | Should -BeTrue
        }

        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveFirewallIpSet'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveFirewallIpSet'
            { Get-PveFirewallIpSet -Level Cluster -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveFirewallIpSet
# ---------------------------------------------------------------------------
Describe 'New-PveFirewallIpSet' {

    BeforeAll { $script:Cmd = Get-Command 'New-PveFirewallIpSet' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveFirewallIpSet'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'New-PveFirewallIpSet'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Name should be Mandatory' {
            Skip-IfMissing 'New-PveFirewallIpSet'
            $isMandatory = $script:Cmd.Parameters['Name'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Optional parameters' {
        It 'Should have Comment parameter' {
            Skip-IfMissing 'New-PveFirewallIpSet'
            $script:Cmd.Parameters.ContainsKey('Comment') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'New-PveFirewallIpSet'
            { New-PveFirewallIpSet -Name 'testipset' -Level Cluster -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveFirewallIpSet
# ---------------------------------------------------------------------------
Describe 'Remove-PveFirewallIpSet' {

    BeforeAll { $script:Cmd = Get-Command 'Remove-PveFirewallIpSet' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Remove-PveFirewallIpSet'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Remove-PveFirewallIpSet'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should declare ConfirmImpact High' {
            Skip-IfMissing 'Remove-PveFirewallIpSet'
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Required parameters' {
        It 'Name should be Mandatory' {
            Skip-IfMissing 'Remove-PveFirewallIpSet'
            $isMandatory = $script:Cmd.Parameters['Name'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Remove-PveFirewallIpSet'
            { Remove-PveFirewallIpSet -Name 'testipset' -Confirm:$false -Level Cluster -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveFirewallIpSetEntry
# ---------------------------------------------------------------------------
Describe 'Get-PveFirewallIpSetEntry' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveFirewallIpSetEntry' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveFirewallIpSetEntry'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveFirewallIpSetEntry'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Required parameters' {
        It 'Name should be Mandatory' {
            Skip-IfMissing 'Get-PveFirewallIpSetEntry'
            $isMandatory = $script:Cmd.Parameters['Name'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Parameter metadata' {
        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveFirewallIpSetEntry'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveFirewallIpSetEntry'
            { Get-PveFirewallIpSetEntry -Name 'testipset' -Level Cluster -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveFirewallIpSetEntry
# ---------------------------------------------------------------------------
Describe 'New-PveFirewallIpSetEntry' {

    BeforeAll { $script:Cmd = Get-Command 'New-PveFirewallIpSetEntry' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveFirewallIpSetEntry'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'New-PveFirewallIpSetEntry'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Name should be Mandatory' {
            Skip-IfMissing 'New-PveFirewallIpSetEntry'
            $isMandatory = $script:Cmd.Parameters['Name'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Cidr should be Mandatory' {
            Skip-IfMissing 'New-PveFirewallIpSetEntry'
            $isMandatory = $script:Cmd.Parameters['Cidr'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'New-PveFirewallIpSetEntry'
            { New-PveFirewallIpSetEntry -Name 'testipset' -Cidr '10.0.0.1' -Level Cluster -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveFirewallIpSetEntry
# ---------------------------------------------------------------------------
Describe 'Set-PveFirewallIpSetEntry' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PveFirewallIpSetEntry' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PveFirewallIpSetEntry'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Set-PveFirewallIpSetEntry'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Name should be Mandatory' {
            Skip-IfMissing 'Set-PveFirewallIpSetEntry'
            $isMandatory = $script:Cmd.Parameters['Name'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Cidr should be Mandatory' {
            Skip-IfMissing 'Set-PveFirewallIpSetEntry'
            $isMandatory = $script:Cmd.Parameters['Cidr'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Set-PveFirewallIpSetEntry'
            { Set-PveFirewallIpSetEntry -Name 'testipset' -Cidr '10.0.0.1' -Level Cluster -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveFirewallIpSetEntry
# ---------------------------------------------------------------------------
Describe 'Remove-PveFirewallIpSetEntry' {

    BeforeAll { $script:Cmd = Get-Command 'Remove-PveFirewallIpSetEntry' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Remove-PveFirewallIpSetEntry'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Remove-PveFirewallIpSetEntry'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should declare ConfirmImpact High' {
            Skip-IfMissing 'Remove-PveFirewallIpSetEntry'
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Required parameters' {
        It 'Name should be Mandatory' {
            Skip-IfMissing 'Remove-PveFirewallIpSetEntry'
            $isMandatory = $script:Cmd.Parameters['Name'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Cidr should be Mandatory' {
            Skip-IfMissing 'Remove-PveFirewallIpSetEntry'
            $isMandatory = $script:Cmd.Parameters['Cidr'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Remove-PveFirewallIpSetEntry'
            { Remove-PveFirewallIpSetEntry -Name 'testipset' -Cidr '10.0.0.1' -Confirm:$false -Level Cluster -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
