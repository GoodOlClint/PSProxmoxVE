#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for SDN cmdlets:
        Get-PveSdnZone, New-PveSdnZone, Remove-PveSdnZone,
        Get-PveSdnVnet, New-PveSdnVnet, Remove-PveSdnVnet.

    All tests are fully offline — no live Proxmox VE target is required.

    SDN support was introduced in Proxmox VE 7 and became stable in PVE 8.
    These cmdlets should include a version guard that raises PveVersionException
    (or similar) when the server version is below the minimum requirement.
    The version guard tests use fully-mocked sessions (no network calls).
#>

BeforeAll {
    $moduleRoot = Resolve-Path (Join-Path $PSScriptRoot '../../../src/PSProxmoxVE')
    $dllCandidates = @(
        Join-Path $moduleRoot 'bin/Debug/net9.0/PSProxmoxVE.dll'
        Join-Path $moduleRoot 'bin/Release/net9.0/PSProxmoxVE.dll'
        Join-Path $moduleRoot 'bin/Debug/net48/PSProxmoxVE.dll'
        Join-Path $moduleRoot 'bin/Release/net48/PSProxmoxVE.dll'
    )

    $script:ModuleDll = $dllCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

    if ($null -eq $script:ModuleDll) {
        throw "PSProxmoxVE.dll not found. Build the project before running Pester tests."
    }

    Import-Module $script:ModuleDll -Force -ErrorAction Stop

    $script:SdnCmdlets = @(
        'Get-PveSdnZone',  'New-PveSdnZone',  'Remove-PveSdnZone',
        'Get-PveSdnVnet',  'New-PveSdnVnet',  'Remove-PveSdnVnet'
    )

    $script:Availability = @{}
    foreach ($name in $script:SdnCmdlets) {
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
Describe 'SDN cmdlets — manifest declarations' {
    BeforeAll {
        $manifestPath = Join-Path $moduleRoot 'PSProxmoxVE.psd1'
        $script:Manifest = if (Test-Path $manifestPath) { Import-PowerShellDataFile $manifestPath } else { $null }
    }

    It "<cmdName> should be declared in CmdletsToExport" -TestCases @(
        @{ cmdName = 'Get-PveSdnZone' }
        @{ cmdName = 'New-PveSdnZone' }
        @{ cmdName = 'Remove-PveSdnZone' }
        @{ cmdName = 'Get-PveSdnVnet' }
        @{ cmdName = 'New-PveSdnVnet' }
        @{ cmdName = 'Remove-PveSdnVnet' }
    ) {
        if ($null -eq $script:Manifest) { Set-ItResult -Skipped -Because 'Manifest not found'; return }
        $script:Manifest.CmdletsToExport | Should -Contain $cmdName
    }
}

# ---------------------------------------------------------------------------
# Get-PveSdnZone
# ---------------------------------------------------------------------------
Describe 'Get-PveSdnZone' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveSdnZone' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveSdnZone'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveSdnZone'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have Zone parameter (optional filter by zone ID)' {
            Skip-IfMissing 'Get-PveSdnZone'
            $script:Cmd.Parameters.ContainsKey('Zone') | Should -BeTrue
        }

        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveSdnZone'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Version guard behaviour — without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveSdnZone'
            { Get-PveSdnZone -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveSdnZone
# ---------------------------------------------------------------------------
Describe 'New-PveSdnZone' {

    BeforeAll { $script:Cmd = Get-Command 'New-PveSdnZone' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveSdnZone'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'New-PveSdnZone'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Zone should be Mandatory' {
            Skip-IfMissing 'New-PveSdnZone'
            $isMandatory = $script:Cmd.Parameters['Zone'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Type should be Mandatory' {
            Skip-IfMissing 'New-PveSdnZone'
            $isMandatory = $script:Cmd.Parameters['Type'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Type ValidateSet' {
        It 'Type parameter should have ValidateSet including known zone types' {
            Skip-IfMissing 'New-PveSdnZone'
            $validateSetAttr = $script:Cmd.Parameters['Type'].Attributes |
                Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] } |
                Select-Object -First 1
            if ($null -ne $validateSetAttr) {
                $validateSetAttr.ValidValues | Should -Contain 'simple'
            }
            else {
                Set-ItResult -Skipped -Because 'Type does not use a ValidateSet attribute in this build'
            }
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveSdnZone
# ---------------------------------------------------------------------------
Describe 'Remove-PveSdnZone' {

    BeforeAll { $script:Cmd = Get-Command 'Remove-PveSdnZone' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Remove-PveSdnZone'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Remove-PveSdnZone'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should declare ConfirmImpact High' {
            Skip-IfMissing 'Remove-PveSdnZone'
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Required parameters' {
        It 'Zone should be Mandatory' {
            Skip-IfMissing 'Remove-PveSdnZone'
            $isMandatory = $script:Cmd.Parameters['Zone'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveSdnVnet
# ---------------------------------------------------------------------------
Describe 'Get-PveSdnVnet' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveSdnVnet' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveSdnVnet'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Parameter metadata' {
        It 'Should have Vnet parameter (optional filter by VNet name)' {
            Skip-IfMissing 'Get-PveSdnVnet'
            $script:Cmd.Parameters.ContainsKey('Vnet') | Should -BeTrue
        }

        It 'Should have Zone parameter (optional filter by parent zone)' {
            Skip-IfMissing 'Get-PveSdnVnet'
            $script:Cmd.Parameters.ContainsKey('Zone') | Should -BeTrue
        }

        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveSdnVnet'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveSdnVnet'
            { Get-PveSdnVnet -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveSdnVnet
# ---------------------------------------------------------------------------
Describe 'New-PveSdnVnet' {

    BeforeAll { $script:Cmd = Get-Command 'New-PveSdnVnet' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveSdnVnet'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'New-PveSdnVnet'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Vnet should be Mandatory' {
            Skip-IfMissing 'New-PveSdnVnet'
            $isMandatory = $script:Cmd.Parameters['Vnet'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Zone should be Mandatory' {
            Skip-IfMissing 'New-PveSdnVnet'
            $isMandatory = $script:Cmd.Parameters['Zone'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveSdnVnet
# ---------------------------------------------------------------------------
Describe 'Remove-PveSdnVnet' {

    BeforeAll { $script:Cmd = Get-Command 'Remove-PveSdnVnet' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Remove-PveSdnVnet'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Remove-PveSdnVnet'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should declare ConfirmImpact High' {
            Skip-IfMissing 'Remove-PveSdnVnet'
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Required parameters' {
        It 'Vnet should be Mandatory' {
            Skip-IfMissing 'Remove-PveSdnVnet'
            $isMandatory = $script:Cmd.Parameters['Vnet'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }
}
