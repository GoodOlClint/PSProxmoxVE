# PSProxmoxVE Module Review Report — Scan 7

```
Scan date:           2026-03-23
Prior report date:   2026-03-23 (scan-6)
PVE API spec date:   2026-03-21T15:04:50.641Z
PVE API spec SHA256: 4af79be30166209a4714b771f65e1e9540c5b738f414ff30c98454402e29d030
PVE version hint:    N/A (not set in spec)
Total API endpoints: 646
Findings DB:         docs/review/findings.json (F001–F083)
Open findings:       12 (before scan) → 11 open + 1 regressed (after scan)
New this scan:       0  Resolved this scan: 1 (F082)  Regressed: 1 (F050)
Last CI run:         integration-tests.yml | success | 2026-03-23T21:42:02Z | https://github.com/goodolclint/PSProxmoxVE/actions/runs/23461565300
```

## Executive Summary

- **Delta**: 1 resolved (F082) | 0 new | 1 regressed (F050) | 11 open + 1 regressed
- **API drift**: 0 breaking changes in implemented endpoints | 42 PVE 9.0 endpoints unimplemented (F061)
- **CI**: Last integration run: **PASSED** | 0 test failures (both PVE 8 and PVE 9)
- **Code quality**: 11 of 12 DECISIONS.md entries upheld — **1 regression** (D003: URL encoding in 5 service files)
- Module has 169 cmdlets, ~170 markdown docs, netstandard2.0 targeting, 1,749 total tests (374 xUnit + 1,268 Pester + 107 integration)
- PSGallery manifest complete except IconUri (F021)
- All community/repo standards pass (issue templates, CONTRIBUTING, SECURITY, CODE_OF_CONDUCT, CODEOWNERS, etc.)

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
| CONTRIBUTING.md | Yes | Dev setup, coding standards, PR process |
| LICENSE | Yes | MIT |
| CODE_OF_CONDUCT.md | Yes | Contributor Covenant 2.1 |
| SECURITY.md | Yes | Vulnerability disclosure policy |
| DECISIONS.md | Yes | 12 active decisions (D001–D012) |
| .editorconfig | Yes | Root config with per-filetype overrides |
| .gitignore | Yes | Comprehensive |
| .gitattributes | Yes | LF enforcement, binary handling, diff drivers |
| CODEOWNERS | Yes | `* @goodolclint` |
| dependabot.yml | Yes | Automated dependency updates |
| Issue templates | Yes | Bug report + feature request (YAML forms) + config.yml |
| PR template | Yes | Summary, type, checklist, API endpoints, testing |
| CI workflows | Yes | build.yml, unit-tests.yml, integration-tests.yml, publish.yml |
| Review system | Yes | docs/review/findings.json, docs/review/REVIEW_REPORT.md |
| Cmdlet docs | Yes | ~170 markdown files in docs/cmdlets/ |

### Missing Items

| Finding ID | Item | Notes |
|---|---|---|
| F021 | IconUri in manifest PSData | PSGallery listings display placeholder without it |

---

## Phase 2 — PVE API Coverage Audit

### Coverage by Functional Area

| Area | Spec Endpoints | Covered | % | Notes |
|------|---------------|---------|---|-------|
| VMs (qemu) | 97 | ~30 | 31% | Core CRUD, lifecycle, guest agent, disk ops, config |
| Containers (lxc) | 62 | ~18 | 29% | CRUD, lifecycle, snapshots, config, interfaces |
| Nodes | 77 | ~10 | 13% | List, status, config, DNS, startall/stopall |
| Firewall | 40 | ~22 | 55% | Rules, groups, aliases, IP sets, options, refs |
| Cluster | 79 | ~3 | 4% | Resources and status only |
| SDN | 60 | ~16 | 27% | Zones, VNets, subnets, IPAM, DNS, controllers |
| Storage | 24 | ~14 | 58% | CRUD, content, upload, download, allocate |
| Backup | 6 | 6 | 100% | Full coverage |
| Access/Users | 45 | ~22 | 49% | Users, groups, roles, domains, tokens, ACL, permissions |
| Tasks | 5 | ~4 | 80% | List, status, log, stop |
| Pools | 7 | ~5 | 71% | CRUD + list |
| HA | 21 | 0 | 0% | F053 — 🆕 Rules subsystem new in PVE 9.0 |
| Ceph | 40 | 0 | 0% | F054 |
| Notifications | 25+ | 0 | 0% | F068 — New in PVE 8.1+ |
| ACME/Certs | 23 | 0 | 0% | F069 |
| Disks | 18 | 0 | 0% | F067 |
| Replication | 5 | 0 | 0% | |
| Services | 7 | 0 | 0% | |
| **Total** | **646** | **~120** | **~18.6%** | |

