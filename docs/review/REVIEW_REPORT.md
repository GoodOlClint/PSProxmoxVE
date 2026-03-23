# PSProxmoxVE Module Review Report — Scan 6

```
Scan date:           2026-03-23
Prior report date:   2026-03-23 (scan-5)
PVE API spec date:   2026-03-21T15:04:50.641Z
PVE API spec SHA256: 4af79be30166209a4714b771f65e1e9540c5b738f414ff30c98454402e29d030
PVE version hint:    N/A (not set in spec)
Total API endpoints: 646
Findings DB:         docs/review/findings.json (F001–F083)
Open findings:       11 (before scan) → 15 (after scan)
New this scan:       4  Resolved this scan: 0  Regressed: 0
Last CI run:         integration-tests.yml | failure | 2026-03-23T18:51:46Z | https://github.com/goodolclint/PSProxmoxVE/actions/runs/23454616043
```

## Executive Summary

- **Delta**: 0 resolved | 4 new | 0 regressed | 15 open
- **API drift**: 0 breaking changes in implemented endpoints | 42 PVE 9.0 endpoints unimplemented (F061)
- **CI**: Last integration run: **FAILED** | 2 test failures on PVE 9 + 1 infrastructure failure on PVE 8
- **Code quality**: All 12 DECISIONS.md entries upheld — zero regressions detected
- Module has 169 cmdlets, 170 markdown docs, netstandard2.0 targeting, ~800+ total tests (374 xUnit + ~400-500 Pester + 107 integration)
- PSGallery manifest complete except IconUri (F021)
- 77 PVE 9.0 parameter changes detected across implemented endpoints (all additive, none breaking)

---

## Phase 1 — Repository Inventory & Structure

### Project Layout

| Item | Present | Path/Notes |
|---|---|---|
| Solution file | Yes | PSProxmoxVE.sln |
| Cmdlet project | Yes | src/PSProxmoxVE/ (netstandard2.0) |
| Core library | Yes | src/PSProxmoxVE.Core/ (netstandard2.0) |
| xUnit tests | Yes | tests/PSProxmoxVE.Core.Tests/ (net10.0;net48) |
| Pester tests | Yes | tests/PSProxmoxVE.Tests/ |
| Module manifest | Yes | src/PSProxmoxVE/PSProxmoxVE.psd1 |
| MAML help | Yes | src/PSProxmoxVE/PSProxmoxVE.dll-Help.xml |
| Format file | Yes | src/PSProxmoxVE/PSProxmoxVE.format.ps1xml |
| README.md | Yes | With badges (Build, Unit Tests, License, PSGallery) |
| CHANGELOG.md | Yes | Keep a Changelog format, 0.1.0-preview entry |
| CONTRIBUTING.md | Yes | References .NET SDK 9.0+ — should be 10.0+ (F083) |
| LICENSE | Yes | MIT |
| CODE_OF_CONDUCT.md | Yes | Contributor Covenant |
| SECURITY.md | Yes | Vulnerability disclosure policy |
| DECISIONS.md | Yes | 12 active decisions (D001–D012) |
| .editorconfig | Yes | Present |
| .gitignore | Yes | Present |
| .gitattributes | Yes | Line ending normalization |
| CODEOWNERS | Yes | Present |
| Issue templates | Yes | Bug report (YAML), feature request (YAML), config.yml |
| PR template | Yes | .github/pull_request_template.md |
| dependabot.yml | Yes | NuGet + GitHub Actions (weekly) |
| CI: Build | Yes | .github/workflows/build.yml (3-matrix) |
| CI: Unit Tests | Yes | .github/workflows/unit-tests.yml (Pester multi-platform) |
| CI: Integration | Yes | .github/workflows/integration-tests.yml (PVE 8+9) |
| CI: Publish | Yes | .github/workflows/publish.yml (tag-triggered, PS 5.1 smoke test) |
| Findings DB | Yes | docs/review/findings.json (F001–F083) |

### Missing Items

