#Requires -Modules Pester

<#
.SYNOPSIS
    Integration tests that run PSProxmoxVE cmdlets against the mock PVE API server.
    These validate end-to-end behavior: HTTP requests, auth, deserialization, pipeline.

.DESCRIPTION
    The mock server is started before all tests and stopped after. Each test group
    exercises real cmdlet execution against simulated PVE API responses. No real
    Proxmox host is needed.
#>

BeforeAll {
    # Find the built mock server
    $mockServerProject = Join-Path $PSScriptRoot '../../PSProxmoxVE.MockServer/PSProxmoxVE.MockServer.csproj'
    if (-not (Test-Path $mockServerProject)) {
        throw "Mock server project not found at $mockServerProject"
    }

    # Find the built module DLL
    $dllSearchPaths = @(
        (Join-Path $PSScriptRoot '../../PSProxmoxVE.MockServer/bin/Release/net9.0/PSProxmoxVE.MockServer.dll'),
        (Join-Path $PSScriptRoot '../../PSProxmoxVE.MockServer/bin/Debug/net9.0/PSProxmoxVE.MockServer.dll')
    )
    $mockServerDll = $dllSearchPaths | Where-Object { Test-Path $_ } | Select-Object -First 1

    if (-not $mockServerDll) {
        # Build it
        $buildOutput = dotnet build $mockServerProject --configuration Release 2>&1 | Out-String
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to build mock server (exit code $LASTEXITCODE): $buildOutput"
        }
        $mockServerDll = $dllSearchPaths | Where-Object { Test-Path $_ } | Select-Object -First 1
        if (-not $mockServerDll) {
            throw "Mock server build succeeded but DLL not found at expected paths: $($dllSearchPaths -join ', ')"
        }
    }

    # Start mock server as a background process on a random port
    $script:MockPort = Get-Random -Minimum 18000 -Maximum 19000
    $script:MockHost = "localhost"
    $script:MockUrl = "https://${script:MockHost}:${script:MockPort}"

    $mockServerDir = Split-Path $mockServerDll -Parent
    $script:MockProcess = Start-Process -FilePath 'dotnet' `
        -ArgumentList @($mockServerDll, '--urls', "https://0.0.0.0:$($script:MockPort)") `
        -WorkingDirectory $mockServerDir `
        -PassThru -NoNewWindow -RedirectStandardOutput (Join-Path $TestDrive 'mock-stdout.log') `
        -RedirectStandardError (Join-Path $TestDrive 'mock-stderr.log')

    # Wait for server to become responsive
    $maxWait = 30
    $waited = 0
    $ready = $false
    while ($waited -lt $maxWait -and -not $ready) {
        Start-Sleep -Seconds 1
        $waited++
        try {
            # Use .NET directly to skip cert validation
            $handler = [System.Net.Http.HttpClientHandler]::new()
            $handler.ServerCertificateCustomValidationCallback = { $true }
            $client = [System.Net.Http.HttpClient]::new($handler)
            $result = $client.GetStringAsync("$($script:MockUrl)/api2/json/version").GetAwaiter().GetResult()
            if ($result -match '"version"') {
                $ready = $true
            }
            $client.Dispose()
            $handler.Dispose()
        } catch {
            # Not ready yet
        }
    }

    if (-not $ready) {
        $stderr = if (Test-Path (Join-Path $TestDrive 'mock-stderr.log')) { Get-Content (Join-Path $TestDrive 'mock-stderr.log') -Raw } else { 'N/A' }
        throw "Mock server failed to start within ${maxWait}s. Stderr: $stderr"
    }

    # Import the module
    Import-Module PSProxmoxVE -Force -ErrorAction Stop
}

AfterAll {
    # Stop mock server
    if ($script:MockProcess -and -not $script:MockProcess.HasExited) {
        $script:MockProcess.Kill()
        $script:MockProcess.WaitForExit(5000)
    }

    # Disconnect any active session
    try { Disconnect-PveServer -ErrorAction SilentlyContinue } catch { }
}

