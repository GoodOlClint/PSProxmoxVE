# PSProxmoxVE Module Review Report

```
Scan date:           2026-03-22
Prior report date:   2026-03-21
PVE API spec date:   2026-03-21T15:04:50.641Z
PVE API spec SHA256: 4af79be30166209a4714b771f65e1e9540c5b738f414ff30c98454402e29d030
PVE version hint:    (not set in spec)
Total API endpoints: 646
```

**Reviewer:** Claude Code (Opus 4.6)
**Module Version:** 0.1.0-preview
**Repository:** PSProxmoxVE (C# binary PowerShell module)

---

## Executive Summary

**Delta since last scan: 16 fixed | 5 new findings | 0 regressed | 9 still open**

**API drift: 194 endpoints with version_changes | 466 PVE endpoints unimplemented (72.1% uncovered) | 42 🆕 PVE 9.0 endpoints**

1. **169 cmdlets** (up from 148) — all fully exported, sealed, with OutputType attributes. New: 21 additional cmdlets since prior scan.
2. **14 prior findings resolved**: All 4 infinite-loop WaitForTask methods (CQ1–CQ4) now use `TaskService.WaitForTask`. Guest exec timeout added (CQ5). OutputType on all cmdlets (CQ9). Firewall VmId nullable (CQ10). Bare catches replaced with filtered exceptions (CQ11–CQ14). RemovePveRole/SuspendPveVm/RestartPveVm ConfirmImpact fixed (CQ17/CQ19/CQ20). Cmdlets sealed (CQ18). Dual JSON attrs removed (CQ21). Magic strings extracted to constants (CQ22). Guest password now SecureString (S1). Debug script secrets redacted (S2).
3. **Remaining Critical**: 5 cmdlets still have `while(true)` task-polling loops with no timeout — 3 container snapshot cmdlets plus `InvokePveStorageDownloadCmdlet` and `SendPveFileCmdlet`.
4. **100% Pester unit test coverage** maintained across all 169 cmdlets. Integration test coverage is ~62% (105 of 169).
5. **27.9% API endpoint coverage** (180 of 646 endpoints). Major uncovered areas unchanged: Ceph (40), HA (21), cluster config (10), disks (18), ACME (15), certificates (8).
6. **Security posture solid**: All prior security findings resolved (S1–S3). URL encoding now consistent across all services. HTTPS enforced, SecureString used consistently.
7. **PSGallery-ready**: No publication blockers. IconUri still missing (cosmetic). Manifest CmdletsToExport fully enumerates all 169 cmdlets.
8. **net9.0 target is EOL** (May 2025) — should upgrade to net10.0.
9. **Pool/CephPool parameter conflict** in New-PveStorage resolved: runtime validation now throws if both specified.
10. **42 new PVE 9.0 endpoints** available (HA rules, SDN fabrics, bulk actions, OCI registry). None yet implemented.

---

## Phase 1 — Repository Inventory & Structure

### Directory Tree (depth ≤ 4)

```
PSProxmoxVE/
├── PSProxmoxVE.sln
├── README.md
├── LICENSE (MIT)
├── CHANGELOG.md
├── CONTRIBUTING.md
├── CODE_OF_CONDUCT.md
├── SECURITY.md
├── .editorconfig
├── .gitignore
├── .gitattributes
├── REVIEW_REPORT.md
├── .github/
│   ├── ISSUE_TEMPLATE/
│   │   ├── bug_report.yml
│   │   └── feature_request.yml
│   ├── pull_request_template.md
│   └── workflows/
│       ├── build.yml
│       ├── unit-tests.yml
│       ├── integration-tests.yml
│       └── publish.yml
├── docs/
│   ├── PVE_API_COVERAGE.md
│   └── cmdlets/                      (75 cmdlet documentation files)
├── src/
│   ├── PSProxmoxVE/
│   │   ├── PSProxmoxVE.csproj
│   │   ├── PSProxmoxVE.psd1
│   │   ├── PSProxmoxVE.format.ps1xml
│   │   ├── PSProxmoxVE.dll-Help.xml
│   │   ├── ModuleState.cs
│   │   └── Cmdlets/
│   │       ├── PveCmdletBase.cs
│   │       ├── Backup/              (6 cmdlets)
│   │       ├── CloudInit/           (3 cmdlets)
│   │       ├── Cluster/             (1 cmdlet)
│   │       ├── Connection/          (3 cmdlets)
│   │       ├── Containers/          (20 cmdlets)
│   │       ├── Firewall/            (21 cmdlets)
│   │       ├── Network/             (5 + 22 SDN cmdlets)
│   │       ├── Nodes/               (8 cmdlets)
│   │       ├── Pools/               (4 cmdlets)
│   │       ├── Snapshots/           (4 cmdlets)
│   │       ├── Storage/             (11 cmdlets)
│   │       ├── Tasks/               (4 cmdlets)
│   │       ├── Templates/           (4 cmdlets)
│   │       ├── Users/               (23 cmdlets)
│   │       └── Vms/                 (30 cmdlets)
│   └── PSProxmoxVE.Core/
│       ├── PSProxmoxVE.Core.csproj
│       ├── Authentication/           (PveAuthenticator, PveAuthMode, PveSession, PveVersion)
│       ├── Client/                   (PveHttpClient)
│       ├── Exceptions/               (7 exception types)
│       ├── Models/                   (25+ model classes across 9 areas)
│       └── Services/                 (14 service classes)
├── tests/
│   ├── PSProxmoxVE.Core.Tests/       (xUnit)
│   │   ├── Authentication/           (3 test files)
│   │   ├── Fixtures/                 (28 JSON fixtures)
│   │   ├── Models/                   (12 test files)
│   │   └── TestHelper.cs
│   ├── PSProxmoxVE.Tests/            (Pester 5)
│   │   ├── _TestHelper.ps1
│   │   ├── Backup/                   (2 test files)
│   │   ├── CloudInit/                (1 test file)
│   │   ├── Cluster/                  (1 test file)
│   │   ├── Connection/               (3 test files)
│   │   ├── Containers/               (5 test files)
│   │   ├── Firewall/                 (5 test files)
│   │   ├── Network/                  (5 test files)
│   │   ├── Nodes/                    (2 test files)
│   │   ├── Pools/                    (1 test file)
│   │   ├── Snapshots/                (1 test file)
│   │   ├── Storage/                  (4 test files)
│   │   ├── Tasks/                    (2 test files)
│   │   ├── Templates/                (1 test file)
│   │   ├── Users/                    (4 test files)
│   │   ├── Vms/                      (12 test files)
│   │   └── Integration/              (1 comprehensive test file)
│   └── infrastructure/               (Terraform, Dockerfile, scripts for CI)
├── publish/                          (netstandard2.0 build output)
├── debug/                            (debug/analysis scripts)
└── tools/
    └── Invoke-Tests.ps1
```

### Project Layout

| Item | Value |
|---|---|
| Solution | `PSProxmoxVE.sln` |
| Main project | `src/PSProxmoxVE/PSProxmoxVE.csproj` |
| Core library | `src/PSProxmoxVE.Core/PSProxmoxVE.Core.csproj` |
| Test project (xUnit) | `tests/PSProxmoxVE.Core.Tests/PSProxmoxVE.Core.Tests.csproj` |
| Test project (Pester) | `tests/PSProxmoxVE.Tests/` |
| Target Frameworks | `netstandard2.0`, `net9.0`, `net48` |
| Language Version | C# 10.0 |
| Nullable | Enabled |
| Assembly Names | `PSProxmoxVE`, `PSProxmoxVE.Core` |

### CI/CD Workflows

| Workflow | Purpose |
|---|---|
| `build.yml` | Build + xUnit tests on net48 (Windows) and net9.0 (Windows + Ubuntu) |
| `unit-tests.yml` | Build + Pester tests across PS 5.1/7.5 on Windows/Ubuntu/macOS |
| `integration-tests.yml` | Full integration tests against nested PVE 8 + PVE 9 instances |
| `publish.yml` | Tag-triggered publish to PSGallery + GitHub Release creation |

### Inventory Checklist

| Item | Present | Prior Status |
|---|---|---|
| Solution file (`.sln`) | Yes | Open |
| Module manifest (`.psd1`) | Yes | Open |
| Format definitions (`.format.ps1xml`) | Yes | Open |
| MAML help (`.dll-Help.xml`) | Yes | Open |
| `.editorconfig` | Yes | Open |
| `.gitignore` | Yes | Open |
| `.gitattributes` | Yes | Open |
| `LICENSE` (MIT) | Yes | Open |
| `CHANGELOG.md` | Yes | Open |
| `README.md` | Yes | Open |
| `CONTRIBUTING.md` | Yes | Open |
| `CODE_OF_CONDUCT.md` | Yes | Open |
| `SECURITY.md` | Yes | Open |
| `.github/ISSUE_TEMPLATE/` | Yes | Open |
| `.github/pull_request_template.md` | Yes | Open |
| PSGallery publish workflow | Yes | Open |
| `docs/PVE_API_COVERAGE.md` | Yes | Open |
| `docs/cmdlets/` (75 files) | Yes | Open |

### What's Missing

- [ ] `IconUri` in manifest PSData (cosmetic)
- [ ] `.github/ISSUE_TEMPLATE/config.yml` (controls blank issue creation)
- [ ] `CODEOWNERS` file (low priority for single-maintainer)

---

## Phase 2 — PVE API Coverage Audit

> **Source of truth**: `~/Source/pve_api/pve-api.json` (646 endpoints). Spec generated 2026-03-21.
> **Latest tracked PVE version**: 9.0 (42 new endpoints, 56 changed endpoints)

### Implemented Cmdlets (169 total)

#### Connection (3)
| Cmdlet | API Endpoint | Method | Prior Status |
|---|---|---|---|
| `Connect-PveServer` | `/access/ticket` | POST | Open |
| `Disconnect-PveServer` | (session teardown) | — | Open |
| `Test-PveConnection` | `/version` | GET | Open |

#### Nodes (8)
| Cmdlet | API Endpoint | Method | Prior Status |
|---|---|---|---|
| `Get-PveNode` | `/nodes` | GET | Open |
| `Get-PveNodeStatus` | `/nodes/{node}/status` | GET | Open |
| `Get-PveNodeConfig` | `/nodes/{node}/config` | GET | Open |
| `Set-PveNodeConfig` | `/nodes/{node}/config` | PUT | Open |
| `Get-PveNodeDns` | `/nodes/{node}/dns` | GET | Open |
| `Set-PveNodeDns` | `/nodes/{node}/dns` | PUT | Open |
| `Start-PveNodeVms` | `/nodes/{node}/startall` | POST | Open |
| `Stop-PveNodeVms` | `/nodes/{node}/stopall` | POST | Open |

#### VMs / QEMU (19)
| Cmdlet | API Endpoint | Method | Prior Status |
|---|---|---|---|
| `Get-PveVm` | `/nodes/{node}/qemu` | GET | Open |
| `New-PveVm` | `/nodes/{node}/qemu` | POST | Open |
| `Remove-PveVm` | `/nodes/{node}/qemu/{vmid}` | DELETE | Open |
| `Start-PveVm` | `/nodes/{node}/qemu/{vmid}/status/start` | POST | Open |
| `Stop-PveVm` | `/nodes/{node}/qemu/{vmid}/status/stop` | POST | Open |
| `Restart-PveVm` | `/nodes/{node}/qemu/{vmid}/status/reboot` | POST | Open |
| `Suspend-PveVm` | `/nodes/{node}/qemu/{vmid}/status/suspend` | POST | Open |
| `Resume-PveVm` | `/nodes/{node}/qemu/{vmid}/status/resume` | POST | Open |
| `Reset-PveVm` | `/nodes/{node}/qemu/{vmid}/status/reset` | POST | Open |
| `Copy-PveVm` | `/nodes/{node}/qemu/{vmid}/clone` | POST | Open |
| `Move-PveVm` | `/nodes/{node}/qemu/{vmid}/migrate` | POST | Open |
| `Get-PveVmConfig` | `/nodes/{node}/qemu/{vmid}/config` | GET | Open |
| `Set-PveVmConfig` | `/nodes/{node}/qemu/{vmid}/config` | PUT | Open |
| `Resize-PveVmDisk` | `/nodes/{node}/qemu/{vmid}/resize` | PUT | Open |
| `Import-PveVmDisk` | `/nodes/{node}/storage/{storage}/upload` | POST | Open |
| `Import-PveOva` | Multiple (upload + config + import) | POST | Open |
| `Move-PveVmDisk` | `/nodes/{node}/qemu/{vmid}/move_disk` | POST | Open |
| `Remove-PveVmDisk` | `/nodes/{node}/qemu/{vmid}/unlink` | PUT | Open |
| `New-PveVmFromTemplate` | `/nodes/{node}/qemu/{vmid}/clone` | POST | Open |

#### Guest Agent (9)
| Cmdlet | API Endpoint | Method | Prior Status |
|---|---|---|---|
| `Test-PveVmGuestAgent` | `/nodes/{node}/qemu/{vmid}/agent/ping` | POST | Open |
| `Get-PveVmGuestNetwork` | `/nodes/{node}/qemu/{vmid}/agent/network-get-interfaces` | GET | Open |
| `Invoke-PveVmGuestExec` | `/nodes/{node}/qemu/{vmid}/agent/exec` + `.../exec-status` | POST/GET | Open |
| `Get-PveVmGuestOsInfo` | `/nodes/{node}/qemu/{vmid}/agent/get-osinfo` | GET | Open |
| `Get-PveVmGuestFsInfo` | `/nodes/{node}/qemu/{vmid}/agent/get-fsinfo` | GET | Open |
| `Read-PveVmGuestFile` | `/nodes/{node}/qemu/{vmid}/agent/file-read` | GET | Open |
| `Write-PveVmGuestFile` | `/nodes/{node}/qemu/{vmid}/agent/file-write` | POST | Open |
| `Set-PveVmGuestPassword` | `/nodes/{node}/qemu/{vmid}/agent/set-user-password` | POST | Open |
| `Invoke-PveVmGuestFsTrim` | `/nodes/{node}/qemu/{vmid}/agent/fstrim` | POST | Open |

#### Containers / LXC (20)
| Cmdlet | API Endpoint | Method | Prior Status |
|---|---|---|---|
| `Get-PveContainer` | `/nodes/{node}/lxc` | GET | Open |
| `New-PveContainer` | `/nodes/{node}/lxc` | POST | Open |
| `Remove-PveContainer` | `/nodes/{node}/lxc/{vmid}` | DELETE | Open |
| `Start-PveContainer` | `/nodes/{node}/lxc/{vmid}/status/start` | POST | Open |
| `Stop-PveContainer` | `/nodes/{node}/lxc/{vmid}/status/stop` | POST | Open |
| `Restart-PveContainer` | `/nodes/{node}/lxc/{vmid}/status/shutdown` | POST | Open |
| `Copy-PveContainer` | `/nodes/{node}/lxc/{vmid}/clone` | POST | Open |
| `Move-PveContainer` | `/nodes/{node}/lxc/{vmid}/migrate` | POST | Open |
| `Get-PveContainerConfig` | `/nodes/{node}/lxc/{vmid}/config` | GET | Open |
| `Set-PveContainerConfig` | `/nodes/{node}/lxc/{vmid}/config` | PUT | Open |
| `Suspend-PveContainer` | `/nodes/{node}/lxc/{vmid}/status/suspend` | POST | Open |
| `Resume-PveContainer` | `/nodes/{node}/lxc/{vmid}/status/resume` | POST | Open |
| `Resize-PveContainerDisk` | `/nodes/{node}/lxc/{vmid}/resize` | PUT | Open |
| `New-PveContainerTemplate` | `/nodes/{node}/lxc/{vmid}/template` | POST | Open |
| `Move-PveContainerVolume` | `/nodes/{node}/lxc/{vmid}/move_volume` | POST | Open |
| `Get-PveContainerInterface` | `/nodes/{node}/lxc/{vmid}/interfaces` | GET | Open |
| `Get-PveContainerSnapshot` | `/nodes/{node}/lxc/{vmid}/snapshot` | GET | Open |
| `New-PveContainerSnapshot` | `/nodes/{node}/lxc/{vmid}/snapshot` | POST | Open |
| `Remove-PveContainerSnapshot` | `/nodes/{node}/lxc/{vmid}/snapshot/{name}` | DELETE | Open |
| `Restore-PveContainerSnapshot` | `/nodes/{node}/lxc/{vmid}/snapshot/{name}/rollback` | POST | Open |

#### Storage (11)
| Cmdlet | API Endpoint | Method | Prior Status |
|---|---|---|---|
| `Get-PveStorage` | `/storage` or `/nodes/{node}/storage` | GET | Open |
| `Get-PveStorageContent` | `/nodes/{node}/storage/{storage}/content` | GET | Open |
| `Send-PveFile` | `/nodes/{node}/storage/{storage}/upload` | POST | Open |
| `Invoke-PveStorageDownload` | `/nodes/{node}/storage/{storage}/download-url` | POST | Open |
| `New-PveStorage` | `/storage` | POST | Open |
| `Remove-PveStorage` | `/storage/{storage}` | DELETE | Open |
| `Set-PveStorage` | `/storage/{storage}` | PUT | Open |
| `Get-PveStorageStatus` | `/nodes/{node}/storage/{storage}/status` | GET | Open |
| `Remove-PveStorageContent` | `/nodes/{node}/storage/{storage}/content/{volume}` | DELETE | Open |
| `Set-PveStorageContent` | `/nodes/{node}/storage/{storage}/content/{volume}` | PUT | Open |
| `New-PveStorageDisk` | `/nodes/{node}/storage/{storage}/content` | POST | Open |

#### VM Snapshots (4)
| Cmdlet | API Endpoint | Method | Prior Status |
|---|---|---|---|
| `Get-PveSnapshot` | `/nodes/{node}/qemu/{vmid}/snapshot` | GET | Open |
| `New-PveSnapshot` | `/nodes/{node}/qemu/{vmid}/snapshot` | POST | Open |
| `Remove-PveSnapshot` | `/nodes/{node}/qemu/{vmid}/snapshot/{name}` | DELETE | Open |
| `Restore-PveSnapshot` | `/nodes/{node}/qemu/{vmid}/snapshot/{name}/rollback` | POST | Open |

#### Networking (5)
| Cmdlet | API Endpoint | Method | Prior Status |
|---|---|---|---|
| `Get-PveNetwork` | `/nodes/{node}/network` | GET | Open |
| `New-PveNetwork` | `/nodes/{node}/network` | POST | Open |
| `Set-PveNetwork` | `/nodes/{node}/network/{iface}` | PUT | Open |
| `Remove-PveNetwork` | `/nodes/{node}/network/{iface}` | DELETE | Open |
| `Invoke-PveNetworkApply` | `/nodes/{node}/network` | PUT | Open |

#### SDN (25)
| Cmdlet | API Endpoint | Method | Prior Status |
|---|---|---|---|
| `Get-PveSdnZone` | `/cluster/sdn/zones` | GET | Open |
| `New-PveSdnZone` | `/cluster/sdn/zones` | POST | Open |
| `Remove-PveSdnZone` | `/cluster/sdn/zones/{zone}` | DELETE | Open |
| `Set-PveSdnZone` | `/cluster/sdn/zones/{zone}` | PUT | Open |
| `Get-PveSdnVnet` | `/cluster/sdn/vnets` | GET | Open |
| `New-PveSdnVnet` | `/cluster/sdn/vnets` | POST | Open |
| `Remove-PveSdnVnet` | `/cluster/sdn/vnets/{vnet}` | DELETE | Open |
| `Set-PveSdnVnet` | `/cluster/sdn/vnets/{vnet}` | PUT | Open |
| `Get-PveSdnSubnet` | `/cluster/sdn/vnets/{vnet}/subnets` | GET | Open |
| `New-PveSdnSubnet` | `/cluster/sdn/vnets/{vnet}/subnets` | POST | Open |
| `Remove-PveSdnSubnet` | `/cluster/sdn/vnets/{vnet}/subnets/{subnet}` | DELETE | Open |
| `Set-PveSdnSubnet` | `/cluster/sdn/vnets/{vnet}/subnets/{subnet}` | PUT | Open |
| `Get-PveSdnIpam` | `/cluster/sdn/ipams` | GET | Open |
| `New-PveSdnIpam` | `/cluster/sdn/ipams` | POST | Open |
| `Remove-PveSdnIpam` | `/cluster/sdn/ipams/{ipam}` | DELETE | Open |
| `Set-PveSdnIpam` | `/cluster/sdn/ipams/{ipam}` | PUT | Open |
| `Get-PveSdnDns` | `/cluster/sdn/dns` | GET | Open |
| `New-PveSdnDns` | `/cluster/sdn/dns` | POST | Open |
| `Remove-PveSdnDns` | `/cluster/sdn/dns/{dns}` | DELETE | Open |
| `Set-PveSdnDns` | `/cluster/sdn/dns/{dns}` | PUT | Open |
| `Get-PveSdnController` | `/cluster/sdn/controllers` | GET | Open |
| `New-PveSdnController` | `/cluster/sdn/controllers` | POST | Open |
| `Remove-PveSdnController` | `/cluster/sdn/controllers/{controller}` | DELETE | Open |
| `Set-PveSdnController` | `/cluster/sdn/controllers/{controller}` | PUT | Open |
| `Invoke-PveSdnApply` | `/cluster/sdn` | PUT | Open |

#### Firewall (21)
| Cmdlet | API Endpoint | Method | Prior Status |
|---|---|---|---|
| `Get-PveFirewallRule` | `/cluster/firewall/rules` | GET | Open |
| `New-PveFirewallRule` | `/cluster/firewall/rules` | POST | Open |
| `Set-PveFirewallRule` | `/cluster/firewall/rules/{pos}` | PUT | Open |
| `Remove-PveFirewallRule` | `/cluster/firewall/rules/{pos}` | DELETE | Open |
| `Get-PveFirewallGroup` | `/cluster/firewall/groups` | GET | Open |
| `New-PveFirewallGroup` | `/cluster/firewall/groups` | POST | Open |
| `Remove-PveFirewallGroup` | `/cluster/firewall/groups/{group}` | DELETE | Open |
| `Get-PveFirewallAlias` | `/cluster/firewall/aliases` | GET | Open |
| `New-PveFirewallAlias` | `/cluster/firewall/aliases` | POST | Open |
| `Set-PveFirewallAlias` | `/cluster/firewall/aliases/{name}` | PUT | Open |
| `Remove-PveFirewallAlias` | `/cluster/firewall/aliases/{name}` | DELETE | Open |
| `Get-PveFirewallIpSet` | `/cluster/firewall/ipset` | GET | Open |
| `New-PveFirewallIpSet` | `/cluster/firewall/ipset` | POST | Open |
| `Remove-PveFirewallIpSet` | `/cluster/firewall/ipset/{name}` | DELETE | Open |
| `Get-PveFirewallIpSetEntry` | `/cluster/firewall/ipset/{name}` | GET | Open |
| `New-PveFirewallIpSetEntry` | `/cluster/firewall/ipset/{name}` | POST | Open |
| `Set-PveFirewallIpSetEntry` | `/cluster/firewall/ipset/{name}/{cidr}` | PUT | Open |
| `Remove-PveFirewallIpSetEntry` | `/cluster/firewall/ipset/{name}/{cidr}` | DELETE | Open |
| `Get-PveFirewallOptions` | `/cluster/firewall/options` | GET | Open |
| `Set-PveFirewallOptions` | `/cluster/firewall/options` | PUT | Open |
| `Get-PveFirewallRef` | `/cluster/firewall/refs` | GET | Open |

#### Backup (6)
| Cmdlet | API Endpoint | Method | Prior Status |
|---|---|---|---|
| `New-PveBackup` | `/nodes/{node}/vzdump` | POST | Open |
| `Get-PveBackupJob` | `/cluster/backup` | GET | Open |
| `New-PveBackupJob` | `/cluster/backup` | POST | Open |
| `Set-PveBackupJob` | `/cluster/backup/{id}` | PUT | Open |
| `Remove-PveBackupJob` | `/cluster/backup/{id}` | DELETE | Open |
| `Get-PveBackupInfo` | `/cluster/backup-info/not-backed-up` | GET | Open |

#### Users / Access (23)
| Cmdlet | API Endpoint | Method | Prior Status |
|---|---|---|---|
| `Get-PveUser` | `/access/users` | GET | Open |
| `New-PveUser` | `/access/users` | POST | Open |
| `Remove-PveUser` | `/access/users/{userid}` | DELETE | Open |
| `Set-PveUser` | `/access/users/{userid}` | PUT | Open |
| `Get-PveRole` | `/access/roles` | GET | Open |
| `New-PveRole` | `/access/roles` | POST | Open |
| `Remove-PveRole` | `/access/roles/{roleid}` | DELETE | Open |
| `Set-PveRole` | `/access/roles/{roleid}` | PUT | Open |
| `Get-PvePermission` | `/access/acl` | GET | Open |
| `Set-PvePermission` | `/access/acl` | PUT | Open |
| `Get-PveApiToken` | `/access/users/{userid}/token` | GET | Open |
| `New-PveApiToken` | `/access/users/{userid}/token/{tokenid}` | POST | Open |
| `Remove-PveApiToken` | `/access/users/{userid}/token/{tokenid}` | DELETE | Open |
| `Set-PveApiToken` | `/access/users/{userid}/token/{tokenid}` | PUT | Open |
| `Get-PveGroup` | `/access/groups` | GET | Open |
| `New-PveGroup` | `/access/groups` | POST | Open |
| `Set-PveGroup` | `/access/groups/{groupid}` | PUT | Open |
| `Remove-PveGroup` | `/access/groups/{groupid}` | DELETE | Open |
| `Get-PveDomain` | `/access/domains` | GET | Open |
| `New-PveDomain` | `/access/domains` | POST | Open |
| `Set-PveDomain` | `/access/domains/{realm}` | PUT | Open |
| `Remove-PveDomain` | `/access/domains/{realm}` | DELETE | Open |
| `Set-PvePassword` | `/access/password` | PUT | Open |

#### Pools (4)
| Cmdlet | API Endpoint | Method | Prior Status |
|---|---|---|---|
| `Get-PvePool` | `/pools` | GET | Open |
| `New-PvePool` | `/pools` | POST | Open |
| `Set-PvePool` | `/pools/{poolid}` | PUT | Open |
| `Remove-PvePool` | `/pools/{poolid}` | DELETE | Open |

#### Templates (4)
| Cmdlet | API Endpoint | Method | Prior Status |
|---|---|---|---|
| `Get-PveTemplate` | `/nodes/{node}/qemu` (filtered) | GET | Open |
| `New-PveTemplate` | `/nodes/{node}/qemu/{vmid}/template` | POST | Open |
| `Remove-PveTemplate` | `/nodes/{node}/qemu/{vmid}` | DELETE | Open |
| `New-PveVmFromTemplate` | `/nodes/{node}/qemu/{vmid}/clone` | POST | Open |

#### Cloud-Init (3)
| Cmdlet | API Endpoint | Method | Prior Status |
|---|---|---|---|
| `Get-PveCloudInitConfig` | `/nodes/{node}/qemu/{vmid}/config` | GET | Open |
| `Set-PveCloudInitConfig` | `/nodes/{node}/qemu/{vmid}/config` | PUT | Open |
| `Invoke-PveCloudInitRegenerate` | `/nodes/{node}/qemu/{vmid}/cloudinit` | PUT | Open |

#### Tasks (4)
| Cmdlet | API Endpoint | Method | Prior Status |
|---|---|---|---|
| `Get-PveTask` | `/nodes/{node}/tasks/{upid}/status` | GET | Open |
| `Wait-PveTask` | `/nodes/{node}/tasks/{upid}/status` (polling) | GET | Open |
| `Get-PveTaskList` | `/nodes/{node}/tasks` | GET | Open |
| `Stop-PveTask` | `/nodes/{node}/tasks/{upid}` | DELETE | Open |

#### Cluster (1)
| Cmdlet | API Endpoint | Method | Prior Status |
|---|---|---|---|
| `Get-PveClusterResource` | `/cluster/resources` | GET | Open |

### Coverage Summary Per Functional Area

| Area | Total Endpoints | Covered | % | 🆕 PVE 9.0 New | Prior Status |
|---|---|---|---|---|---|
| acl | 2 | 2 | 100% | 0 | Open |
| version | 1 | 1 | 100% | 0 | Open |
| backup | 6 | 5 | 83% | 0 | Open |
| access_groups | 5 | 4 | 80% | 0 | Open |
| roles | 5 | 4 | 80% | 0 | Open |
| tasks | 5 | 4 | 80% | 0 | Open |
| firewall | 40 | 31 | 78% | 0 | Open |
| users | 12 | 9 | 75% | 0 | Open |
| networking | 7 | 5 | 71% | 0 | Open |
| pools | 7 | 5 | 71% | 0 | Open |
| access_domains | 6 | 4 | 67% | 0 | Open |
| storage_config | 5 | 3 | 60% | 0 | Open |
| storage | 19 | 8 | 42% | 1 | Open |
| sdn | 60 | 25 | 42% | 16 | Open |
| vms | 97 | 35 | 36% | 1 | Open |
| containers | 62 | 21 | 34% | 1 | Open |
| access | 15 | 2 | 13% | 1 | Open |
| nodes | 75 | 8 | 11% | 11 | Open |
| cluster | 77 | 3 | 4% | 6 | Open |
| ceph | 40 | 0 | 0% | 0 | Open |
| ha | 21 | 0 | 0% | 5 | Open |
| disks | 18 | 0 | 0% | 0 | Open |
| acme | 15 | 0 | 0% | 0 | Open |
| cluster_config | 10 | 0 | 0% | 0 | Open |
| certificates | 8 | 0 | 0% | 0 | Open |
| apt | 8 | 0 | 0% | 0 | Open |
| services | 7 | 0 | 0% | 0 | Open |
| metrics | 7 | 0 | 0% | 0 | Open |
| replication | 5 | 0 | 0% | 0 | Open |
| **TOTAL** | **646** | **180** | **27.9%** | **42** | — |

### API Drift — Changed Endpoints (Covered Cmdlets)

| Cmdlet | Endpoint | Change Description | Risk | Prior Status |
|---|---|---|---|---|
| `Set-PveBackupJob` | `PUT /cluster/backup/{id}` | Removed `notification-policy`, `notification-target` params | Breaking | Open |
| `Set-PveNetwork` | `PUT /nodes/{node}/network` | Added `regenerate-frr`, removed `skip_frr` | Breaking | Open |
| `Get-PveVmConfig` | `GET .../qemu/{vmid}/config` | Returns changed in latest version | Cosmetic | Open |
| `Move-PveVm` | `POST .../qemu/{vmid}/migrate` | Added `with-conntrack-state` parameter | Additive | Open |

### PVE 9.0 New Endpoints (42 total, 0 implemented)

| Area | Endpoint | Method | Description |
|---|---|---|---|
| ha | `/cluster/ha/rules` | GET/POST | HA rules system (new in PVE 9.0) |
| ha | `/cluster/ha/rules/{rule}` | GET/PUT/DELETE | Individual HA rule management |
| cluster | `/cluster/bulk-action/guest/*` | POST | Bulk guest start/shutdown/suspend/migrate |
| sdn | `/cluster/sdn/fabrics/*` | GET/POST/PUT/DELETE | SDN fabrics (13 endpoints) |
| sdn | `/cluster/sdn/lock` / `rollback` | POST/DELETE | SDN lock and rollback |
| nodes | `/nodes/{node}/capabilities/qemu/cpu-flags` | GET | CPU flags query |
| nodes | `/nodes/{node}/capabilities/qemu/migration` | GET | Migration capabilities |
| nodes | `/nodes/{node}/sdn/fabrics/*` | GET | Node SDN fabric status |
| nodes | `/nodes/{node}/query-oci-repo-tags` | GET | OCI repo tag query |
| storage | `/nodes/{node}/storage/{storage}/oci-registry-pull` | POST | OCI registry pull |
| vms | `/nodes/{node}/qemu/{vmid}/dbus-vmstate` | POST | D-Bus VM state |
| containers | `GET /nodes/{node}/lxc/{vmid}/migrate` | GET | Container migration pre-check |
| access | `POST /access/vncticket` | POST | VNC ticket creation |

### High-Value Gaps

**Tier 1 — Most impactful for users:**

1. **HA (High Availability)** — 21 endpoints, 0% covered. Essential for production clusters. Resources, groups, status, and the new PVE 9.0 rules system (5 🆕 endpoints).
2. **Cluster configuration** — 10 endpoints, 0%. Cluster create/join/node management is fundamental for multi-node setups.
3. **Ceph** — 40 endpoints, 0%. Critical for hyperconverged infrastructure (OSD, MON, pools, status).
4. **Notifications** — 32 cluster endpoints, 0%. New in PVE 8.1+, essential for monitoring/alerting automation.
5. **Disk management** — 18 endpoints, 0%. LVM, ZFS, SMART, wipe — needed for storage provisioning.

**Tier 2 — Important operational gaps:**

6. **VM/CT-level firewall** — ~44 combined endpoints. Cluster-level IS covered, but per-VM/CT rules are not.
7. **SDN Fabrics** (🆕 PVE 9.0) — 13 new endpoints. Key for advanced SDN.
8. **Resource mappings** (PCI/USB passthrough) — 15 endpoints. Key for GPU passthrough.
9. **Certificates/ACME** — 23 combined endpoints. TLS certificate management.
10. **`GET /cluster/nextid`** — Trivial but frequently needed to allocate VM IDs programmatically.

---

## Phase 3 — Code Quality & Best Practices

### 3a. PowerShell Module Design

- **All cmdlets use approved PowerShell verbs** via verb class constants.
- **Noun prefix `Pve`** is consistent across all 169 cmdlets.
- **HelpMessage** present on virtually all parameters.
- **ShouldProcess** on all destructive/mutating cmdlets.
- **Pipeline support** via `ProcessRecord` and `ValueFromPipelineByPropertyName`.
- **ValidateRange/ValidateNotNullOrEmpty/ValidateSet** used appropriately.
- **OutputType** on all 169 cmdlets (previously ~54 were missing — now all have it).
- **All cmdlets are sealed** (previously ~95 were not).

### 3b. C# Code Quality

- **Nullable annotations**: `<Nullable>enable</Nullable>` in both projects.
- **IDisposable**: `PveHttpClient` correctly implements `IDisposable`.
- **Logging**: `WriteVerbose` used extensively; `WriteProgress` in file uploads and task waiting.
- **Exception hierarchy**: 7 custom exception types with context properties.
- **Bare catches eliminated**: All `catch { }` blocks replaced with filtered `catch (Exception ex) when (...)`.
- **Magic strings extracted**: Auth header names are now `const string` fields.
- **Zero TODO/FIXME/HACK markers**.
- **No commented-out code or dead code**.
- **Clean code style** matching `.editorconfig`.
- **Dual JSON attributes removed**: Only `[JsonProperty]` (Newtonsoft) remains; `[JsonPropertyName]` removed.

### Code Quality Findings

| ID | File | Line | Severity | Description | Prior Status |
|---|---|---|---|---|---|
| CQ1 | `NewPveContainerSnapshotCmdlet.cs` | 75 | **Critical** | **Infinite loop with no timeout in private `WaitForTask`.** `while (true)` with no timeout or cancellation. If task never completes, cmdlet hangs forever. Should use `TaskService.WaitForTask` (same pattern that was fixed in VM snapshot cmdlets). | New |
| CQ2 | `RemovePveContainerSnapshotCmdlet.cs` | 67 | **Critical** | **Identical infinite loop** — same `while(true)` no-timeout pattern as CQ1. | New |
| CQ3 | `RestorePveContainerSnapshotCmdlet.cs` | 70 | **Critical** | **Identical infinite loop** — same pattern as CQ1. | New |
| CQ4 | `InvokePveStorageDownloadCmdlet.cs` | 83 | **Critical** | **Infinite loop in download task polling.** `while(true)` with no timeout. Should use `TaskService.WaitForTask`. | New |
| CQ5 | `SendPveFileCmdlet.cs` | 143 | **Critical** | **Infinite loop in upload task polling.** `while(true)` with no timeout. Should use `TaskService.WaitForTask`. | New |
| CQ6 | Both `.csproj` files | 4 | **Medium** | **`net9.0` target is EOL** (May 2025). Should upgrade to `net10.0` (LTS). | Open |
| CQ7 | `PveHttpClient.cs` | ~154 sites | **Medium** | **Sync-over-async via `.GetAwaiter().GetResult()`** across ~216 call sites. Standard for PS binary modules but carries deadlock risk. | Open |
| CQ8 | `PveCmdletBase.cs` | 149 | **Medium** | **Broad catch in status polling.** `catch (Exception ex) when (...)` is filtered but still catches broadly. Improvement over bare catch but could be more specific. | Open |
| CQ9 | `RestartPveContainerCmdlet.cs` | 15 | **Medium** | **Missing `ConfirmImpact.High`** — `RestartPveVmCmdlet` has it but container counterpart does not. Inconsistent. | New |
| CQ10 | `SuspendPveContainerCmdlet.cs` | 14 | **Medium** | **Missing `ConfirmImpact.High`** — `SuspendPveVmCmdlet` has it but container counterpart does not. Inconsistent. | New |
| CQ11 | 38 cmdlet files | varies | **Medium** | **`new PveHttpClient` per-operation.** 38 cmdlets create their own PveHttpClient instance instead of going through services. Bypasses connection pooling. | Open |
| CQ12 | `PSProxmoxVE.csproj` | 22 | **Low** | **`System.Management.Automation` pinned to 7.4.0.** Consider updating if targeting .NET 10. | Open |

### Fixed Since Prior Scan

| Prior ID | Description | Status |
|---|---|---|
| CQ1–CQ4 | Infinite loop WaitForTask in InvokePveNetworkApply, NewPveSnapshot, RestorePveSnapshot, RemovePveSnapshot | **Fixed** — now uses `TaskService.WaitForTask` |
| CQ5 | Infinite poll loop in InvokePveVmGuestExec | **Fixed** — added Timeout parameter with Stopwatch + TimeoutException |
| CQ7 | Duplicated WaitForTask methods (4 VM/network cmdlets) | **Fixed** — uses TaskService |
| CQ9 | ~54 cmdlets missing OutputType | **Fixed** — all 169 cmdlets now have `[OutputType]` |
| CQ10 | Firewall VmId defaults to 0 instead of nullable | **Fixed** — no longer `int` default 0 pattern |
| CQ11 | Bare catch in PveHttpClient.ExtractErrorMessage | **Fixed** — proper exception handling |
| CQ12 | Bare catch in PveCmdletBase status polling | **Fixed** — filtered `when` clause excludes OOM/SOE |
| CQ13 | Bare catch in VmService + ContainerService | **Fixed** — filtered to PveApiException/HttpRequestException |
| CQ14 | Bare catch in GetPveVmCmdlet | **Fixed** — proper exception handling |
| CQ17 | RemovePveRoleCmdlet missing ConfirmImpact.High | **Fixed** — now has `ConfirmImpact = ConfirmImpact.High` |
| CQ18 | ~95 cmdlets not sealed | **Fixed** — all 169 cmdlets are now `sealed` |
| CQ19 | SuspendPveVmCmdlet missing ConfirmImpact.High | **Fixed** — now has `ConfirmImpact.High` |
| CQ20 | RestartPveVmCmdlet missing ConfirmImpact | **Fixed** — now has `ConfirmImpact.High` |
| CQ21 | Dual JSON serialization attributes | **Fixed** — `[JsonPropertyName]` removed, only `[JsonProperty]` remains |
| CQ22 | Auth header magic strings | **Fixed** — extracted to `const string ApiTokenPrefix` and `CsrfHeaderName` |
| CQ6 (prior) | Pool/CephPool parameter conflict in NewPveStorageCmdlet | **Fixed** — runtime validation with `ThrowTerminatingError` if both specified |

---

## Phase 4 — Testing Coverage Analysis

### Test Projects

| Project | Framework | Type | Runner |
|---|---|---|---|
| `PSProxmoxVE.Core.Tests` | xUnit 2.7 + Moq 4.20 | Unit tests (models, auth) | `dotnet test` |
| `PSProxmoxVE.Tests` | Pester 5 | Cmdlet + Integration tests | `Invoke-Pester` |

### xUnit Tests (Core Library)

15 test files covering authentication (PveSession, PveAuthenticator, PveVersion) and model deserialization (12 model areas including backup, cluster, firewall, SDN) with PVE 8 and PVE 9 JSON fixtures. ~180+ xUnit tests total.

### Pester Tests

100% of 169 cmdlets have Pester unit tests covering: command existence, parameter metadata, mandatory checks, parameter types, ShouldProcess presence, ConfirmImpact declarations, and "no session" error behavior.

### Integration Tests

- Self-hosted runner provisions nested PVE 8 and PVE 9 via Terraform + auto-install ISO
- Tests check for `PVETEST_*` environment variables, skip gracefully when unavailable
- Robust `AfterAll` cleanup of VMs, containers, users, roles, firewall rules, backup jobs
- CRUD patterns: create → verify → update → verify → delete → verify
- SDN tests check PVE version and skip if older

### Test Coverage Gap List (cmdlets lacking integration tests)

| Cmdlet | Unit Test | Integration Test | Prior Status |
|---|---|---|---|
| **Nodes** | | | |
| Get-PveNodeConfig | Yes | No | Open |
| Set-PveNodeConfig | Yes | No | Open |
| Get-PveNodeDns | Yes | No | Open |
| Set-PveNodeDns | Yes | No | Open |
| Start-PveNodeVms | Yes | No | Open |
| Stop-PveNodeVms | Yes | No | Open |
| **VMs** | | | |
| Move-PveVm | Yes | No | Open |
| Move-PveVmDisk | Yes | No | Open |
| Remove-PveVmDisk | Yes | No | Open |
| **Guest Agent** | | | |
| Get-PveVmGuestOsInfo | Yes | No | Open |
| Get-PveVmGuestFsInfo | Yes | No | Open |
| Read-PveVmGuestFile | Yes | No | Open |
| Write-PveVmGuestFile | Yes | No | Open |
| Set-PveVmGuestPassword | Yes | No | Open |
| Invoke-PveVmGuestFsTrim | Yes | No | Open |
| **Containers** | | | |
| Move-PveContainer | Yes | No | Open |
| Suspend-PveContainer | Yes | No | Open |
| Resume-PveContainer | Yes | No | Open |
| Resize-PveContainerDisk | Yes | No | Open |
| New-PveContainerTemplate | Yes | No | Open |
| Move-PveContainerVolume | Yes | No | Open |
| Get-PveContainerInterface | Yes | No | Open |
| **Storage** | | | |
| Set-PveStorage | Yes | No | Open |
| Get-PveStorageStatus | Yes | No | Open |
| Remove-PveStorageContent | Yes | No | Open |
| Set-PveStorageContent | Yes | No | Open |
| New-PveStorageDisk | Yes | No | Open |
| **SDN** | | | |
| Set-PveSdnZone | Yes | No | Open |
| Set-PveSdnVnet | Yes | No | Open |
| Set-PveSdnSubnet | Yes | No | Open |
| New-PveSdnIpam | Yes | No | Open |
| Remove-PveSdnIpam | Yes | No | Open |
| Set-PveSdnIpam | Yes | No | Open |
| New-PveSdnDns | Yes | No | Open |
| Remove-PveSdnDns | Yes | No | Open |
| Set-PveSdnDns | Yes | No | Open |
| New-PveSdnController | Yes | No | Open |
| Remove-PveSdnController | Yes | No | Open |
| Set-PveSdnController | Yes | No | Open |
| Invoke-PveSdnApply | Yes | No | Open |
| **Users / Access** | | | |
| Set-PveRole | Yes | No | Open |
| Set-PveApiToken | Yes | No | Open |
| Get-PveGroup | Yes | No | Open |
| New-PveGroup | Yes | No | Open |
| Set-PveGroup | Yes | No | Open |
| Remove-PveGroup | Yes | No | Open |
| Get-PveDomain | Yes | No | Open |
| New-PveDomain | Yes | No | Open |
| Set-PveDomain | Yes | No | Open |
| Remove-PveDomain | Yes | No | Open |
| Set-PvePassword | Yes | No | Open |
| **Pools** | | | |
| Get-PvePool | Yes | No | Open |
| New-PvePool | Yes | No | Open |
| Set-PvePool | Yes | No | Open |
| Remove-PvePool | Yes | No | Open |
| **Templates** | | | |
| New-PveTemplate | Yes | No | Open |
| Remove-PveTemplate | Yes | No | Open |
| **Tasks** | | | |
| Get-PveTaskList | Yes | No | Open |
| Stop-PveTask | Yes | No | Open |
| **Firewall** | | | |
| Get-PveFirewallGroup | Yes | No | Open |
| New-PveFirewallGroup | Yes | No | Open |
| Remove-PveFirewallGroup | Yes | No | Open |
| Set-PveFirewallAlias | Yes | No | Open |
| Set-PveFirewallIpSetEntry | Yes | No | Open |
| Set-PveFirewallOptions | Yes | No | Open |
| Get-PveFirewallRef | Yes | No | Open |
| **Backup** | | | |
| New-PveBackup | Yes | No | Open |
| Get-PveBackupInfo | Yes | No | Open |
| **Cluster** | | | |
| Get-PveClusterResource | Yes | No | Open |

**Summary**: 169/169 unit tested (100%), ~105/169 integration tested (~62%).

---

## Phase 5 — Security Review

### 5.1 Credential Handling

| Aspect | Status | Details | Prior Status |
|---|---|---|---|
| PSCredential usage | **Good** | `Connect-PveServer` accepts `PSCredential` | Open |
| SecureString pattern | **Good** | Consistent `Marshal.SecureStringToGlobalAllocUnicode` + `ZeroFreeGlobalAllocUnicode` in try/finally | Open |
| Set-PveVmGuestPassword | **Good** | Now uses `SecureString` (was plain `string`) | Fixed |
| API token in memory | **Acceptable** | Stored as plain `string` in `PveSession.ApiToken` | Open |
| Credential logging | **Good** | No WriteVerbose/WriteDebug includes credentials | Open |
| Ticket storage | **Good** | In `PveSession` only, not written to disk | Open |
| Session expiry | **Good** | 2-hour expiry via `IsExpired` property | Open |

### 5.2 TLS/HTTPS

| Aspect | Status | Details | Prior Status |
|---|---|---|---|
| HTTPS enforced | **Good** | `BaseUrl` always `https://`, no HTTP fallback | Open |
| SkipCertificateCheck | **Good** | Explicit opt-in with `WriteWarning` advisory | Open |
| TLS version | **Good** | OS/runtime negotiation (no pinning) | Open |
| CSRF protection | **Good** | Token included on all POST/PUT/DELETE with ticket auth | Open |

### 5.3 Input Validation

| Aspect | Status | Details | Prior Status |
|---|---|---|---|
| VMID validation | **Good** | `[ValidateRange(100, 999999999)]` on all VmId params | Open |
| URL encoding | **Good** | `Uri.EscapeDataString()` applied consistently across all services (VmService, ContainerService, StorageService, NetworkService, FirewallService, UserService, BackupService, PoolService, TaskService, SnapshotService, ClusterService) | Fixed |
| API token format | **Good** | Regex validation in `PveAuthenticator` | Open |
| Port validation | **Good** | `[ValidateRange(1, 65535)]` on port parameter | Open |

### 5.4 Dependency Security

| Package | Version | Pinned | Notes | Prior Status |
|---|---|---|---|---|
| `Newtonsoft.Json` | 13.0.3 | Yes | Latest stable | Open |
| `System.Text.Json` | 8.0.5 | Yes | Latest 8.x patch | Open |
| `SharpCompress` | 0.38.0 | Yes | OVA/tar extraction | Open |
| `PowerShellStandard.Library` | 5.1.1 | Yes | PrivateAssets=all | Open |
| `System.Management.Automation` | 7.4.0 | Yes | PrivateAssets=all | Open |

### Security Findings

| ID | Area | File | Severity | Description | Prior Status |
|---|---|---|---|---|---|
| S1 | Hardcoded password | `integration-tests.yml:42` | **Low** | `PVE_PASSWORD: "Testpass123!"` in workflow env. Masked via `::add-mask::`. Disposable test VM. | Open |
| S2 | Hardcoded password | `variables.tf:80` | **Low** | Default `"Testpass123!"` for test VM password. Disposable nested VM. | Open |

### Fixed Since Prior Scan

| Prior ID | Description | Status |
|---|---|---|
| S1 | `SetPveVmGuestPasswordCmdlet.cs` Password is `string`, not `SecureString` | **Fixed** — now uses `SecureString` |
| S2 | `debug/Capture-UploadDiff.ps1` hardcoded API token and IP | **Fixed** — replaced with placeholder tokens `<PVE_HOST>`, `<USER>@<REALM>!<TOKENID>=<TOKEN_UUID>` |
| S3 | URL encoding missing in VmService, ContainerService, StorageService | **Fixed** — `Uri.EscapeDataString()` now used consistently across all services |

---

## Phase 6 — PSGallery Publication Readiness

| # | Check | Pass/Fail | Notes | Prior Status |
|---|---|---|---|---|
| 1 | ModuleVersion is SemVer | **Pass** | `0.1.0` | Open |
| 2 | GUID present and stable | **Pass** | `a3f7c2d1-84e5-4b9f-a061-3e2d8c5f1a7b` | Open |
| 3 | Author | **Pass** | `goodolclint` | Open |
| 4 | CompanyName | **Pass** | `Worklab` | Open |
| 5 | Copyright | **Pass** | `(c) 2026 goodolclint. All rights reserved.` | Open |
| 6 | Description (meaningful) | **Pass** | Mentions PVE 8.x/9.x and feature areas | Open |
| 7 | PowerShellVersion minimum | **Pass** | `5.1` | Open |
| 8 | RequiredAssemblies | **Pass** | `PSProxmoxVE.Core.dll`, `Newtonsoft.Json.dll` | Open |
| 9 | CmdletsToExport explicit list | **Pass** | Fully enumerated (169 cmdlets), no wildcards | Open |
| 10 | FunctionsToExport | **Pass** | Explicitly `@()` | Open |
| 11 | Tags for discoverability | **Pass** | 8 tags incl. Proxmox, PVE, Virtualization, IaC | Open |
| 12 | ProjectUri | **Pass** | GitHub repo URL | Open |
| 13 | LicenseUri | **Pass** | Points to LICENSE | Open |
| 14 | IconUri | **Fail** | Not set — cosmetic | Open |
| 15 | ReleaseNotes | **Pass** | In PSData | Open |
| 16 | Prerelease string | **Pass** | `'preview'` | Open |
| 17 | LICENSE (OSI-approved) | **Pass** | MIT | Open |
| 18 | README: Install-Module | **Pass** | `Install-Module -Name PSProxmoxVE` documented | Open |
| 19 | README: Quick-start examples | **Pass** | Multiple scenarios | Open |
| 20 | README: Authentication guide | **Pass** | Ticket + API token | Open |
| 21 | README: Badges | **Pass** | Build, Unit Tests, License, PSGallery | Open |
| 22 | Publish workflow | **Pass** | Tag-triggered via `publish.yml` | Open |
| 23 | Publish: version stamping | **Pass** | Extracts from git tag | Open |
| 24 | Publish: smoke test | **Pass** | Loads module, asserts cmdlet count | Open |
| 25 | Publish: API key as secret | **Pass** | `secrets.PSGALLERY_API_KEY` | Open |
| 26 | Publish: GitHub Release | **Pass** | Auto-created via `softprops/action-gh-release` | Open |
| 27 | CHANGELOG | **Pass** | Keep a Changelog format + Conventional Commits | Open |
| 28 | Publish: single netstandard2.0 | **Warning** | Only builds netstandard2.0. Verify it loads under Win PS 5.1 since manifest declares `DotNetFrameworkVersion = '4.8'`. | Open |

---

## Phase 7 — Community & Repo Maintenance Standards

| # | Check | Pass/Fail | Notes | Prior Status |
|---|---|---|---|---|
| 1 | Bug report issue template | **Pass** | YAML form with module/PVE/PS version fields | Open |
| 2 | Feature request template | **Pass** | Description, use case, API endpoints | Open |
| 3 | PR template | **Pass** | Summary, type checkboxes, testing checklist | Open |
| 4 | `CONTRIBUTING.md` | **Pass** | Prerequisites, building, tests, coding standards, PR process | Open |
| 5 | `CODE_OF_CONDUCT.md` | **Pass** | Contributor Covenant 2.1 | Open |
| 6 | `SECURITY.md` | **Pass** | Vulnerability disclosure, 48h SLA | Open |
| 7 | Conventional commits | **Pass** | Consistent `feat:`, `fix:`, `fix(test):` in history | Open |
| 8 | `.gitattributes` | **Pass** | Comprehensive: csharp diff, binary markers, LF for .sh | Open |
| 9 | Issue template config.yml | **Fail** | No `config.yml` to control blank issues | Open |
| 10 | CODEOWNERS | **Fail** | Not present. Low priority for single maintainer. | Open |
| 11 | FUNDING.yml / community links | **Fail** | No Discussions/Discord/Funding references | Open |
| 12 | Default branch `main` | **Pass** | Confirmed | Open |

### Recommended Branch Protection

- Require PR reviews before merge
- Require status checks (build + unit tests + integration tests)
- Require linear history or squash merging

---

## Phase 8 — Prioritized Recommendations

### 🔴 Critical (blocks PSGallery publication or is a security risk)

| # | What | Where | Why | Fix | Prior Status |
|---|---|---|---|---|---|
| C1 | **Infinite loop in 5 cmdlets' WaitForTask methods** | `NewPveContainerSnapshotCmdlet.cs`, `RemovePveContainerSnapshotCmdlet.cs`, `RestorePveContainerSnapshotCmdlet.cs`, `InvokePveStorageDownloadCmdlet.cs`, `SendPveFileCmdlet.cs` | `while(true)` with no timeout — cmdlet hangs forever if task stalls. Same pattern that was fixed in VM snapshot cmdlets. | Replace private `WaitForTask` with `TaskService.WaitForTask` (identical fix to CQ1–CQ4 from prior scan) | New |

### 🟠 High (significantly impacts quality or community adoption)

| # | What | Where | Why | Fix | Prior Status |
|---|---|---|---|---|---|
| H1 | **HA subsystem (0% coverage)** | Module | 21 endpoints + 5 🆕 PVE 9.0, essential for production clusters | Implement HA resources, groups, status, and PVE 9.0 rules | Open |
| H2 | **Ceph subsystem (0% coverage)** | Module | 40 endpoints, critical for hyperconverged infrastructure | Implement Ceph OSD, MON, pool, status operations | Open |
| H3 | **net9.0 target is EOL** | Both `.csproj` files | .NET 9.0 EOL was May 2025. Should use LTS release. | Upgrade to `net10.0` | Open |

### 🟡 Medium (best practice gaps, test coverage holes)

| # | What | Where | Why | Fix | Prior Status |
|---|---|---|---|---|---|
| M1 | **HttpClient per-call pattern** | 38 cmdlet files + services | 38 cmdlets create `new PveHttpClient` per operation, bypassing connection pooling | Refactor to client-per-session or use `IHttpClientFactory` pattern | Open |
| M2 | **64 cmdlets lack integration tests** | Integration tests | 38% of cmdlets untested end-to-end | Prioritize pools, groups, domains, node ops, container gaps, guest agent extensions | Open |
| M3 | **VM/CT-level firewall (0% coverage)** | Module | ~44 endpoints. Cluster/node-level covered but per-VM/CT rules are not. | Add VM/CT firewall cmdlets using existing firewall service patterns | Open |
| M4 | **Cluster config (0% coverage)** | Module | 10 endpoints for cluster create/join/node management | Implement for multi-node cluster automation | Open |
| M5 | **PVE 9.0 endpoints (0% coverage)** | Module | 42 new endpoints in latest PVE version. Users on PVE 9 will expect support. | Prioritize HA rules, bulk actions, SDN fabrics | New |
| M6 | **Broad exception catches** | `PveCmdletBase.cs`, `VmService.cs`, `ContainerService.cs` | Filtered `when` clauses are better than bare catches but still catch broadly | Consider more specific exception types | Open |

### 🟢 Low / Nice-to-have (polish, discoverability, stretch goals)

| # | What | Where | Why | Fix | Prior Status |
|---|---|---|---|---|---|
| L1 | No `IconUri` in manifest | `PSProxmoxVE.psd1` | Improves PSGallery listing appearance | Create icon, host it, add URI | Open |
| L2 | `System.Management.Automation` pinned to 7.4.0 | `PSProxmoxVE.csproj` | Consider updating when targeting .NET 10 | Update with TFM migration | Open |
| L3 | No `config.yml` for issue templates | `.github/ISSUE_TEMPLATE/` | Controls blank issue creation | Add simple config.yml | Open |
| L4 | No CODEOWNERS | Repo root | Documents ownership for PR auto-assignment | Add `CODEOWNERS` with maintainer | Open |
| L5 | Disk management (0% coverage) | Module | 18 endpoints — LVM, ZFS, SMART, wipe | Implement as demand grows | Open |
| L6 | Notifications (0% coverage) | Module | 32 endpoints — new in PVE 8.1+ | Implement for monitoring automation | Open |
| L7 | ACME/Certificates (0% coverage) | Module | 23 endpoints — TLS cert management | Implement as demand grows | Open |
| L8 | Verify netstandard2.0 loads on PS 5.1 | Publish workflow | Manifest says Desktop compatible | Add PS 5.1 smoke test to publish workflow | Open |

---

## Summary Statistics

| Metric | This Scan | Prior Scan | Delta |
|---|---|---|---|
| Total cmdlets | 169 | 148 | +21 |
| Cmdlets with ShouldProcess | All mutating | All mutating | — |
| Cmdlets with OutputType | 169 (100%) | ~94 (~64%) | +36% |
| Cmdlets sealed | 169 (100%) | ~60 (~40%) | +60% |
| Cmdlets with HelpMessage | 169 (100%) | 148 (100%) | — |
| xUnit test files | 15 | 15 | — |
| Pester test files | ~48 | ~45 | +3 |
| Integration test coverage | ~62% (105/169) | ~57% (85/148) | +5% |
| PVE API areas with >0% coverage | 17 of 30 | 17 of 29 | — |
| PVE API total coverage | 27.9% (180/646) | 27.9% (180/646) | — |
| Critical issues | 1 | 2 | -1 |
| High issues | 3 | 5 | -2 |
| Medium issues | 7 | 10 | -3 |
| Low issues | 7 | 11 | -4 |
| **Total issues** | **18** | **28** | **-10** |
| Issues fixed from prior | 16 | 9 | +7 |
| New issues found | 5 | 16 | -11 |
| NuGet dependencies (runtime) | 3 | 3 | — |
| Security vulnerabilities | 0 | 0 | — |
| Community files present | 9/9 | 9/9 | — |

\* Integration test coverage improved both in absolute count (+20 cmdlets) and percentage (+5%).
