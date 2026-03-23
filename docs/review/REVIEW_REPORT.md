# PSProxmoxVE Comprehensive Review Report

```
Scan date:           2026-03-22
Prior report date:   2026-03-22 (scan-3)
PVE API spec date:   2026-03-21T15:04:50.641Z
PVE API spec SHA256: 4af79be30166209a4714b771f65e1e9540c5b738f414ff30c98454402e29d030
PVE version hint:    (not set in spec)
Total API endpoints: 646
Findings DB:         docs/review/findings.json (F001–F076)
Open findings:       22 (before scan) → 27 (after scan)
New this scan:       6   Resolved this scan: 1   Regressed: 0
```

## Executive Summary

- **Delta**: 1 resolved | 6 new | 0 regressed | 27 open
- **API drift**: 0 breaking changes in PVE 9.x affecting current cmdlets | 42 new PVE 9.0 endpoints unimplemented
- **API coverage**: 169 cmdlets covering ~180 of 646 endpoints (28%)
- **Critical open**: F058 — 5 cmdlets with infinite-loop task polling (D001 violation)
- **New high**: F073 — build.yml references net9.0 but test project targets net10.0 (workflow will fail)
- **New medium**: F071 — ~15 cmdlets missing Uri.EscapeDataString (D003 violation)
- **Resolved**: F040 — Broad exception catches now properly filtered
- **All D007/D005/D008/D011/D012 decisions hold**: sealed, OutputType, Newtonsoft-only, verb constants all pass 169/169
- **Security**: All password params use SecureString, all service URLs properly encoded, no credential logging
- **Testing**: 100% Pester parameter validation, ~105/169 integration coverage

---

## Phase 1 — Repository Inventory & Structure

### Project Layout

| Component | Path | Framework |
|---|---|---|
| PSProxmoxVE (cmdlets) | `src/PSProxmoxVE/` | netstandard2.0; net9.0; net48 |
| PSProxmoxVE.Core (services) | `src/PSProxmoxVE.Core/` | netstandard2.0; net9.0; net48 |
| xUnit Tests | `tests/PSProxmoxVE.Core.Tests/` | net10.0; net48 |
| Pester Tests | `tests/PSProxmoxVE.Tests/` | PowerShell 5.1+ |
| Integration Infra | `tests/infrastructure/` | Terraform + Docker |

### CI/CD Workflows

| Workflow | Trigger | Status |
|---|---|---|
| build.yml | push/PR to main | ⚠ References net9.0 (F073) |
| unit-tests.yml | push/PR to main | ✓ Matrix: Win PS 5.1, Win PS 7.5, Ubuntu PS 7.5, macOS PS 7.5 |
| integration-tests.yml | push/PR + dispatch | ✓ Self-hosted runner, nested PVE 8 + PVE 9 |
| publish.yml | tag push `v*` | ⚠ Low smoke threshold (F074) |

### Documentation

| File | Present | Notes |
|---|---|---|
| README.md | ✓ | Comprehensive with badges, cmdlet reference |
| CHANGELOG.md | ✓ | Keep a Changelog format |
| CONTRIBUTING.md | ✓ | Dev setup, coding standards, PR process |
| LICENSE | ✓ | MIT |
| CODE_OF_CONDUCT.md | ✓ | Contributor Covenant adapted |
| SECURITY.md | ✓ | Vulnerability disclosure + response SLA |
| DECISIONS.md | ✓ | 12 decisions (D001–D012) |
| .editorconfig | ✓ | 4-space C#/PS, 2-space XML/JSON |
| .gitignore | ✓ | |
| .gitattributes | ✓ | |

### Missing Items

| Finding ID | Item | Notes |
|---|---|---|
| F075 | ~88 cmdlets lack markdown help docs | 81 of 169 covered |
| F065 | No config.yml for issue templates | |
| F066 | No CODEOWNERS file | |
| F076 | No dependabot/renovate | |

---

## Phase 2 — PVE API Coverage Audit

### Coverage by Functional Area

