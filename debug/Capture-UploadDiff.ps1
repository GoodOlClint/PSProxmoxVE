#Requires -Version 7.0
# Capture-UploadDiff.ps1
# Starts mitmproxy, captures working PS approach vs C# module upload,
# then prints a side-by-side diff of the multipart framing.

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$DebugDir   = $PSScriptRoot
$ProxyPort  = 8080
$TestFile   = '/tmp/pvetest.iso'
$UploadUri  = 'https://172.16.100.205:8006/api2/json/nodes/pve/storage/local/upload'
$AuthHeader = 'PVEAPIToken=root@pam!integration=4c4b598b-fef2-4e6f-8471-316b35ff40c3'
$ModuleDll  = '/Users/goodolclint/Source/PSProxmoxVE/src/PSProxmoxVE/bin/Debug/net9.0/PSProxmoxVE.dll'

if (-not (Test-Path $TestFile)) {
    Write-Error "Test file not found: $TestFile"
}

# ---------------------------------------------------------------------------
function Start-Mitmdump([string]$FlowFile) {
    Get-Process mitmdump -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
    Remove-Item $FlowFile -ErrorAction SilentlyContinue

    $p = Start-Process mitmdump -ArgumentList @(
        '--mode', 'regular',
        '--ssl-insecure',
        '--listen-port', $ProxyPort,
        '-w', $FlowFile
    ) -PassThru -RedirectStandardOutput '/tmp/mitmdump-out.log' -RedirectStandardError '/tmp/mitmdump-err.log'

    Start-Sleep -Seconds 2
    if ($p.HasExited) {
        Write-Error "mitmdump failed to start: $(Get-Content /tmp/mitmdump-err.log -Raw)"
    }
    Write-Host "  mitmdump PID $($p.Id)" -ForegroundColor DarkGray
    return $p
}

function Stop-Mitmdump($p) {
    Start-Sleep -Seconds 1
    if (-not $p.HasExited) { $p.Kill(); $p.WaitForExit(3000) | Out-Null }
}

# ---------------------------------------------------------------------------
Write-Host '=== Step 1: Capture working PowerShell HttpClient upload ===' -ForegroundColor Cyan
$psFlow  = "$DebugDir/ps-module-capture.flow"
$mitm = Start-Mitmdump $psFlow

