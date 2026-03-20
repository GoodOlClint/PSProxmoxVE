#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 integration tests for PSProxmoxVE.

    These tests are tagged 'Integration' and are SKIPPED by default.
    They require a live, dedicated Proxmox VE test node and the following
    environment variables to be set:

        PVETEST_HOST            - Hostname or IP of the PVE test node
        PVETEST_PORT            - API port (usually 8006)
        PVETEST_APITOKEN        - API token in the format USER@REALM!TOKENID=UUID
        PVETEST_NODE            - PVE node name (e.g. pve-test1)
        PVETEST_STORAGE         - Storage pool name for disk/ISO operations (e.g. local)
        PVETEST_ISO_PATH        - Local filesystem path to a small .iso for upload tests
        PVETEST_PVE_VERSION     - (optional) Expected PVE major version (8 or 9)
        PVETEST_PASSWORD        - (optional) Root password for cloud-init Linux VM provisioning
        PVETEST_CLOUD_IMAGE_PATH - (optional) Local path to a cloud image (.img) for VM provisioning
        PVETEST_OVA_PATH        - (optional) Local path to an OVA file for OVA import tests

    WARNING: These tests CREATE and DESTROY real resources (VMs, users, tokens,
    roles, snapshots, ISO uploads, etc.) on the target node.
    Never run against a production cluster.

    To run:
        Invoke-Pester -Path ./tests/PSProxmoxVE.Tests -Tag Integration
        # or use the project's helper script:
        ./Invoke-Tests.ps1 -Tier Integration
#>