### API Drift Table

No breaking changes detected in endpoints the module currently implements. All changes are additive (new optional parameters).

| Finding ID | Area | Change | PVE Version | Risk |
|---|---|---|---|---|
| F061 | HA rules | New `/cluster/ha/rules` subsystem | 9.0 | Additive |
| F061 | Bulk actions | New `/cluster/bulk-action` | 9.0 | Additive |
| F061 | SDN fabrics | New `/cluster/sdn/fabrics` | 9.0 | Additive |
| — | Various | 77 additive parameter changes across implemented endpoints | 8.x–9.x | Additive |

### High-Value Uncovered Endpoints

**Priority 1 — Commonly needed:**
- `GET /access/acl` — List ACLs (only PUT covered)
- `GET /cluster/nextid` — Get next free VM ID
- `GET /cluster/ha/resources` — HA resource management
- VNC/SPICE console proxy endpoints
- `GET /nodes/{node}/subscription` — Subscription status

**Priority 2 — Frequently useful:**
- `GET /nodes/{node}/syslog` / `journal` — Log access
- `GET /nodes/{node}/hardware` — PCI passthrough info
- `GET /nodes/{node}/scan/*` — Storage discovery
- `POST /access/domains/{realm}/sync` — LDAP/AD sync
- APT package management

---

## Phase 3 — Code Quality & Best Practices

### DECISIONS.md Compliance

| Decision | Status | Notes |
|---|---|---|
| D001 — TaskService.WaitForTask | ✅ Compliant | No inline polling loops in cmdlets |
| D002 — SecureString passwords | ✅ Compliant | All 6 password params use SecureString |
| D003 — URL encoding | ❌ **REGRESSED (F050)** | 20 instances in 5 service files |
| D004 — No bare catch blocks | ✅ Compliant | All catches are specific or filtered |
| D005 — OutputType on all cmdlets | ✅ Compliant | All 169 cmdlets have OutputType |
| D006 — ConfirmImpact.High | ✅ Compliant | All destructive cmdlets confirmed |
| D007 — Sealed cmdlet classes | ✅ Compliant | All 169 cmdlets sealed |
| D008 — Newtonsoft.Json only | ✅ Compliant | No System.Text.Json attributes |
| D009 — Framework targets | ✅ Compliant | netstandard2.0 / net10.0;net48 |
| D010 — VmId ValidateRange | ✅ Compliant | All VmId params validated |
| D011 — Verb class constants | ✅ Compliant | No string literals |
| D012 — Magic string constants | ✅ Compliant | Auth headers extracted |

### D003 Regression Detail (F050)

20 instances of missing `Uri.EscapeDataString()` across 5 service files:

| File | Methods Affected | Unescaped Params |
|---|---|---|
| NodeService.cs | 7 (GetNodeStatus, GetNodeConfig, SetNodeConfig, GetDns, SetDns, StartAll, StopAll) | `node` |
| NetworkService.cs | 5 node-network (GetNetworks, CreateNetwork, SetNetwork, RemoveNetwork, ApplyNetworkConfig) | `node`, `iface` |
| NetworkService.cs | 2 SDN (RemoveSdnZone, RemoveSdnVnet) | `zone`, `vnet` |
| CloudInitService.cs | 3 (GetCloudInitConfig, SetCloudInitConfig, RegenerateCloudInitImage) | `node` |
| TaskService.cs | 2 (GetTask, GetTaskLog) | `node` |
| TemplateService.cs | 1 (CreateTemplate) | `node` |

