#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for extended guest agent cmdlets:
        Get-PveVmGuestOsInfo, Get-PveVmGuestFsInfo, Read-PveVmGuestFile,
        Write-PveVmGuestFile, Set-PveVmGuestPassword, Invoke-PveVmGuestFsTrim.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Get-PveVmGuestOsInfo
# ---------------------------------------------------------------------------
Describe 'Get-PveVmGuestOsInfo' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveVmGuestOsInfo' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveVmGuestOsInfo -Node 'pve' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveVmGuestFsInfo
# ---------------------------------------------------------------------------
Describe 'Get-PveVmGuestFsInfo' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveVmGuestFsInfo' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveVmGuestFsInfo -Node 'pve' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Read-PveVmGuestFile
# ---------------------------------------------------------------------------
Describe 'Read-PveVmGuestFile' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Read-PveVmGuestFile' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Read-PveVmGuestFile -Node 'pve' -VmId 100 -File '/etc/hostname' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Write-PveVmGuestFile
# ---------------------------------------------------------------------------
Describe 'Write-PveVmGuestFile' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Write-PveVmGuestFile' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Write-PveVmGuestFile -Node 'pve' -VmId 100 -File '/tmp/test.txt' -Content 'hello' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveVmGuestPassword
# ---------------------------------------------------------------------------
Describe 'Set-PveVmGuestPassword' -Tag 'Unit' {
    BeforeAll { $script:Cmd = Get-Command 'Set-PveVmGuestPassword' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Parameter types' {
        It 'Password should be SecureString' {
            $script:Cmd.Parameters['Password'].ParameterType |
                Should -Be ([System.Security.SecureString])
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
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
    BeforeAll { $script:Cmd = Get-Command 'Invoke-PveVmGuestFsTrim' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Invoke-PveVmGuestFsTrim -Node 'pve' -VmId 100 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
