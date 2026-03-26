#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for cluster config and management cmdlets:
        Get-PveClusterStatus, Get-PveClusterNextId, Get-PveClusterOption,
        Set-PveClusterOption, Get-PveClusterConfig, Get-PveClusterConfigNode,
        Add-PveClusterConfigNode, Remove-PveClusterConfigNode,
        Get-PveClusterJoinInfo, Add-PveClusterMember, New-PveCluster.

    All tests are fully offline — no live Proxmox VE target is required.
    If a cmdlet is not yet compiled the test is marked Skipped.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Availability = @{}
    foreach ($name in @(
        'Get-PveClusterStatus',
        'Get-PveClusterNextId',
        'Get-PveClusterOption',
        'Set-PveClusterOption',
        'Get-PveClusterConfig',
        'Get-PveClusterConfigNode',
        'Add-PveClusterConfigNode',
        'Remove-PveClusterConfigNode',
        'Get-PveClusterJoinInfo',
        'Add-PveClusterMember',
        'New-PveCluster'
    )) {
        $script:Availability[$name] = $null -ne (Get-Command $name -ErrorAction SilentlyContinue)
    }

    function Skip-IfMissing([string]$Name) {
        if (-not $script:Availability[$Name]) {
            Set-ItResult -Skipped -Because "$Name is not yet implemented in this build"
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveClusterStatus
# ---------------------------------------------------------------------------
Describe 'Get-PveClusterStatus' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveClusterStatus' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveClusterStatus'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveClusterStatus'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveClusterStatus'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveClusterStatus'
            { Get-PveClusterStatus -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveClusterNextId
# ---------------------------------------------------------------------------
Describe 'Get-PveClusterNextId' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveClusterNextId' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveClusterNextId'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveClusterNextId'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have optional VmId parameter' {
            Skip-IfMissing 'Get-PveClusterNextId'
            $script:Cmd.Parameters.ContainsKey('VmId') | Should -BeTrue
        }
        It 'VmId should not be Mandatory' {
            Skip-IfMissing 'Get-PveClusterNextId'
            $p = $script:Cmd.Parameters['VmId']
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }
        It 'VmId should have ValidateRange(100, 999999999)' {
            Skip-IfMissing 'Get-PveClusterNextId'
            $p = $script:Cmd.Parameters['VmId']
            $rangeAttr = $p.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateRangeAttribute] }
            $rangeAttr | Should -Not -BeNullOrEmpty
            $rangeAttr.MinRange | Should -Be 100
            $rangeAttr.MaxRange | Should -Be 999999999
        }
        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveClusterNextId'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveClusterNextId'
            { Get-PveClusterNextId -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveClusterOption
# ---------------------------------------------------------------------------
Describe 'Get-PveClusterOption' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveClusterOption' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveClusterOption'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveClusterOption'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveClusterOption'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveClusterOption'
            { Get-PveClusterOption -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveClusterOption
# ---------------------------------------------------------------------------
Describe 'Set-PveClusterOption' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PveClusterOption' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PveClusterOption'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Set-PveClusterOption'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have Keyboard parameter' {
            Skip-IfMissing 'Set-PveClusterOption'
            $script:Cmd.Parameters.ContainsKey('Keyboard') | Should -BeTrue
        }
        It 'Keyboard should have ValidateSet' {
            Skip-IfMissing 'Set-PveClusterOption'
            $p = $script:Cmd.Parameters['Keyboard']
            $vsAttr = $p.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $vsAttr | Should -Not -BeNullOrEmpty
        }
        It 'Should have Language parameter' {
            Skip-IfMissing 'Set-PveClusterOption'
            $script:Cmd.Parameters.ContainsKey('Language') | Should -BeTrue
        }
        It 'Should have Console parameter' {
            Skip-IfMissing 'Set-PveClusterOption'
            $script:Cmd.Parameters.ContainsKey('Console') | Should -BeTrue
        }
        It 'Console should have ValidateSet' {
            Skip-IfMissing 'Set-PveClusterOption'
            $p = $script:Cmd.Parameters['Console']
            $vsAttr = $p.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $vsAttr | Should -Not -BeNullOrEmpty
        }
        It 'Should have Fencing parameter with ValidateSet' {
            Skip-IfMissing 'Set-PveClusterOption'
            $p = $script:Cmd.Parameters['Fencing']
            $p | Should -Not -BeNullOrEmpty
            $vsAttr = $p.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $vsAttr | Should -Not -BeNullOrEmpty
        }
        It 'Should have Session parameter' {
            Skip-IfMissing 'Set-PveClusterOption'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
        It 'Should support ShouldProcess' {
            Skip-IfMissing 'Set-PveClusterOption'
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.SupportsShouldProcess | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Set-PveClusterOption'
            { Set-PveClusterOption -Keyboard 'en-us' -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveClusterConfig
# ---------------------------------------------------------------------------
Describe 'Get-PveClusterConfig' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveClusterConfig' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveClusterConfig'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveClusterConfig'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveClusterConfig'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveClusterConfig'
            { Get-PveClusterConfig -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveClusterConfigNode
# ---------------------------------------------------------------------------
Describe 'Get-PveClusterConfigNode' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveClusterConfigNode' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveClusterConfigNode'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveClusterConfigNode'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveClusterConfigNode'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveClusterConfigNode'
            { Get-PveClusterConfigNode -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Add-PveClusterConfigNode
# ---------------------------------------------------------------------------
Describe 'Add-PveClusterConfigNode' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Add-PveClusterConfigNode' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Add-PveClusterConfigNode'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Add-PveClusterConfigNode'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have mandatory Node parameter' {
            Skip-IfMissing 'Add-PveClusterConfigNode'
            $p = $script:Cmd.Parameters['Node']
            $p | Should -Not -BeNullOrEmpty
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Should have optional NewNodeIp parameter' {
            Skip-IfMissing 'Add-PveClusterConfigNode'
            $script:Cmd.Parameters.ContainsKey('NewNodeIp') | Should -BeTrue
        }
        It 'Should have optional NodeId parameter' {
            Skip-IfMissing 'Add-PveClusterConfigNode'
            $script:Cmd.Parameters.ContainsKey('NodeId') | Should -BeTrue
        }
        It 'Should have optional Votes parameter' {
            Skip-IfMissing 'Add-PveClusterConfigNode'
            $script:Cmd.Parameters.ContainsKey('Votes') | Should -BeTrue
        }
        It 'Should have optional Force switch' {
            Skip-IfMissing 'Add-PveClusterConfigNode'
            $script:Cmd.Parameters.ContainsKey('Force') | Should -BeTrue
        }
        It 'Should have Session parameter' {
            Skip-IfMissing 'Add-PveClusterConfigNode'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
        It 'Should support ShouldProcess' {
            Skip-IfMissing 'Add-PveClusterConfigNode'
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.SupportsShouldProcess | Should -BeTrue
        }
        It 'Should have ConfirmImpact High' {
            Skip-IfMissing 'Add-PveClusterConfigNode'
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.ConfirmImpact | Should -Be 'High'
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Add-PveClusterConfigNode'
            { Add-PveClusterConfigNode -Node 'pve2' -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveClusterConfigNode
# ---------------------------------------------------------------------------
Describe 'Remove-PveClusterConfigNode' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Remove-PveClusterConfigNode' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Remove-PveClusterConfigNode'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Remove-PveClusterConfigNode'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have mandatory Node parameter' {
            Skip-IfMissing 'Remove-PveClusterConfigNode'
            $p = $script:Cmd.Parameters['Node']
            $p | Should -Not -BeNullOrEmpty
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Should have Session parameter' {
            Skip-IfMissing 'Remove-PveClusterConfigNode'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
        It 'Should support ShouldProcess' {
            Skip-IfMissing 'Remove-PveClusterConfigNode'
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.SupportsShouldProcess | Should -BeTrue
        }
        It 'Should have ConfirmImpact High' {
            Skip-IfMissing 'Remove-PveClusterConfigNode'
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.ConfirmImpact | Should -Be 'High'
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Remove-PveClusterConfigNode'
            { Remove-PveClusterConfigNode -Node 'pve2' -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveClusterJoinInfo
# ---------------------------------------------------------------------------
Describe 'Get-PveClusterJoinInfo' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveClusterJoinInfo' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveClusterJoinInfo'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveClusterJoinInfo'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have optional Node parameter' {
            Skip-IfMissing 'Get-PveClusterJoinInfo'
            $script:Cmd.Parameters.ContainsKey('Node') | Should -BeTrue
        }
        It 'Node should not be Mandatory' {
            Skip-IfMissing 'Get-PveClusterJoinInfo'
            $p = $script:Cmd.Parameters['Node']
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }
        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveClusterJoinInfo'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveClusterJoinInfo'
            { Get-PveClusterJoinInfo -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Add-PveClusterMember
# ---------------------------------------------------------------------------
Describe 'Add-PveClusterMember' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Add-PveClusterMember' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Add-PveClusterMember'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Add-PveClusterMember'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have mandatory Hostname parameter' {
            Skip-IfMissing 'Add-PveClusterMember'
            $p = $script:Cmd.Parameters['Hostname']
            $p | Should -Not -BeNullOrEmpty
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Should have mandatory Fingerprint parameter' {
            Skip-IfMissing 'Add-PveClusterMember'
            $p = $script:Cmd.Parameters['Fingerprint']
            $p | Should -Not -BeNullOrEmpty
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Should have mandatory Password parameter' {
            Skip-IfMissing 'Add-PveClusterMember'
            $p = $script:Cmd.Parameters['Password']
            $p | Should -Not -BeNullOrEmpty
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Password should be SecureString type' {
            Skip-IfMissing 'Add-PveClusterMember'
            $script:Cmd.Parameters['Password'].ParameterType | Should -Be ([System.Security.SecureString])
        }
        It 'Should have optional Force switch' {
            Skip-IfMissing 'Add-PveClusterMember'
            $script:Cmd.Parameters.ContainsKey('Force') | Should -BeTrue
        }
        It 'Should have Session parameter' {
            Skip-IfMissing 'Add-PveClusterMember'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
        It 'Should support ShouldProcess' {
            Skip-IfMissing 'Add-PveClusterMember'
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.SupportsShouldProcess | Should -BeTrue
        }
        It 'Should have ConfirmImpact High' {
            Skip-IfMissing 'Add-PveClusterMember'
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.ConfirmImpact | Should -Be 'High'
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Add-PveClusterMember'
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

    BeforeAll { $script:Cmd = Get-Command 'New-PveCluster' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveCluster'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'New-PveCluster'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have mandatory ClusterName parameter' {
            Skip-IfMissing 'New-PveCluster'
            $p = $script:Cmd.Parameters['ClusterName']
            $p | Should -Not -BeNullOrEmpty
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'ClusterName should have ValidateLength(1, 15)' {
            Skip-IfMissing 'New-PveCluster'
            $p = $script:Cmd.Parameters['ClusterName']
            $lenAttr = $p.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateLengthAttribute] }
            $lenAttr | Should -Not -BeNullOrEmpty
            $lenAttr.MinLength | Should -Be 1
            $lenAttr.MaxLength | Should -Be 15
        }
        It 'Should have optional NodeId parameter' {
            Skip-IfMissing 'New-PveCluster'
            $script:Cmd.Parameters.ContainsKey('NodeId') | Should -BeTrue
        }
        It 'Should have optional Votes parameter' {
            Skip-IfMissing 'New-PveCluster'
            $script:Cmd.Parameters.ContainsKey('Votes') | Should -BeTrue
        }
        It 'Should have optional Links parameter' {
            Skip-IfMissing 'New-PveCluster'
            $script:Cmd.Parameters.ContainsKey('Links') | Should -BeTrue
        }
        It 'Should have Session parameter' {
            Skip-IfMissing 'New-PveCluster'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
        It 'Should support ShouldProcess' {
            Skip-IfMissing 'New-PveCluster'
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.SupportsShouldProcess | Should -BeTrue
        }
        It 'Should have ConfirmImpact High' {
            Skip-IfMissing 'New-PveCluster'
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.ConfirmImpact | Should -Be 'High'
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'New-PveCluster'
            { New-PveCluster -ClusterName 'testcluster' -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
