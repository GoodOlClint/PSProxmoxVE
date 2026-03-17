#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 integration tests for PSProxmoxVE.

    These tests are tagged 'Integration' and are SKIPPED by default.
    They require a live, dedicated Proxmox VE test node and the following
    environment variables to be set:

        PVETEST_HOST        - Hostname or IP of the PVE test node
        PVETEST_PORT        - API port (usually 8006)
        PVETEST_APITOKEN    - API token in the format USER@REALM!TOKENID=UUID
        PVETEST_NODE        - PVE node name (e.g. pve-test1)
        PVETEST_STORAGE     - Storage pool name for disk/ISO operations (e.g. local)
        PVETEST_ISO_PATH    - Local filesystem path to a small .iso for upload tests

    WARNING: These tests CREATE and DESTROY real resources (VMs, snapshots,
    ISO uploads, etc.) on the target node. Never run against a production cluster.

    To run:
        Invoke-Pester -Path ./tests/PSProxmoxVE.Tests -Tag Integration
        # or use the project's helper script:
        ./Invoke-Tests.ps1 -Tier Integration
#>

BeforeAll {
    # -----------------------------------------------------------------------
    # Resolve and import module
    # -----------------------------------------------------------------------
    $moduleRoot = Resolve-Path (Join-Path $PSScriptRoot '../../../src/PSProxmoxVE')
    $dllCandidates = @(
        Join-Path $moduleRoot 'bin/Debug/net8.0/PSProxmoxVE.dll'
        Join-Path $moduleRoot 'bin/Release/net8.0/PSProxmoxVE.dll'
        Join-Path $moduleRoot 'bin/Debug/net48/PSProxmoxVE.dll'
        Join-Path $moduleRoot 'bin/Release/net48/PSProxmoxVE.dll'
    )

    $moduleDll = $dllCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

    if ($null -eq $moduleDll) {
        throw "PSProxmoxVE.dll not found. Build the project before running Pester tests."
    }

    Import-Module $moduleDll -Force -ErrorAction Stop

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
    $script:Port     = [int]([System.Environment]::GetEnvironmentVariable('PVETEST_PORT') ?? '8006')
    $script:ApiToken = [System.Environment]::GetEnvironmentVariable('PVETEST_APITOKEN')
    $script:Node     = [System.Environment]::GetEnvironmentVariable('PVETEST_NODE')
    $script:Storage  = [System.Environment]::GetEnvironmentVariable('PVETEST_STORAGE')
    $script:IsoPath  = [System.Environment]::GetEnvironmentVariable('PVETEST_ISO_PATH')

    # Track resources created during the run so AfterAll can clean up.
    $script:CreatedVmIds   = [System.Collections.Generic.List[int]]::new()
    $script:CreatedVmSnaps = [System.Collections.Generic.List[hashtable]]::new()
}