Properly escaped services (no issues): VmService, ContainerService, SnapshotService, FirewallService, PoolService, UserService, StorageService.

### Other Code Quality Findings

- No TODO/FIXME/HACK comments
- No commented-out code
- No unused using directives detected
- HttpClient lifecycle is well-managed (1:1 ownership, proper disposal)
- `PveCmdletBase.WaitForStatusTransition` power-state polling loop is distinct from task polling (D001 compliant)

---

## Phase 4 — Testing Coverage Analysis

### Test Summary

| Category | Files | Tests |
|---|---|---|
| xUnit (C# unit) | 25 | 374 |
| Pester (PS unit) | 48 | 1,268 |
| Pester (integration) | 1 | 107 |
| **Total** | **74** | **1,749** |

### Coverage Highlights

- **100% cmdlet unit test coverage** — all 169 cmdlets have Pester parameter/metadata tests
- **xUnit service tests**: UserService (78), StorageService (40), FirewallModel (24), BackupService (23), SdnModel (21)
- **32 JSON fixture files** from real PVE 8/9 API responses for model deserialization tests
- **Integration tests**: 107 tests across connection, nodes, VMs, containers, snapshots, storage, users, firewall, SDN, backup, templates, cloud-init, guest agent

### Coverage Gaps (F046)

| Gap | Severity | Detail |
|---|---|---|
| No VmService xUnit tests | Medium | VM ops (clone, migrate, import) have complex logic untested at service layer |
| No ContainerService xUnit tests | Medium | Similar complexity to VmService |
| No NetworkService xUnit tests | Low | Thin CRUD wrappers |
| No FirewallService xUnit tests | Low | Thin CRUD wrappers |
| Pool/Cluster cmdlets lack integration tests | Low | Simple read-only operations |

---

## Phase 4b — CI Integration Test Results

### Last CI Run

| Field | Value |
|---|---|
| Run ID | 23461565300 |
| Conclusion | **SUCCESS** |
| Date | 2026-03-23T21:42:02Z |
| SHA | `1faaba0` |
| Branch | main |

All 7 jobs passed: container-image, build, provision, test(8), test(9), cleanup, cleanup-images.

### CI Findings

| Finding ID | Test / Cmdlet | Status | Notes |
|---|---|---|---|
| F080 | Restart-PveContainer | Resolved (scan-6) | ConfirmImpact.High fix applied |
| F081 | Copy-PveContainer | Resolved (scan-6) | Cascade from F080 |
| F082 | PVE 8 Docker image | **Resolved (this scan)** | Container-image job now succeeds |

---

## Phase 5 — Security Review

| Finding ID | Area | File(s) | Severity | Status | Description |
|---|---|---|---|---|---|
| F050 | URL encoding | 5 service files | Medium | **Regressed** | 20 instances of unescaped path params (see Phase 3) |
| F031 | Secrets | terraform.tfvars, CI | Low | Open | Lab IPs in terraform.tfvars (gitignored); CI uses ${{ secrets.* }} properly |
| — | Credential handling | All password cmdlets | Pass | — | SecureString with Marshal try/finally throughout |
| — | TLS/HTTPS | PveHttpClient.cs | Pass | — | HTTPS-only, SkipCertificateCheck opt-in with warning |
| — | CSRF | PveHttpClient.cs | Pass | — | CSRFPreventionToken on all mutating requests |
| — | Bare catches | All .cs files | Pass | — | No D004 violations |
| — | Boundary generation | PveHttpClient.cs | Pass | — | Uses RandomNumberGenerator (CSPRNG) |
| — | Dependencies | Both .csproj | Pass | — | Newtonsoft.Json 13.0.3, SharpCompress 0.47.3 (current) |

---

## Phase 6 — PSGallery Publication Readiness

| Finding ID | Check | Pass/Fail | Notes |
|---|---|---|---|
| — | ModuleVersion | Pass | 0.1.0, dynamically set from git tag |
| — | GUID | Pass | Present and unique |
| — | Author/CompanyName | Pass | goodolclint / Worklab |
| — | Description | Pass | Descriptive, mentions PVE 8.x/9.x |
| — | PowerShellVersion | Pass | 5.1 minimum |
| — | CompatiblePSEditions | Pass | Desktop + Core |
| — | CmdletsToExport | Pass | Explicit list of 169 cmdlets |
| — | Tags | Pass | 8 tags for discoverability |
| — | LicenseUri/ProjectUri | Pass | GitHub links |
| F021 | IconUri | **Fail** | Not set — PSGallery shows placeholder |
| — | ReleaseNotes | Pass | Present in PSData |
| — | Framework targets | Pass | netstandard2.0 for both publishable projects |
| — | Publish workflow | Pass | Tag-triggered, 3-job pipeline with PS 5.1 smoke test |
| — | CHANGELOG | Pass | Keep a Changelog format |

---

## Phase 7 — Community & Repo Maintenance Standards

| Finding ID | Check | Pass/Fail | Notes |
|---|---|---|---|
| — | Bug report template | Pass | YAML form with version fields |
| — | Feature request template | Pass | YAML form with use case |
| — | Issue template config | Pass | Disables blank issues, links to Discussions |
| — | PR template | Pass | Checklist with build, tests, psd1, changelog |
| — | CONTRIBUTING.md | Pass | Complete dev setup and coding standards |
| — | CODE_OF_CONDUCT.md | Pass | Contributor Covenant 2.1 |
| — | SECURITY.md | Pass | Responsible disclosure instructions |
| — | DECISIONS.md | Pass | 12 decisions, linked from CLAUDE.md |
| — | CODEOWNERS | Pass | `* @goodolclint` |
| — | .editorconfig | Pass | Per-filetype overrides |
| — | .gitattributes | Pass | LF enforcement, binary handling |
| — | Commit conventions | Pass | Conventional Commits consistently used |
| — | Release process | Pass | Tag-based publish with GitHub Release |
| — | Dependabot | Pass | Automated dependency PRs |

---

## Phase 9 — Prioritized Recommendations

### 🔴 Critical

No critical findings.

### 🟠 High

No high findings.

### 🟡 Medium

| Finding ID | What | Where | Why | Fix |
|---|---|---|---|---|
| F050 | **Uri.EscapeDataString missing (REGRESSED)** | NodeService, NetworkService, CloudInitService, TaskService, TemplateService | D003 violation — defense-in-depth URL encoding missing on 20 path parameter interpolations | Add `Uri.EscapeDataString()` to all `node`, `iface`, `zone`, `vnet` params in URL paths |
| F046 | Integration test coverage gaps | tests/ | VmService, ContainerService lack xUnit service-layer tests; Pools/Cluster lack integration tests | Add xUnit tests for VmService and ContainerService |
| F059 | VM/CT-level firewall 0% coverage | — | Firewall endpoints exist for VMs and containers but no cmdlets implemented | Add VM/CT-scoped firewall cmdlets |
| F060 | Cluster config 0% coverage | — | Cluster options, join/create, notifications, metrics uncovered | Add cluster config cmdlets |
| F061 | PVE 9.0 endpoints 0% coverage | — | 42 new endpoints (HA rules, bulk actions, SDN fabrics) not implemented | Prioritize HA rules and bulk actions |

### 🟢 Low

| Finding ID | What | Where | Why | Fix |
|---|---|---|---|---|
| F021 | No IconUri in manifest | PSProxmoxVE.psd1 | PSGallery listing looks less professional | Add 128x128 PNG icon and set IconUri |
| F031 | Hardcoded test IPs | terraform.tfvars, CI | Lab IPs committed (gitignored locally) | Verify tfvars not tracked in git |
| F053 | HA subsystem 0% coverage | — | No HA cmdlets | Implement when HA rules (PVE 9.0) are prioritized |
| F054 | Ceph subsystem 0% coverage | — | Large specialized surface | Low priority unless Ceph users identified |
| F067 | Disk management 0% coverage | — | Node disk ops (LVM, ZFS, init) | Useful for provisioning workflows |
| F068 | Notifications 0% coverage | — | 25+ endpoints (PVE 8.1+) | Medium-value for operational users |
| F069 | ACME/Certificates 0% coverage | — | Certificate management | Low priority |
