#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests for Invoke-PveStorageDownload.

    All tests are fully offline — no live Proxmox VE target is required.
    If the cmdlet is not yet compiled the test is marked Skipped.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:CmdExists = $null -ne (Get-Command 'Invoke-PveStorageDownload' -ErrorAction SilentlyContinue)

    function Skip-IfMissing([string]$Name) {
        if (-not $script:CmdExists) {
            Set-ItResult -Skipped -Because "$Name is not yet implemented in this build"
        }
    }
}

# ---------------------------------------------------------------------------
# Manifest contract
# ---------------------------------------------------------------------------
Describe 'Invoke-PveStorageDownload — manifest declaration' {
    It 'Should be declared in CmdletsToExport' {
        $manifestPath = Join-Path (Get-Module PSProxmoxVE).ModuleBase 'PSProxmoxVE.psd1'
        if (-not (Test-Path $manifestPath)) { Set-ItResult -Skipped -Because 'Manifest not found'; return }
        $manifest = Import-PowerShellDataFile $manifestPath
        $manifest.CmdletsToExport | Should -Contain 'Invoke-PveStorageDownload'
    }
}

# ---------------------------------------------------------------------------
# Invoke-PveStorageDownload
# ---------------------------------------------------------------------------
Describe 'Invoke-PveStorageDownload' {

    BeforeAll { $script:Cmd = Get-Command 'Invoke-PveStorageDownload' -ErrorAction SilentlyContinue }

    Context 'Command existence' {
        It 'Should be available after module import' {
            Skip-IfMissing 'Invoke-PveStorageDownload'
            $script:Cmd | Should -Not -BeNullOrEmpty
        }

        It 'Should be a CmdletInfo (binary cmdlet)' {
            Skip-IfMissing 'Invoke-PveStorageDownload'
            $script:Cmd.CommandType | Should -Be 'Cmdlet'
        }
    }

    Context 'ShouldProcess support' {
        It 'Should support WhatIf' {
            Skip-IfMissing 'Invoke-PveStorageDownload'
            $script:Cmd.Parameters.ContainsKey('WhatIf') | Should -BeTrue
        }

        It 'Should support Confirm' {
            Skip-IfMissing 'Invoke-PveStorageDownload'
            $script:Cmd.Parameters.ContainsKey('Confirm') | Should -BeTrue
        }
    }

    Context 'Required parameters' {
        It 'Node should be Mandatory' {
            Skip-IfMissing 'Invoke-PveStorageDownload'
            $isMandatory = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Node should be at Position 0' {
            Skip-IfMissing 'Invoke-PveStorageDownload'
            $pos = $script:Cmd.Parameters['Node'].ParameterSets.Values |
                ForEach-Object { $_.Position }
            $pos | Should -Contain 0
        }

        It 'Storage should be Mandatory' {
            Skip-IfMissing 'Invoke-PveStorageDownload'
            $isMandatory = $script:Cmd.Parameters['Storage'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Storage should be at Position 1' {
            Skip-IfMissing 'Invoke-PveStorageDownload'
            $pos = $script:Cmd.Parameters['Storage'].ParameterSets.Values |
                ForEach-Object { $_.Position }
            $pos | Should -Contain 1
        }

        It 'Url should be Mandatory' {
            Skip-IfMissing 'Invoke-PveStorageDownload'
            $isMandatory = $script:Cmd.Parameters['Url'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Url should be at Position 2' {
            Skip-IfMissing 'Invoke-PveStorageDownload'
            $pos = $script:Cmd.Parameters['Url'].ParameterSets.Values |
                ForEach-Object { $_.Position }
            $pos | Should -Contain 2
        }

        It 'Filename should be Mandatory' {
            Skip-IfMissing 'Invoke-PveStorageDownload'
            $isMandatory = $script:Cmd.Parameters['Filename'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -Not -BeNullOrEmpty
        }

        It 'Filename should be at Position 3' {
            Skip-IfMissing 'Invoke-PveStorageDownload'
            $pos = $script:Cmd.Parameters['Filename'].ParameterSets.Values |
                ForEach-Object { $_.Position }
            $pos | Should -Contain 3
        }
    }

    Context 'Optional parameters' {
        It 'Should have ContentType parameter' {
            Skip-IfMissing 'Invoke-PveStorageDownload'
            $script:Cmd.Parameters.ContainsKey('ContentType') | Should -BeTrue
        }

        It 'ContentType should not be Mandatory' {
            Skip-IfMissing 'Invoke-PveStorageDownload'
            $isMandatory = $script:Cmd.Parameters['ContentType'].ParameterSets.Values |
                Where-Object { $_.IsMandatory }
            $isMandatory | Should -BeNullOrEmpty
        }

        It 'ContentType should have a ValidateSet of iso, vztmpl, backup, import' {
            Skip-IfMissing 'Invoke-PveStorageDownload'
            $validateSet = $script:Cmd.Parameters['ContentType'].Attributes |
                Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
            $validateSet | Should -Not -BeNullOrEmpty
            $validValues = $validateSet.ValidValues
            $validValues | Should -Contain 'iso'
            $validValues | Should -Contain 'vztmpl'
            $validValues | Should -Contain 'backup'
            $validValues | Should -Contain 'import'
        }

        It 'Should have Wait switch parameter' {
            Skip-IfMissing 'Invoke-PveStorageDownload'
            $script:Cmd.Parameters.ContainsKey('Wait') | Should -BeTrue
            $script:Cmd.Parameters['Wait'].SwitchParameter | Should -BeTrue
        }

        It 'Should have TimeoutSeconds parameter' {
            Skip-IfMissing 'Invoke-PveStorageDownload'
            $script:Cmd.Parameters.ContainsKey('TimeoutSeconds') | Should -BeTrue
        }

        It 'TimeoutSeconds should reject negative values' {
            Skip-IfMissing 'Invoke-PveStorageDownload'
            { Invoke-PveStorageDownload -Node 'pve1' -Storage 'local' -Url 'https://example.com/test.iso' -Filename 'test.iso' -TimeoutSeconds -1 -Confirm:$false -ErrorAction Stop } |
                Should -Throw
        }
    }

    Context 'Session parameter' {
        It 'Should have Session parameter (inherited from PveCmdletBase)' {
            Skip-IfMissing 'Invoke-PveStorageDownload'
            $script:Cmd.Parameters.ContainsKey('Session') | Should -BeTrue
        }
    }

    Context 'Without active session' {
        It 'Should throw when no session is active' {
            Skip-IfMissing 'Invoke-PveStorageDownload'
            { Invoke-PveStorageDownload -Node 'pve1' -Storage 'local' -Url 'https://example.com/test.iso' -Filename 'test.iso' -Confirm:$false -ErrorAction Stop } |
                Should -Throw '*No active Proxmox VE session*'
        }
    }
}
