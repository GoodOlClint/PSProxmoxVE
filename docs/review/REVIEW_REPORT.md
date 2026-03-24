# PSProxmoxVE Review Report — Scan 8

```
Scan date:           2026-03-24
Prior report date:   2026-03-23
PVE API spec date:   2026-03-21T15:04:50.641Z
PVE API spec SHA256: 4af79be30166209a4714b771f65e1e9540c5b738f414ff30c98454402e29d030
PVE version hint:    (not set)
Total API endpoints: 646
Findings DB:         docs/review/findings.json (F001–F084)
Open findings:       10 (before scan) → 11 (after scan, including 1 regressed)
New this scan:       1  Resolved this scan: 1 (F059)  Regressed: 1 (F039)
Last CI run:         integration-tests.yml | failure (provision infra) | 2026-03-24 | run 23511293737
                     (newer run in_progress — prior successful run: 23511223201)
```

## Executive Summary

- **Delta**: 1 resolved (F059) | 1 new (F084) | 1 regressed (F039) | 11 open (10 open + 1 regressed)
- **API drift**: 24 breaking parameter changes in PVE 9.0 (mostly cluster config `link[n]` type changes) | 42 new PVE 9.0 endpoints unimplemented
- **CI**: Last completed integration run: FAILED (provision infrastructure — cloud image download race condition, already resolved in subsequent run) | 0 test failures
- Module has **169 cmdlets** covering ~155 of 646 API endpoints (**24% coverage**)
- All prior code quality findings (F032–F044, F049–F051, F058, F062–F063) remain resolved — no regressions detected
- All DECISIONS.md patterns verified: no bare catches, no System.Text.Json, no plain string passwords, no inline task polling, URL encoding applied, all cmdlets sealed with OutputType
- PSGallery publication readiness is high — manifest complete, publish workflow in place, PS 5.1 smoke test passes
- 12 open findings: 1 regression (F039 bare catch blocks in VmService + ImportPveOvaCmdlet), 1 new security finding (F084 PveSession exposes auth secrets), API coverage gaps, and manifest cosmetic issue (F021 IconUri)

---

## Phase 1 — Repository Inventory & Structure

### Project Layout

| Item | Present | Path | Notes |
|---|---|---|---|
| Solution file | Yes | PSProxmoxVE.sln | 2 src projects + 1 test project |
| Module project | Yes | src/PSProxmoxVE/ | netstandard2.0, 169 cmdlets |
| Core project | Yes | src/PSProxmoxVE.Core/ | netstandard2.0, services + models |
| xUnit tests | Yes | tests/PSProxmoxVE.Core.Tests/ | net10.0 + net48 |
| Pester tests | Yes | tests/PSProxmoxVE.Tests/ | 30+ test files |
| Integration tests | Yes | tests/PSProxmoxVE.Tests/Integration/ | PVE 8 + PVE 9 |
| Module manifest | Yes | src/PSProxmoxVE/PSProxmoxVE.psd1 | v0.1.0-preview |
| MAML help | Yes | src/PSProxmoxVE/PSProxmoxVE.dll-Help.xml | Generated |
| Format file | Yes | src/PSProxmoxVE/PSProxmoxVE.format.ps1xml | Custom formatting |
| Cmdlet docs | Yes | docs/cmdlets/ | 169 markdown files |
| README.md | Yes | README.md | Comprehensive, badges |
| CHANGELOG.md | Yes | CHANGELOG.md | Keep-a-Changelog format |
| CONTRIBUTING.md | Yes | CONTRIBUTING.md | Development setup + guidelines |
| LICENSE | Yes | LICENSE | MIT |
| CODE_OF_CONDUCT.md | Yes | CODE_OF_CONDUCT.md | Contributor Covenant |
| SECURITY.md | Yes | SECURITY.md | Security policy |
| DECISIONS.md | Yes | DECISIONS.md | 12 architectural decisions |
| CODEOWNERS | Yes | CODEOWNERS | @goodolclint |
| .editorconfig | Yes | .editorconfig | Consistent style |
| .gitignore | Yes | .gitignore | Comprehensive |
| .gitattributes | Yes | .gitattributes | Present |
| Issue templates | Yes | .github/ISSUE_TEMPLATE/ | Bug report + feature request + config.yml |
| PR template | Yes | .github/pull_request_template.md | Present |
| Dependabot | Yes | .github/dependabot.yml | NuGet + GitHub Actions weekly |

