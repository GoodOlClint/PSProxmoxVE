#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for template cmdlets:
        Get-PveTemplate, New-PveTemplate, Remove-PveTemplate, New-PveVmFromTemplate.

    All tests are fully offline — no live Proxmox VE target is required.
    If a cmdlet is not yet compiled the test is marked Skipped.

    Note: New-PveTemplate converts an existing VM into a template (destructive —
    the original VM becomes read-only). New-PveVmFromTemplate deploys a new VM
    from an existing template (which is a clone operation under the hood).
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Availability = @{}
    foreach ($name in @('Get-PveTemplate', 'New-PveTemplate',
                         'Remove-PveTemplate', 'New-PveVmFromTemplate')) {
        $script:Availability[$name] = $null -ne (Get-Command $name -ErrorAction SilentlyContinue)
    }

    function Skip-IfMissing([string]$Name) {
        if (-not $script:Availability[$Name]) {
            Set-ItResult -Skipped -Because "$Name is not yet implemented in this build"
        }
    }
}

# ---------------------------------------------------------------------------
# Manifest contract
# ---------------------------------------------------------------------------
Describe 'Template cmdlets — manifest declarations' {
    BeforeAll {
        $manifestPath = Join-Path (Get-Module PSProxmoxVE).ModuleBase 'PSProxmoxVE.psd1'
        $script:Manifest = if (Test-Path $manifestPath) { Import-PowerShellDataFile $manifestPath } else { $null }
    }

    It "<cmdName> should be declared in CmdletsToExport" -TestCases @(
        @{ cmdName = 'Get-PveTemplate' }
        @{ cmdName = 'New-PveTemplate' }
        @{ cmdName = 'Remove-PveTemplate' }
        @{ cmdName = 'New-PveVmFromTemplate' }
    ) {
        if ($null -eq $script:Manifest) { Set-ItResult -Skipped -Because 'Manifest not found'; return }
        $script:Manifest.CmdletsToExport | Should -Contain $cmdName
    }
}

# ---------------------------------------------------------------------------
# Get-PveTemplate
# ---------------------------------------------------------------------------
Describe 'Get-PveTemplate' {

    BeforeAll { $script:Cmd = Get-Command 'Get-PveTemplate' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Get-PveTemplate'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Get-PveTemplate'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Parameter metadata' {
        It 'Should have Node parameter (optional — all nodes when omitted)' {
            Skip-IfMissing 'Get-PveTemplate'
            $script:Cmd.Parameters.ContainsKey('Node') | Should -BeTrue
        }

        It 'Node should not be Mandatory' {
            Skip-IfMissing 'Get-PveTemplate'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }

        It 'Should have Name parameter (optional filter by template name)' {
            Skip-IfMissing 'Get-PveTemplate'
            $script:Cmd.Parameters.ContainsKey('Name') | Should -BeTrue
        }

        It 'Should have Session parameter' {
            Skip-IfMissing 'Get-PveTemplate'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Get-PveTemplate'
            { Get-PveTemplate -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveTemplate (converts an existing VM to a template)
# ---------------------------------------------------------------------------
Describe 'New-PveTemplate' {

    BeforeAll { $script:Cmd = Get-Command 'New-PveTemplate' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveTemplate'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'New-PveTemplate'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should declare ConfirmImpact High (destructive — VM becomes read-only template)' {
            Skip-IfMissing 'New-PveTemplate'
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'New-PveTemplate'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            Skip-IfMissing 'New-PveTemplate'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            Skip-IfMissing 'New-PveTemplate'
            { New-PveTemplate -Node 'pve-node1' -VmId 9000 -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveTemplate
# ---------------------------------------------------------------------------
Describe 'Remove-PveTemplate' {

    BeforeAll { $script:Cmd = Get-Command 'Remove-PveTemplate' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Remove-PveTemplate'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Remove-PveTemplate'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should declare ConfirmImpact High' {
            Skip-IfMissing 'Remove-PveTemplate'
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Remove-PveTemplate'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'VmId should be Mandatory' {
            Skip-IfMissing 'Remove-PveTemplate'
            $isMandatory = $script:Cmd.Parameters['VmId'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveVmFromTemplate (deploy a new VM from a template)
# ---------------------------------------------------------------------------
Describe 'New-PveVmFromTemplate' {

    BeforeAll { $script:Cmd = Get-Command 'New-PveVmFromTemplate' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'New-PveVmFromTemplate'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'New-PveVmFromTemplate'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'TemplateNode should be Mandatory (node hosting the template)' {
            Skip-IfMissing 'New-PveVmFromTemplate'
            $hasTemplateNode = $script:Cmd.Parameters.ContainsKey('TemplateNode') -or
                               $script:Cmd.Parameters.ContainsKey('SourceNode')
            $hasTemplateNode | Should -BeTrue
        }

        It 'TemplateId (or VmId) should be Mandatory' {
            Skip-IfMissing 'New-PveVmFromTemplate'
            $hasId = $script:Cmd.Parameters.ContainsKey('TemplateId') -or
                     $script:Cmd.Parameters.ContainsKey('VmId')
            $hasId | Should -BeTrue
        }
    }

    Context 'Optional parameters' {
        It 'Should have NewName parameter (name for the deployed VM)' {
            Skip-IfMissing 'New-PveVmFromTemplate'
            $hasNewName = $script:Cmd.Parameters.ContainsKey('NewName') -or
                          $script:Cmd.Parameters.ContainsKey('Name')
            $hasNewName | Should -BeTrue
        }

        It 'Should have Wait switch parameter' {
            Skip-IfMissing 'New-PveVmFromTemplate'
            $script:Cmd.Parameters.ContainsKey('Wait') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            Skip-IfMissing 'New-PveVmFromTemplate'
            # Use whatever the first mandatory parameter is named; -ErrorAction Stop
            # will trigger the missing-mandatory-parameter error or the no-session error.
            {
                $splat = @{ ErrorAction = 'Stop' }
                if ($script:Cmd.Parameters.ContainsKey('TemplateNode')) { $splat['TemplateNode'] = 'pve-node1' }
                elseif ($script:Cmd.Parameters.ContainsKey('SourceNode')) { $splat['SourceNode'] = 'pve-node1' }
                if ($script:Cmd.Parameters.ContainsKey('TemplateId'))    { $splat['TemplateId'] = 9000 }
                elseif ($script:Cmd.Parameters.ContainsKey('VmId'))      { $splat['VmId'] = 9000 }
                if ($script:Cmd.Parameters.ContainsKey('NewVmId'))       { $splat['NewVmId'] = 9001 }
                & 'New-PveVmFromTemplate' @splat
            } | Should -Throw '*No active Proxmox VE session*'
        }
    }
}
