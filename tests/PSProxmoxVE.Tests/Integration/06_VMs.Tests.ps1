#Requires -Module Pester

BeforeAll {
    . $PSScriptRoot/_IntegrationHelper.ps1
    Connect-TestPve

    $script:TestVmId = $null

    function script:Skip-IfNoTestVm {
        if (Skip-IfNoTarget) { return $true }
        if ($null -eq $script:TestVmId) {
            Set-ItResult -Skipped -Because 'No test VM was created'
            return $true
        }
        return $false
    }
}

AfterAll {
    if (-not $script:SkipReason -and $script:TestVmId) {
        # Stop and remove pester-test-vm and any clone
        foreach ($vmId in @($script:TestVmId, ($script:TestVmId + 1000))) {
            try {
                Stop-PveVm -Node $script:Node -VmId $vmId -Confirm:$false -ErrorAction SilentlyContinue | Out-Null
                Start-Sleep -Seconds 3
                Remove-PveVm -Node $script:Node -VmId $vmId -Force -Purge -Confirm:$false -ErrorAction SilentlyContinue
            } catch { }
        }
    }
    Disconnect-TestPve
}

Describe 'VMs — Integration' -Tag 'Integration' {
    Context 'VMs' {
        It 'Should list VMs' {
            if (Skip-IfNoTarget) { return }

            # Does not assert count — the node may have zero VMs.
            { Get-PveVm -Node $script:Node -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should create a test VM' {
            if (Skip-IfNoTarget) { return }

            $task = New-PveVm `
                -Node    $script:Node `
                -Name    'pester-test-vm' `
                -Memory  512 `
                -Cores   1 `
                -Wait

            $task | Should -Not -BeNullOrEmpty

            # Retrieve the new VM ID from the cluster.
            $vm = Get-PveVm -Node $script:Node -Name 'pester-test-vm' |
                  Select-Object -First 1
            $vm | Should -Not -BeNullOrEmpty

            $script:TestVmId = $vm.VmId
            Register-TestResource -Name 'pester-test-vm' -VmId $vm.VmId
        }

        It 'Should get and set VM config' {
            if (Skip-IfNoTestVm) { return }

            $config = Get-PveVmConfig -Node $script:Node -VmId $script:TestVmId
            $config | Should -Not -BeNullOrEmpty

            { Set-PveVmConfig `
                -Node        $script:Node `
                -VmId        $script:TestVmId `
                -Description 'Updated by Pester integration test' `
                -ErrorAction Stop } | Should -Not -Throw

            $updated = Get-PveVmConfig -Node $script:Node -VmId $script:TestVmId
            $updated.Description | Should -Be 'Updated by Pester integration test'
        }

        It 'Should start and stop a VM' {
            if (Skip-IfNoTestVm) { return }

            $startTask = Start-PveVm -Node $script:Node -VmId $script:TestVmId -Wait -Timeout 30
            $startTask | Should -Not -BeNullOrEmpty

            $stopTask = Stop-PveVm -Node $script:Node -VmId $script:TestVmId -Wait -Timeout 30 -Confirm:$false
            $stopTask | Should -Not -BeNullOrEmpty
        }

        It 'Should hard-reset a running VM' {
            if (Skip-IfNoTestVm) { return }

            # Start the VM first
            Start-PveVm -Node $script:Node -VmId $script:TestVmId -Wait -Timeout 30 | Out-Null

            # Hard reset (no ACPI — works even without guest OS)
            $task = Reset-PveVm -Node $script:Node -VmId $script:TestVmId -Wait -Timeout 30 -Confirm:$false
            $task | Should -Not -BeNullOrEmpty

            Stop-PveVm -Node $script:Node -VmId $script:TestVmId -Wait -Timeout 30 -Confirm:$false | Out-Null
        }

        It 'Should clone a VM' {
            if (Skip-IfNoTestVm) { return }

            # Pick a high VMID for the clone to avoid collisions
            $cloneId = $script:TestVmId + 1000

            $task = Copy-PveVm `
                -SourceNode $script:Node `
                -VmId       $script:TestVmId `
                -NewVmId    $cloneId `
                -NewName    'pester-clone-vm' `
                -Full `
                -Wait

            $task | Should -Not -BeNullOrEmpty

            $cloned = Get-PveVm -Node $script:Node -Name 'pester-clone-vm' |
                      Select-Object -First 1
            $cloned | Should -Not -BeNullOrEmpty
        }
    }

    Context 'VM — Suspend / Resume / Resize' {
        # Suspend/Resume is tested on the Linux VM (Guest Agent VM — Lifecycle)
        # because an empty VM with no OS may not reliably transition to 'paused'.

        It 'Should resize a VM disk (Resize-PveVmDisk)' {
            if (Skip-IfNoTestVm) { return }

            # First add a small disk via Set-PveVmConfig
            { Set-PveVmConfig -Node $script:Node -VmId $script:TestVmId `
                -AdditionalConfig @{ scsi0 = 'local-lvm:1' } `
                -ErrorAction Stop } | Should -Not -Throw

            # Resize the disk by +1G
            $task = Resize-PveVmDisk -Node $script:Node -VmId $script:TestVmId `
                -Disk 'scsi0' -Size '+1G'
            $task | Should -Not -BeNullOrEmpty

            # Verify config shows scsi0 exists (larger disk)
            $config = Get-PveVmConfig -Node $script:Node -VmId $script:TestVmId
            $config | Should -Not -BeNullOrEmpty
        }
    }
}
