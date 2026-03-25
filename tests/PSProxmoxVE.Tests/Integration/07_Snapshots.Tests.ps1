#Requires -Module Pester

BeforeAll {
    . $PSScriptRoot/_IntegrationHelper.ps1
    Connect-TestPve

    # Find the test VM created by 06_VMs.Tests.ps1
    $script:TestVmId = Find-TestVm

    function script:Skip-IfNoTestVm {
        if (Skip-IfNoTarget) { return $true }
        if ($null -eq $script:TestVmId) {
            Set-ItResult -Skipped -Because 'No test VM found (pester-test-vm must exist from 06_VMs)'
            return $true
        }
        return $false
    }
}

AfterAll {
    # Snapshot cleanup is inline (remove within the test).
    # No VM cleanup here — 06_VMs owns pester-test-vm.
    Disconnect-TestPve
}

Describe 'Snapshots — Integration' -Tag 'Integration' {
    Context 'Snapshots' {
        It 'Should create, list, and remove a snapshot' {
            if (Skip-IfNoTestVm) { return }

            # Ensure the VM is stopped for snapshot
            $vm = Get-PveVm -Node $script:Node | Where-Object { $_.VmId -eq $script:TestVmId }
            if ($vm.Status -eq 'running') {
                Stop-PveVm -Node $script:Node -VmId $script:TestVmId -Wait -Timeout 30 -Confirm:$false | Out-Null
            }

            $snapName = 'pester-snap'

            # Create
            $createTask = New-PveSnapshot `
                -Node        $script:Node `
                -VmId        $script:TestVmId `
                -Name        $snapName `
                -Description 'Created by Pester integration test' `
                -Wait

            $createTask | Should -Not -BeNullOrEmpty

            # List and verify
            $snapshots = Get-PveSnapshot -Node $script:Node -VmId $script:TestVmId
            $snap = $snapshots | Where-Object { $_.Name -eq $snapName }
            $snap | Should -Not -BeNullOrEmpty

            # Restore
            { Restore-PveSnapshot `
                -Node    $script:Node `
                -VmId    $script:TestVmId `
                -Name    $snapName `
                -Confirm:$false `
                -Wait } | Should -Not -Throw

            # Remove
            $removeTask = Remove-PveSnapshot `
                -Node    $script:Node `
                -VmId    $script:TestVmId `
                -Name    $snapName `
                -Confirm:$false `
                -Wait

            $removeTask | Should -Not -BeNullOrEmpty
        }
    }
}
