#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for API token cmdlets:
        Get-PveApiToken, New-PveApiToken, Remove-PveApiToken.

    All tests are fully offline — no live Proxmox VE target is required.
    If a cmdlet is not yet compiled the test is marked Skipped.
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

    $allNames = @('Get-PveApiToken', 'New-PveApiToken', 'Remove-PveApiToken')

    $script:Availability = @{}
    foreach ($name in $allNames) {
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
Describe 'API Token cmdlets — manifest declarations' {
    BeforeAll {
        $manifestPath = Join-Path $moduleRoot 'PSProxmoxVE.psd1'
        $script:Manifest = if (Test-Path $manifestPath) { Import-PowerShellDataFile $manifestPath } else { $null }
    }

    It "<cmdName> should be declared in CmdletsToExport" -TestCases @(
        @{ cmdName = 'Get-PveApiToken' }
        @{ cmdName = 'New-PveApiToken' }
        @{ cmdName = 'Remove-PveApiToken' }
    ) {
        if ($null -eq $script:Manifest) { Set-ItResult -Skipped -Because 'Manifest not found'; return }
        $script:Manifest.CmdletsToExport | Should -Contain $cmdName
    }
}

# ---------------------------------------------------------------------------
# Get-PveApiToken
# ---------------------------------------------------------------------------
Describe 'Get-PveApiToken' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveApiToken' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveApiToken'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveApiToken'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have UserId as mandatory parameter' {
            Skip-IfMissing 'Get-PveApiToken'
            $isMandatory = $script:Cmd.Parameters['UserId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'UserId should accept pipeline input by property name' {
            Skip-IfMissing 'Get-PveApiToken'
            $byPropName = $script:Cmd.Parameters['UserId'].ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $byPropName | Should -Not -BeNullOrEmpty
        }

        It 'Should have TokenId as optional filter parameter' {
            Skip-IfMissing 'Get-PveApiToken'
            $script:Cmd.Parameters.ContainsKey('TokenId') | Should -BeTrue
        }

        It 'TokenId should not be Mandatory' {
            Skip-IfMissing 'Get-PveApiToken'
            $isMandatory = $script:Cmd.Parameters['TokenId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }

        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveApiToken'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveApiToken'
            { Get-PveApiToken -UserId 'admin@pam' -ErrorAction Stop } | Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveApiToken
# ---------------------------------------------------------------------------
Describe 'New-PveApiToken' {

    BeforeAll { $script:Cmd = Get-Command 'New-PveApiToken' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveApiToken'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'New-PveApiToken'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'New-PveApiToken'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'WhatIf should not throw even without a session' {
            Skip-IfMissing 'New-PveApiToken'
            { New-PveApiToken -UserId 'admin@pam' -TokenId 'test' -WhatIf } | Should -Not -Throw
        }
    }

    Context 'Required parameters' {
        It 'UserId should be Mandatory' {
            Skip-IfMissing 'New-PveApiToken'
            $isMandatory = $script:Cmd.Parameters['UserId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'UserId should accept pipeline input by property name' {
            Skip-IfMissing 'New-PveApiToken'
            $byPropName = $script:Cmd.Parameters['UserId'].ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $byPropName | Should -Not -BeNullOrEmpty
        }

        It 'TokenId should be Mandatory' {
            Skip-IfMissing 'New-PveApiToken'
            $isMandatory = $script:Cmd.Parameters['TokenId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Optional parameters' {
        It 'Should have Comment parameter' {
            Skip-IfMissing 'New-PveApiToken'
            $script:Cmd.Parameters.ContainsKey('Comment') | Should -BeTrue
        }

        It 'Should have Expire parameter' {
            Skip-IfMissing 'New-PveApiToken'
            $script:Cmd.Parameters.ContainsKey('Expire') | Should -BeTrue
        }

        It 'Should have PrivilegeSeparation switch' {
            Skip-IfMissing 'New-PveApiToken'
            $script:Cmd.Parameters.ContainsKey('PrivilegeSeparation') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'New-PveApiToken'
            { New-PveApiToken -UserId 'admin@pam' -TokenId 'test' -Confirm:$false -ErrorAction Stop } | Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveApiToken
# ---------------------------------------------------------------------------
Describe 'Remove-PveApiToken' {

    BeforeAll { $script:Cmd = Get-Command 'Remove-PveApiToken' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Remove-PveApiToken'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Remove-PveApiToken'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Remove-PveApiToken'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should declare ConfirmImpact High' {
            Skip-IfMissing 'Remove-PveApiToken'
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Required parameters' {
        It 'UserId should be Mandatory' {
            Skip-IfMissing 'Remove-PveApiToken'
            $isMandatory = $script:Cmd.Parameters['UserId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'UserId should accept pipeline input by property name' {
            Skip-IfMissing 'Remove-PveApiToken'
            $byPropName = $script:Cmd.Parameters['UserId'].ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $byPropName | Should -Not -BeNullOrEmpty
        }

        It 'TokenId should be Mandatory' {
            Skip-IfMissing 'Remove-PveApiToken'
            $isMandatory = $script:Cmd.Parameters['TokenId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'TokenId should accept pipeline input by property name' {
            Skip-IfMissing 'Remove-PveApiToken'
            $byPropName = $script:Cmd.Parameters['TokenId'].ParameterSets.Values |
                Where-Object { $_.ValueFromPipelineByPropertyName }
            $byPropName | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Remove-PveApiToken'
            { Remove-PveApiToken -UserId 'admin@pam' -TokenId 'test' -Confirm:$false -ErrorAction Stop } | Should -Throw '*No active Proxmox VE session*'
        }
    }
}
