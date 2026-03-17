#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for cloud-init cmdlets:
        Get-PveCloudInitConfig, Set-PveCloudInitConfig, Invoke-PveCloudInitRegenerate.

    All tests are fully offline — no live Proxmox VE target is required.
    If a cmdlet is not yet compiled the test is marked Skipped.

    Cloud-Init cmdlets operate on an existing QEMU VM's cloud-init drive.
    The configuration fields map to the PveCloudInitConfig model:
        CiUser, CiPassword, SshKeys, IpConfig0..3, Nameserver,
        Searchdomain, CiCustom.
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
    foreach ($name in @('Get-PveCloudInitConfig', 'Set-PveCloudInitConfig',
                         'Invoke-PveCloudInitRegenerate')) {
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
Describe 'Cloud-Init cmdlets — manifest declarations' {
    BeforeAll {
        $manifestPath = Join-Path $moduleRoot 'PSProxmoxVE.psd1'
        $script:Manifest = if (Test-Path $manifestPath) { Import-PowerShellDataFile $manifestPath } else { $null }
    }

    foreach ($cmdName in @('Get-PveCloudInitConfig', 'Set-PveCloudInitConfig',
                            'Invoke-PveCloudInitRegenerate')) {
        It "$cmdName should be declared in CmdletsToExport" {
            if ($null -eq $script:Manifest) { Set-ItResult -Skipped -Because 'Manifest not found'; return }
            $script:Manifest.CmdletsToExport | Should -Contain $cmdName
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveCloudInitConfig
# ---------------------------------------------------------------------------
Describe 'Get-PveCloudInitConfig' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveCloudInitConfig' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveCloudInitConfig'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveCloudInitConfig'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Get-PveCloudInitConfig'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Get-PveCloudInitConfig'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveCloudInitConfig'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveCloudInitConfig'
            { Get-PveCloudInitConfig -Node 'pve-node1' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveCloudInitConfig
# ---------------------------------------------------------------------------
Describe 'Set-PveCloudInitConfig' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PveCloudInitConfig' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PveCloudInitConfig'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Set-PveCloudInitConfig'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Set-PveCloudInitConfig'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Set-PveCloudInitConfig'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Optional cloud-init configuration parameters' {
        It 'Should have CiUser parameter' {
            Skip-IfMissing 'Set-PveCloudInitConfig'
            $script:Cmd.Parameters.ContainsKey('CiUser') | Should -BeTrue
        }

        It 'Should have CiPassword parameter (SecureString or plain-text)' {
            Skip-IfMissing 'Set-PveCloudInitConfig'
            $hasPassword = $script:Cmd.Parameters.ContainsKey('CiPassword') -or
                           $script:Cmd.Parameters.ContainsKey('Password')
            $hasPassword | Should -BeTrue
        }

        It 'Should have SshKeys parameter' {
            Skip-IfMissing 'Set-PveCloudInitConfig'
            $hasSsh = $script:Cmd.Parameters.ContainsKey('SshKeys') -or
                      $script:Cmd.Parameters.ContainsKey('SshPublicKey')
            $hasSsh | Should -BeTrue
        }

        It 'Should have IpConfig0 parameter' {
            Skip-IfMissing 'Set-PveCloudInitConfig'
            $script:Cmd.Parameters.ContainsKey('IpConfig0') | Should -BeTrue
        }

        It 'Should have Nameserver parameter' {
            Skip-IfMissing 'Set-PveCloudInitConfig'
            $script:Cmd.Parameters.ContainsKey('Nameserver') | Should -BeTrue
        }

        It 'Should have Searchdomain parameter' {
            Skip-IfMissing 'Set-PveCloudInitConfig'
            $script:Cmd.Parameters.ContainsKey('Searchdomain') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            Skip-IfMissing 'Set-PveCloudInitConfig'
            { Set-PveCloudInitConfig -Node 'pve-node1' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Invoke-PveCloudInitRegenerate
# ---------------------------------------------------------------------------
Describe 'Invoke-PveCloudInitRegenerate' {

    BeforeAll { $script:Cmd = Get-Command 'Invoke-PveCloudInitRegenerate' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Invoke-PveCloudInitRegenerate'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Invoke-PveCloudInitRegenerate'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Invoke-PveCloudInitRegenerate'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Invoke-PveCloudInitRegenerate'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Optional parameters' {
        It 'Should have Wait switch parameter' {
            Skip-IfMissing 'Invoke-PveCloudInitRegenerate'
            $script:Cmd.Parameters.ContainsKey('Wait') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            Skip-IfMissing 'Invoke-PveCloudInitRegenerate'
            { Invoke-PveCloudInitRegenerate -Node 'pve-node1' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
