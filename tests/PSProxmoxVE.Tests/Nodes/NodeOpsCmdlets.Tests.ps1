#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for node operation cmdlets:
        Get-PveNodeConfig, Set-PveNodeConfig, Get-PveNodeDns, Set-PveNodeDns,
        Start-PveNodeVms, Stop-PveNodeVms.

    All tests are fully offline — no live Proxmox VE target is required.
    If a cmdlet is not yet compiled the test is marked Skipped.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Availability = @{}
    foreach ($name in @(
        'Get-PveNodeConfig', 'Set-PveNodeConfig', 'Get-PveNodeDns', 'Set-PveNodeDns',
        'Start-PveNodeVms', 'Stop-PveNodeVms'
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
# Get-PveNodeConfig
# ---------------------------------------------------------------------------
Describe 'Get-PveNodeConfig' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveNodeConfig' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveNodeConfig'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveNodeConfig'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Get-PveNodeConfig'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveNodeConfig'
            { Get-PveNodeConfig -Node 'pve' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveNodeConfig
# ---------------------------------------------------------------------------
Describe 'Set-PveNodeConfig' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PveNodeConfig' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PveNodeConfig'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Set-PveNodeConfig'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Set-PveNodeConfig'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Set-PveNodeConfig'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Set-PveNodeConfig'
            { Set-PveNodeConfig -Node 'pve' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveNodeDns
# ---------------------------------------------------------------------------
Describe 'Get-PveNodeDns' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveNodeDns' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveNodeDns'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveNodeDns'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Get-PveNodeDns'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveNodeDns'
            { Get-PveNodeDns -Node 'pve' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveNodeDns
# ---------------------------------------------------------------------------
Describe 'Set-PveNodeDns' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PveNodeDns' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PveNodeDns'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Set-PveNodeDns'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Set-PveNodeDns'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Set-PveNodeDns'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Search should be Mandatory' {
            Skip-IfMissing 'Set-PveNodeDns'
            $isMandatory = $script:Cmd.Parameters['Search'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Set-PveNodeDns'
            { Set-PveNodeDns -Node 'pve' -Search 'example.com' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Start-PveNodeVms
# ---------------------------------------------------------------------------
Describe 'Start-PveNodeVms' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Start-PveNodeVms' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Start-PveNodeVms'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Start-PveNodeVms'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Start-PveNodeVms'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Start-PveNodeVms'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Start-PveNodeVms'
            { Start-PveNodeVms -Node 'pve' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Stop-PveNodeVms
# ---------------------------------------------------------------------------
Describe 'Stop-PveNodeVms' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Stop-PveNodeVms' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Stop-PveNodeVms'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Stop-PveNodeVms'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
        It 'Should declare ConfirmImpact High' {
            Skip-IfMissing 'Stop-PveNodeVms'
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Stop-PveNodeVms'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Stop-PveNodeVms'
            { Stop-PveNodeVms -Node 'pve' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
