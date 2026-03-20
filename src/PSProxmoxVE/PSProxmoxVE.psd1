#
# Module manifest for module 'PSProxmoxVE'
#
# Generated on: 2026-03-17
#

@{

    # Script module or binary module file associated with this manifest.
    RootModule        = 'PSProxmoxVE.dll'

    # Version number of this module.
    ModuleVersion     = '0.1.0'

    # Supported PSEditions
    CompatiblePSEditions = @('Desktop', 'Core')

    # ID used to uniquely identify this module
    GUID              = 'a3f7c2d1-84e5-4b9f-a061-3e2d8c5f1a7b'

    # Author of this module
    Author            = 'goodolclint'

    # Company or vendor of this module
    CompanyName       = 'Worklab'

    # Copyright statement for this module
    Copyright         = '(c) 2026 goodolclint. All rights reserved.'

    # URI for online help
    HelpInfoUri       = 'https://github.com/goodolclint/PSProxmoxVE/tree/main/docs/cmdlets'

    # Description of the functionality provided by this module
    Description       = 'PowerShell module for managing Proxmox VE environments. Supports PVE 8.x and 9.x with full VM, container, storage, network, and cluster management capabilities.'

    # Minimum version of the PowerShell engine required by this module
    PowerShellVersion = '5.1'

    # Minimum version of the .NET Framework required by this module
    DotNetFrameworkVersion = '4.8'

    # Assemblies that must be loaded prior to importing this module
    RequiredAssemblies = @(
        'PSProxmoxVE.Core.dll',
        'Newtonsoft.Json.dll'
    )

    # Format files (.ps1xml) to be loaded when importing this module
    FormatsToProcess  = @('PSProxmoxVE.format.ps1xml')

    # Functions to export from this module
    FunctionsToExport = @()

    # Cmdlets to export from this module
    CmdletsToExport   = @(
        # Connection
        'Connect-PveServer',
        'Disconnect-PveServer',
        'Test-PveConnection',

        # Nodes
        'Get-PveNode',
        'Get-PveNodeStatus',

        # Virtual Machines
        'Get-PveVm',
        'New-PveVm',
        'Remove-PveVm',
        'Start-PveVm',
        'Stop-PveVm',
        'Suspend-PveVm',
        'Resume-PveVm',
        'Reset-PveVm',
        'Restart-PveVm',
        'Copy-PveVm',
        'Move-PveVm',
        'Get-PveVmConfig',
        'Set-PveVmConfig',
        'Resize-PveVmDisk',
        'Import-PveVmDisk',
        'Import-PveOva',

        # QEMU Guest Agent
        'Test-PveVmGuestAgent',
        'Get-PveVmGuestNetwork',
        'Invoke-PveVmGuestExec',

        # Containers
        'Get-PveContainer',
        'New-PveContainer',
        'Remove-PveContainer',
        'Start-PveContainer',
        'Stop-PveContainer',
        'Restart-PveContainer',
        'Copy-PveContainer',
        'Move-PveContainer',
        'Get-PveContainerConfig',
        'Set-PveContainerConfig',
        # Container Snapshots (4)
        'Get-PveContainerSnapshot',
        'New-PveContainerSnapshot',
        'Remove-PveContainerSnapshot',
        'Restore-PveContainerSnapshot',

        # Storage
        'Get-PveStorage',
        'Get-PveStorageContent',
        'Send-PveFile',
        'Invoke-PveStorageDownload',
        'New-PveStorage',
        'Remove-PveStorage',

        # Snapshots
        'Get-PveSnapshot',
        'New-PveSnapshot',
        'Remove-PveSnapshot',
        'Restore-PveSnapshot',

        # Networking
        'Get-PveNetwork',
        'New-PveNetwork',
        'Set-PveNetwork',
        'Remove-PveNetwork',
        'Invoke-PveNetworkApply',

        # SDN - Zones
        'Get-PveSdnZone',
        'New-PveSdnZone',
        'Remove-PveSdnZone',

        # SDN - VNets
        'Get-PveSdnVnet',
        'New-PveSdnVnet',
        'Remove-PveSdnVnet',
        # SDN Subnets (3)
        'Get-PveSdnSubnet',
        'New-PveSdnSubnet',
        'Remove-PveSdnSubnet',

        # Users
        'Get-PveUser',
        'New-PveUser',
        'Remove-PveUser',
        'Set-PveUser',

        # Roles
        'Get-PveRole',
        'New-PveRole',
        'Remove-PveRole',

        # Permissions
        'Get-PvePermission',
        'Set-PvePermission',

        # API Tokens
        'Get-PveApiToken',
        'New-PveApiToken',
        'Remove-PveApiToken',

        # Templates
        'Get-PveTemplate',
        'New-PveTemplate',
        'Remove-PveTemplate',
        'New-PveVmFromTemplate',

        # Cloud-Init
        'Get-PveCloudInitConfig',
        'Set-PveCloudInitConfig',
        'Invoke-PveCloudInitRegenerate',

        # Tasks
        'Get-PveTask',
        'Wait-PveTask'
    )

    # Variables to export from this module
    VariablesToExport = @()

    # Aliases to export from this module
    AliasesToExport   = @(
        'cpve',
        'dpve',
        'gpvm',
        'gpct',
        'gpn',
        'gpvs',
        'gpt'
    )

    # Private data to pass to the module specified in RootModule
    PrivateData       = @{

        PSData = @{

            # Prerelease string for the module
            Prerelease   = 'preview'

            # Tags applied to this module
            Tags         = @(
                'Proxmox',
                'ProxmoxVE',
                'PVE',
                'Virtualization',
                'IaC',
                'Homelab',
                'ProxmoxVE8',
                'ProxmoxVE9'
            )

            # URI to the license for this module
            LicenseUri   = 'https://github.com/goodolclint/PSProxmoxVE/blob/main/LICENSE'

            # URI to the project for this module
            ProjectUri   = 'https://github.com/goodolclint/PSProxmoxVE'

            # Release notes for this version
            ReleaseNotes = 'Initial preview release. Supports PVE 8.x and 9.x with VM, container, storage, network, SDN, user/role/permission, template, cloud-init, snapshot, and task management.'

        }

    }

}
