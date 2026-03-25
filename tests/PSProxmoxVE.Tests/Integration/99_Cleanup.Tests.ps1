#Requires -Module Pester

BeforeAll {
    . $PSScriptRoot/_IntegrationHelper.ps1
    Connect-TestPve
}

AfterAll {
    Disconnect-TestPve
}

Describe 'Safety-Net Cleanup — Integration' -Tag 'Integration' {

    Context 'Resource cleanup' {
        It 'Should remove all pester-* VMs' {
            if (Skip-IfNoTarget) { return }

            $vms = Get-PveVm -Node $script:Node -ErrorAction SilentlyContinue
            $pesterVms = $vms | Where-Object { $_.Name -like 'pester-*' }
            foreach ($vm in $pesterVms) {
                try {
                    Stop-PveVm -Node $script:Node -VmId $vm.VmId -Confirm:$false -ErrorAction SilentlyContinue | Out-Null
                    Start-Sleep -Seconds 3
                } catch { }
                try {
                    # Try removing as template first (templates need Remove-PveTemplate)
                    Remove-PveTemplate -Node $script:Node -VmId $vm.VmId -Confirm:$false -ErrorAction SilentlyContinue
                } catch {
                    try {
                        Remove-PveVm -Node $script:Node -VmId $vm.VmId -Force -Purge -Confirm:$false -ErrorAction SilentlyContinue
                    } catch { }
                }
            }

            # Verify
            $remaining = Get-PveVm -Node $script:Node -ErrorAction SilentlyContinue |
                Where-Object { $_.Name -like 'pester-*' }
            $remaining | Should -BeNullOrEmpty
        }

        It 'Should remove all pester-* containers' {
            if (Skip-IfNoTarget) { return }

            $containers = Get-PveContainer -Node $script:Node -ErrorAction SilentlyContinue
            $pesterCts = $containers | Where-Object { $_.Name -like 'pester-*' }
            foreach ($ct in $pesterCts) {
                try {
                    Stop-PveContainer -Node $script:Node -VmId $ct.VmId -Confirm:$false -ErrorAction SilentlyContinue | Out-Null
                    Start-Sleep -Seconds 3
                } catch { }
                try {
                    Remove-PveContainer -Node $script:Node -VmId $ct.VmId -Force -Purge -Confirm:$false -ErrorAction SilentlyContinue
                } catch { }
            }

            # Verify
            $remaining = Get-PveContainer -Node $script:Node -ErrorAction SilentlyContinue |
                Where-Object { $_.Name -like 'pester-*' }
            $remaining | Should -BeNullOrEmpty
        }

        It 'Should remove all pester-* users' {
            if (Skip-IfNoTarget) { return }

            $users = Get-PveUser -ErrorAction SilentlyContinue
            $pesterUsers = $users | Where-Object { $_.UserId -like 'pester-*' }
            foreach ($user in $pesterUsers) {
                try {
                    Remove-PveUser -UserId $user.UserId -Confirm:$false -ErrorAction SilentlyContinue
                } catch { }
            }
        }

        It 'Should remove all pester-* roles' {
            if (Skip-IfNoTarget) { return }

            $roles = Get-PveRole -ErrorAction SilentlyContinue
            $pesterRoles = $roles | Where-Object { $_.RoleId -like 'pester-*' -or $_.RoleId -like 'Pester*' }
            foreach ($role in $pesterRoles) {
                try {
                    Remove-PveRole -RoleId $role.RoleId -Confirm:$false -ErrorAction SilentlyContinue
                } catch { }
            }
        }

        It 'Should remove pester firewall rules, aliases, and IP sets' {
            if (Skip-IfNoTarget) { return }

            # Firewall rules with pester comments
            try {
                $rules = Get-PveFirewallRule -Level Cluster -ErrorAction SilentlyContinue
                $testRules = $rules | Where-Object { $_.Comment -like 'pester-*' -or $_.Comment -like 'pester *' }
                # Remove in reverse position order to avoid position shifts
                $testRules | Sort-Object -Property Pos -Descending | ForEach-Object {
                    Remove-PveFirewallRule -Level Cluster -Position $_.Pos -Confirm:$false -ErrorAction SilentlyContinue
                }
            } catch { }

            # Firewall aliases
            try {
                $aliases = Get-PveFirewallAlias -Level Cluster -ErrorAction SilentlyContinue
                $pesterAliases = $aliases | Where-Object { $_.Name -like 'pester-*' }
                foreach ($alias in $pesterAliases) {
                    Remove-PveFirewallAlias -Level Cluster -Name $alias.Name -Confirm:$false -ErrorAction SilentlyContinue
                }
            } catch { }

            # Firewall IP sets
            try {
                $ipsets = Get-PveFirewallIpSet -Level Cluster -ErrorAction SilentlyContinue
                $pesterIpsets = $ipsets | Where-Object { $_.Name -like 'pester-*' }
                foreach ($ipset in $pesterIpsets) {
                    # Remove entries first
                    try {
                        $entries = Get-PveFirewallIpSetEntry -Level Cluster -Name $ipset.Name -ErrorAction SilentlyContinue
                        foreach ($entry in $entries) {
                            Remove-PveFirewallIpSetEntry -Level Cluster -Name $ipset.Name -Cidr $entry.Cidr -Confirm:$false -ErrorAction SilentlyContinue
                        }
                    } catch { }
                    Remove-PveFirewallIpSet -Level Cluster -Name $ipset.Name -Confirm:$false -ErrorAction SilentlyContinue
                }
            } catch { }
        }

        It 'Should remove pester backup jobs' {
            if (Skip-IfNoTarget) { return }

            try {
                $jobs = Get-PveBackupJob -ErrorAction SilentlyContinue
                $pesterJobs = $jobs | Where-Object { $_.Comment -like 'pester-*' -or $_.Comment -like 'pester *' }
                foreach ($job in $pesterJobs) {
                    Remove-PveBackupJob -Id $job.Id -Confirm:$false -ErrorAction SilentlyContinue
                }
            } catch { }
        }

        It 'Should remove pester SDN resources' {
            if (Skip-IfNoTarget) { return }

            # Subnets first (depend on vnets)
            try {
                $vnets = Get-PveSdnVnet -ErrorAction SilentlyContinue
                $pesterVnets = $vnets | Where-Object { $_.Vnet -like 'pester*' }
                foreach ($vnet in $pesterVnets) {
                    try {
                        $subnets = Get-PveSdnSubnet -Vnet $vnet.Vnet -ErrorAction SilentlyContinue
                        foreach ($subnet in $subnets) {
                            Remove-PveSdnSubnet -Vnet $vnet.Vnet -Subnet $subnet.Subnet -Confirm:$false -ErrorAction SilentlyContinue
                        }
                    } catch { }
                    Start-Sleep -Seconds 2
                    Remove-PveSdnVnet -Vnet $vnet.Vnet -Confirm:$false -ErrorAction SilentlyContinue
                }
            } catch { }

            # Zones
            try {
                $zones = Get-PveSdnZone -ErrorAction SilentlyContinue
                $pesterZones = $zones | Where-Object { $_.Zone -like 'pester*' }
                foreach ($zone in $pesterZones) {
                    Start-Sleep -Seconds 2
                    Remove-PveSdnZone -Zone $zone.Zone -Confirm:$false -ErrorAction SilentlyContinue
                }
            } catch { }
        }

        It 'Should remove pester storage definitions' {
            if (Skip-IfNoTarget) { return }

            try {
                $storages = Get-PveStorage -Node $script:Node -ErrorAction SilentlyContinue
                $pesterStorages = $storages | Where-Object { $_.Storage -like 'pester-*' }
                foreach ($storage in $pesterStorages) {
                    Remove-PveStorage -Storage $storage.Storage -Confirm:$false -ErrorAction SilentlyContinue
                }
            } catch { }
        }

        It 'Should remove pester network interfaces' {
            if (Skip-IfNoTarget) { return }

            try {
                $networks = Get-PveNetwork -Node $script:Node -ErrorAction SilentlyContinue
                $pesterNets = $networks | Where-Object { $_.Iface -like 'pester*' -or ($_.Comments -and $_.Comments -like '*pester*') }
                foreach ($net in $pesterNets) {
                    Remove-PveNetwork -Node $script:Node -Iface $net.Iface -Confirm:$false -ErrorAction SilentlyContinue
                }
                if ($pesterNets) {
                    Invoke-PveNetworkApply -Node $script:Node -Wait -ErrorAction SilentlyContinue
                }
            } catch { }
        }
    }
}
