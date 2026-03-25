#Requires -Module Pester

BeforeAll {
    . $PSScriptRoot/_IntegrationHelper.ps1
    Connect-TestPve

    $script:CiVmId = $null

    function script:Skip-IfNoCiVm {
        if (Skip-IfNoTarget) { return $true }
        if ($null -eq $script:CiVmId) {
            Set-ItResult -Skipped -Because 'Cloud-init test VM was not created'
            return $true
        }
        return $false
    }
}

AfterAll {
    if (-not $script:SkipReason -and $script:CiVmId) {
        try {
            Stop-PveVm -Node $script:Node -VmId $script:CiVmId -Wait -Timeout 30 -Confirm:$false -ErrorAction SilentlyContinue | Out-Null
        } catch { }
        Start-Sleep -Seconds 2
        try {
            Remove-PveVm -Node $script:Node -VmId $script:CiVmId -Force -Purge -Confirm:$false -ErrorAction SilentlyContinue
        } catch { }
    }
    Disconnect-TestPve
}

Describe 'Cloud-Init — Integration' -Tag 'Integration' {

    Context 'Setup' {
        It 'Should create a VM with cloud-init drive' {
            if (Skip-IfNoTarget) { return }

            $task = New-PveVm `
                -Node    $script:Node `
                -Name    'pester-ci-vm' `
                -Memory  128 `
                -Cores   1 `
                -Wait

            $task | Should -Not -BeNullOrEmpty

            $vm = Get-PveVm -Node $script:Node -Name 'pester-ci-vm' |
                  Select-Object -First 1
            $vm | Should -Not -BeNullOrEmpty
            $script:CiVmId = $vm.VmId

            # Add a cloud-init drive
            Set-PveVmConfig -Node $script:Node -VmId $script:CiVmId `
                -AdditionalConfig @{ ide2 = "$($script:Storage):cloudinit" } `
                -ErrorAction Stop
        }
    }

    Context 'Cloud-Init' {
        It 'Should get cloud-init config' {
            if (Skip-IfNoCiVm) { return }

            { Get-PveCloudInitConfig -Node $script:Node -VmId $script:CiVmId -ErrorAction Stop } |
                Should -Not -Throw
        }

        It 'Should set cloud-init config' {
            if (Skip-IfNoCiVm) { return }

            { Set-PveCloudInitConfig -Node $script:Node -VmId $script:CiVmId `
                -CiUser 'pester-user' -ErrorAction Stop } |
                Should -Not -Throw

            $config = Get-PveCloudInitConfig -Node $script:Node -VmId $script:CiVmId -ErrorAction Stop
            $config.CiUser | Should -Be 'pester-user'
        }

        It 'Should regenerate cloud-init image' {
            if (Skip-IfNoCiVm) { return }

            { Invoke-PveCloudInitRegenerate -Node $script:Node -VmId $script:CiVmId -Wait -ErrorAction Stop } |
                Should -Not -Throw
        }
    }
}
