#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for extended guest agent cmdlets:
        Get-PveVmGuestOsInfo, Get-PveVmGuestFsInfo, Read-PveVmGuestFile,
        Write-PveVmGuestFile, Set-PveVmGuestPassword, Invoke-PveVmGuestFsTrim.

    All tests are fully offline — no live Proxmox VE target is required.
    If a cmdlet is not yet compiled the test is marked Skipped.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Availability = @{}
    foreach ($name in @(
        'Get-PveVmGuestOsInfo', 'Get-PveVmGuestFsInfo', 'Read-PveVmGuestFile',
        'Write-PveVmGuestFile', 'Set-PveVmGuestPassword', 'Invoke-PveVmGuestFsTrim'
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
# Get-PveVmGuestOsInfo
# ---------------------------------------------------------------------------
Describe 'Get-PveVmGuestOsInfo' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveVmGuestOsInfo' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveVmGuestOsInfo'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveVmGuestOsInfo'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Get-PveVmGuestOsInfo'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Get-PveVmGuestOsInfo'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveVmGuestOsInfo'
            { Get-PveVmGuestOsInfo -Node 'pve' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveVmGuestFsInfo
# ---------------------------------------------------------------------------
Describe 'Get-PveVmGuestFsInfo' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveVmGuestFsInfo' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveVmGuestFsInfo'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveVmGuestFsInfo'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Get-PveVmGuestFsInfo'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Get-PveVmGuestFsInfo'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveVmGuestFsInfo'
            { Get-PveVmGuestFsInfo -Node 'pve' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Read-PveVmGuestFile
# ---------------------------------------------------------------------------
Describe 'Read-PveVmGuestFile' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Read-PveVmGuestFile' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Read-PveVmGuestFile'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Read-PveVmGuestFile'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Read-PveVmGuestFile'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Read-PveVmGuestFile'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'File should be Mandatory' {
            Skip-IfMissing 'Read-PveVmGuestFile'
            $isMandatory = $script:Cmd.Parameters['File'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Read-PveVmGuestFile'
            { Read-PveVmGuestFile -Node 'pve' -VmId 100 -File '/etc/hostname' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Write-PveVmGuestFile
# ---------------------------------------------------------------------------
Describe 'Write-PveVmGuestFile' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Write-PveVmGuestFile' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Write-PveVmGuestFile'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Write-PveVmGuestFile'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Write-PveVmGuestFile'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Write-PveVmGuestFile'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Write-PveVmGuestFile'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'File should be Mandatory' {
            Skip-IfMissing 'Write-PveVmGuestFile'
            $isMandatory = $script:Cmd.Parameters['File'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Content should be Mandatory' {
            Skip-IfMissing 'Write-PveVmGuestFile'
            $isMandatory = $script:Cmd.Parameters['Content'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Write-PveVmGuestFile'
            { Write-PveVmGuestFile -Node 'pve' -VmId 100 -File '/tmp/test.txt' -Content 'hello' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveVmGuestPassword
# ---------------------------------------------------------------------------
Describe 'Set-PveVmGuestPassword' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PveVmGuestPassword' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PveVmGuestPassword'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Set-PveVmGuestPassword'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Set-PveVmGuestPassword'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Set-PveVmGuestPassword'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Set-PveVmGuestPassword'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Username should be Mandatory' {
            Skip-IfMissing 'Set-PveVmGuestPassword'
            $isMandatory = $script:Cmd.Parameters['Username'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Password should be Mandatory' {
            Skip-IfMissing 'Set-PveVmGuestPassword'
            $isMandatory = $script:Cmd.Parameters['Password'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Parameter types' {
        It 'Password should be SecureString' {
            Skip-IfMissing 'Set-PveVmGuestPassword'
            $script:Cmd.Parameters['Password'].ParameterType |
                Should -Be ([System.Security.SecureString])
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Set-PveVmGuestPassword'
            $secPwd = ConvertTo-SecureString 'dummy' -AsPlainText -Force
            { Set-PveVmGuestPassword -Node 'pve' -VmId 100 -Username 'root' -Password $secPwd -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Invoke-PveVmGuestFsTrim
# ---------------------------------------------------------------------------
Describe 'Invoke-PveVmGuestFsTrim' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Invoke-PveVmGuestFsTrim' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Invoke-PveVmGuestFsTrim'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Invoke-PveVmGuestFsTrim'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Invoke-PveVmGuestFsTrim'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Invoke-PveVmGuestFsTrim'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Invoke-PveVmGuestFsTrim'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Invoke-PveVmGuestFsTrim'
            { Invoke-PveVmGuestFsTrim -Node 'pve' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
