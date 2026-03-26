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
    If a cmdlet is not yet compiled the test is marked Skipped.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Availability = @{}
    foreach ($name in @(
        'Get-PveHaResource',
        'New-PveHaResource',
        'Set-PveHaResource',
        'Remove-PveHaResource',
        'Move-PveHaResource',
        'Get-PveHaGroup',
        'New-PveHaGroup',
        'Set-PveHaGroup',
        'Remove-PveHaGroup',
        'Get-PveHaStatus',
        'Get-PveHaRule',
        'New-PveHaRule',
        'Set-PveHaRule',
        'Remove-PveHaRule'
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
# Get-PveHaResource
# ---------------------------------------------------------------------------
Describe 'Get-PveHaResource' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveHaResource' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveHaResource'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveHaResource'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have optional Sid parameter' {
            Skip-IfMissing 'Get-PveHaResource'
            $script:Cmd.Parameters.ContainsKey('Sid') | Should -BeTrue
        }
        It 'Sid should not be Mandatory' {
            Skip-IfMissing 'Get-PveHaResource'
            $p = $script:Cmd.Parameters['Sid']
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }
        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveHaResource'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveHaResource'
            { Get-PveHaResource -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveHaResource
# ---------------------------------------------------------------------------
Describe 'New-PveHaResource' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'New-PveHaResource' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveHaResource'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'New-PveHaResource'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have mandatory Sid parameter' {
            Skip-IfMissing 'New-PveHaResource'
            $p = $script:Cmd.Parameters['Sid']
            $p | Should -Not -BeNullOrEmpty
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Should have optional State parameter with ValidateSet' {
            Skip-IfMissing 'New-PveHaResource'
            $p = $script:Cmd.Parameters['State']
            $p | Should -Not -BeNullOrEmpty
            $vsAttr = $p.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $vsAttr | Should -Not -BeNullOrEmpty
        }
        It 'Should have optional Group parameter' {
            Skip-IfMissing 'New-PveHaResource'
            $script:Cmd.Parameters.ContainsKey('Group') | Should -BeTrue
        }
        It 'Should have optional MaxRelocate parameter with ValidateRange' {
            Skip-IfMissing 'New-PveHaResource'
            $p = $script:Cmd.Parameters['MaxRelocate']
            $p | Should -Not -BeNullOrEmpty
            $rangeAttr = $p.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateRangeAttribute] }
            $rangeAttr | Should -Not -BeNullOrEmpty
            $rangeAttr.MinRange | Should -Be 0
            $rangeAttr.MaxRange | Should -Be 10
        }
        It 'Should have optional MaxRestart parameter with ValidateRange' {
            Skip-IfMissing 'New-PveHaResource'
            $p = $script:Cmd.Parameters['MaxRestart']
            $p | Should -Not -BeNullOrEmpty
            $rangeAttr = $p.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateRangeAttribute] }
            $rangeAttr | Should -Not -BeNullOrEmpty
            $rangeAttr.MinRange | Should -Be 0
            $rangeAttr.MaxRange | Should -Be 10
        }
        It 'Should have Session parameter' {
            Skip-IfMissing 'New-PveHaResource'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
        It 'Should support ShouldProcess' {
            Skip-IfMissing 'New-PveHaResource'
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.SupportsShouldProcess | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'New-PveHaResource'
            { New-PveHaResource -Sid 'vm:100' -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveHaResource
# ---------------------------------------------------------------------------
Describe 'Set-PveHaResource' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PveHaResource' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PveHaResource'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Set-PveHaResource'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have mandatory Sid parameter' {
            Skip-IfMissing 'Set-PveHaResource'
            $p = $script:Cmd.Parameters['Sid']
            $p | Should -Not -BeNullOrEmpty
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Should have optional State parameter with ValidateSet' {
            Skip-IfMissing 'Set-PveHaResource'
            $p = $script:Cmd.Parameters['State']
            $p | Should -Not -BeNullOrEmpty
            $vsAttr = $p.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $vsAttr | Should -Not -BeNullOrEmpty
        }
        It 'Should have Session parameter' {
            Skip-IfMissing 'Set-PveHaResource'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
        It 'Should support ShouldProcess' {
            Skip-IfMissing 'Set-PveHaResource'
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.SupportsShouldProcess | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Set-PveHaResource'
            { Set-PveHaResource -Sid 'vm:100' -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveHaResource
# ---------------------------------------------------------------------------
Describe 'Remove-PveHaResource' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Remove-PveHaResource' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Remove-PveHaResource'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Remove-PveHaResource'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have mandatory Sid parameter' {
            Skip-IfMissing 'Remove-PveHaResource'
            $p = $script:Cmd.Parameters['Sid']
            $p | Should -Not -BeNullOrEmpty
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Should have Session parameter' {
            Skip-IfMissing 'Remove-PveHaResource'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
        It 'Should support ShouldProcess' {
            Skip-IfMissing 'Remove-PveHaResource'
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.SupportsShouldProcess | Should -BeTrue
        }
        It 'Should have ConfirmImpact High' {
            Skip-IfMissing 'Remove-PveHaResource'
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.ConfirmImpact | Should -Be 'High'
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Remove-PveHaResource'
            { Remove-PveHaResource -Sid 'vm:100' -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Move-PveHaResource
# ---------------------------------------------------------------------------
Describe 'Move-PveHaResource' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Move-PveHaResource' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Move-PveHaResource'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Move-PveHaResource'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have mandatory Sid parameter' {
            Skip-IfMissing 'Move-PveHaResource'
            $p = $script:Cmd.Parameters['Sid']
            $p | Should -Not -BeNullOrEmpty
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Should have mandatory Node parameter' {
            Skip-IfMissing 'Move-PveHaResource'
            $p = $script:Cmd.Parameters['Node']
            $p | Should -Not -BeNullOrEmpty
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Should have Mode parameter with ValidateSet(Migrate, Relocate)' {
            Skip-IfMissing 'Move-PveHaResource'
            $p = $script:Cmd.Parameters['Mode']
            $p | Should -Not -BeNullOrEmpty
            $vsAttr = $p.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $vsAttr | Should -Not -BeNullOrEmpty
            $vsAttr.ValidValues | Should -Contain 'Migrate'
            $vsAttr.ValidValues | Should -Contain 'Relocate'
        }
        It 'Should have Session parameter' {
            Skip-IfMissing 'Move-PveHaResource'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
        It 'Should support ShouldProcess' {
            Skip-IfMissing 'Move-PveHaResource'
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.SupportsShouldProcess | Should -BeTrue
        }
        It 'Should have ConfirmImpact High' {
            Skip-IfMissing 'Move-PveHaResource'
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.ConfirmImpact | Should -Be 'High'
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Move-PveHaResource'
            { Move-PveHaResource -Sid 'vm:100' -Node 'pve2' -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveHaGroup
# ---------------------------------------------------------------------------
Describe 'Get-PveHaGroup' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveHaGroup' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveHaGroup'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveHaGroup'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have optional Group parameter' {
            Skip-IfMissing 'Get-PveHaGroup'
            $script:Cmd.Parameters.ContainsKey('Group') | Should -BeTrue
        }
        It 'Group should not be Mandatory' {
            Skip-IfMissing 'Get-PveHaGroup'
            $p = $script:Cmd.Parameters['Group']
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }
        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveHaGroup'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveHaGroup'
            { Get-PveHaGroup -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveHaGroup
# ---------------------------------------------------------------------------
Describe 'New-PveHaGroup' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'New-PveHaGroup' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveHaGroup'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'New-PveHaGroup'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have mandatory Group parameter' {
            Skip-IfMissing 'New-PveHaGroup'
            $p = $script:Cmd.Parameters['Group']
            $p | Should -Not -BeNullOrEmpty
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Should have mandatory Nodes parameter' {
            Skip-IfMissing 'New-PveHaGroup'
            $p = $script:Cmd.Parameters['Nodes']
            $p | Should -Not -BeNullOrEmpty
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Should have optional Restricted switch' {
            Skip-IfMissing 'New-PveHaGroup'
            $script:Cmd.Parameters.ContainsKey('Restricted') | Should -BeTrue
        }
        It 'Should have optional NoFailback switch' {
            Skip-IfMissing 'New-PveHaGroup'
            $script:Cmd.Parameters.ContainsKey('NoFailback') | Should -BeTrue
        }
        It 'Should have optional Comment parameter' {
            Skip-IfMissing 'New-PveHaGroup'
            $script:Cmd.Parameters.ContainsKey('Comment') | Should -BeTrue
        }
        It 'Should have Session parameter' {
            Skip-IfMissing 'New-PveHaGroup'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
        It 'Should support ShouldProcess' {
            Skip-IfMissing 'New-PveHaGroup'
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.SupportsShouldProcess | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'New-PveHaGroup'
            { New-PveHaGroup -Group 'grp1' -Nodes 'pve1:1,pve2:2' -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveHaGroup
# ---------------------------------------------------------------------------
Describe 'Set-PveHaGroup' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PveHaGroup' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PveHaGroup'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Set-PveHaGroup'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have mandatory Group parameter' {
            Skip-IfMissing 'Set-PveHaGroup'
            $p = $script:Cmd.Parameters['Group']
            $p | Should -Not -BeNullOrEmpty
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Should have optional Nodes parameter' {
            Skip-IfMissing 'Set-PveHaGroup'
            $script:Cmd.Parameters.ContainsKey('Nodes') | Should -BeTrue
        }
        It 'Should have optional Restricted parameter with ValidateRange(0, 1)' {
            Skip-IfMissing 'Set-PveHaGroup'
            $p = $script:Cmd.Parameters['Restricted']
            $p | Should -Not -BeNullOrEmpty
            $rangeAttr = $p.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateRangeAttribute] }
            $rangeAttr | Should -Not -BeNullOrEmpty
            $rangeAttr.MinRange | Should -Be 0
            $rangeAttr.MaxRange | Should -Be 1
        }
        It 'Should have optional NoFailback parameter with ValidateRange(0, 1)' {
            Skip-IfMissing 'Set-PveHaGroup'
            $p = $script:Cmd.Parameters['NoFailback']
            $p | Should -Not -BeNullOrEmpty
            $rangeAttr = $p.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateRangeAttribute] }
            $rangeAttr | Should -Not -BeNullOrEmpty
            $rangeAttr.MinRange | Should -Be 0
            $rangeAttr.MaxRange | Should -Be 1
        }
        It 'Should have Session parameter' {
            Skip-IfMissing 'Set-PveHaGroup'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
        It 'Should support ShouldProcess' {
            Skip-IfMissing 'Set-PveHaGroup'
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.SupportsShouldProcess | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Set-PveHaGroup'
            { Set-PveHaGroup -Group 'grp1' -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveHaGroup
# ---------------------------------------------------------------------------
Describe 'Remove-PveHaGroup' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Remove-PveHaGroup' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Remove-PveHaGroup'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Remove-PveHaGroup'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have mandatory Group parameter' {
            Skip-IfMissing 'Remove-PveHaGroup'
            $p = $script:Cmd.Parameters['Group']
            $p | Should -Not -BeNullOrEmpty
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Should have Session parameter' {
            Skip-IfMissing 'Remove-PveHaGroup'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
        It 'Should support ShouldProcess' {
            Skip-IfMissing 'Remove-PveHaGroup'
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.SupportsShouldProcess | Should -BeTrue
        }
        It 'Should have ConfirmImpact High' {
            Skip-IfMissing 'Remove-PveHaGroup'
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.ConfirmImpact | Should -Be 'High'
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Remove-PveHaGroup'
            { Remove-PveHaGroup -Group 'grp1' -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveHaStatus
# ---------------------------------------------------------------------------
Describe 'Get-PveHaStatus' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveHaStatus' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveHaStatus'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveHaStatus'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveHaStatus'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveHaStatus'
            { Get-PveHaStatus -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveHaRule
# ---------------------------------------------------------------------------
Describe 'Get-PveHaRule' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveHaRule' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveHaRule'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveHaRule'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have optional Rule parameter' {
            Skip-IfMissing 'Get-PveHaRule'
            $script:Cmd.Parameters.ContainsKey('Rule') | Should -BeTrue
        }
        It 'Rule should not be Mandatory' {
            Skip-IfMissing 'Get-PveHaRule'
            $p = $script:Cmd.Parameters['Rule']
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }
        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveHaRule'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveHaRule'
            { Get-PveHaRule -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveHaRule
# ---------------------------------------------------------------------------
Describe 'New-PveHaRule' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'New-PveHaRule' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveHaRule'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'New-PveHaRule'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have mandatory Type parameter' {
            Skip-IfMissing 'New-PveHaRule'
            $p = $script:Cmd.Parameters['Type']
            $p | Should -Not -BeNullOrEmpty
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Should have optional State parameter with ValidateSet' {
            Skip-IfMissing 'New-PveHaRule'
            $p = $script:Cmd.Parameters['State']
            $p | Should -Not -BeNullOrEmpty
            $vsAttr = $p.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $vsAttr | Should -Not -BeNullOrEmpty
        }
        It 'Should have optional Comment parameter' {
            Skip-IfMissing 'New-PveHaRule'
            $script:Cmd.Parameters.ContainsKey('Comment') | Should -BeTrue
        }
        It 'Should have optional Properties parameter' {
            Skip-IfMissing 'New-PveHaRule'
            $script:Cmd.Parameters.ContainsKey('Properties') | Should -BeTrue
        }
        It 'Should have Session parameter' {
            Skip-IfMissing 'New-PveHaRule'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
        It 'Should support ShouldProcess' {
            Skip-IfMissing 'New-PveHaRule'
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.SupportsShouldProcess | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'New-PveHaRule'
            { New-PveHaRule -Type 'location' -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveHaRule
# ---------------------------------------------------------------------------
Describe 'Set-PveHaRule' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PveHaRule' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PveHaRule'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Set-PveHaRule'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have mandatory Rule parameter' {
            Skip-IfMissing 'Set-PveHaRule'
            $p = $script:Cmd.Parameters['Rule']
            $p | Should -Not -BeNullOrEmpty
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Should have mandatory Type parameter' {
            Skip-IfMissing 'Set-PveHaRule'
            $p = $script:Cmd.Parameters['Type']
            $p | Should -Not -BeNullOrEmpty
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Should have optional State parameter with ValidateSet' {
            Skip-IfMissing 'Set-PveHaRule'
            $p = $script:Cmd.Parameters['State']
            $p | Should -Not -BeNullOrEmpty
            $vsAttr = $p.Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $vsAttr | Should -Not -BeNullOrEmpty
        }
        It 'Should have optional Properties parameter' {
            Skip-IfMissing 'Set-PveHaRule'
            $script:Cmd.Parameters.ContainsKey('Properties') | Should -BeTrue
        }
        It 'Should have Session parameter' {
            Skip-IfMissing 'Set-PveHaRule'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
        It 'Should support ShouldProcess' {
            Skip-IfMissing 'Set-PveHaRule'
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.SupportsShouldProcess | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Set-PveHaRule'
            { Set-PveHaRule -Rule 'rule1' -Type 'node-affinity' -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveHaRule
# ---------------------------------------------------------------------------
Describe 'Remove-PveHaRule' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Remove-PveHaRule' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Remove-PveHaRule'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Remove-PveHaRule'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have mandatory Rule parameter' {
            Skip-IfMissing 'Remove-PveHaRule'
            $p = $script:Cmd.Parameters['Rule']
            $p | Should -Not -BeNullOrEmpty
            $isMandatory = $p.ParameterSets.Values | Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Should have Session parameter' {
            Skip-IfMissing 'Remove-PveHaRule'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
        It 'Should support ShouldProcess' {
            Skip-IfMissing 'Remove-PveHaRule'
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.SupportsShouldProcess | Should -BeTrue
        }
        It 'Should have ConfirmImpact High' {
            Skip-IfMissing 'Remove-PveHaRule'
            $cmdletAttr = $script:Cmd.ImplementingType.GetCustomAttributes($true) |
                Where-Object { $_ -is [System.Management.Automation.CmdletAttribute] }
            $cmdletAttr.ConfirmImpact | Should -Be 'High'
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Remove-PveHaRule'
            { Remove-PveHaRule -Rule 'rule1' -ErrorAction Stop -Confirm:$false } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