None — all expected files for a well-maintained open-source PowerShell module are present.

---

## Phase 2 — PVE API Coverage Audit

### Coverage by Functional Area

| Area | Total Endpoints | Cmdlets | Est. Coverage | Notes |
|---|---|---|---|---|
| VMs (qemu) | ~80 | 28 | ~35% | Core CRUD, lifecycle, guest agent, disk, config |
| Containers (lxc) | ~45 | 20 | ~44% | Full lifecycle + snapshots + templates |
| Storage | ~30 | 11 | ~37% | CRUD, content, upload, download, disk alloc |
| Network | ~15 | 5 | ~33% | CRUD + apply |
| SDN | ~45 | 19 | ~42% | Zones, VNets, subnets, IPAM, DNS, controllers, apply |
| Firewall (cluster) | ~20 | 21 | ~100% | Rules, groups, aliases, IP sets, options, refs |
| Access/Users | ~25 | 21 | ~84% | Users, roles, groups, domains, tokens, permissions, password |
| Nodes | ~40 | 8 | ~20% | Status, config, DNS, start/stop VMs |
| Tasks | ~5 | 4 | ~80% | Get, list, stop, wait |
| Backup | ~10 | 6 | ~60% | Ad-hoc + job CRUD + compliance |
| Templates | ~5 | 4 | ~80% | Get, create, remove, clone |
| CloudInit | ~3 | 3 | ~100% | Get, set, regenerate |
| Cluster | ~77 | 1 | ~1% | Only Get-PveClusterResource |
| Snapshots | ~5 | 4 | ~80% | VM snapshots: get/new/remove/restore |
| Pools | ~5 | 4 | ~80% | CRUD |
| HA | ~26 | 0 | 0% | F053 |
| Ceph | ~40 | 0 | 0% | F054 |
| Notifications | ~32 | 0 | 0% | F068 |
| ACME/Certificates | ~23 | 0 | 0% | F069 |
| Disks (LVM/ZFS) | ~18 | 0 | 0% | F067 |
| **Total** | **~646** | **169** | **~26%** | |

### PVE 9.0 New Endpoints (42 total, 0 implemented — F061)

Key new subsystems in PVE 9.0:

| Category | Endpoints | Description |
|---|---|---|
| HA Rules | 5 | Affinity/anti-affinity rules for HA resources |
| Bulk Actions | 5 | Cluster-wide guest start/shutdown/suspend/migrate |
| SDN Fabrics | 13 | Fabric management, node assignments |
| SDN Lock/Rollback | 3 | Transactional SDN changes |
| OCI Registry | 1 | Container image pulls from OCI registries |
| Node SDN | 6 | Per-node SDN fabric/zone/vnet inspection |
| Node Capabilities | 2 | CPU flags, migration capabilities |
| VM/Container | 2 | DBus VM state, container migration preflight |
| Access | 1 | VNC ticket creation |

### API Drift — PVE 9.0 Parameter Changes

77 parameter changes detected across implemented endpoints. All are additive (new optional parameters) except:

| Risk | Endpoint | Change |
|---|---|---|
| Mild | POST /cluster/backup | `-notification-policy,-notification-target` (removed) |
| Mild | PUT /cluster/backup/{id} | `-notification-policy,-notification-target` (removed) |
| Additive | SDN zone/vnet/subnet/controller/ipam/dns | `+lock-token` parameter |
| Additive | SDN zones/controllers | `+fabric` parameter |
| Additive | POST /nodes/{node}/qemu | `+allow-ksm,+intel-tdx` |
| Additive | PUT /nodes/{node}/lxc/{vmid}/config | `+entrypoint,+env` |
| Additive | POST /storage, PUT /storage/{storage} | `+snapshot-as-volume-chain,+zfs-base-path` |

The removed `notification-policy` and `notification-target` parameters on backup endpoints should be verified — if New-PveBackupJob or Set-PveBackupJob send these parameters, PVE 9.0 will reject them.

---

## Phase 3 — Code Quality & Best Practices

