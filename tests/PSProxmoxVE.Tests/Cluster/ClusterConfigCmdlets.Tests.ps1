#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for cluster config and management cmdlets:
        Get-PveClusterStatus, Get-PveClusterNextId, Get-PveClusterOption,
        Set-PveClusterOption, Get-PveClusterConfig, Get-PveClusterConfigNode,
        Add-PveClusterConfigNode, Remove-PveClusterConfigNode,
        Get-PveClusterJoinInfo, Add-PveClusterMember, New-PveCluster.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Get-PveClusterStatus
# ---------------------------------------------------------------------------
Describe 'Get-PveClusterStatus' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveClusterStatus' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveClusterStatus -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveClusterNextId
# ---------------------------------------------------------------------------
Describe 'Get-PveClusterNextId' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveClusterNextId' }

    Context 'Parameter metadata' {
        It 'VmId should have ValidateRange(100, 999999999)' {
            $p = $script:Cmd.Parameters['VmId']
            $rangeAttr = $p.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateRangeAttribute] }
            $rangeAttr | Should -Not -BeNullOrEmpty
            $rangeAttr.MinRange | Should -Be 100
            $rangeAttr.MaxRange | Should -Be 999999999
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveClusterNextId -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveClusterOption
# ---------------------------------------------------------------------------
Describe 'Get-PveClusterOption' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveClusterOption' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveClusterOption -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveClusterOption
# ---------------------------------------------------------------------------
Describe 'Set-PveClusterOption' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PveClusterOption' }

    Context 'Parameter metadata' {
        It 'Keyboard should have ValidateSet' {
            $p = $script:Cmd.Parameters['Keyboard']
            $vsAttr = $p.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $vsAttr | Should -Not -BeNullOrEmpty
        }
        It 'Console should have ValidateSet' {
            $p = $script:Cmd.Parameters['Console']
            $vsAttr = $p.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $vsAttr | Should -Not -BeNullOrEmpty
        }
        It 'Should have Fencing parameter with ValidateSet' {
            $p = $script:Cmd.Parameters['Fencing']
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
            { Set-PveClusterOption -Keyboard 'en-us' -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveClusterConfig
# ---------------------------------------------------------------------------
Describe 'Get-PveClusterConfig' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveClusterConfig' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveClusterConfig -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveClusterConfigNode
# ---------------------------------------------------------------------------
Describe 'Get-PveClusterConfigNode' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveClusterConfigNode' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveClusterConfigNode -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Add-PveClusterConfigNode
# ---------------------------------------------------------------------------
Describe 'Add-PveClusterConfigNode' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Add-PveClusterConfigNode' }

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
            { Add-PveClusterConfigNode -Node 'pve2' -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveClusterConfigNode
# ---------------------------------------------------------------------------
Describe 'Remove-PveClusterConfigNode' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveClusterConfigNode' }

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
            { Remove-PveClusterConfigNode -Node 'pve2' -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveClusterJoinInfo
# ---------------------------------------------------------------------------
Describe 'Get-PveClusterJoinInfo' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveClusterJoinInfo' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveClusterJoinInfo -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Add-PveClusterMember
# ---------------------------------------------------------------------------
Describe 'Add-PveClusterMember' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Add-PveClusterMember' }

    Context 'Parameter metadata' {
        It 'Password should be SecureString type' {
            $script:Cmd.Parameters['Password'].ParameterType | Should -Be ([System.Security.SecureString])
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
            $secPw = ConvertTo-SecureString 'test' -AsPlainText -Force
            { Add-PveClusterMember -Hostname 'pve1' -Fingerprint 'AA:BB' -Password $secPw -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveCluster
# ---------------------------------------------------------------------------
Describe 'New-PveCluster' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveCluster' }

    Context 'Parameter metadata' {
        It 'ClusterName should have ValidateLength(1, 15)' {
            $p = $script:Cmd.Parameters['ClusterName']
            $lenAttr = $p.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateLengthAttribute] }
            $lenAttr | Should -Not -BeNullOrEmpty
            $lenAttr.MinLength | Should -Be 1
            $lenAttr.MaxLength | Should -Be 15
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
            { New-PveCluster -ClusterName 'testcluster' -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