| Area | Total Endpoints | Covered | % | Notes |
|---|---|---|---|---|
| backup | ~6 | ~6 | 100% | Full coverage |
| tasks | ~5 | ~5 | 100% | Full coverage |
| storage_config | ~5 | ~4 | 80% | |
| access_domains | ~6 | ~5 | 83% | |
| roles | ~5 | ~4 | 80% | |
| access_groups | ~5 | ~4 | 80% | |
| firewall | ~40 | ~30 | 75% | Cluster-level excellent |
| networking | ~7 | ~5 | 71% | |
| pools | ~7 | ~5 | 71% | |
| users | ~12 | ~8 | 67% | |
| storage | ~19 | ~12 | 63% | |
| sdn | ~59 | ~25 | 42% | Missing fabrics (PVE 9.0) |
| containers | ~62 | ~25 | 40% | Missing VNC/SPICE, firewall |
| vms | ~97 | ~35 | 36% | Missing VNC/SPICE, RRD, agent sub-commands |
| access | ~15 | ~3 | 20% | Missing TFA, OpenID |
| nodes | ~58 | ~10 | 17% | Missing Ceph, disks, apt, services |
| cluster | ~81 | ~3 | 4% | Only resources/status |
| **ha** | **~21** | **0** | **0%** | F053 |
| **ceph** | **~40** | **0** | **0%** | F054 |
| **cluster_config** | **~10** | **0** | **0%** | F060 |
| **disks** | **~18** | **0** | **0%** | F067 |
| **notifications** | **~32** | **0** | **0%** | F068 |
| **acme/certs** | **~23** | **0** | **0%** | F069 |
| **replication** | **~5** | **0** | **0%** | |
| **services** | **~7** | **0** | **0%** | |
| **TOTAL** | **646** | **~180** | **28%** | |

### PVE 9.0 New Endpoints (F061)

42 new endpoints in PVE 9.0 — **0 implemented**:

- **SDN Fabrics** (~17 endpoints) — entirely new subsystem for fabric networking
- **Cluster Bulk Actions** (~6 endpoints) — cluster-wide guest start/shutdown/suspend/migrate
- **HA Affinity Rules** (~5 endpoints) — new HA group affinity management
- **Miscellaneous** (~14 endpoints) — scattered across nodes, storage, access

### API Drift

No breaking changes detected in PVE 9.x affecting currently implemented cmdlets. All existing cmdlets should continue to work against PVE 9.x servers.

### High-Value Gaps

1. **HA (High Availability)** — 21 endpoints, essential for production clusters
2. **Ceph** — 40 endpoints, critical for hyperconverged infrastructure
3. **Cluster Configuration** — 10 endpoints for cluster create/join
4. **VNC/SPICE Console Access** — frequently requested for remote management
5. **Cluster Notifications** — new in PVE 8.1+, essential for monitoring automation
6. **`GET /cluster/nextid`** — critical for scripted VM creation

---

## Phase 3 — Code Quality & Best Practices

### DECISIONS.md Compliance

| Decision | Rule | Status | Notes |
|---|---|---|---|
| D001 | TaskService.WaitForTask | ⛔ VIOLATION | F058: 5 cmdlets still use while(true) |
| D002 | SecureString for passwords | ✓ PASS | All 7 password cmdlets |
| D003 | Uri.EscapeDataString | ⛔ VIOLATION | F071: ~15 cmdlets bypass services |
| D004 | No bare catch blocks | ✓ PASS | Zero instances in src/ |
| D005 | OutputType on all cmdlets | ✓ PASS | 169/169 |
| D006 | ConfirmImpact.High | ⚠ PARTIAL | F062, F063: container Restart/Suspend |
| D007 | All cmdlets sealed | ✓ PASS | 169/169 |
| D008 | Newtonsoft.Json only | ✓ PASS (source) | F072: STJ dependency in csproj |
| D009 | netstandard2.0 for publish | ⚠ PARTIAL | F047: net9.0 still in targets |
| D010 | VmId nullable + ValidateRange | ✓ PASS | |
| D011 | Verb class constants | ✓ PASS | |
| D012 | Magic strings extracted | ✓ PASS | |

### Findings

| Finding ID | File(s) | Severity | Status | Description |
|---|---|---|---|---|
| F058 | 5 container/storage cmdlets | Critical | Open | while(true) infinite loops without timeout (D001) |
| F073 | build.yml, test .csproj | High | **New** | build.yml uses net9.0 but test project targets net10.0 |
| F071 | ~15 cmdlet files | Medium | **New** | Missing Uri.EscapeDataString on inline URL paths (D003) |
| F062 | RestartPveContainerCmdlet.cs | Medium | Open | Missing ConfirmImpact.High (D006) |
| F063 | SuspendPveContainerCmdlet.cs | Medium | Open | Missing ConfirmImpact.High (D006) |
| F047 | Both publishable .csproj | Medium | Open | net9.0 target is EOL (D009) |
| F048 | ~216 call sites | Medium | Open | Sync-over-async — accepted PS 5.1 tradeoff |
| F045 | ~207 new PveHttpClient | Medium | Open | Per-call HTTP client — accepted design |
| F072 | PSProxmoxVE.Core.csproj | Low | **New** | Unnecessary System.Text.Json dependency |
| F064 | PSProxmoxVE.csproj | Low | Open | SMA pinned to 7.4.0 |
| F040 | PveCmdletBase, services | Medium | **Resolved** | Catches now properly filtered with `when` |

---

## Phase 4 — Testing Coverage Analysis

### Test Inventory

