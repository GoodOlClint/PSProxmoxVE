#Requires -Module Pester

BeforeAll {
    . $PSScriptRoot/_IntegrationHelper.ps1
    Connect-TestPve

    $script:TaskVmId = $null

    function script:Skip-IfNoTaskVm {
        if (Skip-IfNoTarget) { return $true }
        if ($null -eq $script:TaskVmId) {
            Set-ItResult -Skipped -Because 'Task test VM was not created'
            return $true
        }
        return $false
    }
}

AfterAll {
    if (-not $script:SkipReason -and $script:TaskVmId) {
        try {
            Stop-PveVm -Node $script:Node -VmId $script:TaskVmId -Wait -Timeout 30 -Confirm:$false -ErrorAction SilentlyContinue | Out-Null
        } catch { }
        Start-Sleep -Seconds 2
        try {
            Remove-PveVm -Node $script:Node -VmId $script:TaskVmId -Force -Purge -Confirm:$false -ErrorAction SilentlyContinue
        } catch { }
    }
    Disconnect-TestPve
}

Describe 'Tasks — Integration' -Tag 'Integration' {

    Context 'Setup' {
        It 'Should create a VM for task tests' {
            if (Skip-IfNoTarget) { return }

            $task = New-PveVm `
                -Node    $script:Node `
                -Name    'pester-task-vm' `
                -Memory  128 `
                -Cores   1 `
                -Wait

            $task | Should -Not -BeNullOrEmpty

            $vm = Get-PveVm -Node $script:Node -Name 'pester-task-vm' |
                  Select-Object -First 1
            $vm | Should -Not -BeNullOrEmpty
            $script:TaskVmId = $vm.VmId
        }
    }

    Context 'Tasks' {
        It 'Should get a task by UPID and wait for completion' {
            if (Skip-IfNoTaskVm) { return }

            # Start the VM to get a task object
            $startResult = Start-PveVm -Node $script:Node -VmId $script:TaskVmId
            $startResult | Should -Not -BeNullOrEmpty
            $startResult.Upid | Should -Not -BeNullOrEmpty

            # Wait for the task to complete
            { Wait-PveTask -Node $script:Node -Upid $startResult.Upid } | Should -Not -Throw

            # Get the task status
            $task = Get-PveTask -Node $script:Node -Upid $startResult.Upid
            $task | Should -Not -BeNullOrEmpty
            $task.IsSuccessful | Should -BeTrue

            # List tasks
            $tasks = Get-PveTaskList -Node $script:Node
            $tasks | Should -Not -BeNullOrEmpty

            # Clean up — stop the VM
            Stop-PveVm -Node $script:Node -VmId $script:TaskVmId -Wait -Timeout 30 -Confirm:$false | Out-Null
        }
    }
}