AfterAll {
    # Best-effort cleanup — remove any VMs created during the test run.
    if ($null -eq $script:SkipReason -and $script:CreatedVmIds.Count -gt 0) {
        foreach ($vmId in $script:CreatedVmIds) {
            try {
                Remove-PveVm -Node $script:Node -VmId $vmId -Force -Purge -Confirm:$false -ErrorAction SilentlyContinue
            }
            catch { <# non-fatal #> }
        }
    }
}

# ===========================================================================
# Integration Tests
# ===========================================================================
Describe 'Integration Tests' -Tag 'Integration' {

    # -----------------------------------------------------------------------
    Context 'Connection' {
        It 'Should connect to PVE server' {
            if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }

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
            if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }

            $detail = Test-PveConnection -Detailed
            $detail                         | Should -Not -BeNullOrEmpty
            $detail.ServerVersion           | Should -Not -BeNullOrEmpty
            $detail.ServerVersion.Major     | Should -BeIn @(8, 9)
        }
    }

    # -----------------------------------------------------------------------
    Context 'Nodes' {
        It 'Should list nodes' {
            if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }

            $nodes = Get-PveNode
            $nodes | Should -Not -BeNullOrEmpty
        }

        It 'Should get node status' {
            if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }

            $status = Get-PveNodeStatus -Node $script:Node
            $status            | Should -Not -BeNullOrEmpty
            $status.Node       | Should -Be $script:Node
        }
    }

    # -----------------------------------------------------------------------
    Context 'VMs' {
        It 'Should list VMs' {
            if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }

            # Does not assert count — the node may have zero VMs.
            { Get-PveVm -Node $script:Node -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should create and remove a test VM' {
            if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }

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

            $script:CreatedVmIds.Add($vm.VmId)

            $removeTask = Remove-PveVm `
                -Node    $script:Node `
                -VmId    $vm.VmId `
                -Confirm:$false `
                -Wait

            $removeTask | Should -Not -BeNullOrEmpty
            $script:CreatedVmIds.Remove($vm.VmId) | Out-Null
        }

        It 'Should start and stop a VM' {
            if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }

            # Requires at least one VM on the test node; skip if none found.
            $vm = Get-PveVm -Node $script:Node | Where-Object { $_.Status -eq 'stopped' } |
                  Select-Object -First 1

            if ($null -eq $vm) {
                Set-ItResult -Skipped -Because 'No stopped VM available on the test node'
                return
            }

            $startTask = Start-PveVm -Node $script:Node -VmId $vm.VmId -Wait
            $startTask | Should -Not -BeNullOrEmpty

            $stopTask = Stop-PveVm -Node $script:Node -VmId $vm.VmId -Wait
            $stopTask | Should -Not -BeNullOrEmpty
        }

        It 'Should clone a VM' {
            if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }

            $templateVm = Get-PveVm -Node $script:Node -TemplatesOnly | Select-Object -First 1

            if ($null -eq $templateVm) {
                Set-ItResult -Skipped -Because 'No template VM available on the test node'
                return
            }

            $task = Copy-PveVm `
                -SourceNode $script:Node `
                -VmId       $templateVm.VmId `
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
    Context 'Storage' {
        It 'Should list storage' {
            if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }

            $storage = Get-PveStorage -Node $script:Node
            $storage | Should -Not -BeNullOrEmpty
        }

        It 'Should upload an ISO' {
            if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }

            if (-not (Test-Path $script:IsoPath)) {
                Set-ItResult -Skipped -Because "ISO file not found at PVETEST_ISO_PATH: $($script:IsoPath)"
                return
            }

            {
                Send-PveIso `
                    -Node    $script:Node `
                    -Storage $script:Storage `
                    -Path    $script:IsoPath `
                    -ErrorAction Stop
            } | Should -Not -Throw
        }
    }

    # -----------------------------------------------------------------------
    Context 'Snapshots' {
        It 'Should create and remove a snapshot' {
            if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }

            $vm = Get-PveVm -Node $script:Node | Where-Object { $_.Status -eq 'stopped' } |
                  Select-Object -First 1

            if ($null -eq $vm) {
                Set-ItResult -Skipped -Because 'No stopped VM available for snapshot test'
                return
            }

            $snapName = 'pester-snap'

            $createTask = New-PveSnapshot `
                -Node        $script:Node `
                -VmId        $vm.VmId `
                -Name        $snapName `
                -Description 'Created by Pester integration test' `
                -Wait

            $createTask | Should -Not -BeNullOrEmpty

            $snapshots = Get-PveSnapshot -Node $script:Node -VmId $vm.VmId
            $snapshots | Where-Object { $_.Name -eq $snapName } | Should -Not -BeNullOrEmpty

            $removeTask = Remove-PveSnapshot `
                -Node    $script:Node `
                -VmId    $vm.VmId `
                -Name    $snapName `
                -Confirm:$false `
                -Wait

            $removeTask | Should -Not -BeNullOrEmpty
        }
    }

    # -----------------------------------------------------------------------
    Context 'Network' {
        It 'Should list networks' {
            if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }

            $networks = Get-PveNetwork -Node $script:Node
            $networks | Should -Not -BeNullOrEmpty
        }
    }

    # -----------------------------------------------------------------------
    Context 'Users' {
        It 'Should list users' {
            if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }

            $users = Get-PveUser
            $users | Should -Not -BeNullOrEmpty
            # root@pam is always present
            $users | Where-Object { $_.UserId -eq 'root@pam' } | Should -Not -BeNullOrEmpty
        }
    }

    # -----------------------------------------------------------------------
    Context 'Templates' {
        It 'Should list templates' {
            if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }

            # Zero templates is acceptable; just verify the cmdlet does not throw.
            { Get-PveTemplate -Node $script:Node -ErrorAction Stop } | Should -Not -Throw
        }
    }

    # -----------------------------------------------------------------------
    Context 'Cloud-Init' {
        It 'Should get cloud-init config' {
            if ($script:SkipReason) { Set-ItResult -Skipped -Because $script:SkipReason; return }

            $ciVm = Get-PveVm -Node $script:Node | Where-Object { $_.Status -eq 'stopped' } |
                    Select-Object -First 1

            if ($null -eq $ciVm) {
                Set-ItResult -Skipped -Because 'No stopped VM available for cloud-init test'
                return
            }

            # Get-PveCloudInitConfig should not throw even if the VM has no cloud-init drive.
            { Get-PveCloudInitConfig -Node $script:Node -VmId $ciVm.VmId -ErrorAction Stop } |
                Should -Not -Throw
        }
    }
}
