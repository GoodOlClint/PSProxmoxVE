#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 integration tests for cluster configuration and HA lifecycle.

    These tests exercise the full cluster lifecycle: creating a cluster on
    node A, joining node B, managing HA groups, and removing node B.

    Nodes are provisioned STANDALONE (not clustered). The tests create and
    destroy the cluster during the run.

    Required environment variables:
        PVETEST_HOST       - Node A hostname or IP
        PVETEST_PORT       - API port (default 8006)
        PVETEST_APITOKEN   - Node A API token
        PVETEST_NODE       - Node A node name

    Optional (multi-node tests):
        PVETEST_HOST_B     - Node B hostname or IP
        PVETEST_APITOKEN_B - Node B API token
        PVETEST_PASSWORD   - Root password for nested PVE instances

    Optional:
        PVETEST_PVE_VERSION - PVE major version (8 or 9)

    WARNING: These tests CREATE and DESTROY a cluster on the target nodes.
    Never run against a production cluster.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    # --- Required env vars ---
    $requiredVars = @('PVETEST_HOST', 'PVETEST_PORT', 'PVETEST_APITOKEN', 'PVETEST_NODE')
    $script:SkipReason = $null

    foreach ($var in $requiredVars) {
        if (-not [System.Environment]::GetEnvironmentVariable($var)) {
            $script:SkipReason = "No live Proxmox VE target configured. Set: $($requiredVars -join ', ')"
            break
        }
    }

    # --- Convenience accessors ---
    $script:Host_       = [System.Environment]::GetEnvironmentVariable('PVETEST_HOST')
    $portEnv            = [System.Environment]::GetEnvironmentVariable('PVETEST_PORT')
    $script:Port        = [int]$(if ($portEnv) { $portEnv } else { '8006' })
    $script:ApiToken    = [System.Environment]::GetEnvironmentVariable('PVETEST_APITOKEN')
    $script:Node        = [System.Environment]::GetEnvironmentVariable('PVETEST_NODE')

    # --- Optional multi-node vars ---
    $script:HostB       = [System.Environment]::GetEnvironmentVariable('PVETEST_HOST_B')
    $script:ApiTokenB   = [System.Environment]::GetEnvironmentVariable('PVETEST_APITOKEN_B')
    $script:Password    = [System.Environment]::GetEnvironmentVariable('PVETEST_PASSWORD')
    $script:PveVersion  = [System.Environment]::GetEnvironmentVariable('PVETEST_PVE_VERSION')

    # --- State tracking ---
    $script:ClusterCreated = $false
    $script:JoinInfo       = $null
    $script:NodeBName      = $null

    # --- Skip helpers ---
    function script:Skip-IfNoTarget {
        if ($script:SkipReason) {
            Set-ItResult -Skipped -Because $script:SkipReason
            return $true
        }
        return $false
    }

    function script:Skip-IfNoNodeB {
        if (Skip-IfNoTarget) { return $true }
        if (-not $script:HostB -or -not $script:ApiTokenB -or -not $script:Password) {
            Set-ItResult -Skipped -Because 'Multi-node env vars not set (PVETEST_HOST_B, PVETEST_APITOKEN_B, PVETEST_PASSWORD)'
            return $true
        }
        return $false
    }

    function script:Skip-IfNoCluster {
        if (Skip-IfNoTarget) { return $true }
        if (-not $script:ClusterCreated) {
            Set-ItResult -Skipped -Because 'Cluster was not created (multi-node env vars may not be set)'
            return $true
        }
        return $false
    }

    function script:Skip-IfNoPve9 {
        if (Skip-IfNoCluster) { return $true }
        if (-not $script:PveVersion -or [int]$script:PveVersion -lt 9) {
            Set-ItResult -Skipped -Because 'HA rules require PVE 9.0+'
            return $true
        }
        return $false
    }

    function script:Skip-IfPve9HaGroups {
        # PVE 9.0+ migrated HA groups to rules — groups API returns 500
        if (Skip-IfNoCluster) { return $true }
        if ($script:PveVersion -and [int]$script:PveVersion -ge 9) {
            Set-ItResult -Skipped -Because 'PVE 9.0+ migrated HA groups to rules — use Get/New/Set/Remove-PveHaRule instead'
            return $true
        }
        return $false
    }
}

