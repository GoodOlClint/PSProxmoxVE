#Requires -Module Pester
<#
.SYNOPSIS
    Pester 5 tests proving the active PVE session is per-runspace state, not a
    process-wide static shared by every runspace in the host.
    Fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    # A dll import produces a module with no session state, so the runspaces import the
    # manifest that sits beside it.
    $script:ModulePath = Join-Path (Split-Path (Get-Module PSProxmoxVE).Path) 'PSProxmoxVE.psd1'

    function New-PveTestRunspace {
        param([string]$Path)

        $ps = [powershell]::Create()
        $null = $ps.AddScript({
            param($p)
            Import-Module $p -Force -ErrorAction Stop
        }).AddArgument($Path)
        $null = $ps.Invoke()
        if ($ps.Streams.Error.Count -gt 0) {
            $err = $ps.Streams.Error[0]
            $ps.Dispose()
            throw "Runspace module import failed: $err"
        }
        $ps.Commands.Clear()
        $ps.Streams.Error.Clear()
        $ps
    }

    function Invoke-InRunspace {
        param([System.Management.Automation.PowerShell]$Runspace, [scriptblock]$Script, $Argument)

        $Runspace.Commands.Clear()
        $Runspace.Streams.Error.Clear()
        $null = $Runspace.AddScript($Script)
        if ($PSBoundParameters.ContainsKey('Argument')) { $null = $Runspace.AddArgument($Argument) }
        $out = $Runspace.Invoke()
        if ($Runspace.Streams.Error.Count -gt 0) {
            throw "Runspace script failed: $($Runspace.Streams.Error[0])"
        }
        , $out
    }
}

Describe 'Active session isolation between runspaces' {
    BeforeAll {
        $script:SetSession = {
            param($hostname)
            $ctor = [PSProxmoxVE.Core.Authentication.PveSession].GetConstructor(
                [System.Reflection.BindingFlags]'NonPublic, Instance',
                $null,
                [type[]]@([string], [int], [bool], [string]),
                $null)
            $session = $ctor.Invoke(@($hostname, 8006, $false, 'token'))
            (Get-Module PSProxmoxVE).SessionState.PSVariable.Set('PSProxmoxVE.ActiveSession', $session)
        }

        $script:ClearSession = {
            (Get-Module PSProxmoxVE).SessionState.PSVariable.Set('PSProxmoxVE.ActiveSession', $null)
        }

        $script:GetHostname = {
            $detailed = Test-PveConnection -Detailed
            if ($null -eq $detailed) { '<none>' } else { $detailed.Hostname }
        }

        $script:RunspaceA = New-PveTestRunspace -Path $script:ModulePath
        $script:RunspaceB = New-PveTestRunspace -Path $script:ModulePath
    }

    AfterAll {
        if ($script:RunspaceA) { $script:RunspaceA.Dispose() }
        if ($script:RunspaceB) { $script:RunspaceB.Dispose() }
    }

    It 'Should not expose one runspace''s session to another' {
        Invoke-InRunspace -Runspace $script:RunspaceB -Script $script:ClearSession
        Invoke-InRunspace -Runspace $script:RunspaceA -Script $script:SetSession -Argument 'runspace-a.example'

        $connectedInB = Invoke-InRunspace -Runspace $script:RunspaceB -Script { Test-PveConnection }
        $connectedInB[0] | Should -BeFalse
    }

    It 'Should keep each runspace on its own server after both connect' {
        Invoke-InRunspace -Runspace $script:RunspaceA -Script $script:SetSession -Argument 'runspace-a.example'
        Invoke-InRunspace -Runspace $script:RunspaceB -Script $script:SetSession -Argument 'runspace-b.example'

        $inA = Invoke-InRunspace -Runspace $script:RunspaceA -Script $script:GetHostname
        $inB = Invoke-InRunspace -Runspace $script:RunspaceB -Script $script:GetHostname

        $inA[0] | Should -Be 'runspace-a.example'
        $inB[0] | Should -Be 'runspace-b.example'
    }

    It 'Should not let one runspace''s disconnect clear another''s session' {
        Invoke-InRunspace -Runspace $script:RunspaceA -Script $script:SetSession -Argument 'runspace-a.example'
        Invoke-InRunspace -Runspace $script:RunspaceB -Script $script:SetSession -Argument 'runspace-b.example'

        Invoke-InRunspace -Runspace $script:RunspaceB -Script { Disconnect-PveServer -Confirm:$false }

        $inA = Invoke-InRunspace -Runspace $script:RunspaceA -Script $script:GetHostname
        $inB = Invoke-InRunspace -Runspace $script:RunspaceB -Script $script:GetHostname
        $inA[0] | Should -Be 'runspace-a.example'
        $inB[0] | Should -Be '<none>'
    }
}