### CI/CD Workflows

| Workflow | File | Trigger | Status |
|---|---|---|---|
| Build | build.yml | push/PR to main | matrix: windows+ubuntu, net48+net10.0 |
| Unit Tests | unit-tests.yml | push/PR to main | Pester on PS 5.1, 7.5 (win/ubuntu/macos) |
| Integration Tests | integration-tests.yml | push to main | Self-hosted, PVE 8+9, concurrency group |
| Publish | publish.yml | tag push (v*) | PSGallery + GitHub Release |

### Missing Items

| Finding ID | Item | Notes |
|---|---|---|
| F021 | IconUri in manifest PSData | Cosmetic — recommended for PSGallery listing |

---

## Phase 2 — PVE API Coverage Audit

### Coverage by Functional Area

| Area | Total Endpoints | Covered | % | New in 9.0 (unimpl) | Notes |
|---|---|---|---|---|---|
| access | 15 | 7 | 47% | 1 | API tokens, permissions, password |
| access_domains | 6 | 4 | 67% | 0 | CRUD complete |
| access_groups | 5 | 4 | 80% | 0 | CRUD complete |
| acl | 2 | 0 | 0% | 0 | |
| acme | 15 | 0 | 0% | 0 | F069 |
| apt | 8 | 0 | 0% | 0 | Node package management |
| backup | 6 | 6 | 100% | 0 | Full coverage |
| ceph | 40 | 0 | 0% | 0 | F054 |
| certificates | 8 | 0 | 0% | 0 | F069 |
| cluster | 77 | ~1 | 1% | 6 | Only Get-PveClusterResource |
| cluster_config | 10 | 0 | 0% | 0 | F060 |
| containers | 62 | ~25 | 40% | 1 | Good lifecycle coverage |
| disks | 18 | 0 | 0% | 0 | F067 |
| firewall | 40 | ~20 | 50% | 0 | All levels (cluster/node/VM/CT) via Level param; F059 resolved |
| ha | 21 | 0 | 0% | 5 | F053 |
| metrics | 7 | 0 | 0% | 0 | |
| networking | 7 | 5 | 71% | 0 | Good coverage |
| nodes | 75 | ~8 | 11% | 11 | Basic ops covered |
| other | 1 | 0 | 0% | 0 | |
| pools | 7 | 5 | 71% | 0 | CRUD + update |
| replication | 5 | 0 | 0% | 0 | |
| roles | 5 | 4 | 80% | 0 | CRUD complete |
| sdn | 60 | ~19 | 32% | 16 | Zones/vnets/subnets/IPAM/DNS/ctrl |
| services | 7 | 0 | 0% | 0 | Node service management |
| storage | 19 | ~11 | 58% | 1 | Good coverage |
| storage_config | 5 | 0 | 0% | 0 | Separate from storage CRUD |
| tasks | 5 | 4 | 80% | 0 | Good coverage |
| users | 12 | 4 | 33% | 0 | CRUD only |
| version | 1 | 1 | 100% | 0 | Via Connect-PveServer |
| vms | 97 | ~30 | 31% | 1 | Good lifecycle + guest agent |
| **TOTAL** | **646** | **~155** | **~24%** | **42** | |

### API Drift — PVE 9.0 Breaking Changes

24 endpoints have breaking parameter changes in PVE 9.0. Most are `link[n]` type changes on cluster config endpoints (not covered by this module). Relevant changes affecting implemented endpoints:

