#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 integration tests for shared storage backends (NFS, iSCSI).

    These tests require the multi-node integration test infrastructure with
    Docker-based storage services (iSCSI target + NFS server). They are
    SKIPPED when the storage env vars are not set.

    Required environment variables (in addition to base integration vars):
        PVETEST_STORAGE_VM_IP   - IP of the storage host running iSCSI/NFS
        PVETEST_ISCSI_IQN       - iSCSI target IQN
        PVETEST_NFS_EXPORT      - NFS export path (e.g. 10.0.0.1:/srv/nfs/shared)

    WARNING: These tests CREATE and DESTROY storage definitions on the target
    PVE node. Never run against a production cluster.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    # --- Base integration vars ---
    $baseVars = @('PVETEST_HOST', 'PVETEST_PORT', 'PVETEST_APITOKEN', 'PVETEST_NODE')
    $script:SkipReason = $null

    foreach ($var in $baseVars) {
        if (-not [System.Environment]::GetEnvironmentVariable($var)) {
            $script:SkipReason = "No live Proxmox VE target configured. Set: $($baseVars -join ', ')"
            break
        }
    }

    # --- Shared storage vars ---
    $script:StorageVmIp = [System.Environment]::GetEnvironmentVariable('PVETEST_STORAGE_VM_IP')
    $script:IscsiIqn    = [System.Environment]::GetEnvironmentVariable('PVETEST_ISCSI_IQN')
    $script:NfsExport   = [System.Environment]::GetEnvironmentVariable('PVETEST_NFS_EXPORT')

    $script:Host_    = [System.Environment]::GetEnvironmentVariable('PVETEST_HOST')
    $portEnv = [System.Environment]::GetEnvironmentVariable('PVETEST_PORT')
    $script:Port     = [int]$(if ($portEnv) { $portEnv } else { '8006' })
    $script:ApiToken = [System.Environment]::GetEnvironmentVariable('PVETEST_APITOKEN')
    $script:Node     = [System.Environment]::GetEnvironmentVariable('PVETEST_NODE')

    # Track created storages for cleanup
    $script:CreatedStorages = [System.Collections.Generic.List[string]]::new()

    function script:Skip-IfNoTarget {
        if ($script:SkipReason) {
            Set-ItResult -Skipped -Because $script:SkipReason
            return $true
        }
        return $false
    }

    function script:Skip-IfNoSharedStorage {
        if (Skip-IfNoTarget) { return $true }
        if (-not $script:StorageVmIp -or -not $script:IscsiIqn -or -not $script:NfsExport) {
            Set-ItResult -Skipped -Because 'Shared storage env vars not set (PVETEST_STORAGE_VM_IP, PVETEST_ISCSI_IQN, PVETEST_NFS_EXPORT)'
            return $true
        }
        return $false
    }
}

AfterAll {
    # Best-effort cleanup of any storages we created
    foreach ($name in $script:CreatedStorages) {
        try { Remove-PveStorage -Storage $name -Confirm:$false -ErrorAction Stop }
        catch { Write-Warning "Cleanup: failed to remove storage '$name': $_" }
    }
}

Describe 'Shared Storage — Integration' -Tag 'Integration' {

    # -------------------------------------------------------------------
    Context 'Connection' {
        It 'Should connect to PVE node' {
            if (Skip-IfNoTarget) { return }

            $session = Connect-PveServer `
                -Server $script:Host_ `
                -Port $script:Port `
                -ApiToken $script:ApiToken `
                -SkipCertificateCheck `
                -PassThru

            $session | Should -Not -BeNullOrEmpty
        }
    }

    # -------------------------------------------------------------------
    Context 'NFS Storage' {
        It 'Should create NFS storage' {
            if (Skip-IfNoSharedStorage) { return }

            # Parse server and export from the full NFS export path (e.g. "10.0.0.1:/srv/nfs/shared")
            $nfsParts = $script:NfsExport -split ':', 2
            $nfsServer = $nfsParts[0]
            $nfsExportPath = $nfsParts[1]

            # Note: NFS is implicitly shared in PVE — do not pass -Shared
            $result = New-PveStorage -Storage 'pester-nfs' -Type 'nfs' `
                -Server $nfsServer `
                -Export $nfsExportPath `
                -Content 'images,iso,backup' `
                -ErrorAction Stop

            $result | Should -Not -BeNullOrEmpty
            $result.Storage | Should -Be 'pester-nfs'
            $script:CreatedStorages.Add('pester-nfs')
        }

        It 'Should list and find the NFS storage' {
            if (Skip-IfNoSharedStorage) { return }

            $storages = Get-PveStorage -Node $script:Node
            $nfs = $storages | Where-Object { $_.Storage -eq 'pester-nfs' }
            $nfs | Should -Not -BeNullOrEmpty
            $nfs.Type | Should -Be 'nfs'
        }

        It 'Should get NFS storage status' {
            if (Skip-IfNoSharedStorage) { return }

            $status = Get-PveStorageStatus -Node $script:Node -Storage 'pester-nfs' -ErrorAction Stop
            $status | Should -Not -BeNullOrEmpty
            $status.Active | Should -Be 1
        }

        It 'Should remove NFS storage' {
            if (Skip-IfNoSharedStorage) { return }

            { Remove-PveStorage -Storage 'pester-nfs' -Confirm:$false -ErrorAction Stop } |
                Should -Not -Throw
            $script:CreatedStorages.Remove('pester-nfs')
        }
    }

    # -------------------------------------------------------------------
    Context 'iSCSI Storage' {
        It 'Should create iSCSI storage with -Target parameter' {
            if (Skip-IfNoSharedStorage) { return }

            # iSCSI uses -Portal (not -Server) for the target address.
            # iSCSI is implicitly shared in PVE — do not pass -Shared.
            $result = New-PveStorage -Storage 'pester-iscsi' -Type 'iscsi' `
                -Portal $script:StorageVmIp `
                -Target $script:IscsiIqn `
                -ErrorAction Stop

            $result | Should -Not -BeNullOrEmpty
            $result.Storage | Should -Be 'pester-iscsi'
            $script:CreatedStorages.Add('pester-iscsi')
        }

        It 'Should list and find the iSCSI storage' {
            if (Skip-IfNoSharedStorage) { return }

            $storages = Get-PveStorage -Node $script:Node
            $iscsi = $storages | Where-Object { $_.Storage -eq 'pester-iscsi' }
            $iscsi | Should -Not -BeNullOrEmpty
            $iscsi.Type | Should -Be 'iscsi'
        }

        It 'Should get iSCSI storage status' {
            if (Skip-IfNoSharedStorage) { return }

            $status = Get-PveStorageStatus -Node $script:Node -Storage 'pester-iscsi' -ErrorAction Stop
            $status | Should -Not -BeNullOrEmpty
        }

        It 'Should remove iSCSI storage' {
            if (Skip-IfNoSharedStorage) { return }

            { Remove-PveStorage -Storage 'pester-iscsi' -Confirm:$false -ErrorAction Stop } |
                Should -Not -Throw
            $script:CreatedStorages.Remove('pester-iscsi')
        }
    }
}
