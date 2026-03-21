#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for storage management cmdlets:
        Set-PveStorage, Get-PveStorageStatus, Remove-PveStorageContent,
        Set-PveStorageContent, New-PveStorageDisk.

    All tests are fully offline — no live Proxmox VE target is required.
    If a cmdlet is not yet compiled the test is marked Skipped.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Availability = @{}
    foreach ($name in @(
        'Set-PveStorage', 'Get-PveStorageStatus', 'Remove-PveStorageContent',
        'Set-PveStorageContent', 'New-PveStorageDisk'
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
# Set-PveStorage
# ---------------------------------------------------------------------------
Describe 'Set-PveStorage' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PveStorage' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PveStorage'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Set-PveStorage'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Set-PveStorage'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Storage should be Mandatory' {
            Skip-IfMissing 'Set-PveStorage'
            $isMandatory = $script:Cmd.Parameters['Storage'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Set-PveStorage'
            { Set-PveStorage -Storage 'local' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Get-PveStorageStatus
# ---------------------------------------------------------------------------
Describe 'Get-PveStorageStatus' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveStorageStatus' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveStorageStatus'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveStorageStatus'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Get-PveStorageStatus'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Storage should be Mandatory' {
            Skip-IfMissing 'Get-PveStorageStatus'
            $isMandatory = $script:Cmd.Parameters['Storage'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveStorageStatus'
            { Get-PveStorageStatus -Node 'pve' -Storage 'local' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveStorageContent
# ---------------------------------------------------------------------------
Describe 'Remove-PveStorageContent' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Remove-PveStorageContent' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Remove-PveStorageContent'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Remove-PveStorageContent'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
        It 'Should declare ConfirmImpact High' {
            Skip-IfMissing 'Remove-PveStorageContent'
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Remove-PveStorageContent'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Storage should be Mandatory' {
            Skip-IfMissing 'Remove-PveStorageContent'
            $isMandatory = $script:Cmd.Parameters['Storage'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Volume should be Mandatory' {
            Skip-IfMissing 'Remove-PveStorageContent'
            $isMandatory = $script:Cmd.Parameters['Volume'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Remove-PveStorageContent'
            { Remove-PveStorageContent -Node 'pve' -Storage 'local' -Volume 'local:iso/test.iso' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Set-PveStorageContent
# ---------------------------------------------------------------------------
Describe 'Set-PveStorageContent' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Set-PveStorageContent' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Set-PveStorageContent'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Set-PveStorageContent'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Set-PveStorageContent'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Set-PveStorageContent'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Storage should be Mandatory' {
            Skip-IfMissing 'Set-PveStorageContent'
            $isMandatory = $script:Cmd.Parameters['Storage'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Volume should be Mandatory' {
            Skip-IfMissing 'Set-PveStorageContent'
            $isMandatory = $script:Cmd.Parameters['Volume'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Set-PveStorageContent'
            { Set-PveStorageContent -Node 'pve' -Storage 'local' -Volume 'local:iso/test.iso' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveStorageDisk
# ---------------------------------------------------------------------------
Describe 'New-PveStorageDisk' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'New-PveStorageDisk' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveStorageDisk'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'New-PveStorageDisk'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'New-PveStorageDisk'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'New-PveStorageDisk'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Storage should be Mandatory' {
            Skip-IfMissing 'New-PveStorageDisk'
            $isMandatory = $script:Cmd.Parameters['Storage'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Filename should be Mandatory' {
            Skip-IfMissing 'New-PveStorageDisk'
            $isMandatory = $script:Cmd.Parameters['Filename'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Size should be Mandatory' {
            Skip-IfMissing 'New-PveStorageDisk'
            $isMandatory = $script:Cmd.Parameters['Size'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'New-PveStorageDisk'
            { New-PveStorageDisk -Node 'pve' -Storage 'local' -Filename 'vm-100-disk-1' -Size '32G' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
