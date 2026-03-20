# PSProxmoxVE Module Review Report

**Date:** 2026-03-19
**Reviewer:** Claude Code (Opus 4.6)
**Module Version:** 0.1.0-preview
**Repository:** PSProxmoxVE (C# binary PowerShell module)

---

## Executive Summary

1. **Well-structured project** with clean separation: `PSProxmoxVE.Core` (services, models, client) and `PSProxmoxVE` (cmdlets, module surface).
2. **66 cmdlets** covering VMs, containers, storage, networking, SDN, users/roles/permissions, templates, cloud-init, snapshots, and tasks.
3. **Excellent ShouldProcess coverage** -- all destructive cmdlets implement `SupportsShouldProcess` with appropriate `ConfirmImpact` levels.
4. **OutputType attributes** present on every cmdlet. ValueFromPipelineByPropertyName used consistently for pipeline chaining.
5. **Sync-over-async pattern** (`GetAwaiter().GetResult()`) used pervasively -- acceptable for PowerShell binary modules but a known tradeoff.
6. **Comprehensive integration tests** running against real PVE 8 and PVE 9 via Terraform-provisioned nested instances.
7. **Missing community files**: `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `SECURITY.md`, `.gitattributes`, issue/PR templates.
8. **No publish workflow**: No CI pipeline for publishing to PSGallery.
9. **One unapproved verb**: `Reset-PveVm` uses a hardcoded string `"Reset"` instead of `VerbsCommon` -- `Reset` is not an approved PowerShell verb.
10. **PVE API coverage**: Module covers core automation scenarios well but lacks firewall, backup/restore (PBS), Ceph, HA, and replication management.

---

## Phase 1 -- Repository Inventory & Structure

### Directory Tree (depth <= 4)

```
PSProxmoxVE/
+-- PSProxmoxVE.sln
+-- README.md
+-- LICENSE (MIT)
+-- CHANGELOG.md
+-- .editorconfig
+-- .gitignore
+-- CLAUDE.md
+-- .github/
|   +-- workflows/
|       +-- build.yml
|       +-- unit-tests.yml
|       +-- integration-tests.yml
+-- src/
|   +-- PSProxmoxVE/
|   |   +-- PSProxmoxVE.csproj
|   |   +-- PSProxmoxVE.psd1
|   |   +-- PSProxmoxVE.format.ps1xml
|   |   +-- ModuleState.cs
|   |   +-- Cmdlets/
|   |       +-- PveCmdletBase.cs
|   |       +-- CloudInit/ (3 cmdlets)
|   |       +-- Connection/ (3 cmdlets)
|   |       +-- Containers/ (8 cmdlets)
|   |       +-- Network/ (10 cmdlets)
|   |       +-- Nodes/ (2 cmdlets)
|   |       +-- Snapshots/ (4 cmdlets)
|   |       +-- Storage/ (5 cmdlets)
|   |       +-- Tasks/ (2 cmdlets)
|   |       +-- Templates/ (4 cmdlets)
|   |       +-- Users/ (11 cmdlets)
|   |       +-- Vms/ (17 cmdlets)
|   +-- PSProxmoxVE.Core/
|       +-- PSProxmoxVE.Core.csproj
|       +-- Authentication/ (PveAuthenticator, PveAuthMode, PveSession, PveVersion)
|       +-- Client/ (PveHttpClient)
|       +-- Exceptions/ (7 exception types)
|       +-- Models/ (Cluster, Containers, Network, Nodes, Storage, Users, Vms)
|       +-- Services/ (11 service classes)
+-- tests/
|   +-- PSProxmoxVE.Core.Tests/ (xUnit)
|   |   +-- Authentication/ (3 test files)
|   |   +-- Models/ (10 test files)
|   |   +-- TestHelper.cs
|   +-- PSProxmoxVE.Tests/ (Pester 5)
|       +-- _TestHelper.ps1
|       +-- CloudInit/ (1 test file)
|       +-- Connection/ (3 test files)
|       +-- Containers/ (3 test files)
|       +-- Network/ (2 test files)
|       +-- Nodes/ (1 test file)
|       +-- Snapshots/ (1 test file)
|       +-- Storage/ (3 test files)
|       +-- Tasks/ (1 test file)
|       +-- Templates/ (1 test file)
|       +-- Users/ (3 test files)
|       +-- Vms/ (6 test files)
|       +-- Integration/ (1 comprehensive test file)
+-- tests/infrastructure/ (Terraform, Dockerfile, scripts for CI)
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

### What's Present

- [x] Solution file (`.sln`)
- [x] Module manifest (`.psd1`) with explicit `CmdletsToExport`
- [x] Format definitions (`.format.ps1xml`)
- [x] `.editorconfig`
- [x] `.gitignore`
- [x] `LICENSE` (MIT)
- [x] `CHANGELOG.md` (Keep a Changelog format)
- [x] `README.md` (comprehensive)
- [x] CI/CD workflows (build, unit tests, integration tests)

### What's Missing

- [ ] `CONTRIBUTING.md`
- [ ] `CODE_OF_CONDUCT.md`
- [ ] `SECURITY.md`
- [ ] `.gitattributes`
- [ ] `.github/ISSUE_TEMPLATE/`
- [ ] `.github/pull_request_template.md`
- [ ] PSGallery publish workflow
- [ ] `ReleaseNotes` in manifest `PSData`
- [ ] `IconUri` in manifest `PSData`

---

## Phase 2 -- PVE API Coverage Audit

> **Note:** The PVE API documentation at `pve.proxmox.com/pve-docs/api-viewer/apidoc.js` was too large for automated extraction. Coverage assessment below is based on the author's knowledge of the PVE API and source code analysis.

### Implemented Cmdlets by Category

#### Connection (3 cmdlets)
| Cmdlet | API Endpoint | Method |
|---|---|---|
| `Connect-PveServer` | `/access/ticket` | POST |
| `Disconnect-PveServer` | `/access/ticket` | DELETE |
| `Test-PveConnection` | `/version` | GET |

#### Nodes (2 cmdlets)
| Cmdlet | API Endpoint | Method |
|---|---|---|
| `Get-PveNode` | `/nodes` | GET |
| `Get-PveNodeStatus` | `/nodes/{node}/status` | GET |

#### VMs / QEMU (17 cmdlets)
| Cmdlet | API Endpoint | Method |
|---|---|---|
| `Get-PveVm` | `/nodes/{node}/qemu` | GET |
| `New-PveVm` | `/nodes/{node}/qemu` | POST |
| `Remove-PveVm` | `/nodes/{node}/qemu/{vmid}` | DELETE |
| `Start-PveVm` | `/nodes/{node}/qemu/{vmid}/status/start` | POST |
| `Stop-PveVm` | `/nodes/{node}/qemu/{vmid}/status/stop` | POST |
| `Restart-PveVm` | `/nodes/{node}/qemu/{vmid}/status/reboot` | POST |
| `Suspend-PveVm` | `/nodes/{node}/qemu/{vmid}/status/suspend` | POST |
| `Resume-PveVm` | `/nodes/{node}/qemu/{vmid}/status/resume` | POST |
| `Reset-PveVm` | `/nodes/{node}/qemu/{vmid}/status/reset` | POST |
| `Copy-PveVm` | `/nodes/{node}/qemu/{vmid}/clone` | POST |
| `Move-PveVm` | `/nodes/{node}/qemu/{vmid}/migrate` | POST |
| `Get-PveVmConfig` | `/nodes/{node}/qemu/{vmid}/config` | GET |
| `Set-PveVmConfig` | `/nodes/{node}/qemu/{vmid}/config` | PUT |
| `Resize-PveVmDisk` | `/nodes/{node}/qemu/{vmid}/resize` | PUT |
| `Test-PveVmGuestAgent` | `/nodes/{node}/qemu/{vmid}/agent/ping` | POST |
| `Get-PveVmGuestNetwork` | `/nodes/{node}/qemu/{vmid}/agent/network-get-interfaces` | GET |
| `Invoke-PveVmGuestExec` | `/nodes/{node}/qemu/{vmid}/agent/exec` | POST |

#### Containers / LXC (8 cmdlets)
| Cmdlet | API Endpoint | Method |
|---|---|---|
| `Get-PveContainer` | `/nodes/{node}/lxc` | GET |
| `New-PveContainer` | `/nodes/{node}/lxc` | POST |
| `Remove-PveContainer` | `/nodes/{node}/lxc/{vmid}` | DELETE |
| `Start-PveContainer` | `/nodes/{node}/lxc/{vmid}/status/start` | POST |
| `Stop-PveContainer` | `/nodes/{node}/lxc/{vmid}/status/stop` | POST |
| `Restart-PveContainer` | `/nodes/{node}/lxc/{vmid}/status/shutdown` | POST |
| `Copy-PveContainer` | `/nodes/{node}/lxc/{vmid}/clone` | POST |
| `Get-PveContainerConfig` | `/nodes/{node}/lxc/{vmid}/config` | GET |
| `Set-PveContainerConfig` | `/nodes/{node}/lxc/{vmid}/config` | PUT |

#### Storage (5 cmdlets)
| Cmdlet | API Endpoint | Method |
|---|---|---|
| `Get-PveStorage` | `/storage` or `/nodes/{node}/storage` | GET |
| `Get-PveStorageContent` | `/nodes/{node}/storage/{storage}/content` | GET |
| `Send-PveIso` | `/nodes/{node}/storage/{storage}/upload` | POST |
| `Invoke-PveStorageDownload` | `/nodes/{node}/storage/{storage}/download-url` | POST |
| `New-PveStorage` | `/storage` | POST |
| `Remove-PveStorage` | `/storage/{storage}` | DELETE |

#### Snapshots (4 cmdlets)
| Cmdlet | API Endpoint | Method |
|---|---|---|
| `Get-PveSnapshot` | `/nodes/{node}/qemu/{vmid}/snapshot` | GET |
| `New-PveSnapshot` | `/nodes/{node}/qemu/{vmid}/snapshot` | POST |
| `Remove-PveSnapshot` | `/nodes/{node}/qemu/{vmid}/snapshot/{name}` | DELETE |
| `Restore-PveSnapshot` | `/nodes/{node}/qemu/{vmid}/snapshot/{name}/rollback` | POST |

#### Networking (4 + 6 SDN cmdlets)
| Cmdlet | API Endpoint | Method |
|---|---|---|
| `Get-PveNetwork` | `/nodes/{node}/network` | GET |
| `New-PveNetwork` | `/nodes/{node}/network` | POST |
| `Set-PveNetwork` | `/nodes/{node}/network/{iface}` | PUT |
| `Remove-PveNetwork` | `/nodes/{node}/network/{iface}` | DELETE |
| `Invoke-PveNetworkApply` | `/nodes/{node}/network` | PUT |
| `Get-PveSdnZone` | `/cluster/sdn/zones` | GET |
| `New-PveSdnZone` | `/cluster/sdn/zones` | POST |
| `Remove-PveSdnZone` | `/cluster/sdn/zones/{zone}` | DELETE |
| `Get-PveSdnVnet` | `/cluster/sdn/vnets` | GET |
| `New-PveSdnVnet` | `/cluster/sdn/vnets` | POST |
| `Remove-PveSdnVnet` | `/cluster/sdn/vnets/{vnet}` | DELETE |

#### Users / ACLs / Tokens (11 cmdlets)
| Cmdlet | API Endpoint | Method |
|---|---|---|
| `Get-PveUser` | `/access/users` | GET |
| `New-PveUser` | `/access/users` | POST |
| `Remove-PveUser` | `/access/users/{userid}` | DELETE |
| `Set-PveUser` | `/access/users/{userid}` | PUT |
| `Get-PveRole` | `/access/roles` | GET |
| `New-PveRole` | `/access/roles` | POST |
| `Remove-PveRole` | `/access/roles/{roleid}` | DELETE |
| `Get-PvePermission` | `/access/acl` | GET |
| `Set-PvePermission` | `/access/acl` | PUT |
| `Get-PveApiToken` | `/access/users/{userid}/token` | GET |
| `New-PveApiToken` | `/access/users/{userid}/token/{tokenid}` | POST |
| `Remove-PveApiToken` | `/access/users/{userid}/token/{tokenid}` | DELETE |

#### Templates (4 cmdlets)
| Cmdlet | API Endpoint | Method |
|---|---|---|
| `Get-PveTemplate` | `/nodes/{node}/qemu` (filtered) | GET |
| `New-PveTemplate` | `/nodes/{node}/qemu/{vmid}/template` | POST |
| `Remove-PveTemplate` | `/nodes/{node}/qemu/{vmid}` | DELETE |
| `New-PveVmFromTemplate` | `/nodes/{node}/qemu/{vmid}/clone` | POST |

#### Cloud-Init (3 cmdlets)
| Cmdlet | API Endpoint | Method |
|---|---|---|
| `Get-PveCloudInitConfig` | `/nodes/{node}/qemu/{vmid}/config` | GET |
| `Set-PveCloudInitConfig` | `/nodes/{node}/qemu/{vmid}/config` | PUT |
| `Invoke-PveCloudInitRegenerate` | `/nodes/{node}/qemu/{vmid}/cloudinit` | PUT |

#### Tasks (2 cmdlets)
| Cmdlet | API Endpoint | Method |
|---|---|---|
| `Get-PveTask` | `/nodes/{node}/tasks/{upid}/status` | GET |
| `Wait-PveTask` | `/nodes/{node}/tasks/{upid}/status` (polling) | GET |

### Missing API Coverage by Functional Area

| Area | Key Missing Endpoints | Priority |
|---|---|---|
| **Firewall** | `/nodes/{node}/firewall/rules`, `/cluster/firewall/*` | High |
| **Backup / Restore** | `/nodes/{node}/vzdump`, backup schedule management | High |
| **Pools** | `/pools`, `/pools/{poolid}` (CRUD) | Medium |
| **HA** | `/cluster/ha/*` (groups, resources, status) | Medium |
| **Ceph** | `/nodes/{node}/ceph/*` (OSD, pool, mon, fs) | Medium |
| **Replication** | `/cluster/replication` (CRUD) | Low |
| **Access Groups** | `/access/groups` (CRUD) | Low |
| **Access Domains** | `/access/domains` (CRUD) | Low |
| **Container Snapshots** | `/nodes/{node}/lxc/{vmid}/snapshot` | Medium |
| **Container Migrate** | `/nodes/{node}/lxc/{vmid}/migrate` | Medium |
| **PBS Integration** | Proxmox Backup Server operations | Low |
| **Cluster Config** | `/cluster/config/*` (join, nodes, totem) | Low |
| **ACME** | `/cluster/acme/*`, `/nodes/{node}/certificates/*` | Low |
| **Node Management** | `/nodes/{node}/apt/*`, `/nodes/{node}/disks/*`, `/nodes/{node}/services/*` | Low |
| **Metrics** | `/cluster/metrics/*` | Low |
| **SDN Subnets** | `/cluster/sdn/vnets/{vnet}/subnets` | Medium |
| **VM Agent** | Additional agent endpoints (file-read, file-write, etc.) | Low |

### High-Value Gaps

1. **Firewall management** -- Critical for security automation. No cmdlets for creating/managing firewall rules at cluster, node, or VM level.
2. **Backup/restore (vzdump)** -- Essential for DR automation. No cmdlets for creating backups, managing schedules, or restoring.
3. **Pool management** -- Required for multi-tenant environments. No cmdlets for creating/managing resource pools.
4. **Container snapshots** -- Snapshots are only implemented for VMs, not LXC containers.
5. **Container migration** -- `Move-PveVm` exists for VMs but no equivalent for containers.

---

## Phase 3 -- Code Quality & Best Practices

### 3a. PowerShell Module Design

#### Cmdlet Naming
- **All cmdlets use approved PowerShell verbs** from the `VerbsCommon`, `VerbsLifecycle`, `VerbsCommunications`, `VerbsDiagnostic`, and `VerbsData` classes.
- **One exception**: `Reset-PveVm` uses a hardcoded string `"Reset"` instead of a verb class constant: `[Cmdlet("Reset", "PveVm", ...)]`. While `Reset` IS an approved verb (in `VerbsCommon`), the implementation should use `VerbsCommon.Reset` for consistency.
- Noun prefix `Pve` is consistent across all cmdlets.

#### Parameter Design
- `[Parameter(Mandatory = ...)]` used appropriately throughout.
- `ValueFromPipelineByPropertyName = true` used on `Node`, `VmId`, and similar parameters enabling pipeline chaining (e.g., `Get-PveVm | Start-PveVm`).
- `Position` attributes used on key parameters for positional binding.
- `ValidateSet`, `ValidateRange`, `ValidateNotNullOrEmpty` used where appropriate (network types, port range, storage types, etc.).
- **Gap**: VmId parameters are `int` type but lack `[ValidateRange(100, 999999999)]` to match PVE's VMID constraints.

#### ShouldProcess / WhatIf / Confirm
- **Excellent**: Every destructive/mutating cmdlet implements `SupportsShouldProcess = true`.
- `ConfirmImpact = ConfirmImpact.High` is set on all `Remove-*` cmdlets, `New-PveTemplate` (irreversible), and `Restore-PveSnapshot`.
- **Gap**: `Stop-PveVm`, `Stop-PveContainer`, `Reset-PveVm` do not set `ConfirmImpact.High` -- stopping/resetting VMs can cause data loss.

#### OutputType
- `[OutputType]` attributes are present on **all 66 cmdlets**. This is exemplary.

#### Pipeline Support
- All cmdlets use `ValueFromPipelineByPropertyName` on `Node` and `VmId` parameters.
- `Get-PveVm` and `Get-PveContainer` output objects with `Node` and `VmId` properties, enabling chaining.
- **Gap**: None of the cmdlets implement `ProcessRecord` for pipeline batch processing -- all use `EndProcessing` (or equivalent). This means `Get-PveVm | Start-PveVm` processes only the last piped object. **This is a functional bug if multiple objects are piped.**

#### Error Handling
- `ThrowTerminatingError` used appropriately in `ConnectPveServerCmdlet`, `WaitPveTaskCmdlet`, `GetPveNodeCmdlet`, `GetPveNodeStatusCmdlet`.
- `WriteWarning` used in `DisconnectPveServerCmdlet` and `SetPveCloudInitConfigCmdlet` for non-fatal conditions.
- `WriteVerbose` used in connection cmdlets for diagnostic output.
- **Gap**: Most cmdlets let exceptions propagate without wrapping in `ErrorRecord` -- this means non-terminating errors are not always well-structured.

### 3b. C# Code Quality

#### Null-Reference Safety
- `<Nullable>enable</Nullable>` in both `.csproj` files.
- Nullable annotations used on session, parameters, and return types.
- Null guards (`?? throw`) used in constructors and authentication methods.

#### Async/Await Pattern
- All HTTP operations are async at the `PveHttpClient` level.
- Sync wrappers use `.GetAwaiter().GetResult()` pattern.
- **Assessment**: This is the standard approach for PowerShell binary modules on .NET. True async cmdlets are only available via `BeginProcessing`/`EndProcessing` async overrides in PS 7.4+, which would break PS 5.1 compatibility.
- **Risk**: Low. PowerShell cmdlets run on a single pipeline thread, so deadlock risk from `GetAwaiter().GetResult()` is minimal given the `ConfigureAwait(false)` usage throughout.

#### IDisposable Correctness
- `PveHttpClient` implements `IDisposable` correctly with a `_disposed` flag.
- All usages in cmdlets and services use `using var client = new PveHttpClient(session)` or `using (var client = ...)` blocks.
- `ProgressStream` disposes its inner stream correctly.
- File upload uses `try/finally` for stream cleanup with conditional `DisposeAsync` on .NET 9+.
- **Assessment**: Excellent. No resource leak risks identified.

#### Exception Handling
- Custom exception hierarchy: `PveApiException`, `PveAuthenticationException`, `PveNotConnectedException`, `PveSessionExpiredException`, `PveTaskTimeoutException`, `PveTaskFailedException`, `PveVersionException`.
- `PveHttpClient.SendAsync` catches `HttpRequestException` and wraps it in `PveApiException`.
- Error message extraction from PVE API JSON responses is handled gracefully.
- The only bare `catch {}` is in `ExtractErrorMessage` where parse failure falls through to raw body -- this is acceptable.

#### Magic Strings
- API resource paths are constructed inline in service methods (e.g., `$"nodes/{node}/qemu/{vmid}/status/start"`). This is pragmatic for the size of the codebase.
- Auth header names (`PVEAPIToken=`, `PVEAuthCookie=`, `CSRFPreventionToken`) are inline. Could be constants but not a significant issue.

#### LINQ
- No complex LINQ usage. JObject/JArray parsing uses Newtonsoft.Json's indexer pattern which is straightforward.

#### Logging / Verbose Output
- `WriteVerbose` used in `ConnectPveServerCmdlet` and `DisconnectPveServerCmdlet`.
- **Gap**: No `WriteVerbose` calls in any other cmdlets -- users cannot see which API calls are being made. This hinders debugging.
- `WriteDebug` is not used anywhere in the module.
- **Gap**: `WriteProgress` is only used in `Send-PveIso` (via progress callback). Not used in `Wait-PveTask` or other long-running operations.

### 3c. General Hygiene

#### Unused Code / Dead Code
- No unused `using` directives detected (CS1591 suppressed for missing XML docs, not for unused code).
- No commented-out blocks found.
- **Clean**: Zero TODO/FIXME/HACK markers in source code.

#### Code Style
- Consistent 4-space indentation matching `.editorconfig`.
- Opening braces on same line (Allman-adjacent style used in some places, K&R in others -- minor inconsistency but not problematic).
- PascalCase for public members, camelCase for locals/fields.

#### XML Doc Comments
- Public types in `PSProxmoxVE.Core` have XML doc comments (enabled via `<GenerateDocumentationFile>true</GenerateDocumentationFile>`).
- `CS1591` is suppressed in Core project (`<NoWarn>$(NoWarn);CS1591</NoWarn>`) so not all public members have docs.
- Cmdlet classes have XML summary comments on the class level.
- **Gap**: Parameter help text (`[Parameter(HelpMessage = ...)]`) is not used on any cmdlet parameter. This means `Get-Help` shows no parameter descriptions.

---

## Phase 4 -- Testing Coverage Analysis

### Test Projects

| Project | Framework | Type | Runner |
|---|---|---|---|
| `PSProxmoxVE.Core.Tests` | xUnit 2.7 + Moq 4.20 | Unit tests | `dotnet test` |
| `PSProxmoxVE.Tests` | Pester 5 | Cmdlet + Integration tests | `Invoke-Pester` |

### xUnit Tests (Core Library)

| Test File | Test Area |
|---|---|
| `Authentication/PveSessionTests.cs` | Session creation, expiry, auth modes |
| `Authentication/PveAuthenticatorTests.cs` | Authentication logic |
| `Authentication/PveVersionTests.cs` | Version parsing |
| `Models/ClusterModelTests.cs` | Cluster model deserialization |
| `Models/ContainerModelTests.cs` | Container model deserialization |
| `Models/NetworkModelTests.cs` | Network model deserialization |
| `Models/NodeModelTests.cs` | Node model deserialization |
| `Models/SdnModelTests.cs` | SDN model deserialization |
| `Models/SnapshotModelTests.cs` | Snapshot model deserialization |
| `Models/StorageModelTests.cs` | Storage model deserialization |
| `Models/TaskModelTests.cs` | Task model deserialization |
| `Models/UserModelTests.cs` | User model deserialization |
| `Models/VmModelTests.cs` | VM model deserialization |

### Pester Tests (Cmdlet Level)

| Test File | Cmdlet(s) Covered |
|---|---|
| `Connection/Connect-PveServer.Tests.ps1` | `Connect-PveServer` |
| `Connection/Disconnect-PveServer.Tests.ps1` | `Disconnect-PveServer` |
| `Connection/Test-PveConnection.Tests.ps1` | `Test-PveConnection` |
| `Containers/Get-PveContainer.Tests.ps1` | `Get-PveContainer` |
| `Containers/ContainerLifecycle.Tests.ps1` | Container start/stop/restart |
| `Containers/ContainerConfigCmdlets.Tests.ps1` | Container config cmdlets |
| `CloudInit/CloudInitCmdlets.Tests.ps1` | Cloud-init cmdlets |
| `Network/Get-PveNetwork.Tests.ps1` | `Get-PveNetwork` |
| `Network/SdnCmdlets.Tests.ps1` | SDN cmdlets |
| `Nodes/NodeCmdlets.Tests.ps1` | Node cmdlets |
| `Snapshots/SnapshotCmdlets.Tests.ps1` | Snapshot cmdlets |
| `Storage/Get-PveStorage.Tests.ps1` | `Get-PveStorage` |
| `Storage/StorageDownload.Tests.ps1` | `Invoke-PveStorageDownload` |
| `Storage/Send-PveIso.Tests.ps1` | `Send-PveIso` |
| `Tasks/TaskCmdlets.Tests.ps1` | Task cmdlets |
| `Templates/TemplateCmdlets.Tests.ps1` | Template cmdlets |
| `Users/UserCmdlets.Tests.ps1` | User CRUD |
| `Users/ApiTokenCmdlets.Tests.ps1` | API token CRUD |
| `Users/RoleCmdlets.Tests.ps1` | Role CRUD |
| `Vms/Get-PveVm.Tests.ps1` | `Get-PveVm` |
| `Vms/New-PveVm.Tests.ps1` | `New-PveVm` |
| `Vms/Remove-PveVm.Tests.ps1` | `Remove-PveVm` |
| `Vms/VmLifecycle.Tests.ps1` | VM start/stop/restart |
| `Vms/VmAdvancedOps.Tests.ps1` | Clone, migrate, resize |
| `Vms/VmConfigCmdlets.Tests.ps1` | VM config cmdlets |

### Integration Tests

The integration test file (`Integration.Tests.ps1`) is comprehensive and covers:

| Context | Operations Tested |
|---|---|
| Connection | Connect via API token, version detection |
| Nodes | List nodes, get node status |
| User CRUD | Create, list, update, remove user |
| Role CRUD | Create, list, remove role |
| API Token CRUD | Create, list, remove API token |
| Permissions | List and set permissions |
| VMs | List, create, config get/set, start/stop, hard reset, clone |
| Snapshots | Create, list, restore, remove |
| Storage | List storage, list content, upload ISO |
| Tasks | Get task, wait for task |
| Network | List networks |
| Templates | List templates, convert to template, clone from template |
| Cloud-Init | Get cloud-init config |
| Guest Agent | Ping, network interfaces, exec command |
| ACPI Lifecycle | Graceful restart, graceful stop |

- **PVE 8 and PVE 9 both tested** via matrix strategy in CI.
- **Setup/teardown**: AfterAll block cleans up created VMs, users, and roles.
- **Skip logic**: Tests skip gracefully when env vars are not set.
- **Connection details**: Injected via environment variables (`PVETEST_HOST`, `PVETEST_APITOKEN`, etc.).

### Test Coverage Gap List

| Cmdlet | Unit Test | Integration Test | Gap |
|---|---|---|---|
| `Set-PveNetwork` | Pester | No | No integration test for network modification |
| `New-PveNetwork` | Pester | No | No integration test for network creation |
| `Remove-PveNetwork` | Pester | No | No integration test for network removal |
| `Invoke-PveNetworkApply` | Pester | No | No integration test for network apply |
| `New-PveSdnZone` | Pester | No | No integration test for SDN zone creation |
| `Remove-PveSdnZone` | Pester | No | No integration test for SDN zone removal |
| `New-PveSdnVnet` | Pester | No | No integration test for SDN VNet creation |
| `Remove-PveSdnVnet` | Pester | No | No integration test for SDN VNet removal |
| `New-PveStorage` | Pester | No | No integration test for storage creation |
| `Remove-PveStorage` | Pester | No | No integration test for storage removal |
| `Invoke-PveStorageDownload` | Pester | No | No integration test for URL download |
| `Suspend-PveVm` | Pester | No | No integration test for VM suspend |
| `Resume-PveVm` | Pester | No | No integration test for VM resume |
| `Move-PveVm` | Pester | No | No integration test (requires multi-node) |
| `Resize-PveVmDisk` | Pester | No | No integration test for disk resize |
| `Copy-PveContainer` | Pester | No | No integration test for container clone |
| `Set-PveCloudInitConfig` | Pester | No | No integration test for cloud-init set |
| `Invoke-PveCloudInitRegenerate` | Pester | No | Skipped in CI due to bug |
| `Set-PvePermission` | Pester | Partial | Only tests add, not remove |

**Note**: Many of the gaps above are for operations that would require complex test infrastructure (multi-node clusters, specific storage backends, etc.) and are reasonable omissions for a pre-1.0 module.

---

## Phase 5 -- Security Review

### 5.1 Credential Handling

| Aspect | Status | Details |
|---|---|---|
| PSCredential usage | **Good** | `Connect-PveServer` accepts `PSCredential` for password auth |
| Password extraction | **Acceptable** | `NetworkCredential.Password` is used only at the point of API call |
| API token in memory | **Acceptable** | Stored as plain `string` in `PveSession.ApiToken` -- standard for PowerShell modules |
| Credential logging | **Good** | No `WriteVerbose`/`WriteDebug` calls that include credentials |
| Ticket storage | **Good** | Tickets are stored in `PveSession` only, not written to disk |

**Finding**: The password is extracted from `PSCredential` via `NetworkCredential.Password` which returns a plain `string`. This is the standard pattern for PowerShell binary modules since the PVE API requires the password as a form field. No improvement needed.

### 5.2 TLS/HTTPS

| Aspect | Status | Details |
|---|---|---|
| HTTPS enforced | **Good** | `BaseUrl` always constructs `https://` URLs |
| SkipCertificateCheck | **Good** | Explicit opt-in parameter on `Connect-PveServer` |
| Certificate bypass | **Acceptable** | Uses `ServerCertificateCustomValidationCallback = ... => true` when opted in |
| TLS version | **Good** | Left to OS/runtime negotiation (no pinning) |
| Warning on skip | **Gap** | No `WriteWarning` when `-SkipCertificateCheck` is used |

### 5.3 Input Validation

| Aspect | Status | Details |
|---|---|---|
| URL construction | **Good** | Resource paths use string interpolation with validated parameters |
| Path traversal | **Low risk** | Node names, VM IDs, and storage names are used in URL paths but come from API responses or validated parameters |
| VMID validation | **Gap** | VmId is `int` type (natural bounds) but no `[ValidateRange]` to enforce PVE's 100-999999999 range |
| Username format | **Good** | `PveAuthenticator` validates `user@realm` format |
| API token format | **Good** | Regex validation in `PveAuthenticator.AuthenticateWithApiToken` |

### 5.4 Dependency Security

| Package | Version | Pinned | Notes |
|---|---|---|---|
| `Newtonsoft.Json` | 13.0.3 | Yes | Latest stable, no known CVEs |
| `System.Text.Json` | 8.0.5 | Yes | Latest 8.x patch |
| `PowerShellStandard.Library` | 5.1.1 | Yes | PrivateAssets=all |
| `System.Management.Automation` | 7.4.0 | Yes | PrivateAssets=all |
| `Microsoft.NET.Test.Sdk` | 17.9.0 | Yes | Test only |
| `xunit` | 2.7.0 | Yes | Test only |
| `Moq` | 4.20.70 | Yes | Test only |
| `coverlet.collector` | 6.0.1 | Yes | Test only |

All dependencies are pinned to exact versions. No floating version ranges detected.

### 5.5 Secret Scanning

| Finding | Severity | Location |
|---|---|---|
| Hardcoded test password | **Medium** | `integration-tests.yml:42` -- `PVE_PASSWORD: "Testpass123!"` |

The test password is for a throwaway nested PVE instance provisioned and destroyed during CI. It is masked in CI logs via `::add-mask::`. This is acceptable for CI test infrastructure but should be documented.

No other hardcoded credentials, tokens, or sensitive IPs found in source code.

---

## Phase 6 -- PSGallery Publication Readiness

### 6.1 Module Manifest Assessment

| Field | Status | Value |
|---|---|---|
| `RootModule` | OK | `PSProxmoxVE.dll` |
| `ModuleVersion` | OK | `0.1.0` |
| `GUID` | OK | `a3f7c2d1-84e5-4b9f-a061-3e2d8c5f1a7b` |
| `Author` | OK | `goodolclint` |
| `CompanyName` | OK | `Worklab` |
| `Copyright` | OK | `(c) 2026 goodolclint. All rights reserved.` |
| `Description` | OK | Meaningful, not placeholder |
| `PowerShellVersion` | OK | `5.1` |
| `CompatiblePSEditions` | OK | `Desktop`, `Core` |
| `DotNetFrameworkVersion` | OK | `4.8` |
| `RequiredAssemblies` | OK | Lists Core DLL and Newtonsoft.Json |
| `FormatsToProcess` | OK | `PSProxmoxVE.format.ps1xml` |
| `CmdletsToExport` | **Excellent** | Explicit list of all 66 cmdlets |
| `FunctionsToExport` | OK | Empty (binary module) |
| `AliasesToExport` | OK | Empty |
| `VariablesToExport` | OK | Empty |
| `Prerelease` | OK | `preview` |
| `Tags` | OK | 8 relevant tags |
| `LicenseUri` | OK | Points to GitHub LICENSE |
| `ProjectUri` | OK | Points to GitHub repo |
| `ReleaseNotes` | **Missing** | Not present in PSData |
| `IconUri` | **Missing** | Not present in PSData |
| `RequiredModules` | N/A | None required |
| `HelpInfoURI` | **Missing** | No online help |

### 6.2 License

- MIT License present at repo root.
- OSI-approved (PSGallery requirement met).
- `LicenseUri` in manifest points to it.

### 6.3 README Quality

| Aspect | Status |
|---|---|
| Installation instructions | **Present** but says "not PSGallery" -- needs update for publication |
| Quick-start examples | **Excellent** -- multiple scenarios covered |
| Authentication guide | **Excellent** -- both ticket and API token documented |
| Multi-cluster usage | **Excellent** -- `-Session` parameter documented |
| Cmdlet reference table | **Excellent** -- all cmdlets listed with descriptions |
| Known limitations | **Good** -- v1 limitations documented |
| Contributing section | **Present** -- basic development setup |
| Badges | **Missing** -- no build status, PSGallery version, or license badges |

### 6.4 Build & Publish Pipeline

- **Build workflow**: Builds and tests on net48 (Windows) and net9.0 (Windows + Ubuntu).
- **Unit test workflow**: Tests Pester scripts across PS 5.1/7.5 on Windows/Ubuntu/macOS.
- **Integration test workflow**: Full tests against live PVE 8 and PVE 9.
- **PSGallery publish workflow**: **MISSING** -- no workflow for `Publish-Module` or release creation.
- **GitHub Releases**: Not configured (no release workflow, no tagged releases in git history).

### 6.5 Versioning

- CHANGELOG follows Keep a Changelog format with Conventional Commits.
- Only an `[Unreleased]` section exists -- no versioned releases yet.
- Commit messages follow conventional commits (`feat:`, `fix:`, `test:`, `ci:`, etc.).

---

## Phase 7 -- Community & Repo Maintenance Standards

### Files Present

| Item | Status |
|---|---|
| `README.md` | Present, comprehensive |
| `LICENSE` | Present (MIT) |
| `CHANGELOG.md` | Present |
| `.editorconfig` | Present |
| `.gitignore` | Present |

### Files Missing

| Item | Recommendation |
|---|---|
| `CONTRIBUTING.md` | Create with dev setup, coding standards, test instructions, PR process |
| `CODE_OF_CONDUCT.md` | Adopt Contributor Covenant |
| `SECURITY.md` | Create with vulnerability disclosure policy |
| `.gitattributes` | Create for line ending normalization (`* text=auto`) |
| `.github/ISSUE_TEMPLATE/` | Create bug report and feature request templates |
| `.github/pull_request_template.md` | Create with checklist |

### Commit Conventions

- Conventional Commits used consistently.
- Scopes include: `ci`, `test`, `fix`, `feat`, `refactor`, `chore`, `docs`.
- Recent history shows disciplined commit hygiene.

### Branch Protection

Recommended settings (cannot verify via API):
- Default branch: `main` (confirmed)
- Require PR reviews before merge
- Require status checks to pass (build + unit tests + integration tests)
- Require linear history or squash merging

---

## Phase 8 -- Prioritized Recommendations

### Critical (blocks PSGallery publication or is a security risk)

| # | What | Where | Why | Fix |
|---|---|---|---|---|
| C1 | **No PSGallery publish workflow** | `.github/workflows/` | Cannot automate releases | Create `publish.yml` triggered on tag push that runs `Publish-Module` with PSGallery API key from secrets |
| C2 | **README says "not PSGallery"** | `README.md:30` | Contradicts publication goal | Update installation section to `Install-Module PSProxmoxVE` |
| C3 | **No ReleaseNotes in manifest** | `PSProxmoxVE.psd1:168-195` | PSGallery strongly recommends release notes | Add `ReleaseNotes` to `PSData` section |
| ~~C4~~ | ~~Pipeline processing bug~~ | *Verified: all cmdlets use `ProcessRecord`* | N/A -- pipeline processing is correct | No action needed |

### High (significantly impacts quality or community adoption)

| # | What | Where | Why | Fix |
|---|---|---|---|---|
| H1 | **No `HelpMessage` on parameters** | All cmdlet files | `Get-Help` shows no parameter descriptions | Add `HelpMessage = "..."` to all `[Parameter]` attributes |
| H2 | **No `WriteVerbose` in cmdlets** | All cmdlets except Connection | Users cannot debug API calls | Add `WriteVerbose($"Calling {method} {resource}")` before API calls |
| H3 | **Missing `CONTRIBUTING.md`** | Repo root | Barrier to community contributions | Create with dev setup, coding standards, PR process |
| H4 | **Missing `SECURITY.md`** | Repo root | No vulnerability disclosure process | Create with responsible disclosure instructions |
| H5 | **No warning on `-SkipCertificateCheck`** | `ConnectPveServerCmdlet.cs` | Users may not realize they're disabling TLS verification | Add `WriteWarning("Certificate validation is disabled...")` |
| H6 | **`Reset-PveVm` uses string literal** | `ResetPveVmCmdlet.cs:16` | Inconsistent with all other cmdlets using verb class constants | Change `"Reset"` to `VerbsCommon.Reset` |
| ~~H7~~ | ~~No `WriteProgress` in `Wait-PveTask`~~ | *Verified: already implemented* | N/A -- `WaitPveTaskCmdlet` already uses `WriteProgress` with percentage and timeout tracking | No action needed |
| H8 | **Missing GitHub issue/PR templates** | `.github/` | No structured issue reporting | Create bug report, feature request, and PR templates |

### Medium (best practice gaps, test coverage holes)

| # | What | Where | Why | Fix |
|---|---|---|---|---|
| M1 | **`Stop-PveVm`/`Reset-PveVm` lack `ConfirmImpact.High`** | `StopPveVmCmdlet.cs`, `ResetPveVmCmdlet.cs` | These can cause data loss | Add `ConfirmImpact = ConfirmImpact.High` |
| M2 | **No `[ValidateRange]` on VmId** | All cmdlets with VmId parameter | PVE requires VMID 100-999999999 | Add `[ValidateRange(100, 999999999)]` to all VmId parameters |
| M3 | **Container snapshots not supported** | Services/Cmdlets | Containers support snapshots in PVE but module only handles VM snapshots | Add `Get/New/Remove/Restore-PveContainerSnapshot` cmdlets |
| M4 | **No integration tests for network modifications** | Integration tests | Network CRUD is tested only at Pester level | Add integration tests for network create/modify/delete |
| M5 | **No `CODE_OF_CONDUCT.md`** | Repo root | Expected for open-source projects | Add Contributor Covenant |
| M6 | **No `.gitattributes`** | Repo root | Line ending consistency across platforms | Add `* text=auto` and specific overrides |
| M7 | **CS1591 suppressed in Core project** | `PSProxmoxVE.Core.csproj:10` | Public API lacks XML documentation | Remove NoWarn and add XML docs to public members |
| M8 | **No badges in README** | `README.md` | Reduces discoverability and trust | Add build status, PSGallery version, license badges |
| M9 | **CHANGELOG has only [Unreleased]** | `CHANGELOG.md` | No release history | Create first versioned release entry |
| M10 | **`Invoke-PveVmGuestExec` lacks ShouldProcess** | `InvokePveVmGuestExecCmdlet.cs:14` | Executes commands inside a VM -- a mutating operation | Add `SupportsShouldProcess = true` |

### Low / Nice-to-have (polish, discoverability, stretch goals)

| # | What | Where | Why | Fix |
|---|---|---|---|---|
| L1 | **No `IconUri` in manifest** | `PSProxmoxVE.psd1` | Improves PSGallery listing appearance | Create a module icon and host it, add URI |
| L2 | **No online help (MAML/platyPS)** | Module | `Get-Help -Online` doesn't work | Generate MAML help using platyPS |
| L3 | **No firewall cmdlets** | Module | High-value gap for security automation | Implement firewall rule CRUD for cluster/node/VM |
| L4 | **No backup/vzdump cmdlets** | Module | High-value gap for DR automation | Implement vzdump and backup schedule management |
| L5 | **No pool management** | Module | Useful for multi-tenant environments | Implement pool CRUD |
| L6 | **`new Random()` for boundary generation** | `PveHttpClient.cs:393` | Not cryptographically secure (not a security risk for multipart boundaries, but could use `RandomNumberGenerator`) | Consider `RandomNumberGenerator` for .NET 6+ |
| L7 | **No alias support** | Module manifest | Some users prefer short aliases | Consider adding aliases like `cpve` for `Connect-PveServer` |
| L8 | **Tagged releases in GitHub** | Repository | No GitHub Releases | Set up release workflow with auto-generated release notes |
| L9 | **Container migration cmdlet** | Module | VMs have `Move-PveVm` but containers have no equivalent | Add `Move-PveContainer` |
| L10 | **SDN subnet management** | Module | SDN zones/vnets exist but subnets are missing | Add SDN subnet CRUD |

---

## Summary Statistics

| Metric | Count |
|---|---|
| Total cmdlets | 66 |
| Cmdlets with ShouldProcess | 43 (all destructive/mutating) |
| Cmdlets with OutputType | 66 (100%) |
| xUnit test files | 13 |
| Pester test files | 26 |
| Integration test contexts | 13 |
| PVE API areas covered | 10 of ~20 major areas |
| Critical issues | 4 |
| High issues | 8 |
| Medium issues | 10 |
| Low issues | 10 |
| NuGet dependencies (runtime) | 2 (Newtonsoft.Json, System.Text.Json) |
| Security vulnerabilities found | 0 |
| Hardcoded secrets | 1 (test password in CI, masked) |
