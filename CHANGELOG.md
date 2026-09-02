# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/).

## [Unreleased]

### Added

- `New-PveNetwork` and `Set-PveNetwork` gained `-BridgeVlanAware`, so a VLAN-aware Linux bridge can be created and toggled from the module instead of only being read back. The model already surfaced `bridge_vlan_aware` as `BridgeVlanAware`, so this closed a write-path gap. On `Set-PveNetwork` the switch is only sent when explicitly bound, so an update that omits it leaves the flag alone. Clearing it goes through the endpoint's `delete` list rather than `bridge_vlan_aware=0`: PVE merges supplied keys onto the stored stanza and accepts the `0` without acting on it, so the obvious form is a silent no-op — confirmed against a live PVE 9 cluster, where the bridge stayed VLAN-aware. `bridge_vids` is not covered; it is an independent parameter and PVE defaults to 2-4094. (#92)

### Changed

- `Newtonsoft.Json` is now 13.0.4 in both shipped assemblies, with every package version managed centrally in `Directory.Packages.props` so the two can no longer drift; the SDK is pinned by `global.json` (10.0, latest feature band) and the net48 test build no longer emits the `System.Memory` MSB3277 conflict. (#156)

### Fixed

- `Copy-PveVm` and `Copy-PveContainer` now allocate a valid guest ID through `cluster/nextid` when `-NewVmId` is omitted, instead of sending `newid=0`, which PVE rejects. Both cmdlets now forward `-Storage` to the clone request; it was declared and silently dropped, so a full clone always landed on the source storage. `New-PveVm` and `Import-PveOva` use the same service call for their ID allocation, so a response without `data` is a clear error rather than a `NullReferenceException`. (#135)
- `Import-PveOva` no longer throws an unhandled `InvalidOperationException` when the created VM is not yet listed on the node (the disk import still running without `-Wait`); it returns the basic VM record instead, as the cmdlet always intended. Its upload no longer runs under the session's 100-second timeout, so an OVA that takes longer to transfer completes; `-TimeoutSeconds` was added (default 30 minutes, `0` for none), mirroring `Send-PveFile`. (#139)
- `Wait-PveTask` polls through `TaskService.WaitForTask` like every other `-Wait` path, so it clamps the poll interval to one second, checks the task before sleeping, and no longer overflows on intervals over 24 days. An omitted `-Timeout` still waits indefinitely. (#140)
- `Get-PveVm` and `Get-PveContainer` warn for each node skipped because it was unreachable or returned a server error, instead of silently returning a partial or empty list, and a permission error on any node now surfaces instead of reading as "no guests". Lifecycle cmdlets with `-Wait` surface an expired session, a permission error or a missing guest during the status poll immediately, instead of failing with a generic timeout after the full `-Timeout` window. (#142)
- `Remove-PveStorage`, `Remove-PveSdnVnet` and `Remove-PveSdnZone` now percent-encode the name before building the API path and reject names outside `A-Z a-z 0-9 . _ -`, so a name containing `../` can no longer be turned into a request against a different endpoint. The three cmdlets now call the existing service methods instead of building their own request. (#145)
- `Invoke-PveVmGuestExec` now recognises a boolean `exited` from the guest agent, instead of polling until `-Timeout` on PVE builds that return `true` rather than `1`. (#141)
- `Disconnect-PveServer` no longer issues a `DELETE` to `/access/ticket`, an endpoint PVE does not have; the call always failed and was hidden. It now discards the local session only and gained `-Session` so an explicitly created session can be disconnected. The warning for a session that is not the module-level one names the credential's real lifecycle: tickets expire on their own, API tokens do not and can be revoked with `Remove-PveApiToken`. (#144)
- `Import-PveOva` now places disks on the bus the OVF descriptor names. The controller mapping had SCSI and SATA swapped for VMware-produced OVAs, and the computed bus was then ignored in favour of `scsi` for every disk. (#138)
- `Import-PveOva` rejects an OVF descriptor whose disk file name contains anything outside `A-Z a-z 0-9 . _ -`, and refuses descriptors with a DTD. A crafted archive could otherwise inject extra keys into the disk config line or exhaust memory through entity expansion. (#148)
- `Get-PvePermission` now returns the privileges granted on each path in a `Privileges` property; previously the map PVE returned was dropped and every result carried only the path. The key's presence is the grant; the value is whether it propagates to sub-paths. (#137)
- `Remove-PveVm -Force` now sends `skiplock=1` (PVE honours it for `root@pam` only) and `Remove-PveContainer -Force` sends `force=1`; both switches were accepted and ignored before. (#136)
- `Restart-PveVm` now uses PVE's native reboot endpoint (`POST {vmid}/status/reboot`) instead of composing a shutdown followed by a start. The two-call form raced Proxmox's own post-stop cleanup: the start won the guest's config lock, `qm cleanup` then held that lock for 30 seconds waiting on the newly started process, and the caller's next operation failed with `can't lock file '/var/lock/qemu-server/lock-<vmid>.conf' - got timeout`. Reproduced in integration runs 183, 185 and 186 as a cascade of 4 failures. `Restart-PveContainer` is unchanged — LXC has no reboot endpoint. See `DECISIONS.md` D016.
- Guest operations that Proxmox rejects with `can't lock file '/var/lock/qemu-server/lock-<vmid>.conf' - got timeout` are now reissued for up to 45 seconds instead of surfacing as an error. That flock is taken by `qm cleanup` for up to 30 seconds after a guest stops and is not exposed through the API in any form, so it can only be retried past, never waited on. Covers both the synchronous form (`Set-PveVmConfig`, `Resize-PveVmDisk`, and every other call through the HTTP client) and the asynchronous form, where the request succeeds and the PVE task then fails (`Reset-PveVm`, `Copy-PveVm`). Reproduced on a client ~40% slower than CI, which failed three VM tests on a commit CI passed. (#113) See `DECISIONS.md` D020.
- Lifecycle cmdlets with `-Wait` (`Start`/`Stop`/`Restart`/`Reset`/`Resume` for VMs and containers) also wait for the guest's config lock (the `lock:` property, e.g. `backup` or `migrate`) to clear before returning, and the post-timeout fallback tests the most recent poll rather than whether a match was ever seen. See `DECISIONS.md` D015 — that guard covers the config lock only; the separate flock race is D020.
- `New-PveCluster -Wait` now blocks until the cluster reports quorum, not merely until the creation task finishes. PVE's create task returns before corosync converges (~6s earlier in testing), so the natural `New-PveCluster -Wait` → `Add-PveClusterMember` sequence failed with `cluster not ready - no quorum?`. Adds `-Timeout` (seconds, default 60, range 1-3600) following the `-Wait` timeout convention used by `Stop-PveContainer` and `Reset-PveVm`. See `DECISIONS.md` D014.

## [0.2.0] - 2026-05-22

### Added

- `New-PveVm` disk controller / IO options: `-DiskBus` (virtio/scsi/sata/ide), `-ScsiHardware` (scsihw), `-DiskIoThread`, `-DiskAio`, `-DiskSsd`, `-DiskDiscard`, `-DiskCache`. Invalid combinations (e.g. `ssd` on virtio, `iothread` on sata/ide or scsi without `virtio-scsi-single`) are rejected up front with a clear error. (#65)
- `Get-PveVmConfig` now surfaces `scsihw`, `efidisk0`, and `tpmstate0` as typed properties, plus an `AdditionalProperties` dictionary capturing any other config key (e.g. `hostpci0`) as native .NET values instead of silently dropping it. (#65)

### Fixed

- Form values containing `;` were split into bogus fields by PVE's parser, so a multi-device boot order set via `Set-PveVmConfig -AdditionalConfig @{ boot = 'order=scsi0;ide2' }` failed with `unable to parse drive options`. Semicolons are now percent-encoded. (#64)
- `Invoke-PveVmGuestExec -Args` were delivered to the guest as JSON on STDIN instead of as argv, so commands ran with no/garbage arguments. Arguments are now sent as the PVE `command` array (repeated keys), reaching the process as real argv. (#68)

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