try {
    $handler = [System.Net.Http.HttpClientHandler]::new()
    $handler.ServerCertificateCustomValidationCallback = [System.Net.Http.HttpClientHandler]::DangerousAcceptAnyServerCertificateValidator
    $handler.Proxy    = [System.Net.WebProxy]::new("http://localhost:$ProxyPort")
    $handler.UseProxy = $true
    $client = [System.Net.Http.HttpClient]::new($handler)
    $client.Timeout = [TimeSpan]::FromMinutes(5)
    $client.DefaultRequestHeaders.TryAddWithoutValidation('Authorization', $AuthHeader) | Out-Null

    $boundary = 'PSDirectBoundary12345678'
    $content  = [System.Net.Http.MultipartFormDataContent]::new($boundary)
    $content.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("multipart/form-data; boundary=$boundary")

    function New-TextPart([string]$name, [string]$value) {
        $part = [System.Net.Http.StringContent]::new($value, [System.Text.Encoding]::UTF8)
        $part.Headers.ContentType = $null
        $part.Headers.ContentDisposition = [System.Net.Http.Headers.ContentDispositionHeaderValue]::new('form-data')
        $part.Headers.ContentDisposition.Name = "`"$name`""
        return $part
    }

    $content.Add((New-TextPart 'content' 'iso'))

    $fileStream = [System.IO.File]::OpenRead($TestFile)
    $fileName   = [System.IO.Path]::GetFileName($TestFile)
    $fileContent = [System.Net.Http.StreamContent]::new($fileStream)
    $fileContent.Headers.ContentDisposition = [System.Net.Http.Headers.ContentDispositionHeaderValue]::new('form-data')
    $fileContent.Headers.ContentDisposition.Name     = '"filename"'
    $fileContent.Headers.ContentDisposition.FileName = "`"$fileName`""
    $fileContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse('application/octet-stream')
    $content.Add($fileContent)

    Write-Host "  Posting..."
    $response = $client.PostAsync($UploadUri, $content).GetAwaiter().GetResult()
    $body     = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    Write-Host "  Status: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "  Body:   $body"
} catch {
    Write-Host "  ERROR: $_" -ForegroundColor Red
} finally {
    if ($fileStream) { $fileStream.Dispose() }
    if ($client)     { $client.Dispose() }
}

Stop-Mitmdump $mitm
Write-Host "  Saved: $psFlow" -ForegroundColor Green

# ---------------------------------------------------------------------------
Write-Host ''
Write-Host '=== Step 2: Capture C# module upload ===' -ForegroundColor Cyan
$moduleFlow = "$DebugDir/cs-module-capture.flow"
$mitm = Start-Mitmdump $moduleFlow

# Route HttpClient through proxy via env vars (.NET honors HTTPS_PROXY)
$env:HTTPS_PROXY = "http://localhost:$ProxyPort"
$env:HTTP_PROXY  = "http://localhost:$ProxyPort"

try {
    Import-Module $ModuleDll -Force -ErrorAction Stop

    Connect-PveServer -Server 172.16.100.205 -Port 8006 `
        -ApiToken 'root@pam!integration=4c4b598b-fef2-4e6f-8471-316b35ff40c3' `
        -SkipCertificateCheck

    Write-Host "  Calling Send-PveIso..."
    Send-PveIso -Node pve -Storage local -Path $TestFile -Confirm:$false -ErrorAction Stop
    Write-Host "  Upload succeeded!" -ForegroundColor Green
} catch {
    Write-Host "  ERROR: $_" -ForegroundColor Red
    Write-Host "  Type: $($_.Exception.GetType().FullName)"
    if ($_.Exception.InnerException) {
        Write-Host "  Inner: $($_.Exception.InnerException.Message)"
    }
} finally {
    $env:HTTPS_PROXY = ''
    $env:HTTP_PROXY  = ''
}

Stop-Mitmdump $mitm
Write-Host "  Saved: $moduleFlow" -ForegroundColor Green

# ---------------------------------------------------------------------------
Write-Host ''
Write-Host '=== Step 3: Diff multipart framing ===' -ForegroundColor Cyan

$analyzeScript = @'
import sys
from mitmproxy.io import FlowReader

def dump_flow(path, label):
    print(f'\n{"=" * 70}')
    print(f' {label}')
    print(f'{"=" * 70}')
    try:
        with open(path, 'rb') as f:
            reader = FlowReader(f)
            flows = list(reader.stream())
        if not flows:
            print('  NO FLOWS CAPTURED')
            return
        for i, flow in enumerate(flows):
            req = flow.request
            print(f'\nFlow {i}: {req.method} {req.pretty_url}')
            print(f'HTTP version: {req.http_version}')
            print(f'\nRequest headers:')
            for k, v in req.headers.items(multi=True):
                display_v = v[:40] + '...[REDACTED]' if k.lower() == 'authorization' else v
                print(f'  {k}: {display_v}')
            body = req.content
            print(f'\nBody size: {len(body)} bytes')
            # Show first 1500 bytes (multipart preamble before file data)
            preview = body[:1500]
            print(f'\nBody start (first 1500 bytes):')
            for line in preview.split(b'\r\n'):
                try:
                    print(f'  {line.decode("ascii")}')
                except Exception:
                    if len(line) > 40:
                        print(f'  [binary: {len(line)} bytes]')
                    else:
                        print(f'  {line!r}')
            if flow.response:
                print(f'\nResponse: {flow.response.status_code} {flow.response.reason}')
                body_text = flow.response.content[:300].decode('utf-8', errors='replace')
                print(f'  Body: {body_text}')
            else:
                print('\nResponse: NONE (connection reset/failed)')
    except Exception as e:
        print(f'Error reading {path}: {e}')

dump_flow(sys.argv[1], 'WORKING: PS Direct HttpClient')
dump_flow(sys.argv[2], 'FAILING: C# Module (Send-PveIso)')
'@

$py = "$DebugDir/_analyze.py"
Set-Content -Path $py -Value $analyzeScript
python3 $py $psFlow $moduleFlow

Write-Host ''
Write-Host '=== Done ===' -ForegroundColor Cyan
