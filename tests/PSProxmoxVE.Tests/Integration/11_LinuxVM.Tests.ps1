#Requires -Module Pester

BeforeAll {
    . $PSScriptRoot/_IntegrationHelper.ps1
    Connect-TestPve

    $script:LinuxVmId = $null
    $script:CreatedVmIds = [System.Collections.Generic.List[int]]::new()

    function script:Skip-IfNoLinuxVm {
        if (Skip-IfNoTarget) { return $true }
        if ($null -eq $script:LinuxVmId) {
            Set-ItResult -Skipped -Because 'Linux VM was not provisioned (PVETEST_PASSWORD may not be set)'
            return $true
        }
        return $false
    }
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

Describe 'Linux VM — Integration' -Tag 'Integration' {

    Context 'Linux VM — Provisioning' {
        It 'Should upload cloud image to PVE storage (Send-PveFile)' {
            if (Skip-IfNoPassword) { return }

            $task = Send-PveFile `
                -Node $script:Node -Storage $script:Storage `
                -Path $script:CloudImagePath `
                -ContentType 'import' -Wait

            $task | Should -Not -BeNullOrEmpty
            $task.IsSuccessful | Should -BeTrue
        }

        It 'Should create a Linux VM (New-PveVm)' {
            if (Skip-IfNoPassword) { return }

            $task = New-PveVm `
                -Node $script:Node `
                -Name 'pester-linux-vm' `
                -Memory 512 -Cores 1 -OsType 'l26' -Wait

            $task | Should -Not -BeNullOrEmpty

            $vm = Get-PveVm -Node $script:Node -Name 'pester-linux-vm' |
                Select-Object -First 1
            $vm | Should -Not -BeNullOrEmpty

            $script:LinuxVmId = $vm.VmId
            $script:CreatedVmIds.Add($vm.VmId)
            Register-TestResource -Name 'pester-linux-vm' -VmId $vm.VmId
        }

        It 'Should import cloud image disk (Import-PveVmDisk)' {
            if (Skip-IfNoLinuxVm) { return }

            $cloudImageFilename = [System.IO.Path]::GetFileName($script:CloudImagePath)
            $task = Import-PveVmDisk `
                -Node $script:Node -VmId $script:LinuxVmId `
                -Disk 'scsi0' -TargetStorage 'local-lvm' `
                -Source "$($script:Storage):import/$cloudImageFilename" `
                -Wait

            $task | Should -Not -BeNullOrEmpty
            $task.IsSuccessful | Should -BeTrue
        }

        It 'Should configure VM hardware (Set-PveVmConfig)' {
            if (Skip-IfNoLinuxVm) { return }

            { Set-PveVmConfig -Node $script:Node -VmId $script:LinuxVmId `
                -AdditionalConfig @{
                    scsihw   = 'virtio-scsi-single'
                    boot     = 'order=scsi0'
                    serial0  = 'socket'
                    agent    = '1'
                    net0     = 'virtio,bridge=vmbr0'
                    ide2     = 'local-lvm:cloudinit'
                    cicustom = "user=$($script:Storage):snippets/test-vm-userdata.yml"
                } -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should set cloud-init config (Set-PveCloudInitConfig)' {
            if (Skip-IfNoLinuxVm) { return }

            { Set-PveCloudInitConfig -Node $script:Node -VmId $script:LinuxVmId `
                -CiUser 'root' `
                -Password (ConvertTo-SecureString $script:Password -AsPlainText -Force) `
                -IpConfig0 'ip=dhcp' `
                -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should start the Linux VM (Start-PveVm)' {
            if (Skip-IfNoLinuxVm) { return }

            $task = Start-PveVm -Node $script:Node -VmId $script:LinuxVmId -Wait -Timeout 30
            $task | Should -Not -BeNullOrEmpty
        }

        It 'Should wait for guest agent (Test-PveVmGuestAgent)' {
            if (Skip-IfNoLinuxVm) { return }

            $timeout = 300; $elapsed = 0
            $agentReady = $false
            while ($elapsed -lt $timeout) {
                if (Test-PveVmGuestAgent -Node $script:Node -VmId $script:LinuxVmId) {
                    $agentReady = $true
                    break
                }
                Start-Sleep -Seconds 10
                $elapsed += 10
            }

            $agentReady | Should -BeTrue -Because "Guest agent should respond within ${timeout}s"
        }
    }

    Context 'Guest Agent — Cmdlets' {
        It 'Should ping guest agent (Test-PveVmGuestAgent)' {
            if (Skip-IfNoLinuxVm) { return }

            $result = Test-PveVmGuestAgent -Node $script:Node -VmId $script:LinuxVmId
            $result | Should -BeTrue
        }

        It 'Should discover guest network interfaces (Get-PveVmGuestNetwork)' {
            if (Skip-IfNoLinuxVm) { return }

            $interfaces = Get-PveVmGuestNetwork -Node $script:Node -VmId $script:LinuxVmId
            $interfaces | Should -Not -BeNullOrEmpty

            # Should have at least one interface with an IPv4 address
            $withIp = $interfaces | Where-Object {
                $_.IpAddresses | Where-Object { $_.Type -eq 'ipv4' -and $_.Address -ne '127.0.0.1' }
            }
            $withIp | Should -Not -BeNullOrEmpty
        }

        It 'Should execute a command in guest (Invoke-PveVmGuestExec)' {
            if (Skip-IfNoLinuxVm) { return }

            $result = Invoke-PveVmGuestExec -Node $script:Node -VmId $script:LinuxVmId -Command 'hostname'
            $result | Should -Not -BeNullOrEmpty
            $result.ExitCode | Should -Be 0
            $result.Stdout | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Guest Agent VM — Lifecycle' {
        It 'Should have a running Linux VM with guest agent' {
            if (Skip-IfNoLinuxVm) { return }

            $vm = Get-PveVm -Node $script:Node |
                Where-Object { $_.VmId -eq $script:LinuxVmId }
            $vm | Should -Not -BeNullOrEmpty
            $vm.Status | Should -Be 'running'
        }

        It 'Should suspend and resume a running VM (Suspend-PveVm / Resume-PveVm)' {
            if (Skip-IfNoLinuxVm) { return }

            # Suspend — -Wait -Timeout polls qmpstatus until 'paused'
            $suspendTask = Suspend-PveVm -Node $script:Node -VmId $script:LinuxVmId -Wait -Timeout 30 -Confirm:$false
            $suspendTask | Should -Not -BeNullOrEmpty

            # Verify with -Detailed (fetches qmpstatus from status/current endpoint)
            $vm = Get-PveVm -Node $script:Node -VmId $script:LinuxVmId -Detailed
            $vm.EffectiveStatus | Should -Be 'paused'

            # Resume — -Wait -Timeout polls qmpstatus until 'running'
            $resumeTask = Resume-PveVm -Node $script:Node -VmId $script:LinuxVmId -Wait -Timeout 30
            $resumeTask | Should -Not -BeNullOrEmpty

            $vm = Get-PveVm -Node $script:Node -VmId $script:LinuxVmId -Detailed
            $vm.EffectiveStatus | Should -Be 'running'
        }

        It 'Should gracefully restart a VM via ACPI (Restart-PveVm)' {
            if (Skip-IfNoLinuxVm) { return }

            # Ensure VM is running (resume if paused from a failed suspend test)
            try { Resume-PveVm -Node $script:Node -VmId $script:LinuxVmId -Wait -Timeout 10 -ErrorAction SilentlyContinue | Out-Null }
            catch { <# already running #> }

            $task = Restart-PveVm -Node $script:Node -VmId $script:LinuxVmId -Wait -Timeout 60 -Confirm:$false
            $task | Should -Not -BeNullOrEmpty

            $vm = Get-PveVm -Node $script:Node |
                Where-Object { $_.VmId -eq $script:LinuxVmId }
            $vm.Status | Should -Be 'running'
        }

        It 'Should gracefully stop a VM via ACPI (Stop-PveVm)' {
            if (Skip-IfNoLinuxVm) { return }

            $task = Stop-PveVm -Node $script:Node -VmId $script:LinuxVmId -Wait -Timeout 30 -Confirm:$false
            $task | Should -Not -BeNullOrEmpty

            $vm = Get-PveVm -Node $script:Node |
                Where-Object { $_.VmId -eq $script:LinuxVmId }
            $vm.Status | Should -Be 'stopped'
        }
    }

    Context 'Templates — Convert and Clone' {
        It 'Should convert the Linux VM to a template' {
            if (Skip-IfNoLinuxVm) { return }

            # Ensure stopped first
            $vm = Get-PveVm -Node $script:Node |
                Where-Object { $_.VmId -eq $script:LinuxVmId }
            if ($vm.Status -eq 'running') {
                Stop-PveVm -Node $script:Node -VmId $script:LinuxVmId -Wait -Timeout 30 -Confirm:$false | Out-Null
            }

            { New-PveTemplate -Node $script:Node -VmId $script:LinuxVmId -Confirm:$false -ErrorAction Stop } |
                Should -Not -Throw

            # Allow a moment for the template flag to propagate
            Start-Sleep -Seconds 3

            # Verify it appears as a template
            $templates = Get-PveTemplate -Node $script:Node
            $templates | Where-Object { $_.VmId -eq $script:LinuxVmId } |
                Should -Not -BeNullOrEmpty
        }

        It 'Should clone a VM from the template (New-PveVmFromTemplate)' {
            if (Skip-IfNoLinuxVm) { return }

            $cloneId = $script:LinuxVmId + 1000

            $task = New-PveVmFromTemplate `
                -TemplateNode $script:Node `
                -VmId         $script:LinuxVmId `
                -NewVmId      $cloneId `
                -NewName      'pester-from-template' `
                -Full `
                -Wait

            $task | Should -Not -BeNullOrEmpty

            $cloned = Get-PveVm -Node $script:Node -Name 'pester-from-template' |
                Select-Object -First 1
            $cloned | Should -Not -BeNullOrEmpty

            # Track for cleanup
            $script:CreatedVmIds.Add($cloned.VmId)
        }

        It 'Should remove a template (Remove-PveTemplate)' {
            if (Skip-IfNoLinuxVm) { return }

            { Remove-PveTemplate -Node $script:Node -VmId $script:LinuxVmId `
                -Confirm:$false -ErrorAction Stop } | Should -Not -Throw

            $script:CreatedVmIds.Remove($script:LinuxVmId) | Out-Null
            $script:LinuxVmId = $null
        }
    }
}
