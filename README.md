# PSProxmoxVE

[![Build](https://github.com/goodolclint/PSProxmoxVE/actions/workflows/build.yml/badge.svg)](https://github.com/goodolclint/PSProxmoxVE/actions/workflows/build.yml)
[![Unit Tests](https://github.com/goodolclint/PSProxmoxVE/actions/workflows/unit-tests.yml/badge.svg)](https://github.com/goodolclint/PSProxmoxVE/actions/workflows/unit-tests.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/goodolclint/PSProxmoxVE/blob/main/LICENSE)
[![PowerShell Gallery](https://img.shields.io/powershellgallery/v/PSProxmoxVE)](https://www.powershellgallery.com/packages/PSProxmoxVE)

A production-grade C# binary PowerShell module for managing Proxmox VE environments.

## Supported Proxmox VE Versions

| PVE Version | Status |
|---|---|
| 9.x (current, 9.1.6+) | **Primary target — fully supported** |
| 8.x | Supported |
| 7.x (7.0+) | **Best-effort** — core cmdlets work, newer features emit clear version errors |
| 6.x and older | **Not supported** — hard blocked by version checks |

### Version Gating Policy

The module uses a two-tier version check for cmdlets that require newer PVE APIs:

- **Hard block** (introduced version): The API endpoint doesn't exist — the cmdlet emits a terminating error with a clear message like *"This operation requires Proxmox VE 8.1 or later."*
- **Warning** (default version): The feature exists but may not be enabled by default — a warning is emitted but the command proceeds, allowing users who manually enabled the feature to succeed.

Most cmdlets target endpoints available since PVE 4.2 and require no version check. Cmdlets that need newer APIs include SDN (introduced 6.2, default 8.0+), cloud-init management (7.2+), container interfaces (8.1+), VM disk import (8.1+), and pool management (8.1+ for update/delete).

## Prerequisites

- **PowerShell** 5.1 (Windows PowerShell) or 7.2+
- **OS**: Windows, Linux, macOS
- **Network**: HTTPS access to Proxmox VE API (default port 8006)

## .NET Framework Compatibility

| PowerShell Version | .NET Target | Notes |
|---|---|---|
| 5.1 (Windows PowerShell) | net48 (.NET Framework 4.8) | Full feature parity |
| 7.2, 7.4, 7.5 | net8.0 | Full feature parity |

All public API surface compiles and functions correctly on both targets.

## Installation

```powershell
# Install from the PowerShell Gallery
Install-Module -Name PSProxmoxVE -Scope CurrentUser

# Or install a prerelease version
Install-Module -Name PSProxmoxVE -Scope CurrentUser -AllowPrerelease

# Verify installation
Get-Module -ListAvailable PSProxmoxVE
Import-Module PSProxmoxVE
```

## Quick Start

### Connect to a Proxmox VE Server

```powershell
# Using username/password (ticket-based authentication)
$cred = Get-Credential -UserName 'root@pam'
Connect-PveServer -Server 'pve.example.com' -Credential $cred -SkipCertificateCheck

# Using API token
Connect-PveServer -Server 'pve.example.com' -ApiToken 'root@pam!mytoken=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx'

# Verify connection
Test-PveConnection -Detailed
```

### List and Manage VMs

```powershell
# List all VMs
Get-PveVm

# List VMs on a specific node
Get-PveVm -Node 'pve1'

# Filter by status
Get-PveVm -Status 'running'

# Start/stop VMs
Get-PveVm -Name 'my-vm' | Start-PveVm -Wait
Get-PveVm -Name 'my-vm' | Stop-PveVm -Wait

# Clone a VM
Get-PveVm -VmId 100 | Copy-PveVm -NewVmId 200 -NewName 'my-clone' -Full -Wait

# Get VM configuration
Get-PveVm -VmId 100 | Get-PveVmConfig
```

### Upload Files

```powershell
# Upload a local ISO file to Proxmox storage
Send-PveFile -Node 'pve1' -Storage 'local' -Path './ubuntu-24.04-live-server-amd64.iso' -Wait

# Upload a disk image for VM import
Send-PveFile -Node 'pve1' -Storage 'local' -Path './disk.qcow2' -ContentType 'import' -Wait
```

> **Note:** `Send-PveFile` implements a workaround for a long-standing Proxmox API multipart parsing bug
> ([bugzilla 7389](https://bugzilla.proxmox.com/show_bug.cgi?id=7389)). Standard multipart HTTP libraries
> (including .NET's `MultipartFormDataContent`) add sub-headers that Proxmox's `pveproxy` mishandles,
> resulting in corrupt uploads. This cmdlet constructs the multipart body manually to ensure correct uploads
> where other tools may produce corrupt files.

### Work with Snapshots

```powershell
# List snapshots
Get-PveVm -VmId 100 | Get-PveSnapshot

# Create a snapshot
Get-PveVm -VmId 100 | New-PveSnapshot -Name 'before-upgrade' -Description 'Snapshot before OS upgrade' -Wait

# Rollback
Get-PveVm -VmId 100 | Get-PveSnapshot | Where-Object Name -eq 'before-upgrade' | Restore-PveSnapshot -Wait
```

### Cloud-Init Configuration

```powershell
# Get current cloud-init config
Get-PveVm -VmId 100 | Get-PveCloudInitConfig

# Set cloud-init config
Get-PveVm -VmId 100 | Set-PveCloudInitConfig -Hostname 'web01' -User 'admin' -SshKeys @('ssh-ed25519 AAAA...') -IpConfig 'ip=dhcp' -Wait

# Regenerate cloud-init image after changes
Get-PveVm -VmId 100 | Invoke-PveCloudInitRegenerate -Wait
```

## Authentication Guide

### Username/Password (Ticket-Based)

Ticket authentication uses Proxmox's built-in session system. The module POSTs to `/api2/json/access/ticket` and stores the returned ticket cookie and CSRF token.

- **Format:** `user@realm` (e.g., `root@pam`, `admin@pve`, `user@mydomain`)
- **Expiry:** Tickets expire after 2 hours. The module detects expiry and prompts you to reconnect.
- **Realms:** Supports all Proxmox realms — `pam`, `pve`, custom LDAP/AD realms.
- **When to use:** Interactive sessions, ad-hoc management tasks.

```powershell
$cred = Get-Credential -UserName 'admin@pve'
Connect-PveServer -Server 'pve.example.com' -Credential $cred
```

### API Token

API tokens provide persistent, non-expiring authentication. They are the recommended approach for automation.

- **Format:** `USER@REALM!TOKENID=UUID` (e.g., `root@pam!automation=12345678-abcd-efgh-ijkl-123456789012`)
- **No expiry:** Tokens remain valid until explicitly revoked.
- **When to use:** Automation, scripts, CI/CD pipelines.

**Creating an API token in the PVE UI:**
1. Navigate to **Datacenter → Permissions → API Tokens**
2. Click **Add**
3. Select the user, enter a token ID, optionally uncheck "Privilege Separation"
4. Copy the token value — it is shown only once

```powershell
Connect-PveServer -Server 'pve.example.com' -ApiToken 'root@pam!automation=12345678-abcd-efgh-ijkl-123456789012'
```

## Multi-Cluster Usage

Every cmdlet accepts an optional `-Session` parameter. This enables managing multiple Proxmox VE clusters simultaneously:

```powershell
# Connect to two clusters
$prod = Connect-PveServer -Server 'pve-prod.example.com' -ApiToken $prodToken -PassThru
$dev = Connect-PveServer -Server 'pve-dev.example.com' -ApiToken $devToken -PassThru

# Query each cluster explicitly
$prodVms = Get-PveVm -Session $prod
$devVms = Get-PveVm -Session $dev

# The last Connect-PveServer call sets the default session
# So Get-PveVm without -Session uses $dev
Get-PveVm  # Uses $dev session
```

## SDN Management

Software-Defined Networking (SDN) features require **Proxmox VE 8.0 or later**.

```powershell
# List SDN zones and VNets
Get-PveSdnZone
Get-PveSdnVnet

# Create a new zone
New-PveSdnZone -Zone 'myzone' -Type 'simple'

# Create a VNet
New-PveSdnVnet -Vnet 'myvnet' -Zone 'myzone' -Tag 100
```

If connected to a PVE server below version 8.0, SDN cmdlets will throw a clear error:

```
SDN management requires Proxmox VE 8.0 or later. Connected server is version 7.4.
```

## Cmdlet Reference

### Connection
| Cmdlet | Description |
|---|---|
| `Connect-PveServer` | Establish a session to a Proxmox VE server |
| `Disconnect-PveServer` | Close the active session |
| `Test-PveConnection` | Test if the current session is valid |

### Nodes
| Cmdlet | Description |
|---|---|
| `Get-PveNode` | List cluster nodes |
| `Get-PveNodeStatus` | Get detailed node status |

### Virtual Machines
| Cmdlet | Description |
|---|---|
| `Get-PveVm` | List VMs with optional filters |
| `New-PveVm` | Create a new VM |
| `Remove-PveVm` | Delete a VM |
| `Start-PveVm` | Start a VM |
| `Stop-PveVm` | Stop a VM (hard) |
| `Restart-PveVm` | Graceful restart (shutdown + start) |
| `Suspend-PveVm` | Suspend a VM |
| `Resume-PveVm` | Resume a suspended VM |
| `Reset-PveVm` | Hard reset a VM |
| `Copy-PveVm` | Clone a VM (full or linked) |
| `Move-PveVm` | Migrate a VM to another node |
| `Get-PveVmConfig` | Get VM configuration |
| `Set-PveVmConfig` | Modify VM configuration |
| `Resize-PveVmDisk` | Resize a VM disk |
| `Import-PveVmDisk` | Import a disk image (qcow2, raw, vmdk, OVA) into a VM |
| `Import-PveOva` | Import an OVA appliance as a new VM (parses OVF, uploads, creates VM, imports disks) |

### Containers
| Cmdlet | Description |
|---|---|
| `Get-PveContainer` | List LXC containers |
| `New-PveContainer` | Create a new container |
| `Remove-PveContainer` | Delete a container |
| `Start-PveContainer` | Start a container |
| `Stop-PveContainer` | Stop a container |
| `Restart-PveContainer` | Restart a container |
| `Copy-PveContainer` | Clone a container |
| `Move-PveContainer` | Migrate a container to another node |
| `Get-PveContainerConfig` | Get container configuration |
| `Set-PveContainerConfig` | Modify container configuration |
| `Get-PveContainerSnapshot` | List container snapshots |
| `New-PveContainerSnapshot` | Create a container snapshot |
| `Remove-PveContainerSnapshot` | Delete a container snapshot |
| `Restore-PveContainerSnapshot` | Rollback to a container snapshot |

### Storage
| Cmdlet | Description |
|---|---|
| `Get-PveStorage` | List storage pools |
| `Get-PveStorageContent` | List storage content (ISOs, images, etc.) |
| `Send-PveFile` | Upload a file (ISO, disk image, template) to storage |
| `Invoke-PveStorageDownload` | Download a URL to storage (server-side) |
| `New-PveStorage` | Create a storage pool |
| `Remove-PveStorage` | Remove a storage pool |

### Snapshots
| Cmdlet | Description |
|---|---|
| `Get-PveSnapshot` | List VM snapshots |
| `New-PveSnapshot` | Create a snapshot |
| `Remove-PveSnapshot` | Delete a snapshot |
| `Restore-PveSnapshot` | Rollback to a snapshot |

### Network
| Cmdlet | Description |
|---|---|
| `Get-PveNetwork` | List network interfaces |
| `New-PveNetwork` | Create a network interface |
| `Set-PveNetwork` | Modify a network interface |
| `Remove-PveNetwork` | Delete a network interface |
| `Invoke-PveNetworkApply` | Apply pending network changes |

### SDN (PVE 8.0+)
| Cmdlet | Description |
|---|---|
| `Get-PveSdnZone` | List SDN zones |
| `New-PveSdnZone` | Create an SDN zone |
| `Remove-PveSdnZone` | Delete an SDN zone |
| `Get-PveSdnVnet` | List SDN VNets |
| `New-PveSdnVnet` | Create an SDN VNet |
| `Remove-PveSdnVnet` | Delete an SDN VNet |
| `Get-PveSdnSubnet` | List SDN subnets for a VNet |
| `New-PveSdnSubnet` | Create an SDN subnet |
| `Remove-PveSdnSubnet` | Delete an SDN subnet |

### Users & Permissions
| Cmdlet | Description |
|---|---|
| `Get-PveUser` | List users |
| `New-PveUser` | Create a user |
| `Remove-PveUser` | Delete a user |
| `Set-PveUser` | Modify a user |
| `Get-PveRole` | List roles |
| `New-PveRole` | Create a role |
| `Remove-PveRole` | Delete a role |
| `Get-PvePermission` | List permissions |
| `Set-PvePermission` | Set a permission |

### Templates
| Cmdlet | Description |
|---|---|
| `Get-PveTemplate` | List VM templates |
| `New-PveTemplate` | Convert a VM to a template |
| `Remove-PveTemplate` | Delete a template |
| `New-PveVmFromTemplate` | Create a VM from a template |

### Cloud-Init
| Cmdlet | Description |
|---|---|
| `Get-PveCloudInitConfig` | Get cloud-init configuration |
| `Set-PveCloudInitConfig` | Set cloud-init configuration |
| `Invoke-PveCloudInitRegenerate` | Regenerate cloud-init image |

### Tasks
| Cmdlet | Description |
|---|---|
| `Get-PveTask` | Get task status |
| `Wait-PveTask` | Wait for a task to complete |

### Firewall
| Cmdlet | Description |
|---|---|
| `Get-PveFirewallRule` | List firewall rules (cluster/node/VM/container) |
| `New-PveFirewallRule` | Create a firewall rule |
| `Set-PveFirewallRule` | Update a firewall rule |
| `Remove-PveFirewallRule` | Delete a firewall rule |
| `Get-PveFirewallGroup` | List security groups or group rules |
| `New-PveFirewallGroup` | Create a security group |
| `Remove-PveFirewallGroup` | Delete a security group |
| `Get-PveFirewallAlias` | List firewall IP aliases |
| `New-PveFirewallAlias` | Create a firewall IP alias |
| `Set-PveFirewallAlias` | Update a firewall IP alias |
| `Remove-PveFirewallAlias` | Delete a firewall IP alias |
| `Get-PveFirewallIpSet` | List firewall IP sets |
| `New-PveFirewallIpSet` | Create a firewall IP set |
| `Remove-PveFirewallIpSet` | Delete a firewall IP set |
| `Get-PveFirewallIpSetEntry` | List entries in an IP set |
| `New-PveFirewallIpSetEntry` | Add an entry to an IP set |
| `Set-PveFirewallIpSetEntry` | Update an IP set entry |
| `Remove-PveFirewallIpSetEntry` | Remove an entry from an IP set |
| `Get-PveFirewallOptions` | Get firewall options |
| `Set-PveFirewallOptions` | Set firewall options |
| `Get-PveFirewallRef` | List firewall references (aliases, IP sets) |

### Backup
| Cmdlet | Description |
|---|---|
| `New-PveBackup` | Create an ad-hoc backup (vzdump) |
| `Get-PveBackupJob` | List scheduled backup jobs |
| `New-PveBackupJob` | Create a scheduled backup job |
| `Set-PveBackupJob` | Update a scheduled backup job |
| `Remove-PveBackupJob` | Delete a scheduled backup job |
| `Get-PveBackupInfo` | Find VMs/containers not covered by backup jobs |

### SDN — IPAM / DNS / Controllers (PVE 8.0+)
| Cmdlet | Description |
|---|---|
| `Get-PveSdnIpam` | List SDN IPAM plugins |
| `New-PveSdnIpam` | Create an SDN IPAM plugin |
| `Set-PveSdnIpam` | Update an SDN IPAM plugin |
| `Remove-PveSdnIpam` | Remove an SDN IPAM plugin |
| `Get-PveSdnDns` | List SDN DNS plugins |
| `New-PveSdnDns` | Create an SDN DNS plugin |
| `Set-PveSdnDns` | Update an SDN DNS plugin |
| `Remove-PveSdnDns` | Remove an SDN DNS plugin |
| `Get-PveSdnController` | List SDN controllers |
| `New-PveSdnController` | Create an SDN controller |
| `Set-PveSdnController` | Update an SDN controller |
| `Remove-PveSdnController` | Remove an SDN controller |
| `Invoke-PveSdnApply` | Apply pending SDN configuration changes |
| `Set-PveSdnZone` | Update an SDN zone |
| `Set-PveSdnVnet` | Update an SDN VNet |
| `Set-PveSdnSubnet` | Update an SDN subnet |

### Cluster
| Cmdlet | Description |
|---|---|
| `Get-PveClusterResource` | List all resources (VMs, containers, nodes, storage) cluster-wide |

### Pools
| Cmdlet | Description |
|---|---|
| `Get-PvePool` | List resource pools |
| `New-PvePool` | Create a resource pool |
| `Set-PvePool` | Update a resource pool |
| `Remove-PvePool` | Delete a resource pool |

### VM Disk Operations
| Cmdlet | Description |
|---|---|
| `Move-PveVmDisk` | Move a VM disk to a different storage |
| `Remove-PveVmDisk` | Detach and optionally delete a VM disk |

### Guest Agent Extensions
| Cmdlet | Description |
|---|---|
| `Get-PveVmGuestOsInfo` | Get guest OS information via QEMU agent |
| `Get-PveVmGuestFsInfo` | Get guest filesystem information |
| `Read-PveVmGuestFile` | Read a file from inside a guest VM |
| `Write-PveVmGuestFile` | Write a file inside a guest VM |
| `Set-PveVmGuestPassword` | Change a user password inside a guest VM |
| `Invoke-PveVmGuestFsTrim` | TRIM guest VM filesystems |

### Additional Container Operations
| Cmdlet | Description |
|---|---|
| `Suspend-PveContainer` | Suspend (freeze) a container |
| `Resume-PveContainer` | Resume a suspended container |
| `Resize-PveContainerDisk` | Resize a container disk/volume |
| `New-PveContainerTemplate` | Convert a container to a template |
| `Move-PveContainerVolume` | Move a container volume to a different storage |
| `Get-PveContainerInterface` | Get container network interfaces |

### Storage Content
| Cmdlet | Description |
|---|---|
| `Get-PveStorageStatus` | Get storage usage statistics |
| `Remove-PveStorageContent` | Delete a volume, backup, or ISO from storage |
| `Set-PveStorageContent` | Update volume notes/properties |
| `New-PveStorageDisk` | Allocate a new empty disk image |

### Node Operations
| Cmdlet | Description |
|---|---|
| `Get-PveNodeConfig` | Get node configuration |
| `Set-PveNodeConfig` | Update node configuration |
| `Get-PveNodeDns` | Get node DNS configuration |
| `Set-PveNodeDns` | Update node DNS configuration |
| `Start-PveNodeVms` | Start all VMs on a node |
| `Stop-PveNodeVms` | Stop all VMs on a node |

### Access — Groups & Domains
| Cmdlet | Description |
|---|---|
| `Get-PveGroup` | List user groups |
| `New-PveGroup` | Create a user group |
| `Set-PveGroup` | Update a user group |
| `Remove-PveGroup` | Delete a user group |
| `Get-PveDomain` | List authentication realms (PAM, LDAP, AD, OpenID) |
| `New-PveDomain` | Create an authentication realm |
| `Set-PveDomain` | Update an authentication realm |
| `Remove-PveDomain` | Delete an authentication realm |
| `Set-PvePassword` | Change a user's password |
| `Set-PveRole` | Update a role's privileges |
| `Set-PveStorage` | Update a storage definition |
| `Set-PveApiToken` | Update an API token |

## Known Limitations (v1)

- **No automatic retries**: Failed API calls are not retried. Implement your own retry logic if needed.
- **Integration tests require live node**: Integration tests require a dedicated Proxmox VE test node. See `tests/PSProxmoxVE.Tests/Integration/README.md`.
- **No Ceph management**: Ceph pool/OSD/monitor management is not included in v1.
- **No PBS integration**: Proxmox Backup Server operations are not included in v1.
- **Task waiting**: `Wait-PveTask` polls with a minimum 1-second interval. For high-frequency monitoring, use the PVE web UI.

## Contributing

1. Clone the repository
2. Open `PSProxmoxVE.sln` in your IDE
3. Build: `dotnet build`
4. Run unit tests: `./tools/Invoke-Tests.ps1`
5. Run integration tests (requires live PVE node): `./tools/Invoke-Tests.ps1 -Tier Integration`

### Commit Convention

This project uses [Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` — new feature
- `fix:` — bug fix
- `test:` — test additions or changes
- `ci:` — CI/CD changes
- `docs:` — documentation changes
- `refactor:` — code refactoring

## License

[MIT](LICENSE)
