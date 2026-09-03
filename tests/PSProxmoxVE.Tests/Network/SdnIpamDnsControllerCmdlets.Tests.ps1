#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for SDN IPAM, DNS, and controller cmdlets:
        Get-PveSdnIpam, New-PveSdnIpam, Remove-PveSdnIpam,
        Get-PveSdnDns, New-PveSdnDns, Remove-PveSdnDns,
        Get-PveSdnController, New-PveSdnController, Remove-PveSdnController.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ===========================================================================
# IPAM cmdlets
# ===========================================================================

# ---------------------------------------------------------------------------
# Get-PveSdnIpam
# ---------------------------------------------------------------------------
Describe 'Get-PveSdnIpam' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveSdnIpam' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveSdnIpam -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveSdnIpam
# ---------------------------------------------------------------------------
Describe 'New-PveSdnIpam' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveSdnIpam' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { New-PveSdnIpam -Ipam 'testipam' -Type 'pve' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveSdnIpam
# ---------------------------------------------------------------------------
Describe 'Remove-PveSdnIpam' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveSdnIpam' }

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
    BeforeAll { $script:Cmd = Get-Command 'Get-PveSdnDns' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveSdnDns -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveSdnDns
# ---------------------------------------------------------------------------
Describe 'New-PveSdnDns' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveSdnDns' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { New-PveSdnDns -Dns 'testdns' -Type 'powerdns' -Url 'http://localhost:8081' -Key 'testkey' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveSdnDns
# ---------------------------------------------------------------------------
Describe 'Remove-PveSdnDns' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveSdnDns' }

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
    BeforeAll { $script:Cmd = Get-Command 'Get-PveSdnController' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveSdnController -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveSdnController
# ---------------------------------------------------------------------------
Describe 'New-PveSdnController' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveSdnController' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { New-PveSdnController -Controller 'testctrl' -Type 'evpn' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveSdnController
# ---------------------------------------------------------------------------
Describe 'Remove-PveSdnController' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveSdnController' }

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
            { Remove-PveSdnController -Controller 'testctrl' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
