#Requires -Module Pester

BeforeAll {
    . $PSScriptRoot/_IntegrationHelper.ps1
    Connect-TestPve

    # Find a test VM for cloud-init config read test
    $script:TestVmId = Find-TestVm
    $script:LinuxVmId = Find-TestVm -Name 'pester-linux-vm'
}

AfterAll {
    # Read-only tests — no cleanup needed
    Disconnect-TestPve
}

Describe 'Cloud-Init — Integration' -Tag 'Integration' {
    Context 'Cloud-Init' {
        It 'Should get cloud-init config' {
            if (Skip-IfNoTarget) { return }
            if ($null -eq $script:TestVmId) {
                Set-ItResult -Skipped -Because 'No test VM found'
                return
            }

            # Get-PveCloudInitConfig should not throw even if the VM has no cloud-init drive.
            { Get-PveCloudInitConfig -Node $script:Node -VmId $script:TestVmId -ErrorAction Stop } |
                Should -Not -Throw
        }

        It 'Should regenerate cloud-init image (Invoke-PveCloudInitRegenerate)' {
            if (Skip-IfNoTarget) { return }
            if ($null -eq $script:LinuxVmId) {
                Set-ItResult -Skipped -Because 'Linux VM was not provisioned (PVETEST_PASSWORD may not be set)'
                return
            }

            # The Linux VM has a cloud-init drive from provisioning
            { Invoke-PveCloudInitRegenerate -Node $script:Node -VmId $script:LinuxVmId -Wait -ErrorAction Stop } |
                Should -Not -Throw
        }
    }
}
