#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for template cmdlets:
        Get-PveTemplate, New-PveTemplate, Remove-PveTemplate, New-PveVmFromTemplate.

    All tests are fully offline — no live Proxmox VE target is required.

    Note: New-PveTemplate converts an existing VM into a template (destructive —
    the original VM becomes read-only). New-PveVmFromTemplate deploys a new VM
    from an existing template (which is a clone operation under the hood).
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

# ---------------------------------------------------------------------------
# Get-PveTemplate
# ---------------------------------------------------------------------------
Describe 'Get-PveTemplate' {
    BeforeAll { $script:Cmd = Get-Command 'Get-PveTemplate' }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            { Get-PveTemplate -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveTemplate (converts an existing VM to a template)
# ---------------------------------------------------------------------------
Describe 'New-PveTemplate' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveTemplate' }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should declare ConfirmImpact High (destructive — VM becomes read-only template)' {
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            { New-PveTemplate -Node 'pve-node1' -VmId 9000 -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}

# ---------------------------------------------------------------------------
# Remove-PveTemplate
# ---------------------------------------------------------------------------
Describe 'Remove-PveTemplate' {
    BeforeAll { $script:Cmd = Get-Command 'Remove-PveTemplate' }

    Context 'ShouldProcess / ConfirmImpact' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should declare ConfirmImpact High' {
            $attr = $script:Cmd.ImplementingType.GetCustomAttributes(
                [System.Management.Automation.CmdletAttribute], $false) |
                Select-Object -First 1
            $attr.ConfirmImpact | Should -Be ([System.Management.Automation.ConfirmImpact]::High)
        }
    }
}

# ---------------------------------------------------------------------------
# New-PveVmFromTemplate (deploy a new VM from a template)
# ---------------------------------------------------------------------------
Describe 'New-PveVmFromTemplate' {
    BeforeAll { $script:Cmd = Get-Command 'New-PveVmFromTemplate' }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
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
