#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generates platyPS markdown help stubs and MAML XML help for PSProxmoxVE.

.DESCRIPTION
    This script:
    1. Builds the module to publish/netstandard2.0/
    2. Installs platyPS if needed
    3. Generates markdown help stubs in docs/cmdlets/
    4. Generates MAML XML help in publish/netstandard2.0/
    5. Copies the MAML XML to src/PSProxmoxVE/ for inclusion in builds

.NOTES
    Run from the repository root:
        pwsh -NoProfile -File ./generate-help.ps1
#>

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = $PSScriptRoot
Push-Location $repoRoot

try {
    # Step 1: Build the module
    Write-Host "Building module..." -ForegroundColor Cyan
    dotnet publish src/PSProxmoxVE/PSProxmoxVE.csproj --configuration Release --framework netstandard2.0 --output ./publish/netstandard2.0 | Out-Null
    Remove-Item ./publish/netstandard2.0/*.deps.json, ./publish/netstandard2.0/*.runtimeconfig.json -ErrorAction SilentlyContinue

    # Step 2: Install platyPS if needed
    if (-not (Get-Module -ListAvailable -Name platyPS)) {
        Write-Host "Installing platyPS..." -ForegroundColor Cyan
        Install-Module platyPS -Force -Scope CurrentUser
    }

    # Step 3: Import modules
    Write-Host "Importing modules..." -ForegroundColor Cyan
    Import-Module platyPS -Force
    Import-Module ./publish/netstandard2.0/PSProxmoxVE.psd1 -Force

    # Step 4: Create output directory
    $docsDir = Join-Path $repoRoot 'docs/cmdlets'
    if (-not (Test-Path $docsDir)) {
        New-Item -ItemType Directory -Path $docsDir -Force | Out-Null
    }

    # Step 5: Generate markdown help stubs
    Write-Host "Generating markdown help stubs..." -ForegroundColor Cyan
    New-MarkdownHelp -Module PSProxmoxVE -OutputFolder $docsDir -Force | Out-Null

    $mdCount = (Get-ChildItem "$docsDir/*.md" | Measure-Object).Count
    Write-Host "  Generated $mdCount markdown files" -ForegroundColor Green

    # Step 6: Generate MAML XML help
    Write-Host "Generating MAML XML help..." -ForegroundColor Cyan
    New-ExternalHelp -Path $docsDir -OutputPath ./publish/netstandard2.0/ -Force | Out-Null

    # Step 7: Copy MAML XML to src directory for build inclusion
    $mamlFiles = Get-ChildItem ./publish/netstandard2.0/*-Help.xml
    foreach ($f in $mamlFiles) {
        $dest = Join-Path 'src/PSProxmoxVE' $f.Name
        Copy-Item $f.FullName $dest -Force
        Write-Host "  Copied $($f.Name) to src/PSProxmoxVE/" -ForegroundColor Green
    }

    # Step 8: Verify
    Write-Host "`nVerification:" -ForegroundColor Cyan
    Write-Host "  Markdown files: $mdCount" -ForegroundColor Green
    foreach ($f in $mamlFiles) {
        $size = [math]::Round($f.Length / 1024, 1)
        Write-Host "  MAML XML: $($f.Name) (${size} KB)" -ForegroundColor Green
    }

    Write-Host "`nDone! Help generation complete." -ForegroundColor Green
}
finally {
    Pop-Location
}
