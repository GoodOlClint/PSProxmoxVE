#Requires -Module Pester

BeforeAll {
    . $PSScriptRoot/_IntegrationHelper.ps1
    Connect-TestPve

    $script:TestContainerId = $null
    $script:CreatedContainerIds = [System.Collections.Generic.List[int]]::new()

    function script:Skip-IfNoTestContainer {
        if (Skip-IfNoTarget) { return $true }
        if ($null -eq $script:TestContainerId) {
            Set-ItResult -Skipped -Because 'No test container was created'
            return $true
        }
        return $false
    }
}

AfterAll {
    if ($null -eq $script:SkipReason) {
        foreach ($ctId in $script:CreatedContainerIds) {
            try {
                Stop-PveContainer -Node $script:Node -VmId $ctId -Confirm:$false -ErrorAction SilentlyContinue | Out-Null
                Start-Sleep -Seconds 3
                Remove-PveContainer -Node $script:Node -VmId $ctId -Force -Purge -Confirm:$false -ErrorAction SilentlyContinue
            }
            catch { <# non-fatal #> }
        }
    }
    Disconnect-TestPve
}

Describe 'Containers — Integration' -Tag 'Integration' {

    Context 'Containers' {
        BeforeAll {
            if ($null -eq $script:SkipReason) {
                # Download a small Alpine LXC template
                $templateUrl = 'http://download.proxmox.com/images/system/alpine-3.20-default_20240908_amd64.tar.xz'
                try {
                    Invoke-PveStorageDownload -Node $script:Node -Storage $script:Storage `
                        -Url $templateUrl -Filename 'alpine-3.20-default_20240908_amd64.tar.xz' `
                        -ContentType 'vztmpl' -Wait -ErrorAction Stop
                } catch {
                    # Template may already exist from a previous run or the Storage — Download context
                }
            }
            $script:TestContainerId = $null
        }

        It 'Should list containers (Get-PveContainer)' {
            if (Skip-IfNoTarget) { return }

            # Zero containers is acceptable; just verify the cmdlet does not throw.
            { Get-PveContainer -Node $script:Node -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should create a container (New-PveContainer)' {
            if (Skip-IfNoTarget) { return }

            $securePassword = ConvertTo-SecureString 'Pester12345!' -AsPlainText -Force

            $task = New-PveContainer `
                -Node          $script:Node `
                -Hostname      'pester-ct' `
                -Memory        128 `
                -Cores         1 `
                -RootFsStorage 'local-lvm' `
                -RootFsSize    '1' `
                -OsTemplate    "$($script:Storage):vztmpl/alpine-3.20-default_20240908_amd64.tar.xz" `
                -Password      $securePassword `
                -Bridge        'vmbr0' `
                -Wait

            $task | Should -Not -BeNullOrEmpty

            # Retrieve the new container ID from the cluster
            $ct = Get-PveContainer -Node $script:Node -Name 'pester-ct' |
                Select-Object -First 1
            $ct | Should -Not -BeNullOrEmpty

            $script:TestContainerId = $ct.VmId
            $script:CreatedContainerIds.Add($ct.VmId)
        }

        It 'Should get container config (Get-PveContainerConfig)' {
            if (Skip-IfNoTestContainer) { return }

            $config = Get-PveContainerConfig -Node $script:Node -VmId $script:TestContainerId
            $config | Should -Not -BeNullOrEmpty
        }

        It 'Should update container config (Set-PveContainerConfig)' {
            if (Skip-IfNoTestContainer) { return }

            { Set-PveContainerConfig -Node $script:Node -VmId $script:TestContainerId `
                -Description 'Updated by Pester integration test' `
                -ErrorAction Stop } | Should -Not -Throw

            $config = Get-PveContainerConfig -Node $script:Node -VmId $script:TestContainerId
            $config.Description.Trim() | Should -Be 'Updated by Pester integration test'
        }

        It 'Should start a container (Start-PveContainer)' {
            if (Skip-IfNoTestContainer) { return }

            $task = Start-PveContainer -Node $script:Node -VmId $script:TestContainerId -Wait -Timeout 30
            $task | Should -Not -BeNullOrEmpty

            $ct = Get-PveContainer -Node $script:Node -VmId $script:TestContainerId
            $ct.Status | Should -Be 'running'
        }

        It 'Should stop a container (Stop-PveContainer)' {
            if (Skip-IfNoTestContainer) { return }

            $task = Stop-PveContainer -Node $script:Node -VmId $script:TestContainerId -Wait -Timeout 30 -Confirm:$false
            $task | Should -Not -BeNullOrEmpty

            $ct = Get-PveContainer -Node $script:Node -VmId $script:TestContainerId
            $ct.Status | Should -Be 'stopped'
        }

        It 'Should restart a container (Restart-PveContainer)' {
            if (Skip-IfNoTestContainer) { return }

            # Start first so we can restart
            Start-PveContainer -Node $script:Node -VmId $script:TestContainerId -Wait -Timeout 30 | Out-Null

            $task = Restart-PveContainer -Node $script:Node -VmId $script:TestContainerId -Wait -Timeout 30 -Confirm:$false
            $task | Should -Not -BeNullOrEmpty

            $ct = Get-PveContainer -Node $script:Node -VmId $script:TestContainerId
            $ct.Status | Should -Be 'running'

            # Stop for subsequent tests
            Stop-PveContainer -Node $script:Node -VmId $script:TestContainerId -Wait -Timeout 30 -Confirm:$false | Out-Null
        }

        It 'Should clone a container (Copy-PveContainer)' {
            if (Skip-IfNoTestContainer) { return }

            $cloneId = $script:TestContainerId + 1000

            $task = Copy-PveContainer `
                -SourceNode $script:Node `
                -VmId       $script:TestContainerId `
                -NewVmId    $cloneId `
                -NewName    'pester-clone-ct' `
                -Full `
                -Wait

            $task | Should -Not -BeNullOrEmpty

            $cloned = Get-PveContainer -Node $script:Node -Name 'pester-clone-ct' |
                Select-Object -First 1
            $cloned | Should -Not -BeNullOrEmpty

            $script:CreatedContainerIds.Add($cloned.VmId)
        }
    }

    Context 'Container Snapshots' {
        It 'Should create a container snapshot (New-PveContainerSnapshot)' {
            if (Skip-IfNoTestContainer) { return }

            # Ensure container is stopped
            $ct = Get-PveContainer -Node $script:Node -VmId $script:TestContainerId
            if ($ct.Status -eq 'running') {
                Stop-PveContainer -Node $script:Node -VmId $script:TestContainerId -Wait -Timeout 30 -Confirm:$false | Out-Null
            }

            $task = New-PveContainerSnapshot `
                -Node        $script:Node `
                -VmId        $script:TestContainerId `
                -Name        'pester-ct-snap' `
                -Description 'Created by Pester integration test' `
                -Wait

            $task | Should -Not -BeNullOrEmpty
        }

        It 'Should list container snapshots (Get-PveContainerSnapshot)' {
            if (Skip-IfNoTestContainer) { return }

            $snapshots = Get-PveContainerSnapshot -Node $script:Node -VmId $script:TestContainerId
            $snap = $snapshots | Where-Object { $_.Name -eq 'pester-ct-snap' }
            $snap | Should -Not -BeNullOrEmpty
        }

        It 'Should restore a container snapshot (Restore-PveContainerSnapshot)' {
            if (Skip-IfNoTestContainer) { return }

            { Restore-PveContainerSnapshot `
                -Node    $script:Node `
                -VmId    $script:TestContainerId `
                -Name    'pester-ct-snap' `
                -Confirm:$false `
                -Wait } | Should -Not -Throw
        }

        It 'Should remove a container snapshot (Remove-PveContainerSnapshot)' {
            if (Skip-IfNoTestContainer) { return }

            $task = Remove-PveContainerSnapshot `
                -Node    $script:Node `
                -VmId    $script:TestContainerId `
                -Name    'pester-ct-snap' `
                -Confirm:$false `
                -Wait

            $task | Should -Not -BeNullOrEmpty
        }
    }
}