| Endpoint | Change | Module Cmdlet | Risk |
|---|---|---|---|
| POST /nodes/{node}/qemu | `machine` param type changed | New-PveVm | Low |
| POST /nodes/{node}/qemu/{vmid}/config | param changes | Set-PveVmConfig | Low |
| PUT /nodes/{node}/qemu/{vmid}/config | param changes | Set-PveVmConfig | Low |
| POST /nodes/{node}/qemu/{vmid}/status/start | param changes | Start-PveVm | Low |
| POST /nodes/{node}/lxc | param changes | New-PveContainer | Low |
| PUT /nodes/{node}/lxc/{vmid}/config | param changes | Set-PveContainerConfig | Low |
| POST /cluster/backup | param changes | New-PveBackupJob | Low |
| PUT /cluster/backup/{id} | param changes | Set-PveBackupJob | Low |
| PUT /cluster/firewall/options | `log_ratelimit` changed | Set-PveFirewallOptions | Low |
| POST /nodes/{node}/vzdump | param changes | New-PveBackup | Low |
| PUT /nodes/{node}/network | param changes | Set-PveNetwork | Low |
| PUT /nodes/{node}/config | param changes | Set-PveNodeConfig | Low |
| POST /storage | param changes | New-PveStorage | Medium |
| PUT /storage/{storage} | param changes | Set-PveStorage | Medium |

**Assessment**: No high-risk breaking changes for existing cmdlets. The `link[n]` parameter type changes affect cluster config endpoints not yet implemented. Storage param changes add new storage backend options (additive in practice).

### PVE 9.0 New Endpoints (42 total, 0 implemented)

| Area | Count | Notable Endpoints |
|---|---|---|
| SDN | 16 | Fabric management (new subsystem in 9.0) |
| Nodes | 11 | Service state, vzdump defaults, disk management |
| Cluster | 6 | SDN apply, HA resources/rules, metrics export |
| HA | 5 | Rules CRUD + resource migrate/relocate |
| VMs | 1 | Migration endpoint enhancement |
| Containers | 1 | Migration endpoint enhancement |
| Storage | 1 | Storage endpoint enhancement |
| Access | 1 | Access endpoint enhancement |

### High-Value Gaps

1. **Ceph management** (40 endpoints) — Critical for hyperconverged deployments
2. **HA management** (21 endpoints, 5 new in 9.0) — Critical for production clusters
3. **VM/CT-level firewall** (~23 endpoints) — Cluster-level done, per-VM/CT missing
4. **Disk management** (18 endpoints) — ZFS, LVM, directory operations
5. **Cluster configuration** (10 endpoints) — Join/create/manage cluster

---

## Phase 3 — Code Quality & Best Practices

### 3a. PowerShell Module Design

All 169 cmdlets verified:
- All cmdlet classes are `sealed`
- All cmdlets have `[OutputType]` attribute
- All destructive cmdlets have `ConfirmImpact = ConfirmImpact.High` (including Restart/Suspend-PveContainer, F062/F063 resolved)
- VmId parameters use `[ValidateRange(100, 999999999)]`
- Verb class constants used (not string literals)
- ShouldProcess on destructive cmdlets
- HelpMessage on parameters
- Pipeline support via `ValueFromPipelineByPropertyName`

### 3b. C# Code Quality

- No inline task-polling loops — all cmdlets use `TaskService.WaitForTask` (F032/F033/F036/F058 resolved)
- **REGRESSION F039**: Two bare `catch {}` blocks found (see below)
- Password parameters use `SecureString` (F051 resolved)
- URL paths use `Uri.EscapeDataString()` — 164 occurrences across 36 files (F050/F071 resolved)
- No `System.Text.Json` attributes (F044 resolved)
- Magic strings extracted to constants (F049 resolved)
- `GetAwaiter().GetResult()` sync-over-async pattern — accepted per F048/wont_fix
- Cryptographic RNG for boundary generation (F026 resolved)

**InvokePveVmGuestExecCmdlet** has an inline `do/while` loop for guest exec status polling. This is polling the QEMU guest agent exec status endpoint (not a PVE UPID task), with a proper timeout via `Stopwatch` + `TimeSpan.FromSeconds(Timeout)`. Correctly does not use `TaskService.WaitForTask`.