### 3a. PowerShell Module Design

| Check | Status | Evidence |
|---|---|---|
| Approved verbs only | Pass | All 169 cmdlets use standard PS verb classes |
| Verb class constants | Pass | No string literals in [Cmdlet] attributes (D011) |
| All classes sealed | Pass | 169/169 sealed (D007) |
| OutputType on all | Pass | 169/169 have [OutputType] (D005) |
| ShouldProcess on destructive | Pass | All Remove/Stop/Reset/Restart/Suspend/Restore/Template cmdlets |
| ConfirmImpact.High | Pass | All destructive cmdlets (D006) |
| VmId ValidateRange | Pass | All VmId params have [ValidateRange(100, 999999999)] (D010) |
| Pve noun prefix | Pass | All cmdlets use Pve prefix |

### 3b. C# Code Quality

| Check | Status | Evidence |
|---|---|---|
| No inline task-polling | Pass | Only TaskService.WaitForTask and WaitPveTaskCmdlet have while(true) — both with timeout (D001) |
| No bare catch blocks | Pass | Zero `catch { }` or unfiltered `catch (Exception)` in src/ (D004) |
| SecureString passwords | Pass | SetPveVmGuestPassword and SetPvePassword use SecureString (D002) |
| URL encoding | Pass | Uri.EscapeDataString on all path params across all services (D003) |
| Newtonsoft only | Pass | No [JsonPropertyName] attributes anywhere in src/ (D008) |
| No System.Text.Json dep | Pass | Not referenced in any .csproj |
| Magic strings extracted | Pass | Auth header names are const fields (D012) |
| Sync-over-async | wont_fix | .GetAwaiter().GetResult() — accepted PS binary module pattern (F048) |

### 3c. General Hygiene

| Check | Status | Evidence |
|---|---|---|
| Framework targeting | Pass | Publishable: netstandard2.0. Tests: net10.0;net48 (D009) |
| SDK versions | Pass | All workflows use dotnet SDK 10.0.x |
| XML doc generation | Pass | GenerateDocumentationFile=true in Core.csproj |

### 3d. HttpClient Lifecycle

PveHttpClient implements IPveHttpClient interface. All 14 services accept IPveHttpClient via constructor injection, enabling shared client instances per session. HttpClient is created once per PveHttpClient instance. **F045 resolved in prior scan.**

### DECISIONS.md Compliance

All 12 decisions (D001–D012) verified with no regressions detected.

---

## Phase 4 — Testing Coverage Analysis

### Test Infrastructure

| Component | Framework | Count | Notes |
|---|---|---|---|
| xUnit tests | net10.0;net48 | 374 cases | 25 files: services (214), models (133), auth (27) |
| Pester unit tests | PS 5.1, 7.5 | ~400-500 cases | 46 files: parameter validation, metadata, ShouldProcess |
| Pester integration | PS 7.x | 107 cases | 1 file: live PVE 8 + PVE 9 end-to-end |
| **Total** | | **~800+** | Three-tier: unit (xUnit) + cmdlet (Pester) + integration |

### Test Quality Assessment

- **Three-tier strategy**: xUnit validates core business logic (services, models), Pester validates cmdlet surface (parameters, metadata, parameter sets), integration validates end-to-end on live PVE
- **All 169 cmdlets have Pester tests**: Parameter validation, mandatory params, ShouldProcess support, OutputType
- **Arrange/Act/Assert**: xUnit tests consistently structured with Moq for IPveHttpClient mocking
- **Offline-first**: 45/46 Pester test files work without network; xUnit uses mock fixtures
- **Edge cases**: Null parameters, empty arrays, API errors, PVE 8 + PVE 9 response formats
- **Test isolation**: xUnit uses DI via mocked IPveHttpClient; each test is independent

### Coverage Gaps (F046)

