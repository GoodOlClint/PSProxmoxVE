#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for network cmdlets:
        Get-PveNetwork, New-PveNetwork, Set-PveNetwork,
        Remove-PveNetwork, Invoke-PveNetworkApply.

    All tests are fully offline — no live Proxmox VE target is required.
    If a cmdlet is not yet compiled the test is marked Skipped.
#>

BeforeAll {
    $moduleRoot = Resolve-Path (Join-Path $PSScriptRoot '../../../src/PSProxmoxVE')
    $dllCandidates = @(
        Join-Path $moduleRoot 'bin/Debug/net8.0/PSProxmoxVE.dll'
        Join-Path $moduleRoot 'bin/Release/net8.0/PSProxmoxVE.dll'
        Join-Path $moduleRoot 'bin/Debug/net48/PSProxmoxVE.dll'
        Join-Path $moduleRoot 'bin/Release/net48/PSProxmoxVE.dll'
    )

    $script:ModuleDll = $dllCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

    if ($null -eq $script:ModuleDll) {
        throw "PSProxmoxVE.dll not found. Build the project before running Pester tests."
    }

    Import-Module $script:ModuleDll -Force -ErrorAction Stop

    $script:Availability = @{}
    foreach ($name in @('Get-PveNetwork', 'New-PveNetwork', 'Set-PveNetwork',
                         'Remove-PveNetwork', 'Invoke-PveNetworkApply')) {
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
Describe 'Network cmdlets — manifest declarations' {
    BeforeAll {
        $manifestPath = Join-Path $moduleRoot 'PSProxmoxVE.psd1'
        $script:Manifest = if (Test-Path $manifestPath) { Import-PowerShellDataFile $manifestPath } else { $null }
    }

    foreach ($cmdName in @('Get-PveNetwork', 'New-PveNetwork', 'Set-PveNetwork',
                            'Remove-PveNetwork', 'Invoke-PveNetworkApply')) {
        It "$cmdName should be declared in CmdletsToExport" {
            if ($null -eq $script:Manifest) { Set-ItResult -Skipped -Because 'Manifest not found'; return }
            $script:Manifest.CmdletsToExport | Should -Contain $cmdName
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveNetwork
# ---------------------------------------------------------------------------
Describe 'Get-PveNetwork' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveNetwork' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveNetwork'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveNetwork'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have Node parameter (Mandatory — network is always node-specific)' {
            Skip-IfMissing 'Get-PveNetwork'
            $script:Cmd.Parameters.ContainsKey('Node') | Should -BeTrue
        }

        It 'Should have Iface parameter (optional filter)' {
            Skip-IfMissing 'Get-PveNetwork'
            $script:Cmd.Parameters.ContainsKey('Iface') | Should -BeTrue
        }

        It 'Should have Type parameter (optional filter by interface type)' {
            Skip-IfMissing 'Get-PveNetwork'
            $script:Cmd.Parameters.ContainsKey('Type') | Should -BeTrue
        }

        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveNetwork'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveNetwork'
            { Get-PveNetwork -Node 'pve-node1' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveNetwork
# ---------------------------------------------------------------------------
Describe 'New-PveNetwork' {

    BeforeAll { $script:Cmd = Get-Command 'New-PveNetwork' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveNetwork'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'New-PveNetwork'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'New-PveNetwork'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Iface should be Mandatory' {
            Skip-IfMissing 'New-PveNetwork'
            $isMandatory = $script:Cmd.Parameters['Iface'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Type should be Mandatory' {
            Skip-IfMissing 'New-PveNetwork'
            $isMandatory = $script:Cmd.Parameters['Type'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveNetwork
# ---------------------------------------------------------------------------
Describe 'Set-PveNetwork' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PveNetwork' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PveNetwork'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Set-PveNetwork'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Set-PveNetwork'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Iface should be Mandatory' {
            Skip-IfMissing 'Set-PveNetwork'
            $isMandatory = $script:Cmd.Parameters['Iface'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveNetwork
# ---------------------------------------------------------------------------
Describe 'Remove-PveNetwork' {

    BeforeAll { $script:Cmd = Get-Command 'Remove-PveNetwork' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Remove-PveNetwork'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Remove-PveNetwork'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should declare ConfirmImpact High' {
            Skip-IfMissing 'Remove-PveNetwork'
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Remove-PveNetwork'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Iface should be Mandatory' {
            Skip-IfMissing 'Remove-PveNetwork'
            $isMandatory = $script:Cmd.Parameters['Iface'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }
}

# ---------------------------------------------------------------------------
# Invoke-PveNetworkApply
# ---------------------------------------------------------------------------
Describe 'Invoke-PveNetworkApply' {

    BeforeAll { $script:Cmd = Get-Command 'Invoke-PveNetworkApply' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Invoke-PveNetworkApply'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Invoke-PveNetworkApply'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Invoke-PveNetworkApply'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }
}
