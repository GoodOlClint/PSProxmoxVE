#Requires -Module Pester

BeforeAll {
    . $PSScriptRoot/_IntegrationHelper.ps1
    Connect-TestPve

    # Find an existing test VM to use for task tests
    $script:TestVmId = Find-TestVm
}

AfterAll {
    # Read-only tests — no cleanup needed
    Disconnect-TestPve
}

Describe 'Tasks — Integration' -Tag 'Integration' {

    Context 'Tasks' {
        It 'Should get a task by UPID and wait for completion' {
            if (Skip-IfNoTarget) { return }
            if ($null -eq $script:TestVmId) {
                Set-ItResult -Skipped -Because 'No test VM was created'
                return
            }

            # Start the VM to get a task object
            $startResult = Start-PveVm -Node $script:Node -VmId $script:TestVmId
            $startResult | Should -Not -BeNullOrEmpty
            $startResult.Upid | Should -Not -BeNullOrEmpty

            # Wait for the task to complete
            { Wait-PveTask -Node $script:Node -Upid $startResult.Upid } | Should -Not -Throw

            # Get the task status
            $task = Get-PveTask -Node $script:Node -Upid $startResult.Upid
            $task | Should -Not -BeNullOrEmpty
            $task.IsSuccessful | Should -BeTrue

            # Clean up
            Stop-PveVm -Node $script:Node -VmId $script:TestVmId -Wait -Timeout 30 -Confirm:$false | Out-Null
        }
    }
}
