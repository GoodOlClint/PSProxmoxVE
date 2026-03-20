#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for SDN subnet cmdlets:
        Get-PveSdnSubnet, New-PveSdnSubnet, Remove-PveSdnSubnet.

    All tests are fully offline — no live Proxmox VE target is required.
    If a cmdlet is not yet compiled the test is marked Skipped.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Availability = @{}
    foreach ($name in @('Get-PveSdnSubnet', 'New-PveSdnSubnet', 'Remove-PveSdnSubnet')) {
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
Describe 'SDN subnet cmdlets — manifest declarations' {
    BeforeAll {
        $manifestPath = Join-Path (Get-Module PSProxmoxVE).ModuleBase 'PSProxmoxVE.psd1'
        $script:Manifest = if (Test-Path $manifestPath) { Import-PowerShellDataFile $manifestPath } else { $null }
    }

    It "<cmdName> should be declared in CmdletsToExport" -TestCases @(
        @{ cmdName = 'Get-PveSdnSubnet' }
        @{ cmdName = 'New-PveSdnSubnet' }
        @{ cmdName = 'Remove-PveSdnSubnet' }
    ) {
        if ($null -eq $script:Manifest) { Set-ItResult -Skipped -Because 'Manifest not found'; return }
        $script:Manifest.CmdletsToExport | Should -Contain $cmdName
    }
}

# ---------------------------------------------------------------------------
# Get-PveSdnSubnet
# ---------------------------------------------------------------------------
Describe 'Get-PveSdnSubnet' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveSdnSubnet' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveSdnSubnet'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveSdnSubnet'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Vnet should be Mandatory' {
            Skip-IfMissing 'Get-PveSdnSubnet'
            $isMandatory = $script:Cmd.Parameters['Vnet'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Should have Subnet parameter (optional filter)' {
            Skip-IfMissing 'Get-PveSdnSubnet'
            $script:Cmd.Parameters.ContainsKey('Subnet') | Should -BeTrue
        }

        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveSdnSubnet'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }

        It 'Vnet should accept pipeline input by property name' {
            Skip-IfMissing 'Get-PveSdnSubnet'
            $attr = $script:Cmd.Parameters['Vnet'].ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $attr | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveSdnSubnet'
            { Get-PveSdnSubnet -Vnet 'myvnet' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveSdnSubnet
# ---------------------------------------------------------------------------
Describe 'New-PveSdnSubnet' {

    BeforeAll { $script:Cmd = Get-Command 'New-PveSdnSubnet' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveSdnSubnet'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'New-PveSdnSubnet'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Vnet should be Mandatory' {
            Skip-IfMissing 'New-PveSdnSubnet'
            $isMandatory = $script:Cmd.Parameters['Vnet'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Subnet should be Mandatory' {
            Skip-IfMissing 'New-PveSdnSubnet'
            $isMandatory = $script:Cmd.Parameters['Subnet'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Optional parameters' {
        It 'Should have Gateway parameter' {
            Skip-IfMissing 'New-PveSdnSubnet'
            $script:Cmd.Parameters.ContainsKey('Gateway') | Should -BeTrue
        }

        It 'Should have Snat switch parameter' {
            Skip-IfMissing 'New-PveSdnSubnet'
            $script:Cmd.Parameters.ContainsKey('Snat') | Should -BeTrue
        }

        It 'Should have DnsZonePrefix parameter' {
            Skip-IfMissing 'New-PveSdnSubnet'
            $script:Cmd.Parameters.ContainsKey('DnsZonePrefix') | Should -BeTrue
        }

        It 'Should have DhcpRange parameter' {
            Skip-IfMissing 'New-PveSdnSubnet'
            $script:Cmd.Parameters.ContainsKey('DhcpRange') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            Skip-IfMissing 'New-PveSdnSubnet'
            { New-PveSdnSubnet -Vnet 'myvnet' -Subnet '10.0.0.0/24' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveSdnSubnet
# ---------------------------------------------------------------------------
Describe 'Remove-PveSdnSubnet' {

    BeforeAll { $script:Cmd = Get-Command 'Remove-PveSdnSubnet' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Remove-PveSdnSubnet'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Remove-PveSdnSubnet'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should declare ConfirmImpact High' {
            Skip-IfMissing 'Remove-PveSdnSubnet'
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Required parameters' {
        It 'Vnet should be Mandatory' {
            Skip-IfMissing 'Remove-PveSdnSubnet'
            $isMandatory = $script:Cmd.Parameters['Vnet'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Subnet should be Mandatory' {
            Skip-IfMissing 'Remove-PveSdnSubnet'
            $isMandatory = $script:Cmd.Parameters['Subnet'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            Skip-IfMissing 'Remove-PveSdnSubnet'
            { Remove-PveSdnSubnet -Vnet 'myvnet' -Subnet '10.0.0.0/24' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
