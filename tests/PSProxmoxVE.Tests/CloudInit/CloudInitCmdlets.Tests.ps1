#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for cloud-init cmdlets:
        Get-PveCloudInitConfig, Set-PveCloudInitConfig, Invoke-PveCloudInitRegenerate.

    All tests are fully offline — no live Proxmox VE target is required.

    Cloud-Init cmdlets operate on an existing QEMU VM's cloud-init drive.
    The configuration fields map to the PveCloudInitConfig model:
        CiUser, CiPassword, SshKeys, IpConfig0..3, Nameserver,
        Searchdomain, CiCustom.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Get-PveCloudInitConfig
# ---------------------------------------------------------------------------
Describe 'Get-PveCloudInitConfig' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveCloudInitConfig' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveCloudInitConfig -Node 'pve-node1' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveCloudInitConfig
# ---------------------------------------------------------------------------
Describe 'Set-PveCloudInitConfig' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PveCloudInitConfig' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            { Set-PveCloudInitConfig -Node 'pve-node1' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Invoke-PveCloudInitRegenerate
# ---------------------------------------------------------------------------
Describe 'Invoke-PveCloudInitRegenerate' {
    BeforeAll { $script:Cmd = Get-Command 'Invoke-PveCloudInitRegenerate' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            { Invoke-PveCloudInitRegenerate -Node 'pve-node1' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
