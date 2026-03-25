#Requires -Module Pester

BeforeAll {
    . $PSScriptRoot/_IntegrationHelper.ps1
    Connect-TestPve

    $script:SnapVmId = $null

    function script:Skip-IfNoSnapVm {
        if (Skip-IfNoTarget) { return $true }
        if ($null -eq $script:SnapVmId) {
            Set-ItResult -Skipped -Because 'Snapshot test VM was not created'
            return $true
        }
        return $false
    }
}

AfterAll {
    if (-not $script:SkipReason -and $script:SnapVmId) {
        try {
            Stop-PveVm -Node $script:Node -VmId $script:SnapVmId -Wait -Timeout 30 -Confirm:$false -ErrorAction SilentlyContinue | Out-Null
        } catch { }
        Start-Sleep -Seconds 2
        try {
            Remove-PveVm -Node $script:Node -VmId $script:SnapVmId -Force -Purge -Confirm:$false -ErrorAction SilentlyContinue
        } catch { }
    }
    Disconnect-TestPve
}

Describe 'Snapshots — Integration' -Tag 'Integration' {
    Context 'Setup' {
        It 'Should create a VM for snapshot tests' {
            if (Skip-IfNoTarget) { return }

            $task = New-PveVm `
                -Node    $script:Node `
                -Name    'pester-snap-vm' `
                -Memory  128 `
                -Cores   1 `
                -Wait

            $task | Should -Not -BeNullOrEmpty

            $vm = Get-PveVm -Node $script:Node -Name 'pester-snap-vm' |
                  Select-Object -First 1
            $vm | Should -Not -BeNullOrEmpty
            $script:SnapVmId = $vm.VmId
        }
    }

    Context 'Snapshots' {
        It 'Should create, list, and remove a snapshot' {
            if (Skip-IfNoSnapVm) { return }

            $snapName = 'pester-snap'

            # Create
            $createTask = New-PveSnapshot `
                -Node        $script:Node `
                -VmId        $script:SnapVmId `
                -Name        $snapName `
                -Description 'Created by Pester integration test' `
                -Wait

            $createTask | Should -Not -BeNullOrEmpty

            # List and verify
            $snapshots = Get-PveSnapshot -Node $script:Node -VmId $script:SnapVmId
            $snap = $snapshots | Where-Object { $_.Name -eq $snapName }
            $snap | Should -Not -BeNullOrEmpty

            # Restore
            { Restore-PveSnapshot `
                -Node    $script:Node `
                -VmId    $script:SnapVmId `
                -Name    $snapName `
                -Confirm:$false `
                -Wait } | Should -Not -Throw

            # Remove
            $removeTask = Remove-PveSnapshot `
                -Node    $script:Node `
                -VmId    $script:SnapVmId `
                -Name    $snapName `
                -Confirm:$false `
                -Wait

            $removeTask | Should -Not -BeNullOrEmpty
        }
    }
}
