#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for Send-PveFile.
    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1
}

Describe 'Send-PveFile' {
    Context 'ChecksumAlgorithm ValidateSet' {
        BeforeAll {
            $script:Cmd = Get-Command 'Send-PveFile'
        }

        It 'ChecksumAlgorithm should have a ValidateSet attribute' {
            $validateSetAttr = $script:Cmd.Parameters['ChecksumAlgorithm'].Attributes |
                Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $validateSetAttr | Should -Not -BeNullOrEmpty
        }

        It 'ChecksumAlgorithm ValidateSet should include md5' {
            $validateSetAttr = $script:Cmd.Parameters['ChecksumAlgorithm'].Attributes |
                Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] } |
                Select-Object -First 1
            $validateSetAttr.ValidValues | Should -Contain 'md5'
        }

        It 'ChecksumAlgorithm ValidateSet should include sha1' {
            $validateSetAttr = $script:Cmd.Parameters['ChecksumAlgorithm'].Attributes |
                Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] } |
                Select-Object -First 1
            $validateSetAttr.ValidValues | Should -Contain 'sha1'
        }

        It 'ChecksumAlgorithm ValidateSet should include sha256' {
            $validateSetAttr = $script:Cmd.Parameters['ChecksumAlgorithm'].Attributes |
                Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] } |
                Select-Object -First 1
            $validateSetAttr.ValidValues | Should -Contain 'sha256'
        }

        It 'ChecksumAlgorithm ValidateSet should include sha512' {
            $validateSetAttr = $script:Cmd.Parameters['ChecksumAlgorithm'].Attributes |
                Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] } |
                Select-Object -First 1
            $validateSetAttr.ValidValues | Should -Contain 'sha512'
        }
    }

    Context 'ContentType parameter' {
        BeforeAll {
            $script:Cmd = Get-Command 'Send-PveFile'
        }

        It 'ContentType should have a ValidateSet attribute with iso, vztmpl, import' {
            $validateSetAttr = $script:Cmd.Parameters['ContentType'].Attributes |
                Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] } |
                Select-Object -First 1
            $validateSetAttr.ValidValues | Should -Contain 'iso'
            $validateSetAttr.ValidValues | Should -Contain 'vztmpl'
            $validateSetAttr.ValidValues | Should -Contain 'import'
        }
    }

    Context 'ShouldProcess support' {
        BeforeAll {
            $script:Cmd = Get-Command 'Send-PveFile'
        }

        It 'Should support WhatIf' {
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Optional parameters' {
        BeforeAll {
            $script:Cmd = Get-Command 'Send-PveFile'
        }

        It 'TimeoutSeconds should reject negative values' {
            $tmpIso = [System.IO.Path]::GetTempFileName()
            try {
                { Send-PveFile -Node 'n' -Storage 's' -Path $tmpIso -TimeoutSeconds -1 -Confirm:$false -ErrorAction Stop } |
                    Should -Throw
            } finally { Remove-Item $tmpIso -ErrorAction SilentlyContinue }
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            $tmpIso = [System.IO.Path]::GetTempFileName()
            try {
                { Send-PveFile -Node 'pve-node1' -Storage 'local' -Path $tmpIso -Confirm:$false -ErrorAction Stop } |
                    Should -Throw '*No active Proxmox VE session*'
            } finally { Remove-Item $tmpIso -ErrorAction SilentlyContinue }
        }
    }
}
