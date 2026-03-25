#Requires -Module Pester

BeforeAll {
    . $PSScriptRoot/_IntegrationHelper.ps1
    Connect-TestPve
}

AfterAll {
    if (-not $script:SkipReason) {
        # Clean up pester-store if it still exists
        try { Remove-PveStorage -Storage 'pester-store' -Confirm:$false -ErrorAction SilentlyContinue } catch { }
    }
    Disconnect-TestPve
}

Describe 'Storage — Integration' -Tag 'Integration' {
    Context 'Storage' {
        It 'Should list storage' {
            if (Skip-IfNoTarget) { return }

            $storage = Get-PveStorage -Node $script:Node
            $storage | Should -Not -BeNullOrEmpty
        }

        It 'Should list storage content' {
            if (Skip-IfNoTarget) { return }

            { Get-PveStorageContent `
                -Node    $script:Node `
                -Storage $script:Storage `
                -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should upload an ISO' {
            if (Skip-IfNoStorage) { return }

            if (-not (Test-Path $script:IsoPath)) {
                Set-ItResult -Skipped -Because "ISO file not found at PVETEST_ISO_PATH: $($script:IsoPath)"
                return
            }

            {
                Send-PveFile `
                    -Node    $script:Node `
                    -Storage $script:Storage `
                    -Path    $script:IsoPath `
                    -ErrorAction Stop
            } | Should -Not -Throw
        }
    }

    Context 'Storage — CRUD' {
        It 'Should create a directory storage (New-PveStorage)' {
            if (Skip-IfNoTarget) { return }

            $result = New-PveStorage -Storage 'pester-store' -Type 'dir' `
                -Path '/tmp/pester-storage' -Content 'iso,vztmpl,backup' `
                -ErrorAction Stop

            $result | Should -Not -BeNullOrEmpty
        }

        It 'Should list and find the new storage (Get-PveStorage)' {
            if (Skip-IfNoTarget) { return }

            $storages = Get-PveStorage -Node $script:Node
            $storages | Where-Object { $_.Storage -eq 'pester-store' } |
                Should -Not -BeNullOrEmpty
        }

        It 'Should remove the storage (Remove-PveStorage)' {
            if (Skip-IfNoTarget) { return }

            { Remove-PveStorage -Storage 'pester-store' -Confirm:$false -ErrorAction Stop } |
                Should -Not -Throw
        }
    }

    Context 'Storage — Download' {
        It 'Should download a file from URL to storage (Invoke-PveStorageDownload)' {
            if (Skip-IfNoTarget) { return }

            $templateUrl = 'http://download.proxmox.com/images/system/alpine-3.20-default_20240908_amd64.tar.xz'
            $task = Invoke-PveStorageDownload -Node $script:Node -Storage $script:Storage `
                -Url $templateUrl -Filename 'alpine-3.20-default_20240908_amd64.tar.xz' `
                -ContentType 'vztmpl' -Wait -ErrorAction Stop

            $task | Should -Not -BeNullOrEmpty
        }
    }
}