### 3c. General Hygiene

- No TODO/FIXME/HACK markers in source
- No dead code or commented-out blocks found
- Framework targeting correct: publishable projects use `netstandard2.0`, test project uses `net10.0;net48`
- `.editorconfig` present with consistent style rules
- 169 markdown cmdlet docs matching cmdlet count

### 3d. HttpClient Lifecycle

`PveHttpClient` creates a new `HttpClient` per instance. Services use `_injectedClient ?? new PveHttpClient(session)` — per-call when no injected client. F045 resolved by adding DI support (IPveHttpClient interface). The per-call pattern in production is acceptable for a PowerShell module where sessions are short-lived.

### F039 — Bare Catch Blocks (REGRESSED)

Two bare `catch` blocks violating D004 were found:

1. **VmService.PingGuestAgent** (`VmService.cs:553`): `catch { return false; }` — catches all exceptions including `OutOfMemoryException`. Should use `catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)`.

2. **ImportPveOvaCmdlet** (`ImportPveOvaCmdlet.cs:277`): `catch { /* fallback */ }` — bare catch in VM retrieval fallback after OVA import. Same fix needed.

### Code Quality Findings

| Finding ID | Severity | Status | Description |
|---|---|---|---|
| F039 | Medium | **Regressed** | 2 bare catch blocks in VmService + ImportPveOvaCmdlet |
| F032 | Critical | Resolved | Inline task polling → WaitForTask |
| F033 | Critical | Resolved | Guest exec polling → timeout added |
| F058 | Critical | Resolved | Container/storage polling → WaitForTask |
| F041 | Medium | Resolved | Unsealed cmdlets → all sealed |
| F037 | Medium | Resolved | Missing OutputType → all added |
| F044 | Medium | Resolved | Dual JSON attributes → Newtonsoft only |
| F045 | Medium | Resolved | HttpClient per-call → DI support added |
| F062 | Medium | Resolved | Restart-PveContainer ConfirmImpact → High |
| F063 | Medium | Resolved | Suspend-PveContainer ConfirmImpact → High |

---

## Phase 4 — Testing Coverage Analysis

### Test Infrastructure

| Component | Framework | Location | Count |
|---|---|---|---|
| xUnit unit tests | xunit 2.9.3, Moq 4.20.72 | tests/PSProxmoxVE.Core.Tests/ | ~12 test files |
| Pester unit tests | Pester 5.x | tests/PSProxmoxVE.Tests/ | ~30 test files |
| Integration tests | Pester 5.x | tests/PSProxmoxVE.Tests/Integration/ | 2 files |

### xUnit Coverage

Tests cover:
- **Authentication**: PveAuthenticator, PveSession, PveVersion
- **Model deserialization**: All model areas (Backup, Cluster, Container, Firewall, Network, Node, SDN, Snapshot, Storage, Task, User, VM)
- **Services**: Backup, CloudInit, Cluster, Node, Pool, Snapshot, Storage, Task, Template, User

**Gap (F046)**: 5 services lack xUnit tests: VmService, ContainerService, FirewallService, NetworkService, SdnService. These are the most complex service classes and are only covered at the Pester cmdlet-existence level and integration tests.

**Additional test quality observations:**
- No error-response handling tests (HTTP 400/401/403/500 responses not exercised)
- PoolServiceTests lacks null-guard tests (unlike all other service tests)
- No negative integration tests (duplicate resource creation, nonexistent resource removal)
- Integration gaps: Pool CRUD, Group/Domain CRUD, Cluster resources, most SDN Set cmdlets, guest agent extensions, Node config/DNS

### Pester Test Coverage

| Area | Unit Tests | Integration Tests |
|---|---|---|
| Connection | Yes | Yes |
| VMs | Yes | Yes |
| Containers | Yes | Yes |
| Storage | Yes | Yes |
| Network/SDN | Yes | Yes |
| Firewall | Yes | Yes |
| Backup | Yes | Yes |
| Tasks | Yes | Yes |
| Users/Roles | Yes | Yes |
| Pools | Yes | Yes |
| Templates | Yes | Yes |
| Nodes | Yes | Yes |
| Snapshots | Yes | Yes |
| CloudInit | Yes | Yes |
| OVA Import | Yes | Yes |

