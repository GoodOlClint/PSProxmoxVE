#Requires -Module Pester
<#
.SYNOPSIS
    Integration tests for cluster configuration and HA lifecycle.

    Exercises the full cluster lifecycle: create cluster on node A,
    join node B, manage HA groups/rules, and verify cluster state.

    Nodes must be provisioned STANDALONE (not clustered). These tests
    create a cluster during the run. Cleanup is handled by reprovisioning
    (2-node cluster cannot be torn down via API due to quorum loss).

    WARNING: These tests CREATE a cluster on the target nodes.
    Never run against a production cluster.
#>

BeforeAll {
    . $PSScriptRoot/_IntegrationHelper.ps1
    Connect-TestPve

    # --- State tracking ---
    $script:ClusterCreated = $false
    $script:JoinInfo       = $null
    $script:NodeBName      = $null
    $script:HaRuleCreated  = $false
    $script:HaTestVmId     = $null

    # --- Cluster-specific skip helpers ---

    function script:Skip-IfNoCluster {
        if (Skip-IfNoTarget) { return $true }
        if (-not $script:ClusterCreated) {
            Set-ItResult -Skipped -Because 'Cluster was not created (multi-node env vars may not be set)'
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
    # Best-effort cleanup of HA artifacts
    if ($script:HaRuleCreated) {
        try { Remove-PveHaRule -Rule 'pester-rule-1' -Confirm:$false -ErrorAction SilentlyContinue } catch { }
    }
    if ($script:HaTestVmId) {
        try { Remove-PveHaResource -Sid "vm:$($script:HaTestVmId)" -Confirm:$false -ErrorAction SilentlyContinue } catch { }
        try { Remove-PveVm -Node $script:Node -VmId $script:HaTestVmId -Confirm:$false -ErrorAction SilentlyContinue } catch { }
    }

    # Node removal from a 2-node cluster is not possible via API (quorum loss).
    # Test infrastructure handles cleanup via reprovisioning.
    Disconnect-TestPve
}

Describe 'Cluster Config & HA Lifecycle — Integration' -Tag 'Integration' {

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

            { Get-PveClusterConfig -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Get-PveHaStatus returns status without throwing' {
            if (Skip-IfNoTarget) { return }

            { Get-PveHaStatus -ErrorAction Stop } | Should -Not -Throw
        }
    }

    # -------------------------------------------------------------------
    Context 'Create cluster on node A' {
        It 'New-PveCluster creates a cluster named pester-cluster' {
            if (Skip-IfNoNodeB) { return }

            # Already connected as root@pam via Connect-TestPve
            $result = New-PveCluster -ClusterName 'pester-cluster' -Wait -Confirm:$false -ErrorAction Stop
            $result | Should -Not -BeNullOrEmpty
            $script:ClusterCreated = $true

            # Allow corosync to fully stabilize
            Start-Sleep -Seconds 5
        }

        It 'Get-PveClusterStatus shows cluster formed' {
            if (Skip-IfNoCluster) { return }

            $status = Get-PveClusterStatus -ErrorAction Stop
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

            # Nodelist is List<Dictionary<string, object?>> — access pve_fp from first entry
            $script:JoinInfo.Nodelist | Should -Not -BeNullOrEmpty
            $script:JoinInfo.Nodelist.Count | Should -BeGreaterOrEqual 1
            $script:Fingerprint = $script:JoinInfo.Nodelist[0]['pve_fp']
            $script:Fingerprint | Should -Not -BeNullOrEmpty
        }

        It 'Add-PveClusterMember joins node B to cluster' {
            if (Skip-IfNoNodeB) { return }
            if (Skip-IfNoCluster) { return }
            if (-not $script:Fingerprint) {
                Set-ItResult -Skipped -Because 'Fingerprint was not extracted from join info'
                return
            }

            # Connect to node B with root@pam
            $secPw = ConvertTo-SecureString $script:PasswordB -AsPlainText -Force
            $credB = New-Object System.Management.Automation.PSCredential('root@pam', $secPw)
            Connect-PveServer `
                -Server $script:HostB `
                -Port $script:Port `
                -Credential $credB `
                -SkipCertificateCheck

            $fingerprint = $script:Fingerprint
            $joinPw = ConvertTo-SecureString $script:Password -AsPlainText -Force

            try {
                $result = Add-PveClusterMember `
                    -Hostname $script:Host_ `
                    -Fingerprint $fingerprint `
                    -Password $joinPw `
                    -Wait `
                    -Confirm:$false `
                    -ErrorAction Stop

                $result | Should -Not -BeNullOrEmpty

                # Brief pause for pmxcfs to sync
                Start-Sleep -Seconds 5
            }
            finally {
                # Always reconnect to node A
                Connect-TestPve
            }
        }

        It 'Get-PveClusterConfigNode shows both nodes' {
            if (Skip-IfNoNodeB) { return }
            if (Skip-IfNoCluster) { return }

            $nodes = Get-PveClusterConfigNode -ErrorAction Stop
            @($nodes).Count | Should -BeGreaterOrEqual 2

            # Discover and store node B's name
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
                Set-PveClusterOption -Delete 'keyboard' -Confirm:$false -ErrorAction Stop
            }

            $options = Get-PveClusterOption -ErrorAction Stop
            $options.Keyboard | Should -Be $script:OriginalKeyboard
        }
    }

    # -------------------------------------------------------------------
    Context 'HA group management (PVE 8 only)' {
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
        }

        It 'Remove-PveHaGroup deletes test group' {
            if (Skip-IfPve9HaGroups) { return }

            { Remove-PveHaGroup -Group 'pester-group' -Confirm:$false -ErrorAction Stop } |
                Should -Not -Throw
        }
    }

    # -------------------------------------------------------------------
    Context 'HA rules CRUD (PVE 9.0+)' {
        It 'Get-PveHaRule returns rules list without throwing' {
            if (Skip-IfNoPve9) { return }

            { Get-PveHaRule -ErrorAction Stop } | Should -Not -Throw
        }

        It 'New-PveHaRule creates a node-affinity rule' {
            if (Skip-IfNoPve9) { return }
            if (Skip-IfNoCluster) { return }

            # HA rules require a managed resource — create a minimal VM first
            $script:HaTestVmId = Get-PveClusterNextId -ErrorAction Stop
            New-PveVm -Node $script:Node -VmId $script:HaTestVmId `
                -Name 'pester-ha-test' -Memory 128 -Cores 1 `
                -Confirm:$false -ErrorAction Stop | Out-Null

            New-PveHaResource -Sid "vm:$($script:HaTestVmId)" `
                -State 'disabled' `
                -Confirm:$false -ErrorAction Stop

            { New-PveHaRule -Type 'node-affinity' `
                -Properties @{
                    rule      = 'pester-rule-1'
                    resources = "vm:$($script:HaTestVmId)"
                    nodes     = $script:Node
                } `
                -Comment 'Pester test rule' `
                -Confirm:$false `
                -ErrorAction Stop } | Should -Not -Throw

            $script:HaRuleCreated = $true
        }

        It 'Get-PveHaRule lists the created rule' {
            if (Skip-IfNoPve9) { return }
            if (-not $script:HaRuleCreated) {
                Set-ItResult -Skipped -Because 'HA rule was not created'
                return
            }

            $rules = Get-PveHaRule -ErrorAction Stop
            $rules | Should -Not -BeNullOrEmpty
            $match = @($rules) | Where-Object { $_.Rule -eq 'pester-rule-1' }
            $match | Should -Not -BeNullOrEmpty
        }

        It 'Get-PveHaRule returns specific rule by ID' {
            if (Skip-IfNoPve9) { return }
            if (-not $script:HaRuleCreated) {
                Set-ItResult -Skipped -Because 'HA rule was not created'
                return
            }

            $rule = Get-PveHaRule -Rule 'pester-rule-1' -ErrorAction Stop
            $rule | Should -Not -BeNullOrEmpty
            $rule.Rule | Should -Be 'pester-rule-1'
            $rule.Comment | Should -Be 'Pester test rule'
        }

        It 'Set-PveHaRule updates the comment' {
            if (Skip-IfNoPve9) { return }
            if (-not $script:HaRuleCreated) {
                Set-ItResult -Skipped -Because 'HA rule was not created'
                return
            }

            { Set-PveHaRule -Rule 'pester-rule-1' `
                -Comment 'Updated by Pester' `
                -Confirm:$false `
                -ErrorAction Stop } | Should -Not -Throw

            $rule = Get-PveHaRule -Rule 'pester-rule-1' -ErrorAction Stop
            $rule.Comment | Should -Be 'Updated by Pester'
        }

        It 'Remove-PveHaRule deletes the rule' {
            if (Skip-IfNoPve9) { return }
            if (-not $script:HaRuleCreated) {
                Set-ItResult -Skipped -Because 'HA rule was not created'
                return
            }

            { Remove-PveHaRule -Rule 'pester-rule-1' -Confirm:$false -ErrorAction Stop } |
                Should -Not -Throw

            $script:HaRuleCreated = $false

            # Clean up HA resource and test VM
            try { Remove-PveHaResource -Sid "vm:$($script:HaTestVmId)" -Confirm:$false -ErrorAction SilentlyContinue } catch { }
            try { Remove-PveVm -Node $script:Node -VmId $script:HaTestVmId -Confirm:$false -ErrorAction SilentlyContinue } catch { }
            $script:HaTestVmId = $null

            # Verify rule is gone
            $rules = Get-PveHaRule -ErrorAction Stop
            $match = @($rules) | Where-Object { $_.Rule -eq 'pester-rule-1' }
            $match | Should -BeNullOrEmpty
        }
    }

    # -------------------------------------------------------------------
    Context 'Verify cluster state at end of test' {
        It 'Get-PveClusterConfigNode shows both nodes still present' {
            if (Skip-IfNoNodeB) { return }
            if (Skip-IfNoCluster) { return }

            $nodes = Get-PveClusterConfigNode -ErrorAction Stop
            @($nodes).Count | Should -BeGreaterOrEqual 2
        }
    }
}
