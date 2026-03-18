#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for Send-PveIso.
    All tests are fully offline — no live Proxmox VE target is required.
    If the cmdlet is not yet compiled the tests are marked Skipped.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:CmdExists = $null -ne (Get-Command 'Send-PveIso' -ErrorAction SilentlyContinue)
}

Describe 'Send-PveIso' {

    Context 'Manifest declaration' {
        It 'Should be declared in CmdletsToExport' {
            $manifestPath = Join-Path (Get-Module PSProxmoxVE).ModuleBase 'PSProxmoxVE.psd1'
            if (-not (Test-Path $manifestPath)) { Set-ItResult -Skipped -Because 'Manifest not found'; return }
            $manifest = Import-PowerShellDataFile $manifestPath
            $manifest.CmdletsToExport | Should -Contain 'Send-PveIso'
        }
    }

    Context 'Command existence' {
        It 'Should be available after module import' {
            if (-not $script:CmdExists) { Set-ItResult -Skipped -Because 'Not yet compiled'; return }
            (Get-Command 'Send-PveIso').CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'Required parameters' {
        BeforeAll {
            $script:Cmd = Get-Command 'Send-PveIso' -ErrorAction SilentlyContinue
        }

        It 'Should have Node parameter (Mandatory)' {
            if (-not $script:CmdExists) { Set-ItResult -Skipped -Because 'Not yet compiled'; return }
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Should have Storage parameter (Mandatory)' {
            if (-not $script:CmdExists) { Set-ItResult -Skipped -Because 'Not yet compiled'; return }
            $isMandatory = $script:Cmd.Parameters['Storage'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Should have Path parameter (Mandatory)' {
            if (-not $script:CmdExists) { Set-ItResult -Skipped -Because 'Not yet compiled'; return }
            $isMandatory = $script:Cmd.Parameters['Path'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Should throw when Node is omitted' {
            if (-not $script:CmdExists) { Set-ItResult -Skipped -Because 'Not yet compiled'; return }
            { Send-PveIso -Storage 'local' -Path '/tmp/test.iso' -ErrorAction Stop } |
                Should -Throw
        }

        It 'Should throw when Storage is omitted' {
            if (-not $script:CmdExists) { Set-ItResult -Skipped -Because 'Not yet compiled'; return }
            { Send-PveIso -Node 'pve-node1' -Path '/tmp/test.iso' -ErrorAction Stop } |
                Should -Throw
        }

        It 'Should throw when Path is omitted' {
            if (-not $script:CmdExists) { Set-ItResult -Skipped -Because 'Not yet compiled'; return }
            { Send-PveIso -Node 'pve-node1' -Storage 'local' -ErrorAction Stop } |
                Should -Throw
        }
    }

    Context 'ChecksumAlgorithm ValidateSet' {
        BeforeAll {
            $script:Cmd = Get-Command 'Send-PveIso' -ErrorAction SilentlyContinue
        }

        It 'Should have ChecksumAlgorithm parameter' {
            if (-not $script:CmdExists) { Set-ItResult -Skipped -Because 'Not yet compiled'; return }
            $script:Cmd.Parameters.ContainsKey('ChecksumAlgorithm') | Should -BeTrue
        }

        It 'ChecksumAlgorithm should have a ValidateSet attribute' {
            if (-not $script:CmdExists) { Set-ItResult -Skipped -Because 'Not yet compiled'; return }
            $validateSetAttr = $script:Cmd.Parameters['ChecksumAlgorithm'].Attributes |
                Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $validateSetAttr | Should -Not -BeNullOrEmpty
        }

        It 'ChecksumAlgorithm ValidateSet should include md5' {
            if (-not $script:CmdExists) { Set-ItResult -Skipped -Because 'Not yet compiled'; return }
            $validateSetAttr = $script:Cmd.Parameters['ChecksumAlgorithm'].Attributes |
                Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] } |
                Select-Object -First 1
            $validateSetAttr.ValidValues | Should -Contain 'md5'
        }

        It 'ChecksumAlgorithm ValidateSet should include sha1' {
            if (-not $script:CmdExists) { Set-ItResult -Skipped -Because 'Not yet compiled'; return }
            $validateSetAttr = $script:Cmd.Parameters['ChecksumAlgorithm'].Attributes |
                Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] } |
                Select-Object -First 1
            $validateSetAttr.ValidValues | Should -Contain 'sha1'
        }

        It 'ChecksumAlgorithm ValidateSet should include sha256' {
            if (-not $script:CmdExists) { Set-ItResult -Skipped -Because 'Not yet compiled'; return }
            $validateSetAttr = $script:Cmd.Parameters['ChecksumAlgorithm'].Attributes |
                Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] } |
                Select-Object -First 1
            $validateSetAttr.ValidValues | Should -Contain 'sha256'
        }

        It 'ChecksumAlgorithm ValidateSet should include sha512' {
            if (-not $script:CmdExists) { Set-ItResult -Skipped -Because 'Not yet compiled'; return }
            $validateSetAttr = $script:Cmd.Parameters['ChecksumAlgorithm'].Attributes |
                Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] } |
                Select-Object -First 1
            $validateSetAttr.ValidValues | Should -Contain 'sha512'
        }
    }

    Context 'ShouldProcess support' {
        BeforeAll {
            $script:Cmd = Get-Command 'Send-PveIso' -ErrorAction SilentlyContinue
        }

        It 'Should support WhatIf' {
            if (-not $script:CmdExists) { Set-ItResult -Skipped -Because 'Not yet compiled'; return }
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }
    }

    Context 'Optional parameters' {
        BeforeAll {
            $script:Cmd = Get-Command 'Send-PveIso' -ErrorAction SilentlyContinue
        }

        It 'Should have Checksum parameter' {
            if (-not $script:CmdExists) { Set-ItResult -Skipped -Because 'Not yet compiled'; return }
            $script:Cmd.Parameters.ContainsKey('Checksum') | Should -BeTrue
        }

        It 'Should have Session parameter' {
            if (-not $script:CmdExists) { Set-ItResult -Skipped -Because 'Not yet compiled'; return }
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active (without -WhatIf)' {
            if (-not $script:CmdExists) { Set-ItResult -Skipped -Because 'Not yet compiled'; return }
            $tmpIso = [System.IO.Path]::GetTempFileName()
            try {
                { Send-PveIso -Node 'pve-node1' -Storage 'local' -Path $tmpIso -Confirm:$false -ErrorAction Stop } |
                    Should -Throw '*No active Proxmox VE session*'
            } finally { Remove-Item $tmpIso -ErrorAction SilentlyContinue }
        }
    }
}