### Test Quality

- Pester tests use Describe/Context/It structure with descriptive names
- Integration tests tagged with `@("Integration")` for filtering
- Connection details injected via environment variables
- Integration tests cover both PVE 8 and PVE 9
- xUnit tests use Moq for HTTP client mocking with realistic fixtures
- Test cleanup via AfterAll blocks

---

## Phase 4b — CI Integration Test Results

### Most Recent Completed Run

| Field | Value |
|---|---|
| Run ID | 23511293737 |
| Conclusion | failure |
| Created | 2026-03-24T20:42:58Z |
| Head SHA | 0f3e2c1 |
| Branch | main |

**Root cause**: Cloud image download race condition — `mv: cannot stat '...noble-server-cloudimg-amd64.qcow2.downloading'`. Transient infrastructure issue. Prior run (23511223201) succeeded. Newer run (23511879409) in progress.

**No test failures** — tests were skipped due to provision failure.

---

## Phase 5 — Security Review

| Finding ID | Area | Status | Notes |
|---|---|---|---|
| — | Credential handling | Pass | SecureString for all passwords, PSCredential for Connect |
| F084 | Session object exposure | **New** | PveSession exposes Ticket/ApiToken/CsrfToken in default pipeline output |
| — | TLS/HTTPS | Pass | Enforced by default, SkipCertificateCheck opt-in with warning |
| — | Input validation | Pass | Uri.EscapeDataString on all path params, ValidateRange on IDs |
| — | Secret scanning | Pass | No secrets in committed files, .gitignore covers sensitive files |
| — | Dependency security | Pass | All packages current, Dependabot configured |

### F084 — PveSession Exposes Auth Secrets in Pipeline Output (NEW)

`PveSession.Ticket`, `ApiToken`, and `CsrfToken` are public `string` properties with no format-file hiding. When `Connect-PveServer -PassThru` or `Test-PveConnection -Detailed` writes the session to the pipeline, all secret fields are visible in the default table/list view. A user piping `$session | Format-List *` or logging verbose output could inadvertently expose auth tokens.

**Fix**: Add a `PSProxmoxVE.format.ps1xml` entry for `PveSession` that shows only `Hostname`, `Port`, `AuthMode`, `IsExpired`, and `ServerVersion` by default.

---

## Phase 6 — PSGallery Publication Readiness

| Finding ID | Check | Pass/Fail | Notes |
|---|---|---|---|
| — | RootModule | Pass | PSProxmoxVE.dll |
| — | ModuleVersion | Pass | 0.1.0 (updated from tag by publish workflow) |
| — | GUID | Pass | a3f7c2d1-84e5-4b9f-a061-3e2d8c5f1a7b |
| — | Author | Pass | goodolclint |
| — | Description | Pass | Comprehensive description |
| — | PowerShellVersion | Pass | 5.1 |
| — | CompatiblePSEditions | Pass | Desktop, Core |
| — | CmdletsToExport | Pass | 169 cmdlets explicitly listed |
| — | Tags | Pass | 8 relevant tags |
| — | LicenseUri | Pass | GitHub MIT license link |
| — | ProjectUri | Pass | GitHub repo link |
| — | ReleaseNotes | Pass | Present in PSData |
| F021 | IconUri | Fail | Not set — recommended for PSGallery listing |
| — | Prerelease | Pass | 'preview' — appropriate for current state |
| — | Framework target | Pass | netstandard2.0 for publishable projects |
| — | PS 5.1 smoke test | Pass | Verifies >= 150 commands load |
| — | MAML help | Pass | PSProxmoxVE.dll-Help.xml present |

---

## Phase 7 — Community & Repo Maintenance Standards

