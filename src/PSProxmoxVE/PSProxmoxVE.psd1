#
# Module manifest for module 'PSProxmoxVE'
#
# Generated on: 2026-03-17
#

@{

    # Script module or binary module file associated with this manifest.
    RootModule        = 'PSProxmoxVE.dll'

    # Version number of this module.
    ModuleVersion     = '0.3.0'

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
        'Wait-PveTask',

        # Firewall
        'Get-PveFirewallRule',
        'New-PveFirewallRule',
        'Set-PveFirewallRule',
        'Remove-PveFirewallRule',
        'Get-PveFirewallGroup',
        'New-PveFirewallGroup',
        'Remove-PveFirewallGroup',
        'Get-PveFirewallAlias',
        'New-PveFirewallAlias',
        'Set-PveFirewallAlias',
        'Remove-PveFirewallAlias',
        'Get-PveFirewallIpSet',
        'New-PveFirewallIpSet',
        'Remove-PveFirewallIpSet',
        'Get-PveFirewallIpSetEntry',
        'New-PveFirewallIpSetEntry',
        'Set-PveFirewallIpSetEntry',
        'Remove-PveFirewallIpSetEntry',
        'Get-PveFirewallOptions',
        'Set-PveFirewallOptions',
        'Get-PveFirewallRef',

        # Backup
        'New-PveBackup',
        'Get-PveBackupJob',
        'New-PveBackupJob',
        'Set-PveBackupJob',
        'Remove-PveBackupJob',

        # SDN — IPAM
        'Get-PveSdnIpam',
        'New-PveSdnIpam',
        'Remove-PveSdnIpam',

        # SDN — DNS
        'Get-PveSdnDns',
        'New-PveSdnDns',
        'Remove-PveSdnDns',

        # SDN — Controller
        'Get-PveSdnController',
        'New-PveSdnController',
        'Remove-PveSdnController',

        # SDN — Update / Apply
        'Set-PveSdnZone',
        'Set-PveSdnVnet',
        'Set-PveSdnSubnet',
        'Set-PveSdnController',
        'Set-PveSdnIpam',
        'Set-PveSdnDns',
        'Invoke-PveSdnApply',

        # Role / Storage / Token — Update
        'Set-PveRole',
        'Set-PveStorage',
        'Set-PveApiToken',

        # Cluster
        'Get-PveClusterResource',
        'Get-PveClusterStatus',
        'Get-PveClusterNextId',
        'Get-PveClusterOption',
        'Set-PveClusterOption',
        'Get-PveClusterConfig',
        'Get-PveClusterConfigNode',
        'Add-PveClusterConfigNode',
        'Remove-PveClusterConfigNode',
        'Get-PveClusterJoinInfo',
        'Add-PveClusterMember',
        'New-PveCluster',

        # HA — Resources
        'Get-PveHaResource',
        'New-PveHaResource',
        'Set-PveHaResource',
        'Remove-PveHaResource',
        'Move-PveHaResource',

        # HA — Groups
        'Get-PveHaGroup',
        'New-PveHaGroup',
        'Set-PveHaGroup',
        'Remove-PveHaGroup',

        # HA — Status
        'Get-PveHaStatus',

        # HA — Rules (PVE 9.0+)
        'Get-PveHaRule',
        'New-PveHaRule',
        'Set-PveHaRule',
        'Remove-PveHaRule',

        # Tasks
        'Get-PveTaskList',
        'Stop-PveTask',

        # Pools
        'Get-PvePool',
        'New-PvePool',
        'Set-PvePool',
        'Remove-PvePool',

        # Backup Compliance
        'Get-PveBackupInfo',

        # VM Disk Operations
        'Move-PveVmDisk',
        'Remove-PveVmDisk',

        # VM Guest Agent Extensions
        'Get-PveVmGuestOsInfo',
        'Get-PveVmGuestFsInfo',
        'Read-PveVmGuestFile',
        'Write-PveVmGuestFile',
        'Set-PveVmGuestPassword',
        'Invoke-PveVmGuestFsTrim',

        # Container Gaps
        'Suspend-PveContainer',
        'Resume-PveContainer',
        'Resize-PveContainerDisk',
        'New-PveContainerTemplate',
        'Move-PveContainerVolume',
        'Get-PveContainerInterface',

        # Storage Content Management
        'Get-PveStorageStatus',
        'Remove-PveStorageContent',
        'Set-PveStorageContent',
        'New-PveStorageDisk',

        # Node Operations
        'Get-PveNodeConfig',
        'Set-PveNodeConfig',
        'Get-PveNodeDns',
        'Set-PveNodeDns',
        'Start-PveNodeVms',
        'Stop-PveNodeVms',

        # Access — Groups
        'Get-PveGroup',
        'New-PveGroup',
        'Set-PveGroup',
        'Remove-PveGroup',

        # Access — Domains / Realms
        'Get-PveDomain',
        'New-PveDomain',
        'Set-PveDomain',
        'Remove-PveDomain',

        # Access — Password
        'Set-PvePassword'
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

            # Prerelease string for the module (empty = stable release)
            # Prerelease   = 'preview'

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
            ReleaseNotes = @'
## 0.3.0

Added:
- Firewall security-group rules: Get/New/Set/Remove-PveFirewallRule -Level Group -Group <name> (#126).
- New-PveNetwork / Set-PveNetwork -BridgeVlanAware (#92).
- Connect-PveServer -ApiToken now takes a SecureString; a plain string still
  binds this release with a deprecation warning and is removed in the next
  major. The session object no longer exposes ApiToken, Ticket or CsrfToken (#147).

Changed:
- Every cmdlet reports PVE API failures as typed error records
  (PermissionDenied, ObjectNotFound, InvalidArgument, OperationTimeout,
  AuthenticationError, ConnectionError, OperationStopped) with an ErrorId
  naming the resource and status, so -ErrorAction and typed catch work (#155).
- Ticket sessions renew themselves at half their lifetime and retry once after
  a 401; long -Wait operations no longer die at the two-hour mark (#143).
- The session is stored per runspace, not in a process-wide static, so
  ForEach-Object -Parallel and hosted runspaces no longer share it (#150).
- One pooled transport per host; -Wait polling backs off from 1 s to 10 s over
  one connection (#151). Get-PveVm without -Node is one cluster/resources call;
  -Detailed streams (#152).
- 31 cmdlets send their requests through their service, with the payloads
  asserted offline; where service and cmdlet disagreed the shipped cmdlet
  behaviour won (#126). Lifecycle waits run in GuestLifecycleService; OVA
  parsing in OvfReader; Get-PveNodeConfig/NodeDns/ClusterConfig/BackupInfo
  return typed objects (#157). Ten unused service methods removed (#220).
- Restart-PveVm uses PVE's native reboot endpoint; guest operations retry past
  the qemu-server config flock for up to 45 s; -Wait also waits for the config
  lock to clear; New-PveCluster -Wait blocks until quorum (#113, D014-D016, D020).
- Newtonsoft.Json 13.0.4, central package versions, SDK pinned (#156).

Fixed:
- Copy-PveVm/Copy-PveContainer allocate a valid ID and forward -Storage (#135);
  Import-PveOva disk bus mapping, href validation, DTD rejection and upload
  timeout (#138, #139, #148); Remove-* path traversal (#145); Remove-PveVm
  -Force and Remove-PveContainer -Force honoured (#136); Get-PvePermission
  returns Privileges (#137); Invoke-PveVmGuestExec boolean exited (#141);
  Disconnect-PveServer no longer calls a non-existent endpoint and gained
  -Session (#144); Wait-PveTask uses the shared poller (#140); Send-PveFile
  -ContentType vztmpl/import (#126); Get-PveClusterConfig no longer always
  empty (#157); Add-PveClusterMember -Wait keeps its re-auth fallback across
  the join's key rotation (#143).

Full changelog: https://github.com/goodolclint/PSProxmoxVE/blob/main/CHANGELOG.md
'@

        }

    }

}
