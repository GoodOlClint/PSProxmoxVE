#Requires -Module Pester
<#
.SYNOPSIS
    Module-surface contract for PSProxmoxVE.

    Diffs the hand-maintained CmdletsToExport list in the manifest against the
    cmdlets the built assembly actually carries, in both directions, and checks
    the per-cmdlet conventions from CLAUDE.md that reflection can see.

    All tests are fully offline — no live Proxmox VE target is required.
#>

BeforeAll {
    . $PSScriptRoot/../_TestHelper.ps1

    $script:Module = Get-Module PSProxmoxVE
    $script:Manifest = Import-PowerShellDataFile (Join-Path $script:Module.ModuleBase 'PSProxmoxVE.psd1')
    $script:Declared = @($script:Manifest.CmdletsToExport)
    $script:Exported = @($script:Module.ExportedCmdlets.Keys)

    # PowerShell intersects CmdletsToExport with what the binary provides, so
    # ExportedCmdlets cannot reveal a cmdlet the manifest failed to declare.
    # Take the assembly from the loaded appdomain rather than from an export,
    # so an empty CmdletsToExport still reaches the assertions below.
    $script:Assembly = [AppDomain]::CurrentDomain.GetAssemblies() |
        Where-Object { $_.GetName().Name -eq 'PSProxmoxVE' } |
        Select-Object -First 1

    # GetTypes() throws if any one type fails to load; the types that did load
    # still answer the question this file asks.
    try { $allTypes = $script:Assembly.GetTypes() }
    catch {
        # PowerShell wraps a .NET exception thrown from a method call, so walk
        # down to the load exception that carries the partial type list.
        $loadError = $_.Exception
        while ($null -ne $loadError -and $loadError -isnot [System.Reflection.ReflectionTypeLoadException]) {
            $loadError = $loadError.InnerException
        }
        if ($null -eq $loadError) { throw }
        $allTypes = $loadError.Types | Where-Object { $null -ne $_ }
    }

    $script:CmdletTypes = @($allTypes | Where-Object {
        -not $_.IsAbstract -and
        $_.GetCustomAttributes([System.Management.Automation.CmdletAttribute], $false).Count -gt 0
    })

    $script:Implemented = @($script:CmdletTypes | ForEach-Object {
        $attr = $_.GetCustomAttributes([System.Management.Automation.CmdletAttribute], $false)[0]
        '{0}-{1}' -f $attr.VerbName, $attr.NounName
    })

    $script:Because = "module under test: $($script:Module.ModuleBase)"
}

Describe 'Module manifest' -Tag 'Unit' {

    It 'Finds the module, its manifest and its cmdlet types' {
        $script:Assembly | Should -Not -BeNullOrEmpty -Because $script:Because
        $script:Implemented.Count | Should -BeGreaterThan 0 -Because $script:Because
        $script:Declared.Count | Should -BeGreaterThan 0 -Because $script:Because
    }

    It 'Declares every cmdlet the assembly implements' {
        $undeclared = @($script:Implemented | Where-Object { $script:Declared -notcontains $_ } | Sort-Object)
        $undeclared -join ', ' | Should -BeNullOrEmpty -Because "a cmdlet the assembly carries but the manifest does not declare never reaches users ($script:Because)"
    }

    It 'Exports every name the manifest declares' {
        $unexported = @($script:Declared | Where-Object { $script:Exported -notcontains $_ } | Sort-Object)
        $unexported -join ', ' | Should -BeNullOrEmpty -Because "the manifest declares a name the imported module does not export ($script:Because)"
    }

    It 'Lists each cmdlet once' {
        $duplicates = @($script:Declared | Group-Object | Where-Object Count -gt 1 | ForEach-Object Name | Sort-Object)
        $duplicates -join ', ' | Should -BeNullOrEmpty -Because 'CmdletsToExport repeats these names'
    }
}

Describe 'Cmdlet conventions' -Tag 'Unit' {

    It 'Every cmdlet class is sealed' {
        $unsealed = @($script:CmdletTypes | Where-Object { -not $_.IsSealed } | ForEach-Object FullName | Sort-Object)
        $unsealed -join ', ' | Should -BeNullOrEmpty -Because 'cmdlet classes must be sealed'
    }

    It 'Every cmdlet class declares its own [OutputType]' {
        $missing = @($script:CmdletTypes | Where-Object {
            $_.GetCustomAttributes([System.Management.Automation.OutputTypeAttribute], $false).Count -eq 0
        } | ForEach-Object FullName | Sort-Object)
        $missing -join ', ' | Should -BeNullOrEmpty -Because 'an inherited [OutputType] would not describe the derived cmdlet'
    }

    It 'Every cmdlet uses the Pve noun prefix' {
        $offenders = @($script:Implemented | Where-Object { $_ -cnotmatch '^[A-Z]\w*-Pve[A-Z]\w*$' } | Sort-Object)
        $offenders -join ', ' | Should -BeNullOrEmpty -Because 'all cmdlets use the Pve noun prefix'
    }

    It 'Every cmdlet derived from PveCmdletBase exposes -Session' {
        $baseType = $script:Assembly.GetType('PSProxmoxVE.Cmdlets.PveCmdletBase')
        $baseType | Should -Not -BeNullOrEmpty
        $missing = @($script:CmdletTypes |
            Where-Object { $_.IsSubclassOf($baseType) } |
            ForEach-Object {
                $attr = $_.GetCustomAttributes([System.Management.Automation.CmdletAttribute], $false)[0]
                '{0}-{1}' -f $attr.VerbName, $attr.NounName
            } |
            Where-Object { -not (Get-Command $_).Parameters.ContainsKey('Session') } |
            Sort-Object)
        $missing -join ', ' | Should -BeNullOrEmpty -Because '-Session is inherited from PveCmdletBase and is part of every such cmdlet''s surface'
    }
}