Describe 'Mock PVE Server - Authentication' -Tag 'MockIntegration' {

    It 'Should connect with API token' {
        $session = Connect-PveServer -Server $script:MockHost -Port $script:MockPort `
            -ApiToken 'root@pam!test=00000000-0000-0000-0000-000000000000' `
            -SkipCertificateCheck -PassThru

        $session | Should -Not -BeNullOrEmpty
        $session.Hostname | Should -Be $script:MockHost
        $session.Port | Should -Be $script:MockPort
        $session.AuthMode | Should -Be 'ApiToken'
        $session.ServerVersion | Should -Not -BeNullOrEmpty
    }

    It 'Should connect with credentials' {
        $secPassword = ConvertTo-SecureString 'testpass' -AsPlainText -Force
        $cred = [PSCredential]::new('root@pam', $secPassword)

        $session = Connect-PveServer -Server $script:MockHost -Port $script:MockPort `
            -Credential $cred -SkipCertificateCheck -PassThru

        $session | Should -Not -BeNullOrEmpty
        $session.AuthMode | Should -Be 'Ticket'
        $session.Ticket | Should -Not -BeNullOrEmpty
        $session.CsrfToken | Should -Not -BeNullOrEmpty
        $session.IsExpired | Should -Be $false
    }

    It 'Should report connection status' {
        # Connect first
        Connect-PveServer -Server $script:MockHost -Port $script:MockPort `
            -ApiToken 'root@pam!test=00000000-0000-0000-0000-000000000000' `
            -SkipCertificateCheck

        $result = Test-PveConnection
        $result | Should -Be $true
    }

    It 'Should disconnect cleanly' {
        Connect-PveServer -Server $script:MockHost -Port $script:MockPort `
            -ApiToken 'root@pam!test=00000000-0000-0000-0000-000000000000' `
            -SkipCertificateCheck

        { Disconnect-PveServer } | Should -Not -Throw

        $result = Test-PveConnection
        $result | Should -Be $false
    }
}

Describe 'Mock PVE Server - Node Operations' -Tag 'MockIntegration' {

    BeforeAll {
        Connect-PveServer -Server $script:MockHost -Port $script:MockPort `
            -ApiToken 'root@pam!test=00000000-0000-0000-0000-000000000000' `
            -SkipCertificateCheck
    }

    AfterAll {
        try { Disconnect-PveServer -ErrorAction SilentlyContinue } catch { }
    }

    It 'Should list nodes' {
        $nodes = Get-PveNode
        $nodes | Should -Not -BeNullOrEmpty
        $nodes[0].Node | Should -Not -BeNullOrEmpty
        $nodes[0].Status | Should -Not -BeNullOrEmpty
    }

    It 'Should filter nodes by name' {
        $nodes = Get-PveNode -Name 'pve1'
        $nodes | Should -Not -BeNullOrEmpty
        $nodes | ForEach-Object { $_.Node | Should -Be 'pve1' }
    }
}

Describe 'Mock PVE Server - VM Operations' -Tag 'MockIntegration' {

    BeforeAll {
        Connect-PveServer -Server $script:MockHost -Port $script:MockPort `
            -ApiToken 'root@pam!test=00000000-0000-0000-0000-000000000000' `
            -SkipCertificateCheck
    }

    AfterAll {
        try { Disconnect-PveServer -ErrorAction SilentlyContinue } catch { }
    }

    It 'Should list VMs' {
        $vms = Get-PveVm -Node 'pve1'
        $vms | Should -Not -BeNullOrEmpty
        $vms.Count | Should -BeGreaterOrEqual 1
    }

    It 'Should filter VMs by status' {
        $running = Get-PveVm -Node 'pve1' -Status 'running'
        $running | Should -Not -BeNullOrEmpty
        $running | ForEach-Object { $_.Status | Should -Be 'running' }
    }

    It 'Should get VM config' {
        $config = Get-PveVmConfig -Node 'pve1' -VmId 100
        $config | Should -Not -BeNullOrEmpty
        $config.Cores | Should -BeGreaterThan 0
    }

    It 'Should get VM config via pipeline' {
        $vms = Get-PveVm -Node 'pve1' -VmId 100
        $config = $vms | Get-PveVmConfig
        $config | Should -Not -BeNullOrEmpty
    }
}

Describe 'Mock PVE Server - Storage Operations' -Tag 'MockIntegration' {

    BeforeAll {
        Connect-PveServer -Server $script:MockHost -Port $script:MockPort `
            -ApiToken 'root@pam!test=00000000-0000-0000-0000-000000000000' `
            -SkipCertificateCheck
    }

    AfterAll {
        try { Disconnect-PveServer -ErrorAction SilentlyContinue } catch { }
    }

    It 'Should list storage' {
        $storage = Get-PveStorage
        $storage | Should -Not -BeNullOrEmpty
        $storage[0].Storage | Should -Not -BeNullOrEmpty
    }

    It 'Should list storage content' {
        $content = Get-PveStorageContent -Node 'pve1' -Storage 'local'
        $content | Should -Not -BeNullOrEmpty
    }
}

Describe 'Mock PVE Server - Network Operations' -Tag 'MockIntegration' {

    BeforeAll {
        Connect-PveServer -Server $script:MockHost -Port $script:MockPort `
            -ApiToken 'root@pam!test=00000000-0000-0000-0000-000000000000' `
            -SkipCertificateCheck
    }

    AfterAll {
        try { Disconnect-PveServer -ErrorAction SilentlyContinue } catch { }
    }

    It 'Should list networks' {
        $networks = Get-PveNetwork -Node 'pve1'
        $networks | Should -Not -BeNullOrEmpty
        $networks[0].Iface | Should -Not -BeNullOrEmpty
    }
}

Describe 'Mock PVE Server - User Operations' -Tag 'MockIntegration' {

    BeforeAll {
        Connect-PveServer -Server $script:MockHost -Port $script:MockPort `
            -ApiToken 'root@pam!test=00000000-0000-0000-0000-000000000000' `
            -SkipCertificateCheck
    }

    AfterAll {
        try { Disconnect-PveServer -ErrorAction SilentlyContinue } catch { }
    }

    It 'Should list users' {
        $users = Get-PveUser
        $users | Should -Not -BeNullOrEmpty
        $users[0].UserId | Should -Not -BeNullOrEmpty
    }

    It 'Should list roles' {
        $roles = Get-PveRole
        $roles | Should -Not -BeNullOrEmpty
        $roles[0].RoleId | Should -Not -BeNullOrEmpty
    }
}

Describe 'Mock PVE Server - Cluster Operations' -Tag 'MockIntegration' {

    BeforeAll {
        Connect-PveServer -Server $script:MockHost -Port $script:MockPort `
            -ApiToken 'root@pam!test=00000000-0000-0000-0000-000000000000' `
            -SkipCertificateCheck
    }

    AfterAll {
        try { Disconnect-PveServer -ErrorAction SilentlyContinue } catch { }
    }

    It 'Should list snapshots' {
        $snapshots = Get-PveSnapshot -Node 'pve1' -VmId 100
        $snapshots | Should -Not -BeNullOrEmpty
    }
}

Describe 'Mock PVE Server - Container Operations' -Tag 'MockIntegration' {

    BeforeAll {
        Connect-PveServer -Server $script:MockHost -Port $script:MockPort `
            -ApiToken 'root@pam!test=00000000-0000-0000-0000-000000000000' `
            -SkipCertificateCheck
    }

    AfterAll {
        try { Disconnect-PveServer -ErrorAction SilentlyContinue } catch { }
    }

    It 'Should list containers' {
        $containers = Get-PveContainer -Node 'pve1'
        $containers | Should -Not -BeNullOrEmpty
        $containers[0].VmId | Should -BeGreaterThan 0
    }
}
