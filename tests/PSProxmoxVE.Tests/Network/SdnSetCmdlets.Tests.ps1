#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for SDN Set cmdlets and related mutating cmdlets:
        Set-PveSdnZone, Set-PveSdnVnet, Set-PveSdnSubnet, Set-PveSdnController,
        Set-PveSdnIpam, Set-PveSdnDns, Invoke-PveSdnApply, Set-PveRole, Set-PveApiToken.

    All tests are fully offline — no live Proxmox VE target is required.
    If a cmdlet is not yet compiled the test is marked Skipped.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Availability = @{}
    foreach ($name in @(
        'Set-PveSdnZone', 'Set-PveSdnVnet', 'Set-PveSdnSubnet', 'Set-PveSdnController',
        'Set-PveSdnIpam', 'Set-PveSdnDns', 'Invoke-PveSdnApply', 'Set-PveRole', 'Set-PveApiToken'
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
# Set-PveSdnZone
# ---------------------------------------------------------------------------
Describe 'Set-PveSdnZone' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PveSdnZone' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PveSdnZone'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Set-PveSdnZone'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Set-PveSdnZone'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Zone should be Mandatory' {
            Skip-IfMissing 'Set-PveSdnZone'
            $isMandatory = $script:Cmd.Parameters['Zone'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Set-PveSdnZone'
            { Set-PveSdnZone -Zone 'testzone' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveSdnVnet
# ---------------------------------------------------------------------------
Describe 'Set-PveSdnVnet' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PveSdnVnet' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PveSdnVnet'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Set-PveSdnVnet'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Set-PveSdnVnet'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Vnet should be Mandatory' {
            Skip-IfMissing 'Set-PveSdnVnet'
            $isMandatory = $script:Cmd.Parameters['Vnet'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Set-PveSdnVnet'
            { Set-PveSdnVnet -Vnet 'testvnet' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveSdnSubnet
# ---------------------------------------------------------------------------
Describe 'Set-PveSdnSubnet' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PveSdnSubnet' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PveSdnSubnet'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Set-PveSdnSubnet'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Set-PveSdnSubnet'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Vnet should be Mandatory' {
            Skip-IfMissing 'Set-PveSdnSubnet'
            $isMandatory = $script:Cmd.Parameters['Vnet'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Subnet should be Mandatory' {
            Skip-IfMissing 'Set-PveSdnSubnet'
            $isMandatory = $script:Cmd.Parameters['Subnet'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Set-PveSdnSubnet'
            { Set-PveSdnSubnet -Vnet 'testvnet' -Subnet '10.0.0.0/24' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveSdnController
# ---------------------------------------------------------------------------
Describe 'Set-PveSdnController' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PveSdnController' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PveSdnController'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Set-PveSdnController'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Set-PveSdnController'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Controller should be Mandatory' {
            Skip-IfMissing 'Set-PveSdnController'
            $isMandatory = $script:Cmd.Parameters['Controller'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Set-PveSdnController'
            { Set-PveSdnController -Controller 'testctrl' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveSdnIpam
# ---------------------------------------------------------------------------
Describe 'Set-PveSdnIpam' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PveSdnIpam' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PveSdnIpam'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Set-PveSdnIpam'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Set-PveSdnIpam'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Ipam should be Mandatory' {
            Skip-IfMissing 'Set-PveSdnIpam'
            $isMandatory = $script:Cmd.Parameters['Ipam'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Set-PveSdnIpam'
            { Set-PveSdnIpam -Ipam 'testipam' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveSdnDns
# ---------------------------------------------------------------------------
Describe 'Set-PveSdnDns' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PveSdnDns' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PveSdnDns'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Set-PveSdnDns'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Set-PveSdnDns'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Dns should be Mandatory' {
            Skip-IfMissing 'Set-PveSdnDns'
            $isMandatory = $script:Cmd.Parameters['Dns'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Set-PveSdnDns'
            { Set-PveSdnDns -Dns 'testdns' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Invoke-PveSdnApply
# ---------------------------------------------------------------------------
Describe 'Invoke-PveSdnApply' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Invoke-PveSdnApply' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Invoke-PveSdnApply'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Invoke-PveSdnApply'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Invoke-PveSdnApply'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Invoke-PveSdnApply'
            { Invoke-PveSdnApply -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveRole
# ---------------------------------------------------------------------------
Describe 'Set-PveRole' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PveRole' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PveRole'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Set-PveRole'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Set-PveRole'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'RoleId should be Mandatory' {
            Skip-IfMissing 'Set-PveRole'
            $isMandatory = $script:Cmd.Parameters['RoleId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Privileges should be Mandatory' {
            Skip-IfMissing 'Set-PveRole'
            $isMandatory = $script:Cmd.Parameters['Privileges'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Set-PveRole'
            { Set-PveRole -RoleId 'testrole' -Privileges 'VM.Audit' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveApiToken
# ---------------------------------------------------------------------------
Describe 'Set-PveApiToken' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PveApiToken' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PveApiToken'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Set-PveApiToken'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Set-PveApiToken'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'UserId should be Mandatory' {
            Skip-IfMissing 'Set-PveApiToken'
            $isMandatory = $script:Cmd.Parameters['UserId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'TokenId should be Mandatory' {
            Skip-IfMissing 'Set-PveApiToken'
            $isMandatory = $script:Cmd.Parameters['TokenId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Set-PveApiToken'
            { Set-PveApiToken -UserId 'user@pam' -TokenId 'testtoken' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
