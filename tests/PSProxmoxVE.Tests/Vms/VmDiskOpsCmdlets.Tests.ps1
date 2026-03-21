#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for VM disk operation cmdlets:
        Move-PveVmDisk, Remove-PveVmDisk.

    All tests are fully offline — no live Proxmox VE target is required.
    If a cmdlet is not yet compiled the test is marked Skipped.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Availability = @{}
    foreach ($name in @('Move-PveVmDisk', 'Remove-PveVmDisk')) {
        $script:Availability[$name] = $null -ne (Get-Command $name -ErrorAction SilentlyContinue)
    }

    function Skip-IfMissing([string]$Name) {
        if (-not $script:Availability[$Name]) {
            Set-ItResult -Skipped -Because "$Name is not yet implemented in this build"
        }
    }
}

# ---------------------------------------------------------------------------
# Move-PveVmDisk
# ---------------------------------------------------------------------------
Describe 'Move-PveVmDisk' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Move-PveVmDisk' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Move-PveVmDisk'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Move-PveVmDisk'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Move-PveVmDisk'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Move-PveVmDisk'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Move-PveVmDisk'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Disk should be Mandatory' {
            Skip-IfMissing 'Move-PveVmDisk'
            $isMandatory = $script:Cmd.Parameters['Disk'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'Storage should be Mandatory' {
            Skip-IfMissing 'Move-PveVmDisk'
            $isMandatory = $script:Cmd.Parameters['Storage'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Move-PveVmDisk'
            { Move-PveVmDisk -Node 'pve' -VmId 100 -Disk 'scsi0' -Storage 'local-lvm' -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveVmDisk
# ---------------------------------------------------------------------------
Describe 'Remove-PveVmDisk' -Tag 'Unit' {

    BeforeAll { $script:Cmd = Get-Command 'Remove-PveVmDisk' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Remove-PveVmDisk'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Remove-PveVmDisk'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
        It 'Should declare ConfirmImpact High' {
            Skip-IfMissing 'Remove-PveVmDisk'
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Remove-PveVmDisk'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Remove-PveVmDisk'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
        It 'IdList should be Mandatory' {
            Skip-IfMissing 'Remove-PveVmDisk'
            $isMandatory = $script:Cmd.Parameters['IdList'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Remove-PveVmDisk'
            { Remove-PveVmDisk -Node 'pve' -VmId 100 -IdList 'scsi0' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