All cmdlets have Pester parameter validation tests. ~64 of 169 cmdlets lack **integration test** coverage. The integration test suite covers 107 cases:
- Connection, nodes, users, roles, API tokens, permissions
- VMs (CRUD, lifecycle, config, resize, clone, snapshots)
- Containers (CRUD, lifecycle, config, snapshots)
- Storage (CRUD, content, upload, download)
- Network (CRUD, apply), SDN (zones, VNets, subnets, IPAM, DNS, controllers)
- Templates (convert, clone, remove), cloud-init, tasks
- Firewall (rules, aliases, IP sets), backup jobs, OVA import, guest agent

**Untested in integration**: Guest file operations, node config/DNS, pool management, some storage operations, SDN update cmdlets.

---

## Phase 4b — CI Integration Test Results

### Last Run Summary

| Field | Value |
|---|---|
| Run ID | 23454616043 |
| Branch | main |
| Conclusion | **failure** |
| Created | 2026-03-23T18:51:46Z |
| Head SHA | 68dadbf |

### PVE 9: 104 passed, 2 failed, 1 skipped

| Finding ID | Test | Failure Message | Platform | PVE | Severity |
|---|---|---|---|---|---|
| F080 | Should restart a container | NullReferenceException — ConfirmImpact.High prompts for confirmation in non-interactive CI | ubuntu/pwsh | 9 | High |
| F081 | Should clone a container | PveApiException: Cannot do full clones on a running container without snapshots | ubuntu/pwsh | 9 | Medium |

**Root cause**: F062 fix (adding ConfirmImpact.High to Restart-PveContainer) is correct behavior, but the integration test at line 983 does not pass `-Confirm:$false`. The container remained running after the restart failed, causing the subsequent clone test to also fail.

**Fix**: Add `-Confirm:$false` to the Restart-PveContainer call in the integration test.

### PVE 8: Infrastructure failure

| Finding ID | Test | Failure Message | Platform | PVE | Severity |
|---|---|---|---|---|---|
| F082 | Initialize containers | Docker image ghcr.io/goodolclint/psproxmoxve-integration:68dadbf... not found | self-hosted | 8 | Low |

**Root cause**: Container image for this commit SHA was not available in GHCR. No actual test logic failures on PVE 8.

---

## Phase 5 — Security Review

| Finding ID | Area | Severity | Status | Description |
|---|---|---|---|---|
| F031 | Secret scanning | Low | Open | Hardcoded test password in CI workflow — masked, disposable VM |
| — | Credential handling | — | Pass | PSCredential, SecureString with Marshal cleanup (D002) |
| — | TLS/HTTPS | — | Pass | Enforced by default, SkipCertificateCheck opt-in with warning |
| — | URL encoding | — | Pass | Uri.EscapeDataString on all path params (D003) |
| — | Dependencies | — | Pass | Newtonsoft.Json 13.0.3, SharpCompress 0.38.0 — current |
| — | Debug scripts | — | Pass | Placeholder tokens only (F052 resolved) |
| — | Verbose logging | — | Pass | No credentials in WriteVerbose/WriteDebug output |

No new security findings. All security-related DECISIONS.md entries (D002, D003) upheld.

---

## Phase 6 — PSGallery Publication Readiness

| Finding ID | Check | Pass/Fail | Notes |
|---|---|---|---|
| — | ModuleVersion | Pass | 0.1.0 |
| — | GUID | Pass | a3f7c2d1-84e5-4b9f-a061-3e2d8c5f1a7b |
| — | Author | Pass | goodolclint |
| — | Description | Pass | Descriptive |
| — | PowerShellVersion | Pass | 5.1 |
| — | CompatiblePSEditions | Pass | Desktop, Core |
| — | LicenseUri | Pass | MIT license linked |
| — | ProjectUri | Pass | GitHub repo linked |
| — | Tags | Pass | 8 relevant tags |
| — | ReleaseNotes | Pass | Present in PSData |
| F021 | IconUri | Fail | Missing — cosmetic only |
| — | Prerelease | Pass | 'preview' — appropriate for current state |
| — | CmdletsToExport | Pass | 169 cmdlets listed |
| — | AliasesToExport | Pass | 7 aliases |
| — | RequiredAssemblies | Pass | PSProxmoxVE.Core.dll, Newtonsoft.Json.dll |
| — | FormatsToProcess | Pass | PSProxmoxVE.format.ps1xml |
| — | MAML help | Pass | PSProxmoxVE.dll-Help.xml |
| — | Markdown docs | Pass | 170 files in docs/cmdlets/ |
| — | Publish workflow | Pass | Tag-triggered with PS 5.1 smoke test (threshold >= 150) |
| — | netstandard2.0 only | Pass | Both publishable projects (D009) |

