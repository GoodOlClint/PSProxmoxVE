#Requires -Module Pester

BeforeAll {
    . $PSScriptRoot/_IntegrationHelper.ps1
    Connect-TestPve

    $script:CreatedVmIds = [System.Collections.Generic.List[int]]::new()
}

AfterAll {
    if ($null -eq $script:SkipReason) {
        foreach ($vmId in $script:CreatedVmIds) {
            try {
                Stop-PveVm -Node $script:Node -VmId $vmId -Confirm:$false -ErrorAction SilentlyContinue | Out-Null
                Start-Sleep -Seconds 3
                Remove-PveVm -Node $script:Node -VmId $vmId -Force -Purge -Confirm:$false -ErrorAction SilentlyContinue
            }
            catch { <# non-fatal #> }
        }
    }
    Disconnect-TestPve
}

Describe 'OVA Import — Integration' -Tag 'Integration' {

    Context 'OVA Import' {
        It 'Should parse OVF metadata from an OVA (client-side)' {
            if (Skip-IfNoTarget) { return }
            if (-not $script:OvaPath -or -not (Test-Path $script:OvaPath)) {
                Set-ItResult -Skipped -Because 'PVETEST_OVA_PATH not set or file not found'
                return
            }

            $metadata = [PSProxmoxVE.Core.Models.Vms.OvfMetadata]::FromOva($script:OvaPath)
            $metadata | Should -Not -BeNullOrEmpty
            $metadata.Name | Should -Not -BeNullOrEmpty
            $metadata.CpuCount | Should -BeGreaterThan 0
            $metadata.MemoryMB | Should -BeGreaterThan 0
            $metadata.Disks.Count | Should -BeGreaterThan 0
        }

        It 'Should import OVA as a VM with correct metadata (Import-PveOva)' {
            if (Skip-IfNoTarget) { return }
            if (-not $script:OvaPath -or -not (Test-Path $script:OvaPath)) {
                Set-ItResult -Skipped -Because 'PVETEST_OVA_PATH not set or file not found'
                return
            }

            $vm = Import-PveOva -Node $script:Node -Storage $script:Storage `
                -Path $script:OvaPath -TargetStorage 'local-lvm' `
                -Name 'pester-ova-vm' -Wait

            $vm | Should -Not -BeNullOrEmpty
            $script:CreatedVmIds.Add($vm.VmId)

            # Verify VM exists with correct name
            $found = Get-PveVm -Node $script:Node -Name 'pester-ova-vm' | Select-Object -First 1
            $found | Should -Not -BeNullOrEmpty
            $found.Name | Should -Be 'pester-ova-vm'

            # Verify config matches OVF metadata
            $config = Get-PveVmConfig -Node $script:Node -VmId $vm.VmId
            $config.Memory | Should -BeGreaterThan 0
            $config.Cores | Should -BeGreaterThan 0
            $config.OsType | Should -Be 'l26'
            $config.Scsi0 | Should -Not -BeNullOrEmpty -Because 'disk should be imported to scsi0'
            $config.Net0 | Should -Not -BeNullOrEmpty -Because 'network adapter should be configured'
        }

        It 'Should start the OVA-imported VM' {
            if (Skip-IfNoTarget) { return }

            $ovaVm = Get-PveVm -Node $script:Node -Name 'pester-ova-vm' -ErrorAction SilentlyContinue |
                Select-Object -First 1
            if (-not $ovaVm) {
                Set-ItResult -Skipped -Because 'OVA VM was not imported'
                return
            }

            $task = Start-PveVm -Node $script:Node -VmId $ovaVm.VmId -Wait -Timeout 60
            $task | Should -Not -BeNullOrEmpty

            $vm = Get-PveVm -Node $script:Node | Where-Object { $_.VmId -eq $ovaVm.VmId }
            $vm.Status | Should -Be 'running'

            # Clean up
            Stop-PveVm -Node $script:Node -VmId $ovaVm.VmId -Wait -Timeout 30 -Confirm:$false | Out-Null
        }
    }
}