AfterAll {
    # Best-effort cleanup — each step may fail if cluster state is partial
    if ($script:ClusterCreated) {
        # Try to discover node B name and remove it from the cluster
        if ($script:NodeBName) {
            try {
                Write-Warning "Cleanup: removing node '$($script:NodeBName)' from cluster..."
                Remove-PveClusterConfigNode -Node $script:NodeBName -Confirm:$false -ErrorAction Stop
            }
            catch {
                Write-Warning "Cleanup: failed to remove node B from cluster: $_"
            }
        }
    }

    # Disconnect any active sessions
    try { Disconnect-PveServer -ErrorAction SilentlyContinue } catch { }
}

Describe 'Cluster Config & HA Lifecycle — Integration' -Tag 'Integration' {

    # -------------------------------------------------------------------
    Context 'Connection' {
        It 'Should connect to PVE node A' {
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
    Context 'Pre-cluster reads on standalone node' {
        It 'Get-PveClusterStatus returns node info' {
            if (Skip-IfNoTarget) { return }

            $status = Get-PveClusterStatus -ErrorAction Stop
            $status | Should -Not -BeNullOrEmpty
            @($status).Count | Should -BeGreaterOrEqual 1
        }

        It 'Get-PveClusterNextId returns valid VMID' {
            if (Skip-IfNoTarget) { return }

            $nextId = Get-PveClusterNextId -ErrorAction Stop
            $nextId | Should -Not -BeNullOrEmpty
            [int]$nextId | Should -BeGreaterOrEqual 100
        }

        It 'Get-PveClusterOption returns options' {
            if (Skip-IfNoTarget) { return }

            $options = Get-PveClusterOption -ErrorAction Stop
            $options | Should -Not -BeNullOrEmpty
        }

        It 'Get-PveClusterConfig returns config' {
            if (Skip-IfNoTarget) { return }

            # /cluster/config returns a directory listing (array) on standalone nodes
            # and may return an object on clustered nodes — either is valid
            { Get-PveClusterConfig -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Get-PveHaStatus returns status without throwing' {
            if (Skip-IfNoTarget) { return }

            # On a standalone node this may return an empty list — that is fine
            { Get-PveHaStatus -ErrorAction Stop } | Should -Not -Throw
        }
    }

    # -------------------------------------------------------------------
    Context 'Create cluster on node A' {
        It 'New-PveCluster creates a cluster named pester-cluster' {
            if (Skip-IfNoNodeB) { return }

            # Cluster creation requires root@pam ticket auth, not API token
            $secPw = ConvertTo-SecureString $script:Password -AsPlainText -Force
            $cred = New-Object System.Management.Automation.PSCredential('root@pam', $secPw)
            Connect-PveServer `
                -Server $script:Host_ `
                -Port $script:Port `
                -Credential $cred `
                -SkipCertificateCheck

            $result = New-PveCluster -ClusterName 'pester-cluster' -Wait -Confirm:$false -ErrorAction Stop
            $result | Should -Not -BeNullOrEmpty
            $script:ClusterCreated = $true

            # Reconnect with API token for remaining tests
            Connect-PveServer `
                -Server $script:Host_ `
                -Port $script:Port `
                -ApiToken $script:ApiToken `
                -SkipCertificateCheck
        }

        It 'Get-PveClusterStatus shows cluster formed' {
            if (Skip-IfNoCluster) { return }

            $status = Get-PveClusterStatus -ErrorAction Stop
            # On a newly created single-node cluster, we should see at least
            # a node entry for this node. The "cluster" type entry may take
            # a moment to appear depending on corosync state.
            @($status).Count | Should -BeGreaterOrEqual 1
        }

        It 'Get-PveClusterConfigNode lists node A' {
            if (Skip-IfNoCluster) { return }

            $nodes = Get-PveClusterConfigNode -ErrorAction Stop
            $nodes | Should -Not -BeNullOrEmpty
            @($nodes).Count | Should -BeGreaterOrEqual 1
        }
    }

    # -------------------------------------------------------------------
    Context 'Join node B to cluster' {
        It 'Get-PveClusterJoinInfo returns join token from node A' {
            if (Skip-IfNoNodeB) { return }
            if (Skip-IfNoCluster) { return }

            $script:JoinInfo = Get-PveClusterJoinInfo -ErrorAction Stop
            $script:JoinInfo | Should -Not -BeNullOrEmpty

            # Nodelist is a Newtonsoft JArray — convert to native array for PowerShell compatibility
            $nodelistJson = $script:JoinInfo.Nodelist.ToString()
            $nodelist = $nodelistJson | ConvertFrom-Json
            $nodelist | Should -Not -BeNullOrEmpty
            $script:Fingerprint = $nodelist[0].pve_fp
            $script:Fingerprint | Should -Not -BeNullOrEmpty
        }

        It 'Add-PveClusterMember joins node B to cluster' {
            if (Skip-IfNoNodeB) { return }
            if (Skip-IfNoCluster) { return }
            if (-not $script:Fingerprint) {
                Set-ItResult -Skipped -Because 'Fingerprint was not extracted from join info'
                return
            }

            # Connect to node B with root@pam (join requires ticket auth, not API token)
            $secPw = ConvertTo-SecureString $script:Password -AsPlainText -Force
            $credB = New-Object System.Management.Automation.PSCredential('root@pam', $secPw)
            Connect-PveServer `
                -Server $script:HostB `
                -Port $script:Port `
                -Credential $credB `
                -SkipCertificateCheck

            $fingerprint = $script:Fingerprint

            $result = Add-PveClusterMember `
                -Hostname $script:Host_ `
                -Fingerprint $fingerprint `
                -Password $secPw `
                -Wait `
                -Confirm:$false `
                -ErrorAction Stop

            $result | Should -Not -BeNullOrEmpty

            # Brief pause for pmxcfs to sync after task completion
            Start-Sleep -Seconds 5

            # Reconnect to node A for subsequent tests
            Connect-PveServer `
                -Server $script:Host_ `
                -Port $script:Port `
                -ApiToken $script:ApiToken `
                -SkipCertificateCheck
        }

        It 'Get-PveClusterConfigNode shows both nodes' {
            if (Skip-IfNoNodeB) { return }
            if (Skip-IfNoCluster) { return }

            $nodes = Get-PveClusterConfigNode -ErrorAction Stop
            @($nodes).Count | Should -BeGreaterOrEqual 2

            # Discover and store node B's name for cleanup
            $nodeNames = @($nodes) | ForEach-Object { $_.Name }
            $script:NodeBName = $nodeNames | Where-Object { $_ -ne $script:Node } | Select-Object -First 1
            $script:NodeBName | Should -Not -BeNullOrEmpty
        }

        It 'Get-PveClusterStatus shows 2 nodes online' {
            if (Skip-IfNoNodeB) { return }
            if (Skip-IfNoCluster) { return }

            $status = Get-PveClusterStatus -ErrorAction Stop
            $nodeEntries = @($status) | Where-Object { $_.Type -eq 'node' }
            @($nodeEntries).Count | Should -BeGreaterOrEqual 2

            $onlineNodes = @($nodeEntries) | Where-Object { $_.Online -eq 1 }
            @($onlineNodes).Count | Should -BeGreaterOrEqual 2
        }
    }

    # -------------------------------------------------------------------
    Context 'Cluster options' {
        It 'Set-PveClusterOption sets keyboard to en-us' {
            if (Skip-IfNoCluster) { return }

            # Cluster options modification requires root@pam (Sys.Modify on /)
            $secPw = ConvertTo-SecureString $script:Password -AsPlainText -Force
            $cred = New-Object System.Management.Automation.PSCredential('root@pam', $secPw)
            Connect-PveServer -Server $script:Host_ -Port $script:Port -Credential $cred -SkipCertificateCheck

            # Save original keyboard setting
            $options = Get-PveClusterOption -ErrorAction Stop
            $script:OriginalKeyboard = $options.Keyboard

            Set-PveClusterOption -Keyboard 'en-us' -Confirm:$false -ErrorAction Stop
        }

        It 'Get-PveClusterOption reflects the keyboard change' {
            if (Skip-IfNoCluster) { return }

            $options = Get-PveClusterOption -ErrorAction Stop
            $options.Keyboard | Should -Be 'en-us'
        }

        It 'Set-PveClusterOption restores original keyboard' {
            if (Skip-IfNoCluster) { return }

            if ($script:OriginalKeyboard) {
                Set-PveClusterOption -Keyboard $script:OriginalKeyboard -Confirm:$false -ErrorAction Stop
            }
            else {
                # Original was unset — delete the key to restore default
                Set-PveClusterOption -Delete 'keyboard' -Confirm:$false -ErrorAction Stop
            }

            $options = Get-PveClusterOption -ErrorAction Stop
            $options.Keyboard | Should -Be $script:OriginalKeyboard

            # Reconnect with API token for remaining tests
            Connect-PveServer -Server $script:Host_ -Port $script:Port -ApiToken $script:ApiToken -SkipCertificateCheck
        }
    }

    # -------------------------------------------------------------------
    Context 'HA group management' {
        It 'New-PveHaGroup creates test group' {
            if (Skip-IfPve9HaGroups) { return }

            $nodeList = "$($script:Node):1"
            if ($script:NodeBName) {
                $nodeList += ",$($script:NodeBName):2"
            }

            { New-PveHaGroup -Group 'pester-group' -Nodes $nodeList -Confirm:$false -ErrorAction Stop } |
                Should -Not -Throw
        }

        It 'Get-PveHaGroup lists groups including pester-group' {
            if (Skip-IfPve9HaGroups) { return }

            $groups = @(Get-PveHaGroup -ErrorAction Stop)
            $match = $groups | Where-Object { $_.Group -eq 'pester-group' }
            $match | Should -Not -BeNullOrEmpty
        }

        It 'Get-PveHaGroup returns specific group' {
            if (Skip-IfPve9HaGroups) { return }

            $group = Get-PveHaGroup -Group 'pester-group' -ErrorAction Stop
            $group | Should -Not -BeNullOrEmpty
            $group.Group | Should -Be 'pester-group'
        }

        It 'Set-PveHaGroup updates comment' {
            if (Skip-IfPve9HaGroups) { return }

            { Set-PveHaGroup -Group 'pester-group' -Comment 'pester test' -Confirm:$false -ErrorAction Stop } |
                Should -Not -Throw

            $group = Get-PveHaGroup -Group 'pester-group' -ErrorAction Stop
            $group.Comment | Should -Be 'pester test'
        }

        It 'Remove-PveHaGroup deletes test group' {
            if (Skip-IfPve9HaGroups) { return }

            { Remove-PveHaGroup -Group 'pester-group' -Confirm:$false -ErrorAction Stop } |
                Should -Not -Throw

            # Verify the group is gone
            $groups = @(Get-PveHaGroup -ErrorAction Stop)
            $match = $groups | Where-Object { $_.Group -eq 'pester-group' }
            $match | Should -BeNullOrEmpty
        }
    }

    # -------------------------------------------------------------------
    Context 'HA resource management' {
        It 'Skipped — HA resources require a test VM' {
            Set-ItResult -Skipped -Because 'HA resource tests require a running VM, which is not provisioned in this test file'
        }
    }

    # -------------------------------------------------------------------
    Context 'HA rules (PVE 9.0+)' {
        It 'Get-PveHaRule returns rules list without throwing' {
            if (Skip-IfNoPve9) { return }

            # May return an empty list on a fresh cluster — that is fine
            { Get-PveHaRule -ErrorAction Stop } | Should -Not -Throw
        }
    }

    # -------------------------------------------------------------------
    Context 'Remove node B and cleanup' {
        It 'Remove-PveClusterConfigNode removes node B' {
            if (Skip-IfNoNodeB) { return }
            if (Skip-IfNoCluster) { return }
            if (-not $script:NodeBName) {
                Set-ItResult -Skipped -Because 'Node B name was not discovered'
                return
            }

            # Ensure we are connected to node A
            Connect-PveServer `
                -Server $script:Host_ `
                -Port $script:Port `
                -ApiToken $script:ApiToken `
                -SkipCertificateCheck

            { Remove-PveClusterConfigNode -Node $script:NodeBName -Confirm:$false -ErrorAction Stop } |
                Should -Not -Throw

            # Clear the name so AfterAll does not try to remove again
            $script:NodeBName = $null

            # Wait for config to propagate
            Start-Sleep -Seconds 5
        }

        It 'Get-PveClusterConfigNode shows only node A' {
            if (Skip-IfNoNodeB) { return }
            if (Skip-IfNoCluster) { return }

            $nodes = Get-PveClusterConfigNode -ErrorAction Stop
            @($nodes).Count | Should -Be 1
            @($nodes)[0].Name | Should -Be $script:Node
        }
    }
}
