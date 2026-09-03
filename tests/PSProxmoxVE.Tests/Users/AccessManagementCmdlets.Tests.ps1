#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for access management cmdlets:
        Get-PveGroup, New-PveGroup, Set-PveGroup, Remove-PveGroup,
        Get-PveDomain, New-PveDomain, Set-PveDomain, Remove-PveDomain, Set-PvePassword.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Get-PveGroup
# ---------------------------------------------------------------------------
Describe 'Get-PveGroup' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveGroup' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveGroup -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveGroup
# ---------------------------------------------------------------------------
Describe 'New-PveGroup' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveGroup' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { New-PveGroup -GroupId 'testgroup' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveGroup
# ---------------------------------------------------------------------------
Describe 'Set-PveGroup' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PveGroup' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Set-PveGroup -GroupId 'testgroup' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveGroup
# ---------------------------------------------------------------------------
Describe 'Remove-PveGroup' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveGroup' }

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
            { Remove-PveGroup -GroupId 'testgroup' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveDomain
# ---------------------------------------------------------------------------
Describe 'Get-PveDomain' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveDomain' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveDomain -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveDomain
# ---------------------------------------------------------------------------
Describe 'New-PveDomain' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveDomain' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { New-PveDomain -Realm 'testrealm' -Type 'pve' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveDomain
# ---------------------------------------------------------------------------
Describe 'Set-PveDomain' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PveDomain' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Set-PveDomain -Realm 'testrealm' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveDomain
# ---------------------------------------------------------------------------
Describe 'Remove-PveDomain' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveDomain' }

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
            { Remove-PveDomain -Realm 'testrealm' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PvePassword
# ---------------------------------------------------------------------------
Describe 'Set-PvePassword' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PvePassword' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            $secPw = ConvertTo-SecureString 'dummypassword' -AsPlainText -Force
            { Set-PvePassword -UserId 'root@pam' -Password $secPw -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