| Check | Pass/Fail | Notes |
|---|---|---|
| CONTRIBUTING.md | Pass | Development setup, PR process |
| CODE_OF_CONDUCT.md | Pass | Contributor Covenant |
| SECURITY.md | Pass | Security policy |
| DECISIONS.md | Pass | 12 active decisions |
| CODEOWNERS | Pass | @goodolclint |
| Issue templates | Pass | Bug report + feature request + config.yml |
| PR template | Pass | Present |
| Dependabot | Pass | NuGet + GitHub Actions weekly |
| Branch protection | Pass | Required checks + review |
| Commit conventions | Pass | Conventional commits |
| Release process | Pass | Tag-triggered publish |
| CHANGELOG | Pass | Keep-a-Changelog |

---

## Phase 8 — Findings Database Update

### Verification of DECISIONS.md Patterns

| Decision | Pattern | Verified | Method |
|---|---|---|---|
| D001 | TaskService.WaitForTask | Yes | grep `while(true)` — only in TaskService + WaitPveTask cmdlet |
| D002 | SecureString passwords | Yes | grep `public string Password` — none found |
| D003 | Uri.EscapeDataString | Yes | 164 occurrences across 36 files |
| D004 | No bare catches | **FAIL** | 2 bare catches found: VmService.cs:553, ImportPveOvaCmdlet.cs:277 → F039 regressed |
| D005 | OutputType on all cmdlets | Yes | 169 cmdlets, 169 OutputType attributes |
| D006 | ConfirmImpact.High | Yes | All destructive cmdlets including Restart/Suspend-Container |
| D007 | Sealed cmdlet classes | Yes | All 169 classes sealed |
| D008 | Newtonsoft.Json only | Yes | No System.Text.Json references in src/ |
| D009 | netstandard2.0 targets | Yes | Both publishable projects target netstandard2.0 |
| D010 | ValidateRange on VmId | Yes | All VmId parameters have ValidateRange |
| D011 | Verb class constants | Yes | No string literal verbs found |
| D012 | Magic string constants | Yes | ApiTokenPrefix, CsrfHeaderName constants in PveHttpClient |

**1 regression detected**: F039 (bare catch blocks) — D004 violated in 2 locations. No new decisions needed.

---

## Phase 9 — Prioritized Recommendations

### Medium (5 findings)

| Finding ID | What | Where | Why | Fix |
|---|---|---|---|---|
| F039 | **REGRESSED** Bare catch blocks | VmService.cs:553, ImportPveOvaCmdlet.cs:277 | Catches all exceptions including OOM/SOE, violates D004 | Replace with `catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)` |
| F084 | PveSession exposes auth secrets | PveSession.cs, format.ps1xml | Ticket/ApiToken/CsrfToken visible in pipeline output | Add format.ps1xml entry hiding secret properties |
| F046 | Integration test coverage gaps | tests/ | VmService, ContainerService lack xUnit tests | Add xUnit tests with Moq for complex services |
| F060 | Cluster config 0% | src/ | Cannot manage clusters | Add cluster config cmdlets |
| F061 | PVE 9.0 endpoints 0% | src/ | 42 new endpoints, none implemented | Prioritize SDN fabrics + HA rules |

### Low (6 findings)

| Finding ID | What | Where | Why | Fix |
|---|---|---|---|---|
| F021 | No IconUri in manifest | PSProxmoxVE.psd1 | PSGallery listing lacks icon | Add IconUri pointing to project logo |
| F053 | HA subsystem 0% | src/ | 21 endpoints for HA management | Implement HA group/resource/rule cmdlets |
| F054 | Ceph subsystem 0% | src/ | 40 endpoints for hyperconverged | Implement Ceph pool/OSD/monitor cmdlets |
| F067 | Disk management 0% | src/ | ZFS/LVM/directory ops missing | Implement disk management cmdlets |
| F068 | Notifications 0% | src/ | Notification targets not managed | Implement notification cmdlets |
| F069 | ACME/Certificates 0% | src/ | Let's Encrypt + cert mgmt missing | Implement ACME/cert cmdlets |