---

## Phase 7 — Community & Repo Maintenance Standards

| Finding ID | Check | Pass/Fail | Notes |
|---|---|---|---|
| — | README quality | Pass | Comprehensive with examples, badges, version table |
| — | CONTRIBUTING.md | Pass | Dev setup, coding standards, PR process |
| F083 | CONTRIBUTING.md SDK version | Fail | References .NET SDK 9.0+ (should be 10.0+) |
| — | CODE_OF_CONDUCT.md | Pass | Contributor Covenant |
| — | SECURITY.md | Pass | Vulnerability disclosure policy |
| — | DECISIONS.md | Pass | 12 active decisions, linked from CLAUDE.md |
| — | Issue templates | Pass | Bug report + feature request (YAML) + config.yml |
| — | PR template | Pass | Present |
| — | CODEOWNERS | Pass | Present |
| — | .gitattributes | Pass | Line ending normalization |
| — | .editorconfig | Pass | Present |
| — | dependabot.yml | Pass | NuGet + GitHub Actions (weekly) |
| — | CHANGELOG.md | Pass | Keep a Changelog format |
| — | Commit conventions | Pass | Conventional Commits |

---

## Phase 9 — Prioritized Recommendations

### Critical

*(None)*

### High

| Finding ID | What | Where | Why | Fix |
|---|---|---|---|---|
| F080 | Restart-PveContainer CI failure | Integration.Tests.ps1:983 | ConfirmImpact.High prompts in non-interactive CI | Add `-Confirm:$false` to test |

### Medium

| Finding ID | What | Where | Why | Fix |
|---|---|---|---|---|
| F046 | Integration test coverage gaps | tests/ | ~64 cmdlets untested end-to-end | Add tests incrementally |
| F059 | VM/CT-level firewall 0% | — | ~44 endpoints for per-VM/CT firewall | Implement VM/CT firewall cmdlets |
| F060 | Cluster config 0% | — | 10 endpoints for cluster management | Implement cluster config cmdlets |
| F061 | PVE 9.0 endpoints 0% | — | 42 new endpoints | Prioritize HA rules and bulk actions |
| F081 | Copy-PveContainer CI failure | Integration.Tests.ps1:998 | Cascading from F080 | Resolves when F080 is fixed |

### Low

| Finding ID | What | Where | Why | Fix |
|---|---|---|---|---|
| F021 | No IconUri | PSProxmoxVE.psd1 | Cosmetic PSGallery appearance | Add IconUri |
| F031 | Hardcoded test password | integration-tests.yml | Masked, disposable VM | Move to secret (optional) |
| F053 | HA subsystem 0% | — | 21+ HA endpoints | Implement HA cmdlets |
| F054 | Ceph subsystem 0% | — | 40 Ceph endpoints | Implement Ceph cmdlets |
| F067 | Disk management 0% | — | 18 disk endpoints | Implement disk cmdlets |
| F068 | Notifications 0% | — | 32 notification endpoints | Implement notification cmdlets |
| F069 | ACME/Certificates 0% | — | 23 ACME endpoints | Implement ACME cmdlets |
| F082 | PVE 8 Docker image failure | integration-tests.yml | Infrastructure issue | Investigate GHCR push |
| F083 | CONTRIBUTING.md SDK version | CONTRIBUTING.md:9 | Wrong SDK version listed | Change "9.0+" to "10.0+" |
