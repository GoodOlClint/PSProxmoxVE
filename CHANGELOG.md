# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/).

## [Unreleased]

## [0.1.3] - 2026-05-20

### Added

- `Connect-PveServer -TimeoutSeconds` to set the session-default `HttpClient` timeout (default 100s; `0` = infinite). (#59)
- `Send-PveFile -TimeoutSeconds` and `Invoke-PveStorageDownload -TimeoutSeconds` for per-call override with a 30-minute implicit default so large uploads/downloads no longer trip the 100s default. (#59)

### Fixed

- `New-PveVm -DiskSize` and `New-PveContainer -RootFsSize` now normalize unit suffixes (`32G`, `1T`, `32GB`, etc.) to bare GiB before constructing the disk spec. Previously the suffix was passed verbatim, which LVM/LVM-thin storages rejected with `unable to parse lvm volume name '32G'`. Sub-GB units (`M`, `MB`, `K`, `KB`) are now rejected client-side with a clear error. (#58)
- `PveHttpClient.SendAsync` surfaces `HttpClient.Timeout` firings as `PveApiException(RequestTimeout)` with the resource path and configured timeout, instead of leaking a raw `TaskCanceledException`. Works across `net48`, `net10.0`, and `netstandard2.0`. (#59)
- Disk-size validation runs before `ShouldProcess` so typos like `512M` are caught with `-WhatIf`, regardless of whether `-DiskStorage`/`-RootFsStorage` is also supplied. (#58)

## [0.1.2] - 2026-03-27

### Fixed

- `Get-PveApiToken`: `FullTokenId` is now computed from `UserId!TokenId` (was always empty). (#44)
- `Set-PvePermission`: added `token` ACL type with auto-detection from `!` in `-UgId`, enabling permission assignment for API tokens. (#43)
- `Connect-PveServer`: always emits the session to the pipeline. Use `-Quiet` to suppress; `-PassThru` is kept hidden for backwards compatibility. (#45)

## [0.1.1] - 2026-03-26

### Added

- Firewall management cmdlets (21): rules, security groups, aliases, IP sets, options at cluster/node/VM/container levels
- Backup/vzdump cmdlets (5): ad-hoc backup creation and scheduled backup job CRUD
- SDN IPAM cmdlets (3): `Get`/`New`/`Remove-PveSdnIpam` for IPAM plugin management
- SDN DNS cmdlets (3): `Get`/`New`/`Remove-PveSdnDns` for DNS plugin management
- SDN Controller cmdlets (3): `Get`/`New`/`Remove-PveSdnController` for controller management
- SDN Update cmdlets (7): `Set-PveSdnZone`/`Vnet`/`Subnet`/`Controller`/`Ipam`/`Dns` + `Invoke-PveSdnApply`
- `Set-PveRole`, `Set-PveStorage`, `Set-PveApiToken` for missing update operations
- `Get-PveClusterResource`: single-call cluster-wide inventory of all VMs, containers, nodes, storage
- Task management: `Get-PveTaskList` (list tasks on node), `Stop-PveTask` (cancel running tasks)
- Pool management cmdlets (4): `Get`/`New`/`Set`/`Remove-PvePool`
- `Get-PveBackupInfo`: find VMs/containers not covered by backup jobs
- VM disk operations: `Move-PveVmDisk` (storage migration), `Remove-PveVmDisk` (detach/delete)
- Guest agent extensions (6): `Get-PveVmGuestOsInfo`, `Get-PveVmGuestFsInfo`, `Read`/`Write-PveVmGuestFile`, `Set-PveVmGuestPassword`, `Invoke-PveVmGuestFsTrim`
- Container gaps (6): `Suspend`/`Resume-PveContainer`, `Resize-PveContainerDisk`, `New-PveContainerTemplate`, `Move-PveContainerVolume`, `Get-PveContainerInterface`
- Storage content management (4): `Get-PveStorageStatus`, `Remove`/`Set-PveStorageContent`, `New-PveStorageDisk`
- Node operations (6): `Get`/`Set-PveNodeConfig`, `Get`/`Set-PveNodeDns`, `Start`/`Stop-PveNodeVms`
- Access management (9): `Get`/`New`/`Set`/`Remove-PveGroup`, `Get`/`New`/`Set`/`Remove-PveDomain`, `Set-PvePassword`
- Two-tier version gating: introduced vs default version with clear user messaging
- 70 xUnit tests validating every `ValidateSet` against the PVE OpenAPI spec, with `pve-api-enums.json` fixture extracted from the full spec
- Integration tests for firewall rules, aliases, IP sets, backup jobs, and OVA import
- PSGallery version badge in README

### Changed

- All cmdlet classes sealed for design clarity and JIT optimization
- `[OutputType]` attribute added to all 169 cmdlets for IntelliSense and pipeline support
- Publishable projects retargeted to `netstandard2.0` for PS 5.1 + PS 7.x compatibility
- `System.Text.Json` attributes removed — module uses `Newtonsoft.Json` exclusively
- Inline task-polling loops replaced with `TaskService.WaitForTask` (timeout + progress support)
- Password parameters changed from `string` to `SecureString` with secure memory handling
- `ValidateRange(100, 999999999)` added to all `VmId` parameters
- `Uri.EscapeDataString()` applied to all dynamic URL path segments
- Hardcoded verb strings replaced with verb class constants (`VerbsCommon.Get`, etc.)
- Auth header magic strings extracted to named constants in `PveHttpClient`
- Bare `catch` blocks replaced with specific or filtered exception handling
- MAML help (dll-Help.xml) and 170 markdown cmdlet docs generated
- PSGallery publish workflow with PS 5.1 smoke testing

### Fixed

- `ConfirmImpact.High` added to all destructive cmdlets (Stop, Reset, Restart, Suspend, Remove, Restore, New-PveTemplate)
- Storage `ValidateSet`: removed `glusterfs` (dropped in PVE 9), added `btrfs` and `esxi`
- Backup compression: `none` → `0` (PVE expects the string `"0"`, not `"none"`)
- Cluster resource filter: removed `lxc` (PVE uses `vm` for both QEMU and LXC)
- Hardcoded test password moved from CI workflow to GitHub Actions secret
- Terraform variable default password removed (requires env var)

## [0.1.0-preview] - 2026-03-19

### Added

- Initial project structure and solution setup
- Ticket and API token authentication with session management
- HTTP client with manual multipart ISO upload (bugzilla 7389 workaround)
- Typed response models for PVE 8.x and 9.x API resources
- Service layer for all resource domains
- 66 PowerShell cmdlets for VMs, containers, storage, networking, SDN, users, roles, permissions, API tokens, templates, cloud-init, snapshots, and tasks
- QEMU guest agent cmdlets (Test-PveVmGuestAgent, Get-PveVmGuestNetwork, Invoke-PveVmGuestExec)
- xUnit unit tests for core library
- Pester 5 cmdlet tests across OS/PS version matrix (Windows PS 5.1, PS 7.5 on Windows/Linux/macOS)
- Integration tests against live PVE 8 and PVE 9 instances via Terraform-provisioned nested VMs
- GitHub Actions CI/CD workflows (build, unit tests, integration tests)
- Format definitions for default table output on all PS versions
