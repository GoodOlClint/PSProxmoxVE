#Requires -Module Pester

BeforeAll {
    . $PSScriptRoot/_IntegrationHelper.ps1
    Connect-TestPve
}

AfterAll {
    if (-not $script:SkipReason) {
        # Clean up SDN resources in reverse dependency order
        try { Remove-PveSdnSubnet -Vnet 'pestervn' -Subnet 'pesterz-10.99.0.0-24' -Confirm:$false -ErrorAction SilentlyContinue } catch { }
        Start-Sleep -Seconds 2
        try { Remove-PveSdnVnet -Vnet 'pestervn' -Confirm:$false -ErrorAction SilentlyContinue } catch { }
        Start-Sleep -Seconds 2
        try { Remove-PveSdnZone -Zone 'pesterz' -Confirm:$false -ErrorAction SilentlyContinue } catch { }
    }
    Disconnect-TestPve
}

Describe 'SDN — Integration' -Tag 'Integration' {
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

    Context 'SDN' {
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

    Context 'SDN — IPAM, DNS, Controllers' {
        It 'Should list SDN IPAM plugins (Get-PveSdnIpam)' {
            if (Skip-IfNoTarget) { return }
            if ($script:SkipSdn) { Set-ItResult -Skipped -Because $script:SkipSdn; return }

            # PVE always has a built-in 'pve' IPAM plugin
            $ipams = Get-PveSdnIpam
            $ipams | Should -Not -BeNullOrEmpty
            ($ipams | Where-Object { $_.Type -eq 'pve' }) | Should -Not -BeNullOrEmpty
        }

        It 'Should list SDN DNS plugins (Get-PveSdnDns)' {
            if (Skip-IfNoTarget) { return }
            if ($script:SkipSdn) { Set-ItResult -Skipped -Because $script:SkipSdn; return }

            # Zero DNS plugins is acceptable; just verify no throw.
            { Get-PveSdnDns -ErrorAction Stop } | Should -Not -Throw
        }

        It 'Should list SDN controllers (Get-PveSdnController)' {
            if (Skip-IfNoTarget) { return }
            if ($script:SkipSdn) { Set-ItResult -Skipped -Because $script:SkipSdn; return }

            # Zero controllers is acceptable; just verify no throw.
            { Get-PveSdnController -ErrorAction Stop } | Should -Not -Throw
        }
    }
}
