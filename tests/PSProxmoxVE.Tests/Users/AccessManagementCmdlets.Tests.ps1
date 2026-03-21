#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for access management cmdlets:
        Get-PveGroup, New-PveGroup, Set-PveGroup, Remove-PveGroup,
        Get-PveDomain, New-PveDomain, Set-PveDomain, Remove-PveDomain, Set-PvePassword.

    All tests are fully offline — no live Proxmox VE target is required.
    If a cmdlet is not yet compiled the test is marked Skipped.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Availability = @{}
    foreach ($name in @(
        'Get-PveGroup', 'New-PveGroup', 'Set-PveGroup', 'Remove-PveGroup',
        'Get-PveDomain', 'New-PveDomain', 'Set-PveDomain', 'Remove-PveDomain', 'Set-PvePassword'
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
# Get-PveGroup
# ---------------------------------------------------------------------------
Describe 'Get-PveGroup' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveGroup' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveGroup'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveGroup'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have optional GroupId parameter' {
            Skip-IfMissing 'Get-PveGroup'
            $script:Cmd.Parameters.ContainsKey('GroupId') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveGroup'
            { Get-PveGroup -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveGroup
# ---------------------------------------------------------------------------
Describe 'New-PveGroup' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'New-PveGroup' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveGroup'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'New-PveGroup'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'GroupId should be Mandatory' {
            Skip-IfMissing 'New-PveGroup'
            $isMandatory = $script:Cmd.Parameters['GroupId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'New-PveGroup'
            { New-PveGroup -GroupId 'testgroup' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveGroup
# ---------------------------------------------------------------------------
Describe 'Set-PveGroup' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PveGroup' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PveGroup'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Set-PveGroup'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'GroupId should be Mandatory' {
            Skip-IfMissing 'Set-PveGroup'
            $isMandatory = $script:Cmd.Parameters['GroupId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Set-PveGroup'
            { Set-PveGroup -GroupId 'testgroup' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveGroup
# ---------------------------------------------------------------------------
Describe 'Remove-PveGroup' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Remove-PveGroup' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Remove-PveGroup'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Remove-PveGroup'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
        It 'Should declare ConfirmImpact High' {
            Skip-IfMissing 'Remove-PveGroup'
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Required parameters' {
        It 'GroupId should be Mandatory' {
            Skip-IfMissing 'Remove-PveGroup'
            $isMandatory = $script:Cmd.Parameters['GroupId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Remove-PveGroup'
            { Remove-PveGroup -GroupId 'testgroup' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveDomain
# ---------------------------------------------------------------------------
Describe 'Get-PveDomain' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveDomain' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveDomain'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveDomain'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have optional Realm parameter' {
            Skip-IfMissing 'Get-PveDomain'
            $script:Cmd.Parameters.ContainsKey('Realm') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveDomain'
            { Get-PveDomain -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveDomain
# ---------------------------------------------------------------------------
Describe 'New-PveDomain' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'New-PveDomain' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveDomain'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'New-PveDomain'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Realm should be Mandatory' {
            Skip-IfMissing 'New-PveDomain'
            $isMandatory = $script:Cmd.Parameters['Realm'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Type should be Mandatory' {
            Skip-IfMissing 'New-PveDomain'
            $isMandatory = $script:Cmd.Parameters['Type'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'New-PveDomain'
            { New-PveDomain -Realm 'testrealm' -Type 'pve' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveDomain
# ---------------------------------------------------------------------------
Describe 'Set-PveDomain' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PveDomain' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PveDomain'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Set-PveDomain'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Realm should be Mandatory' {
            Skip-IfMissing 'Set-PveDomain'
            $isMandatory = $script:Cmd.Parameters['Realm'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Set-PveDomain'
            { Set-PveDomain -Realm 'testrealm' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveDomain
# ---------------------------------------------------------------------------
Describe 'Remove-PveDomain' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Remove-PveDomain' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Remove-PveDomain'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Remove-PveDomain'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
        It 'Should declare ConfirmImpact High' {
            Skip-IfMissing 'Remove-PveDomain'
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Required parameters' {
        It 'Realm should be Mandatory' {
            Skip-IfMissing 'Remove-PveDomain'
            $isMandatory = $script:Cmd.Parameters['Realm'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Remove-PveDomain'
            { Remove-PveDomain -Realm 'testrealm' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PvePassword
# ---------------------------------------------------------------------------
Describe 'Set-PvePassword' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PvePassword' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PvePassword'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Set-PvePassword'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Set-PvePassword'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'UserId should be Mandatory' {
            Skip-IfMissing 'Set-PvePassword'
            $isMandatory = $script:Cmd.Parameters['UserId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Password should be Mandatory' {
            Skip-IfMissing 'Set-PvePassword'
            $isMandatory = $script:Cmd.Parameters['Password'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Set-PvePassword'
            $secPw = ConvertTo-SecureString 'dummypassword' -AsPlainText -Force
            { Set-PvePassword -UserId 'root@pam' -Password $secPw -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