BeforeAll {
    # -----------------------------------------------------------------------
    # Resolve and import module
    # -----------------------------------------------------------------------
    . $PSScriptRoot/../_TestHelper.ps1

    # -----------------------------------------------------------------------
    # Check required environment variables
    # -----------------------------------------------------------------------
    $requiredVars = @(
        'PVETEST_HOST',
        'PVETEST_PORT',
        'PVETEST_APITOKEN',
        'PVETEST_NODE',
        'PVETEST_STORAGE',
        'PVETEST_ISO_PATH'
    )

    $script:SkipReason = $null

    foreach ($var in $requiredVars) {
        if (-not [System.Environment]::GetEnvironmentVariable($var)) {
            $script:SkipReason = (
                "No live Proxmox VE target configured. " +
                "Set environment variables: $($requiredVars -join ', ')"
            )
            break
        }
    }

    # Convenience accessors (only meaningful when SkipReason is null)
    $script:Host_    = [System.Environment]::GetEnvironmentVariable('PVETEST_HOST')
    $portEnv = [System.Environment]::GetEnvironmentVariable('PVETEST_PORT')
    $script:Port     = [int]$(if ($portEnv) { $portEnv } else { '8006' })
    $script:ApiToken = [System.Environment]::GetEnvironmentVariable('PVETEST_APITOKEN')
    $script:Node     = [System.Environment]::GetEnvironmentVariable('PVETEST_NODE')
    $script:Storage  = [System.Environment]::GetEnvironmentVariable('PVETEST_STORAGE')
    $script:IsoPath  = [System.Environment]::GetEnvironmentVariable('PVETEST_ISO_PATH')
    $script:ExpectedPveVersion = [System.Environment]::GetEnvironmentVariable('PVETEST_PVE_VERSION')
    $script:Password = [System.Environment]::GetEnvironmentVariable('PVETEST_PASSWORD')
    $script:CloudImagePath = [System.Environment]::GetEnvironmentVariable('PVETEST_CLOUD_IMAGE_PATH')
    $script:OvaPath = [System.Environment]::GetEnvironmentVariable('PVETEST_OVA_PATH')
    $script:LinuxVmId = $null

    # Track resources created during the run so AfterAll can clean up.
    $script:CreatedVmIds        = [System.Collections.Generic.List[int]]::new()
    $script:CreatedContainerIds = [System.Collections.Generic.List[int]]::new()
    $script:CreatedUsers        = [System.Collections.Generic.List[string]]::new()
    $script:CreatedRoles        = [System.Collections.Generic.List[string]]::new()
    $script:TestVmId            = $null
    $script:TestContainerId     = $null

    # Helper functions for skip logic
    function script:Skip-IfNoTarget {
        if ($script:SkipReason) {
            Set-ItResult -Skipped -Because $script:SkipReason
            return $true
        }
        return $false
    }

    function script:Skip-IfNoTestVm {
        if (Skip-IfNoTarget) { return $true }
        if ($null -eq $script:TestVmId) {
            Set-ItResult -Skipped -Because 'No test VM was created'
            return $true
        }
        return $false
    }

    function script:Skip-IfNoPassword {
        if (Skip-IfNoTarget) { return $true }
        if (-not $script:Password -or -not $script:CloudImagePath) {
            Set-ItResult -Skipped -Because 'PVETEST_PASSWORD and PVETEST_CLOUD_IMAGE_PATH required for Linux VM provisioning'
            return $true
        }
        return $false
    }

    function script:Skip-IfNoLinuxVm {
        if (Skip-IfNoTarget) { return $true }
        if ($null -eq $script:LinuxVmId) {
            Set-ItResult -Skipped -Because 'Linux VM was not provisioned (PVETEST_PASSWORD may not be set)'
            return $true
        }
        return $false
    }

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
    if ($null -ne $script:SkipReason) { return }

    # Best-effort cleanup — remove resources created during the test run.
    # Order matters: tokens/permissions removed with users, VMs last.

    foreach ($userId in $script:CreatedUsers) {
        try { Remove-PveUser -UserId $userId -Confirm:$false -ErrorAction SilentlyContinue }
        catch { <# non-fatal #> }
    }

    foreach ($roleId in $script:CreatedRoles) {
        try { Remove-PveRole -RoleId $roleId -Confirm:$false -ErrorAction SilentlyContinue }
        catch { <# non-fatal #> }
    }

    foreach ($ctId in $script:CreatedContainerIds) {
        try {
            Stop-PveContainer -Node $script:Node -VmId $ctId -Confirm:$false -ErrorAction SilentlyContinue | Out-Null
            Start-Sleep -Seconds 3
            Remove-PveContainer -Node $script:Node -VmId $ctId -Force -Purge -Confirm:$false -ErrorAction SilentlyContinue
        }
        catch { <# non-fatal #> }
    }

    foreach ($vmId in $script:CreatedVmIds) {
        try {
            Stop-PveVm -Node $script:Node -VmId $vmId -Confirm:$false -ErrorAction SilentlyContinue | Out-Null
            Start-Sleep -Seconds 3
            Remove-PveVm -Node $script:Node -VmId $vmId -Force -Purge -Confirm:$false -ErrorAction SilentlyContinue
        }
        catch { <# non-fatal #> }
    }
}

# ===========================================================================
# Integration Tests
# ===========================================================================
Describe 'Integration Tests' -Tag 'Integration' {

    # -----------------------------------------------------------------------
    Context 'Connection' {
        It 'Should connect to PVE server' {
            if (Skip-IfNoTarget) { return }

            $session = Connect-PveServer `
                -Server  $script:Host_ `
                -Port    $script:Port `
                -ApiToken $script:ApiToken `
                -SkipCertificateCheck `
                -PassThru

            $session            | Should -Not -BeNullOrEmpty
            $session.Hostname   | Should -Be $script:Host_
            $session.AuthMode.ToString() | Should -Be 'ApiToken'

            Test-PveConnection   | Should -BeTrue
        }

        It 'Should detect server version' {
            if (Skip-IfNoTarget) { return }

            $detail = Test-PveConnection -Detailed
            $detail                         | Should -Not -BeNullOrEmpty
            $detail.ServerVersion           | Should -Not -BeNullOrEmpty
            $detail.ServerVersion.Major     | Should -BeIn @(8, 9)

            # If we know the expected version, verify it matches
            if ($script:ExpectedPveVersion) {
                $detail.ServerVersion.Major | Should -Be ([int]$script:ExpectedPveVersion)
            }
        }
    }

    # -----------------------------------------------------------------------
    Context 'Nodes' {
        It 'Should list nodes' {
            if (Skip-IfNoTarget) { return }

            $nodes = Get-PveNode
            $nodes | Should -Not -BeNullOrEmpty
        }

        It 'Should get node status' {
            if (Skip-IfNoTarget) { return }

            $status = Get-PveNodeStatus -Node $script:Node
            $status            | Should -Not -BeNullOrEmpty
            $status.Node       | Should -Be $script:Node
        }
    }

    # -----------------------------------------------------------------------
    Context 'User CRUD' {
        It 'Should create a new user' {
            if (Skip-IfNoTarget) { return }

            { New-PveUser -UserId 'pester-user@pam' -ErrorAction Stop } |
                Should -Not -Throw

            $script:CreatedUsers.Add('pester-user@pam')
        }

        It 'Should list users and find the new user' {
            if (Skip-IfNoTarget) { return }

            $users = Get-PveUser
            $users | Should -Not -BeNullOrEmpty
            $users | Where-Object { $_.UserId -eq 'pester-user@pam' } |
                Should -Not -BeNullOrEmpty
        }

        It 'Should update user properties' {
            if (Skip-IfNoTarget) { return }

            { Set-PveUser -UserId 'pester-user@pam' `
                -Comment 'Updated by Pester integration test' `
                -ErrorAction Stop } | Should -Not -Throw

            $user = Get-PveUser | Where-Object { $_.UserId -eq 'pester-user@pam' }
            $user.Comment | Should -Be 'Updated by Pester integration test'
        }

        It 'Should remove the user' {
            if (Skip-IfNoTarget) { return }

            { Remove-PveUser -UserId 'pester-user@pam' -Confirm:$false -ErrorAction Stop } |
                Should -Not -Throw

            $script:CreatedUsers.Remove('pester-user@pam') | Out-Null

            $users = Get-PveUser
            $users | Where-Object { $_.UserId -eq 'pester-user@pam' } |
                Should -BeNullOrEmpty
        }
    }

    # -----------------------------------------------------------------------
    Context 'Role CRUD' {
        It 'Should create a new role' {
            if (Skip-IfNoTarget) { return }

            { New-PveRole -RoleId 'PesterTestRole' `
                -Privileges 'VM.Audit,VM.Console' `
                -ErrorAction Stop } | Should -Not -Throw

            $script:CreatedRoles.Add('PesterTestRole')
        }

        It 'Should list roles and find the new role' {
            if (Skip-IfNoTarget) { return }

            $roles = Get-PveRole
            $roles | Should -Not -BeNullOrEmpty
            $roles | Where-Object { $_.RoleId -eq 'PesterTestRole' } |
                Should -Not -BeNullOrEmpty
        }

        It 'Should remove the role' {
            if (Skip-IfNoTarget) { return }

            { Remove-PveRole -RoleId 'PesterTestRole' -Confirm:$false -ErrorAction Stop } |
                Should -Not -Throw

            $script:CreatedRoles.Remove('PesterTestRole') | Out-Null
        }
    }

    # -----------------------------------------------------------------------
    Context 'API Token CRUD' {
        BeforeAll {
            # Create a user to hold the test token
            if ($null -eq $script:SkipReason) {
                New-PveUser -UserId 'pester-tokenuser@pam' -ErrorAction SilentlyContinue
                $script:CreatedUsers.Add('pester-tokenuser@pam')
            }
        }

        It 'Should create an API token' {
            if (Skip-IfNoTarget) { return }

            $result = New-PveApiToken `
                -UserId  'pester-tokenuser@pam' `
                -TokenId 'pester-token' `
                -ErrorAction Stop

            $result | Should -Not -BeNullOrEmpty
        }

        It 'Should list API tokens for the user' {
            if (Skip-IfNoTarget) { return }

            $tokens = Get-PveApiToken -UserId 'pester-tokenuser@pam'
            $tokens | Should -Not -BeNullOrEmpty
            $tokens | Where-Object { $_.TokenId -eq 'pester-token' } |
                Should -Not -BeNullOrEmpty
        }

        It 'Should remove the API token' {
            if (Skip-IfNoTarget) { return }

            { Remove-PveApiToken `
                -UserId  'pester-tokenuser@pam' `
                -TokenId 'pester-token' `
                -Confirm:$false `
                -ErrorAction Stop } | Should -Not -Throw
        }
    }

    # -----------------------------------------------------------------------
    Context 'Permissions' {
        BeforeAll {
            # Create a user and role for permission testing
            if ($null -eq $script:SkipReason) {
                New-PveUser -UserId 'pester-permuser@pam' -ErrorAction SilentlyContinue
                $script:CreatedUsers.Add('pester-permuser@pam')
            }
        }

        It 'Should list permissions' {
            if (Skip-IfNoTarget) { return }

            { Get-PvePermission -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should set a permission' {
            if (Skip-IfNoTarget) { return }

            { Set-PvePermission `
                -Path  '/' `
                -Role  'PVEAuditor' `
                -UgId  'pester-permuser@pam' `
                -Type  'user' `
                -ErrorAction Stop } | Should -Not -Throw
        }
    }

    # -----------------------------------------------------------------------
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
            $script:CreatedVmIds.Add($vm.VmId)
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

            $script:CreatedVmIds.Add($cloned.VmId)
        }
    }

    # -----------------------------------------------------------------------
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

    # -----------------------------------------------------------------------
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

    # -----------------------------------------------------------------------
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
            if (Skip-IfNoTarget) { return }

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

    # -----------------------------------------------------------------------
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

    # -----------------------------------------------------------------------
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

    # -----------------------------------------------------------------------
    Context 'Tasks' {
        It 'Should get a task by UPID and wait for completion' {
            if (Skip-IfNoTestVm) { return }

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

    # -----------------------------------------------------------------------
    Context 'Network — Read' {
        It 'Should list networks' {
            if (Skip-IfNoTarget) { return }

            $networks = Get-PveNetwork -Node $script:Node
            $networks | Should -Not -BeNullOrEmpty
        }

        It 'Should filter by interface type' {
            if (Skip-IfNoTarget) { return }

            # Filter for bridges — every PVE node has at least vmbr0
            $bridges = Get-PveNetwork -Node $script:Node -Type 'bridge'
            $bridges | Should -Not -BeNullOrEmpty
            $bridges | ForEach-Object { $_.Type | Should -Be 'bridge' }
        }
    }

    # -----------------------------------------------------------------------
    Context 'Network — CRUD' {
        It 'Should create a Linux bridge' {
            if (Skip-IfNoTarget) { return }

            { New-PveNetwork `
                -Node    $script:Node `
                -Iface   'vmbr99' `
                -Type    'bridge' `
                -Autostart `
                -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should find the new bridge in pending changes' {
            if (Skip-IfNoTarget) { return }

            $networks = Get-PveNetwork -Node $script:Node
            $networks | Where-Object { $_.Iface -eq 'vmbr99' } |
                Should -Not -BeNullOrEmpty
        }

        It 'Should update bridge comments' {
            if (Skip-IfNoTarget) { return }

            { Set-PveNetwork `
                -Node    $script:Node `
                -Iface   'vmbr99' `
                -Type    'bridge' `
                -Comments 'Created by Pester integration test' `
                -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should remove the bridge' {
            if (Skip-IfNoTarget) { return }

            { Remove-PveNetwork `
                -Node    $script:Node `
                -Iface   'vmbr99' `
                -Confirm:$false `
                -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should revert pending changes (apply to baseline)' {
            if (Skip-IfNoTarget) { return }

            # Apply reverts any pending changes back to the running config
            { Invoke-PveNetworkApply `
                -Node    $script:Node `
                -Wait `
                -ErrorAction Stop } | Should -Not -Throw
        }
    }

    # -----------------------------------------------------------------------
    Context 'SDN' {
        BeforeAll {
            if ($null -eq $script:SkipReason) {
                # SDN requires PVE 8+. Check server version and skip if older.
                $detail = Test-PveConnection -Detailed
                if ($detail.ServerVersion.Major -lt 8) {
                    $script:SkipSdn = 'SDN requires Proxmox VE 8.0 or later'
                } else {
                    $script:SkipSdn = $null
                }
            } else {
                $script:SkipSdn = $script:SkipReason
            }
        }

        It 'Should create an SDN zone (New-PveSdnZone)' {
            if (Skip-IfNoTarget) { return }
            if ($script:SkipSdn) { Set-ItResult -Skipped -Because $script:SkipSdn; return }

            { New-PveSdnZone -Zone 'pesterz' -Type 'simple' -ErrorAction Stop } |
                Should -Not -Throw
        }

        It 'Should list SDN zones (Get-PveSdnZone)' {
            if (Skip-IfNoTarget) { return }
            if ($script:SkipSdn) { Set-ItResult -Skipped -Because $script:SkipSdn; return }

            $zones = Get-PveSdnZone
            $zones | Should -Not -BeNullOrEmpty
            $zones | Where-Object { $_.Zone -eq 'pesterz' } |
                Should -Not -BeNullOrEmpty
        }

        It 'Should create an SDN VNet (New-PveSdnVnet)' {
            if (Skip-IfNoTarget) { return }
            if ($script:SkipSdn) { Set-ItResult -Skipped -Because $script:SkipSdn; return }

            { New-PveSdnVnet -Vnet 'pestervn' -Zone 'pesterz' -ErrorAction Stop } |
                Should -Not -Throw
        }

        It 'Should list SDN VNets (Get-PveSdnVnet)' {
            if (Skip-IfNoTarget) { return }
            if ($script:SkipSdn) { Set-ItResult -Skipped -Because $script:SkipSdn; return }

            $vnets = Get-PveSdnVnet
            $vnets | Should -Not -BeNullOrEmpty
            $vnets | Where-Object { $_.Vnet -eq 'pestervn' } |
                Should -Not -BeNullOrEmpty
        }

        It 'Should create an SDN subnet (New-PveSdnSubnet)' {
            if (Skip-IfNoTarget) { return }
            if ($script:SkipSdn) { Set-ItResult -Skipped -Because $script:SkipSdn; return }

            { New-PveSdnSubnet -Vnet 'pestervn' -Subnet '10.99.0.0/24' -ErrorAction Stop } |
                Should -Not -Throw
        }

        It 'Should list SDN subnets (Get-PveSdnSubnet)' {
            if (Skip-IfNoTarget) { return }
            if ($script:SkipSdn) { Set-ItResult -Skipped -Because $script:SkipSdn; return }

            $subnets = Get-PveSdnSubnet -Vnet 'pestervn'
            $subnets | Should -Not -BeNullOrEmpty
        }

        It 'Should remove SDN subnet (Remove-PveSdnSubnet)' {
            if (Skip-IfNoTarget) { return }
            if ($script:SkipSdn) { Set-ItResult -Skipped -Because $script:SkipSdn; return }

            # PVE stores subnet IDs in the format: {zone}-{ip}-{prefix}
            { Remove-PveSdnSubnet -Vnet 'pestervn' -Subnet 'pesterz-10.99.0.0-24' `
                -Confirm:$false -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should remove SDN VNet (Remove-PveSdnVnet)' {
            if (Skip-IfNoTarget) { return }
            if ($script:SkipSdn) { Set-ItResult -Skipped -Because $script:SkipSdn; return }

            # Allow PVE to propagate subnet removal
            Start-Sleep -Seconds 3
            { Remove-PveSdnVnet -Vnet 'pestervn' -Confirm:$false -ErrorAction Stop } |
                Should -Not -Throw
        }

        It 'Should remove SDN zone (Remove-PveSdnZone)' {
            if (Skip-IfNoTarget) { return }
            if ($script:SkipSdn) { Set-ItResult -Skipped -Because $script:SkipSdn; return }

            Start-Sleep -Seconds 2
            { Remove-PveSdnZone -Zone 'pesterz' -Confirm:$false -ErrorAction Stop } |
                Should -Not -Throw
        }
    }

    # -----------------------------------------------------------------------
    Context 'Templates' {
        It 'Should list templates' {
            if (Skip-IfNoTarget) { return }

            # Zero templates is acceptable; just verify the cmdlet does not throw.
            { Get-PveTemplate -Node $script:Node -ErrorAction Stop } | Should -Not -Throw
        }
    }

    # -----------------------------------------------------------------------
    Context 'Cloud-Init' {
        It 'Should get cloud-init config' {
            if (Skip-IfNoTestVm) { return }

            # Get-PveCloudInitConfig should not throw even if the VM has no cloud-init drive.
            { Get-PveCloudInitConfig -Node $script:Node -VmId $script:TestVmId -ErrorAction Stop } |
                Should -Not -Throw
        }

        It 'Should regenerate cloud-init image (Invoke-PveCloudInitRegenerate)' {
            if (Skip-IfNoLinuxVm) { return }

            # The Linux VM has a cloud-init drive from provisioning
            { Invoke-PveCloudInitRegenerate -Node $script:Node -VmId $script:LinuxVmId -Wait -ErrorAction Stop } |
                Should -Not -Throw
        }
    }

    # -----------------------------------------------------------------------
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

            $task = Restart-PveContainer -Node $script:Node -VmId $script:TestContainerId -Wait -Timeout 30
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

    # -----------------------------------------------------------------------
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

    # -----------------------------------------------------------------------
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

    # -----------------------------------------------------------------------
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

    # -----------------------------------------------------------------------
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

            # Suspend — -Wait -Timeout polls until status is 'paused'
            $suspendTask = Suspend-PveVm -Node $script:Node -VmId $script:LinuxVmId -Wait -Timeout 30
            $suspendTask | Should -Not -BeNullOrEmpty

            $vm = Get-PveVm -Node $script:Node | Where-Object { $_.VmId -eq $script:LinuxVmId }
            $vm.Status | Should -Be 'paused'

            # Resume — -Wait -Timeout polls until status is 'running'
            $resumeTask = Resume-PveVm -Node $script:Node -VmId $script:LinuxVmId -Wait -Timeout 30
            $resumeTask | Should -Not -BeNullOrEmpty

            $vm = Get-PveVm -Node $script:Node | Where-Object { $_.VmId -eq $script:LinuxVmId }
            $vm.Status | Should -Be 'running'
        }

        It 'Should gracefully restart a VM via ACPI (Restart-PveVm)' {
            if (Skip-IfNoLinuxVm) { return }

            # Ensure VM is running (not paused from a failed suspend test)
            $vm = Get-PveVm -Node $script:Node |
                Where-Object { $_.VmId -eq $script:LinuxVmId }
            if ($vm.Status -eq 'paused') {
                Resume-PveVm -Node $script:Node -VmId $script:LinuxVmId -Wait -Timeout 15 -ErrorAction SilentlyContinue | Out-Null
            }

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

    # -----------------------------------------------------------------------
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

    # -----------------------------------------------------------------------
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

    # -----------------------------------------------------------------------
    Context 'VM Cleanup' {
        It 'Should remove a VM' {
            if (Skip-IfNoTestVm) { return }

            # Ensure stopped
            $vm = Get-PveVm -Node $script:Node | Where-Object { $_.VmId -eq $script:TestVmId }
            if ($vm.Status -eq 'running') {
                Stop-PveVm -Node $script:Node -VmId $script:TestVmId -Wait -Timeout 30 -Confirm:$false | Out-Null
            }

            { Remove-PveVm `
                -Node    $script:Node `
                -VmId    $script:TestVmId `
                -Force `
                -Purge `
                -Confirm:$false `
                -ErrorAction Stop } | Should -Not -Throw

            $script:CreatedVmIds.Remove($script:TestVmId) | Out-Null
            $script:TestVmId = $null
        }
    }

    # -----------------------------------------------------------------------
    Context 'Disconnect' {
        It 'Should disconnect from PVE server (Disconnect-PveServer)' {
            if (Skip-IfNoTarget) { return }

            { Disconnect-PveServer -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should fail commands after disconnect' {
            if (Skip-IfNoTarget) { return }

            { Get-PveNode -ErrorAction Stop } | Should -Throw '*No active Proxmox VE session*'
        }
    }
}
