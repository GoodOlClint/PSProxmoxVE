#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for SDN Set cmdlets and related mutating cmdlets:
        Set-PveSdnZone, Set-PveSdnVnet, Set-PveSdnSubnet, Set-PveSdnController,
        Set-PveSdnIpam, Set-PveSdnDns, Invoke-PveSdnApply, Set-PveRole, Set-PveApiToken.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Set-PveSdnZone
# ---------------------------------------------------------------------------
Describe 'Set-PveSdnZone' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PveSdnZone' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Set-PveSdnZone -Zone 'testzone' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveSdnVnet
# ---------------------------------------------------------------------------
Describe 'Set-PveSdnVnet' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PveSdnVnet' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Set-PveSdnVnet -Vnet 'testvnet' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveSdnSubnet
# ---------------------------------------------------------------------------
Describe 'Set-PveSdnSubnet' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PveSdnSubnet' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Set-PveSdnSubnet -Vnet 'testvnet' -Subnet '10.0.0.0/24' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveSdnController
# ---------------------------------------------------------------------------
Describe 'Set-PveSdnController' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PveSdnController' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Set-PveSdnController -Controller 'testctrl' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveSdnIpam
# ---------------------------------------------------------------------------
Describe 'Set-PveSdnIpam' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PveSdnIpam' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Set-PveSdnIpam -Ipam 'testipam' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveSdnDns
# ---------------------------------------------------------------------------
Describe 'Set-PveSdnDns' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PveSdnDns' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Set-PveSdnDns -Dns 'testdns' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Invoke-PveSdnApply
# ---------------------------------------------------------------------------
Describe 'Invoke-PveSdnApply' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Invoke-PveSdnApply' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Invoke-PveSdnApply -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveRole
# ---------------------------------------------------------------------------
Describe 'Set-PveRole' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PveRole' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Set-PveRole -RoleId 'testrole' -Privileges 'VM.Audit' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveApiToken
# ---------------------------------------------------------------------------
Describe 'Set-PveApiToken' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PveApiToken' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Set-PveApiToken -UserId 'user@pam' -TokenId 'testtoken' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