| Type | Framework | Files | Coverage |
|---|---|---|---|
| xUnit (C#) | xunit 2.7.0 + Moq | 15 test files | Models + authentication only |
| Pester (PS) | Pester 5 | 50 test files | 169/169 cmdlet parameter validation |
| Integration (PS) | Pester 5 | 1 file (1544 lines) | ~105/169 cmdlets |

### Integration Test Coverage Gaps (F046)

**~64 cmdlets lack integration tests**, including:

- **VM ops**: Move-PveVm, Move-PveVmDisk, Remove-PveVmDisk
- **Guest agent**: Get-PveVmGuestOsInfo, Get-PveVmGuestFsInfo, Read/Write-PveVmGuestFile, Set-PveVmGuestPassword, Invoke-PveVmGuestFsTrim
- **Storage**: Set-PveStorage, Get-PveStorageStatus, Remove/Set-PveStorageContent, New-PveStorageDisk
- **Access**: Set-PveRole, Set-PveApiToken, Set-PvePassword, all Group/Domain CRUD
- **SDN**: All Set-PveSdn*, Invoke-PveSdnApply, New-PveSdn{Ipam,Dns,Controller}
- **Nodes**: Get/Set-PveNodeConfig, Get/Set-PveNodeDns, Start/Stop-PveNodeVms
- **Pools**: All 4 pool cmdlets
- **Cluster**: Get-PveClusterResource
- **Containers**: Move/Suspend/Resume/Resize, Move-PveContainerVolume, New-PveContainerTemplate
- **Firewall**: Set operations, groups, options, refs
- **Backup**: New-PveBackup, Get-PveBackupInfo
- **Tasks**: Get-PveTaskList, Stop-PveTask

### Quality Observations

**Strengths**: 100% Pester parameter coverage, real PVE 9 JSON fixtures in xUnit, well-structured integration tests with cleanup.

**Weaknesses**: No mock-based service unit tests, xUnit covers only models/auth (no service logic), integration test is a single monolithic file, some tests depend on sequential state.

---

## Phase 5 — Security Review

| Area | Status | Details |
|---|---|---|
| Password params use SecureString | ✓ PASS | All 7 cmdlets |
| SecureString freed after use | ✓ PASS | try/finally with ZeroFreeGlobalAllocUnicode |
| No credential logging | ✓ PASS | WriteVerbose never logs passwords |
| HTTPS by default | ✓ PASS | All API calls use https:// |
| SkipCertificateCheck opt-in | ✓ PASS | Warning emitted when used |
| Uri.EscapeDataString in services | ✓ PASS | 100+ usages, consistent |
| Uri.EscapeDataString in cmdlets | ⛔ FAIL | F071: ~15 cmdlets bypass services |
| ValidateRange on VmId | ✓ PASS | 90+ instances, consistent |
| No bare catch blocks | ✓ PASS | Zero in src/ |
| F031: Hardcoded test password | ⚠ Accepted | Testpass123! in CI, masked, disposable VM |

---

## Phase 6 — PSGallery Publication Readiness

| Finding ID | Check | Pass/Fail | Notes |
|---|---|---|---|
| — | ModuleVersion | ✓ Pass | 0.1.0 (publish workflow overrides from tag) |
| — | Author/Company | ✓ Pass | goodolclint / Worklab |
| — | Description | ✓ Pass | Descriptive |
| — | LicenseUri | ✓ Pass | Present |
| — | ProjectUri | ✓ Pass | Present |
| — | Tags | ✓ Pass | 8 tags including ProxmoxVE8/9 |
| — | ReleaseNotes | ✓ Pass | Present |
| — | CmdletsToExport | ✓ Pass | Explicit list (~169) |
| — | CompatiblePSEditions | ✓ Pass | Desktop, Core |
| — | PowerShellVersion | ✓ Pass | 5.1 |
| — | Prerelease | ✓ Pass | 'preview' |
| F021 | IconUri | ✗ Fail | Missing — PSGallery listing will lack icon |
| F047 | netstandard2.0 only for publish | ⚠ Warn | net9.0 in csproj but publish builds netstandard2.0 only |
| F073 | Build workflow alignment | ✗ Fail | build.yml uses net9.0, test project uses net10.0 |
| F074 | Publish smoke test | ⚠ Warn | Threshold 60 too low for 169 cmdlets |
| F070 | PS 5.1 smoke test | ✗ Fail | No Windows PS 5.1 validation before publish |
| F075 | Cmdlet help documentation | ⚠ Warn | 81/169 have markdown docs |

---

## Phase 7 — Community & Repo Maintenance

| Finding ID | Check | Pass/Fail | Notes |
|---|---|---|---|
| — | Bug report template | ✓ Pass | YAML format, collects PVE/PS/OS versions |
| — | Feature request template | ✓ Pass | YAML format |
| F065 | Issue template config.yml | ✗ Fail | Missing — blank issues uncontrolled |
| — | PR template | ✓ Pass | Comprehensive checklist |
| — | CONTRIBUTING.md | ✓ Pass | Good quality |
| — | CODE_OF_CONDUCT.md | ✓ Pass | Contributor Covenant adapted |
| — | SECURITY.md | ✓ Pass | 48h response SLA |
| — | DECISIONS.md | ✓ Pass | 12 decisions, linked from CLAUDE.md |
| — | Commit conventions | ✓ Pass | Conventional Commits consistently used |
| F066 | CODEOWNERS | ✗ Fail | Missing |
| F076 | Dependency automation | ✗ Fail | No dependabot/renovate |
| — | Branch protection | ? | Cannot verify from CLI |

---

## Phase 9 — Prioritized Recommendations

### 🔴 Critical

| Finding ID | What | Where | Why | Fix |
|---|---|---|---|---|
| F058 | Infinite loop task-polling | 5 container/storage cmdlets | Cmdlets hang forever if task stalls | Replace private WaitForTask with TaskService.WaitForTask |

### 🟠 High

| Finding ID | What | Where | Why | Fix |
|---|---|---|---|---|
| F073 | build.yml / test project framework mismatch | build.yml, test .csproj | CI workflow will fail | Update build.yml to use net10.0 or remove net9.0 from publishable csproj |
| F053 | HA subsystem 0% coverage | — | Essential for production clusters | Implement HA resource/group CRUD cmdlets |
| F054 | Ceph subsystem 0% coverage | — | Critical for hyperconverged | Implement Ceph OSD/pool/mon cmdlets |

### 🟡 Medium

| Finding ID | What | Where | Why | Fix |
|---|---|---|---|---|
| F071 | Missing Uri.EscapeDataString | ~15 cmdlet files | D003 violation, URL injection risk | Add Uri.EscapeDataString to all inline URL paths |
| F062 | RestartPveContainer no ConfirmImpact.High | RestartPveContainerCmdlet.cs | D006 inconsistency | Add ConfirmImpact = ConfirmImpact.High |
| F063 | SuspendPveContainer no ConfirmImpact.High | SuspendPveContainerCmdlet.cs | D006 inconsistency | Add ConfirmImpact = ConfirmImpact.High |
| F047 | net9.0 target framework EOL | Both publishable .csproj | .NET 9 EOL May 2025 | Remove net9.0, keep netstandard2.0 (+ optionally add net10.0) |
| F074 | Publish smoke test threshold too low | publish.yml | Won't catch cmdlet regressions | Raise threshold to ~150 |
| F075 | ~88 cmdlets lack help docs | docs/cmdlets/ | Poor user experience | Generate docs for missing cmdlets |
| F046 | Integration test gaps | Integration.Tests.ps1 | 64 cmdlets untested end-to-end | Prioritize destructive/lifecycle cmdlets |
| F059 | VM/CT-level firewall 0% | — | Per-guest firewall is common use case | Implement VM/CT firewall rule cmdlets |
| F060 | Cluster config 0% | — | Multi-node automation | Implement cluster create/join cmdlets |
| F061 | PVE 9.0 endpoints 0% | — | 42 new endpoints unimplemented | Prioritize bulk actions and SDN fabrics |
| F048 | Sync-over-async | ~216 call sites | Theoretical deadlock risk | Accepted for PS 5.1 compat — no fix needed |
| F045 | HttpClient per-call | ~207 instances | Socket exhaustion risk | Consider IHttpClientFactory long-term |
| F070 | No PS 5.1 smoke test | publish.yml | Desktop compatibility untested | Add Windows PS 5.1 step to publish workflow |

### 🟢 Low

| Finding ID | What | Where | Why | Fix |
|---|---|---|---|---|
| F031 | Hardcoded test password | CI workflow, Terraform | Accepted risk for disposable VMs | — |
| F021 | No IconUri | PSProxmoxVE.psd1 | Cosmetic PSGallery listing | Add icon to repo and reference in manifest |
| F064 | SMA pinned to 7.4.0 | PSProxmoxVE.csproj | Review with net10.0 migration | Update when removing net9.0 |
| F072 | Unnecessary STJ dependency | Core.csproj | D008: Newtonsoft only | Remove System.Text.Json PackageReference |
| F065 | No issue template config.yml | .github/ISSUE_TEMPLATE/ | Blank issues uncontrolled | Add config.yml |
| F066 | No CODEOWNERS | root | No auto-review assignment | Add CODEOWNERS |
| F067 | Disk management 0% | — | Storage provisioning | Implement when needed |
| F068 | Notifications 0% | — | Monitoring automation | Implement when needed |
| F069 | ACME/Certificates 0% | — | TLS automation | Implement when needed |
| F076 | No dependabot/renovate | .github/ | Dependency drift | Add dependabot.yml |
