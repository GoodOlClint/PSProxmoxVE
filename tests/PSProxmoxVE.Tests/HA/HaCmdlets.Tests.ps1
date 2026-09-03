#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for HA (High Availability) cmdlets:
        Get-PveHaResource, New-PveHaResource, Set-PveHaResource,
        Remove-PveHaResource, Move-PveHaResource,
        Get-PveHaGroup, New-PveHaGroup, Set-PveHaGroup, Remove-PveHaGroup,
        Get-PveHaStatus,
        Get-PveHaRule, New-PveHaRule, Set-PveHaRule, Remove-PveHaRule.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Get-PveHaResource
# ---------------------------------------------------------------------------
Describe 'Get-PveHaResource' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveHaResource' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveHaResource -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveHaResource
# ---------------------------------------------------------------------------
Describe 'New-PveHaResource' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveHaResource' }

    Context 'Parameter metadata' {
        It 'Should have optional State parameter with ValidateSet' {
            $p = $script:Cmd.Parameters['State']
            $p | Should -Not -BeNullOrEmpty
            $vsAttr = $p.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $vsAttr | Should -Not -BeNullOrEmpty
        }
        It 'Should have optional MaxRelocate parameter with ValidateRange' {
            $p = $script:Cmd.Parameters['MaxRelocate']
            $p | Should -Not -BeNullOrEmpty
            $rangeAttr = $p.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateRangeAttribute] }
            $rangeAttr | Should -Not -BeNullOrEmpty
            $rangeAttr.MinRange | Should -Be 0
            $rangeAttr.MaxRange | Should -Be 10
        }
        It 'Should have optional MaxRestart parameter with ValidateRange' {
            $p = $script:Cmd.Parameters['MaxRestart']
            $p | Should -Not -BeNullOrEmpty
            $rangeAttr = $p.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateRangeAttribute] }
            $rangeAttr | Should -Not -BeNullOrEmpty
            $rangeAttr.MinRange | Should -Be 0
            $rangeAttr.MaxRange | Should -Be 10
        }
        It 'Should support ShouldProcess' {
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.SupportsShouldProcess | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { New-PveHaResource -Sid 'vm:100' -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveHaResource
# ---------------------------------------------------------------------------
Describe 'Set-PveHaResource' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PveHaResource' }

    Context 'Parameter metadata' {
        It 'Should have optional State parameter with ValidateSet' {
            $p = $script:Cmd.Parameters['State']
            $p | Should -Not -BeNullOrEmpty
            $vsAttr = $p.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $vsAttr | Should -Not -BeNullOrEmpty
        }
        It 'Should support ShouldProcess' {
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.SupportsShouldProcess | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Set-PveHaResource -Sid 'vm:100' -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveHaResource
# ---------------------------------------------------------------------------
Describe 'Remove-PveHaResource' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveHaResource' }

    Context 'Parameter metadata' {
        It 'Should support ShouldProcess' {
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.SupportsShouldProcess | Should -BeTrue
        }
        It 'Should have ConfirmImpact High' {
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.ConfirmImpact | Should -Be 'High'
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Remove-PveHaResource -Sid 'vm:100' -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Move-PveHaResource
# ---------------------------------------------------------------------------
Describe 'Move-PveHaResource' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Move-PveHaResource' }

    Context 'Parameter metadata' {
        It 'Should have Mode parameter with ValidateSet(Migrate, Relocate)' {
            $p = $script:Cmd.Parameters['Mode']
            $p | Should -Not -BeNullOrEmpty
            $vsAttr = $p.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $vsAttr | Should -Not -BeNullOrEmpty
            $vsAttr.ValidValues | Should -Contain 'Migrate'
            $vsAttr.ValidValues | Should -Contain 'Relocate'
        }
        It 'Should support ShouldProcess' {
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.SupportsShouldProcess | Should -BeTrue
        }
        It 'Should have ConfirmImpact High' {
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.ConfirmImpact | Should -Be 'High'
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Move-PveHaResource -Sid 'vm:100' -Node 'pve2' -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveHaGroup
# ---------------------------------------------------------------------------
Describe 'Get-PveHaGroup' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveHaGroup' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveHaGroup -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveHaGroup
# ---------------------------------------------------------------------------
Describe 'New-PveHaGroup' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveHaGroup' }

    Context 'Parameter metadata' {
        It 'Should support ShouldProcess' {
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.SupportsShouldProcess | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { New-PveHaGroup -Group 'grp1' -Nodes 'pve1:1,pve2:2' -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveHaGroup
# ---------------------------------------------------------------------------
Describe 'Set-PveHaGroup' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PveHaGroup' }

    Context 'Parameter metadata' {
        It 'Should have optional Restricted parameter with ValidateRange(0, 1)' {
            $p = $script:Cmd.Parameters['Restricted']
            $p | Should -Not -BeNullOrEmpty
            $rangeAttr = $p.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateRangeAttribute] }
            $rangeAttr | Should -Not -BeNullOrEmpty
            $rangeAttr.MinRange | Should -Be 0
            $rangeAttr.MaxRange | Should -Be 1
        }
        It 'Should have optional NoFailback parameter with ValidateRange(0, 1)' {
            $p = $script:Cmd.Parameters['NoFailback']
            $p | Should -Not -BeNullOrEmpty
            $rangeAttr = $p.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateRangeAttribute] }
            $rangeAttr | Should -Not -BeNullOrEmpty
            $rangeAttr.MinRange | Should -Be 0
            $rangeAttr.MaxRange | Should -Be 1
        }
        It 'Should support ShouldProcess' {
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.SupportsShouldProcess | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Set-PveHaGroup -Group 'grp1' -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveHaGroup
# ---------------------------------------------------------------------------
Describe 'Remove-PveHaGroup' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveHaGroup' }

    Context 'Parameter metadata' {
        It 'Should support ShouldProcess' {
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.SupportsShouldProcess | Should -BeTrue
        }
        It 'Should have ConfirmImpact High' {
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.ConfirmImpact | Should -Be 'High'
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Remove-PveHaGroup -Group 'grp1' -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveHaStatus
# ---------------------------------------------------------------------------
Describe 'Get-PveHaStatus' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveHaStatus' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveHaStatus -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveHaRule
# ---------------------------------------------------------------------------
Describe 'Get-PveHaRule' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveHaRule' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveHaRule -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveHaRule
# ---------------------------------------------------------------------------
Describe 'New-PveHaRule' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveHaRule' }

    Context 'Parameter metadata' {
        It 'Should have optional State parameter with ValidateSet' {
            $p = $script:Cmd.Parameters['State']
            $p | Should -Not -BeNullOrEmpty
            $vsAttr = $p.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $vsAttr | Should -Not -BeNullOrEmpty
        }
        It 'Should support ShouldProcess' {
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.SupportsShouldProcess | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { New-PveHaRule -Type 'location' -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveHaRule
# ---------------------------------------------------------------------------
Describe 'Set-PveHaRule' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PveHaRule' }

    Context 'Parameter metadata' {
        It 'Should have optional State parameter with ValidateSet' {
            $p = $script:Cmd.Parameters['State']
            $p | Should -Not -BeNullOrEmpty
            $vsAttr = $p.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $vsAttr | Should -Not -BeNullOrEmpty
        }
        It 'Should support ShouldProcess' {
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.SupportsShouldProcess | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Set-PveHaRule -Rule 'rule1' -Type 'node-affinity' -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveHaRule
# ---------------------------------------------------------------------------
Describe 'Remove-PveHaRule' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveHaRule' }

    Context 'Parameter metadata' {
        It 'Should support ShouldProcess' {
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.SupportsShouldProcess | Should -BeTrue
        }
        It 'Should have ConfirmImpact High' {
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.ConfirmImpact | Should -Be 'High'
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Remove-PveHaRule -Rule 'rule1' -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
