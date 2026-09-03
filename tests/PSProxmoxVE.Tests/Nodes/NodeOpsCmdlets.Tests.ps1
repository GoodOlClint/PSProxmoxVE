#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for node operation cmdlets:
        Get-PveNodeConfig, Set-PveNodeConfig, Get-PveNodeDns, Set-PveNodeDns,
        Start-PveNodeVms, Stop-PveNodeVms.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Get-PveNodeConfig
# ---------------------------------------------------------------------------
Describe 'Get-PveNodeConfig' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveNodeConfig' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveNodeConfig -Node 'pve' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveNodeConfig
# ---------------------------------------------------------------------------
Describe 'Set-PveNodeConfig' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PveNodeConfig' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Set-PveNodeConfig -Node 'pve' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveNodeDns
# ---------------------------------------------------------------------------
Describe 'Get-PveNodeDns' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveNodeDns' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveNodeDns -Node 'pve' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveNodeDns
# ---------------------------------------------------------------------------
Describe 'Set-PveNodeDns' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PveNodeDns' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Set-PveNodeDns -Node 'pve' -Search 'example.com' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Start-PveNodeVms
# ---------------------------------------------------------------------------
Describe 'Start-PveNodeVms' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Start-PveNodeVms' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Start-PveNodeVms -Node 'pve' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Stop-PveNodeVms
# ---------------------------------------------------------------------------
Describe 'Stop-PveNodeVms' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Stop-PveNodeVms' }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
        It 'Should declare ConfirmImpact High' {
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Stop-PveNodeVms -Node 'pve' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
