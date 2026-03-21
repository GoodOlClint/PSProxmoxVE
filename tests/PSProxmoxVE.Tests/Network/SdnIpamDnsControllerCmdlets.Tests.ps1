#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for SDN IPAM, DNS, and controller cmdlets:
        Get-PveSdnIpam, New-PveSdnIpam, Remove-PveSdnIpam,
        Get-PveSdnDns, New-PveSdnDns, Remove-PveSdnDns,
        Get-PveSdnController, New-PveSdnController, Remove-PveSdnController.

    All tests are fully offline — no live Proxmox VE target is required.
    If a cmdlet is not yet compiled the test is marked Skipped.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Availability = @{}
    foreach ($name in @(
        'Get-PveSdnIpam', 'New-PveSdnIpam', 'Remove-PveSdnIpam',
        'Get-PveSdnDns', 'New-PveSdnDns', 'Remove-PveSdnDns',
        'Get-PveSdnController', 'New-PveSdnController', 'Remove-PveSdnController'
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
Describe 'SDN IPAM/DNS/Controller cmdlets — manifest declarations' {
    BeforeAll {
        $manifestPath = Join-Path (Get-Module PSProxmoxVE).ModuleBase 'PSProxmoxVE.psd1'
        $script:Manifest = if (Test-Path $manifestPath) { Import-PowerShellDataFile $manifestPath } else { $null }
    }

    It "<cmdName> should be declared in CmdletsToExport" -TestCases @(
        @{ cmdName = 'Get-PveSdnIpam' }
        @{ cmdName = 'New-PveSdnIpam' }
        @{ cmdName = 'Remove-PveSdnIpam' }
        @{ cmdName = 'Get-PveSdnDns' }
        @{ cmdName = 'New-PveSdnDns' }
        @{ cmdName = 'Remove-PveSdnDns' }
        @{ cmdName = 'Get-PveSdnController' }
        @{ cmdName = 'New-PveSdnController' }
        @{ cmdName = 'Remove-PveSdnController' }
    ) {
        if ($null -eq $script:Manifest) { Set-ItResult -Skipped -Because 'Manifest not found'; return }
        $script:Manifest.CmdletsToExport | Should -Contain $cmdName
    }
}

# ===========================================================================
# IPAM cmdlets
# ===========================================================================

# ---------------------------------------------------------------------------
# Get-PveSdnIpam
# ---------------------------------------------------------------------------
Describe 'Get-PveSdnIpam' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveSdnIpam' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveSdnIpam'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveSdnIpam'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have optional Ipam parameter' {
            Skip-IfMissing 'Get-PveSdnIpam'
            $script:Cmd.Parameters.ContainsKey('Ipam') | Should -BeTrue
        }

        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveSdnIpam'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveSdnIpam'
            { Get-PveSdnIpam -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveSdnIpam
# ---------------------------------------------------------------------------
Describe 'New-PveSdnIpam' {

    BeforeAll { $script:Cmd = Get-Command 'New-PveSdnIpam' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveSdnIpam'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'New-PveSdnIpam'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Ipam should be Mandatory' {
            Skip-IfMissing 'New-PveSdnIpam'
            $isMandatory = $script:Cmd.Parameters['Ipam'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Type should be Mandatory' {
            Skip-IfMissing 'New-PveSdnIpam'
            $isMandatory = $script:Cmd.Parameters['Type'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'New-PveSdnIpam'
            { New-PveSdnIpam -Ipam 'testipam' -Type 'pve' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveSdnIpam
# ---------------------------------------------------------------------------
Describe 'Remove-PveSdnIpam' {

    BeforeAll { $script:Cmd = Get-Command 'Remove-PveSdnIpam' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Remove-PveSdnIpam'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Remove-PveSdnIpam'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should declare ConfirmImpact High' {
            Skip-IfMissing 'Remove-PveSdnIpam'
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Required parameters' {
        It 'Ipam should be Mandatory' {
            Skip-IfMissing 'Remove-PveSdnIpam'
            $isMandatory = $script:Cmd.Parameters['Ipam'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Remove-PveSdnIpam'
            { Remove-PveSdnIpam -Ipam 'testipam' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ===========================================================================
# DNS cmdlets
# ===========================================================================

# ---------------------------------------------------------------------------
# Get-PveSdnDns
# ---------------------------------------------------------------------------
Describe 'Get-PveSdnDns' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveSdnDns' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveSdnDns'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveSdnDns'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have optional Dns parameter' {
            Skip-IfMissing 'Get-PveSdnDns'
            $script:Cmd.Parameters.ContainsKey('Dns') | Should -BeTrue
        }

        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveSdnDns'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveSdnDns'
            { Get-PveSdnDns -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveSdnDns
# ---------------------------------------------------------------------------
Describe 'New-PveSdnDns' {

    BeforeAll { $script:Cmd = Get-Command 'New-PveSdnDns' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveSdnDns'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'New-PveSdnDns'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Dns should be Mandatory' {
            Skip-IfMissing 'New-PveSdnDns'
            $isMandatory = $script:Cmd.Parameters['Dns'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Type should be Mandatory' {
            Skip-IfMissing 'New-PveSdnDns'
            $isMandatory = $script:Cmd.Parameters['Type'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'New-PveSdnDns'
            { New-PveSdnDns -Dns 'testdns' -Type 'powerdns' -Url 'http://localhost:8081' -Key 'testkey' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveSdnDns
# ---------------------------------------------------------------------------
Describe 'Remove-PveSdnDns' {

    BeforeAll { $script:Cmd = Get-Command 'Remove-PveSdnDns' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Remove-PveSdnDns'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Remove-PveSdnDns'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should declare ConfirmImpact High' {
            Skip-IfMissing 'Remove-PveSdnDns'
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Required parameters' {
        It 'Dns should be Mandatory' {
            Skip-IfMissing 'Remove-PveSdnDns'
            $isMandatory = $script:Cmd.Parameters['Dns'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Remove-PveSdnDns'
            { Remove-PveSdnDns -Dns 'testdns' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ===========================================================================
# Controller cmdlets
# ===========================================================================

# ---------------------------------------------------------------------------
# Get-PveSdnController
# ---------------------------------------------------------------------------
Describe 'Get-PveSdnController' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveSdnController' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveSdnController'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveSdnController'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have optional Controller parameter' {
            Skip-IfMissing 'Get-PveSdnController'
            $script:Cmd.Parameters.ContainsKey('Controller') | Should -BeTrue
        }

        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveSdnController'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveSdnController'
            { Get-PveSdnController -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveSdnController
# ---------------------------------------------------------------------------
Describe 'New-PveSdnController' {

    BeforeAll { $script:Cmd = Get-Command 'New-PveSdnController' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveSdnController'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'New-PveSdnController'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Controller should be Mandatory' {
            Skip-IfMissing 'New-PveSdnController'
            $isMandatory = $script:Cmd.Parameters['Controller'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Type should be Mandatory' {
            Skip-IfMissing 'New-PveSdnController'
            $isMandatory = $script:Cmd.Parameters['Type'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'New-PveSdnController'
            { New-PveSdnController -Controller 'testctrl' -Type 'evpn' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveSdnController
# ---------------------------------------------------------------------------
Describe 'Remove-PveSdnController' {

    BeforeAll { $script:Cmd = Get-Command 'Remove-PveSdnController' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Remove-PveSdnController'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Remove-PveSdnController'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should declare ConfirmImpact High' {
            Skip-IfMissing 'Remove-PveSdnController'
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Required parameters' {
        It 'Controller should be Mandatory' {
            Skip-IfMissing 'Remove-PveSdnController'
            $isMandatory = $script:Cmd.Parameters['Controller'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Remove-PveSdnController'
            { Remove-PveSdnController -Controller 'testctrl' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
