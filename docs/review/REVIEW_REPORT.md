# PSProxmoxVE Review Report — Scan 9

```
Scan date:           2026-03-26
Prior report date:   2026-03-24
PVE API spec date:   2026-03-21T15:04:50.641Z
PVE API spec SHA256: 4af79be30166209a4714b771f65e1e9540c5b738f414ff30c98454402e29d030
PVE version hint:    (not set)
Total API endpoints: 646
Findings DB:         docs/review/findings.json (F001–F085)
Open findings:       11 (before scan) → 7 (after scan)
New this scan:       0  Resolved this scan: 4  Regressed: 0
Last CI run:         integration-tests.yml | success | 2026-03-26T00:51:58Z | run 23571920298
```

## Executive Summary

- **Delta**: 4 resolved | 0 new | 0 regressed | 7 still open
- **Resolved this scan**: F039 (bare catch regression fixed), F053 (HA subsystem implemented), F060 (cluster config implemented), F084 (PveSession secret hiding)
- **API drift**: 37 PVE 9.0 endpoints unimplemented (down from 42); 0 breaking changes detected in implemented endpoints
- **CI**: Last integration run PASSED (both PVE 8 and PVE 9) on 2026-03-26; 0 test failures
- **Coverage**: ~172 cmdlets across 16 functional areas; 7 PVE subsystems remain uncovered (Ceph, disks, notifications, ACME/certs, most PVE 9.0 endpoints)
- **Code quality**: All 13 DECISIONS.md patterns verified — no regressions detected
- **Security**: All password params use SecureString; URL encoding consistent; PveSession default output now hides secrets

---

## Phase 1 — Repository Inventory & Structure

| Item | Present | Notes |
|---|---|---|
| Solution file (.sln) | Yes | PSProxmoxVE.sln |
| Source projects | Yes | src/PSProxmoxVE/ (cmdlets), src/PSProxmoxVE.Core/ (services/models) |
| Test projects | Yes | xUnit (PSProxmoxVE.Core.Tests), Pester (PSProxmoxVE.Tests) |
| CI/CD workflows | Yes | build.yml, unit-tests.yml, integration-tests.yml, publish.yml, claude.yml, claude-code-review.yml |
| README.md | Yes | Badges, installation, usage, cmdlet list |
| CHANGELOG.md | Yes | 0.1.0-preview entry |
| CONTRIBUTING.md | Yes | .NET 10.0+ SDK, build/test instructions |
| LICENSE | Yes | MIT |
| CODE_OF_CONDUCT.md | Yes | Contributor Covenant v2.1 |
| SECURITY.md | Yes | Vulnerability disclosure policy |
| DECISIONS.md | Yes | 13 active decisions (D001–D013) |
| CODEOWNERS | Yes | Single maintainer |
| PSGallery manifest (.psd1) | Yes | 0.1.0-preview, 172 cmdlets exported |
| .editorconfig | Yes | C# and PowerShell rules |
| .gitignore | Yes | Standard .NET + PS patterns |
| .gitattributes | Yes | Line ending normalization |
| Issue/PR templates | Yes | Bug report, feature request YAML + PR template |
| Dependabot | Yes | NuGet + GitHub Actions weekly |
| docs/review/ | Yes | findings.json (F001–F085), REVIEW_REPORT.md |
| docs/cmdlets/ | Yes | 170 markdown help docs |
| MAML help | Yes | PSProxmoxVE.dll-Help.xml |
| Format file | Yes | PSProxmoxVE.format.ps1xml (models + PveSession) |

**Missing**: IconUri in manifest (F021 — cosmetic).

---

## Phase 2 — PVE API Coverage Audit

### Coverage by Functional Area

| Area | Total Endpoints | Covered (approx.) | % | Notable Gaps |
|---|---|---|---|---|
| vms | 97 | ~45 | 46% | dbus-vmstate (PVE 9.0), pending/current config |
| containers | 62 | ~25 | 40% | LXC migrate GET (PVE 9.0), firewall per-CT |
| firewall | 40 | ~21 | 53% | VM/CT-level firewall via Level param |
| sdn | 60 | ~20 | 33% | SDN fabrics (14 new PVE 9.0), lock/rollback |
| ha | 21 | ~14 | 67% | Status details, fencing config |
| cluster | 77 | ~12 | 16% | Bulk actions (PVE 9.0), metrics, replication |
| cluster_config | 10 | ~8 | 80% | — |
| nodes | 75 | ~8 | 11% | Capabilities, hardware scan, syslog, journal |
| storage | 19 | ~10 | 53% | OCI registry pull (PVE 9.0) |
| storage_config | 5 | ~3 | 60% | — |
| access | 15 | ~5 | 33% | VNC ticket (PVE 9.0), TFA |
| users | 12 | ~8 | 67% | — |
| access_groups | 5 | ~4 | 80% | — |
| access_domains | 6 | ~4 | 67% | Sync endpoint |
| roles | 5 | ~3 | 60% | — |
| pools | 7 | ~4 | 57% | — |
| acl | 2 | ~2 | 100% | — |
| tasks | 5 | ~4 | 80% | — |
| backup | 6 | ~5 | 83% | — |
| ceph | 40 | 0 | 0% | F054 — entire subsystem |
| disks | 18 | 0 | 0% | F067 — LVM, ZFS, SMART |
| acme | 15 | 0 | 0% | F069 — certificate management |
| certificates | 8 | 0 | 0% | F069 — TLS certs |
| services | 7 | 0 | 0% | Node service management |
| networking | 7 | ~5 | 71% | — |
| apt | 8 | 0 | 0% | Package management |
| metrics | 7 | 0 | 0% | External metric servers |
| replication | 5 | 0 | 0% | Storage replication |
| version | 1 | 0 | 0% | PVE version endpoint |

**Overall**: ~210/646 endpoints covered (~33%)

### PVE 9.0 New Endpoints (42 total)

| Status | Endpoint | Area |
|---|---|---|
| **Covered** | GET/POST/GET/{id}/PUT/{id}/DELETE/{id} /cluster/ha/rules | ha |
| Missing | GET/POST /cluster/bulk-action/guest/* (6) | cluster |
| Missing | /cluster/sdn/fabrics/* (14) | sdn |
| Missing | /cluster/sdn/lock, /cluster/sdn/rollback (3) | sdn |
| Missing | POST /nodes/{node}/qemu/{vmid}/dbus-vmstate | vms |
| Missing | GET /nodes/{node}/lxc/{vmid}/migrate | containers |
| Missing | GET /nodes/{node}/capabilities/qemu/* (2) | nodes |
| Missing | POST /nodes/{node}/storage/{storage}/oci-registry-pull | storage |
| Missing | /nodes/{node}/sdn/* (8) | nodes |
| Missing | GET /nodes/{node}/query-oci-repo-tags | nodes |
| Missing | POST /access/vncticket | access |

**PVE 9.0 coverage: 5/42 (12%)**

### API Drift — Breaking Changes

No breaking changes detected in PVE 9.0 for currently implemented endpoints. The 248 parameter changes in PVE 9.0 are predominantly additive (new optional parameters) and do not break existing cmdlet behavior.

---

## Phase 3 — Code Quality & Best Practices

### DECISIONS.md Compliance Check

| Decision | Status | Evidence |
|---|---|---|
| D001 — TaskService.WaitForTask | Compliant | `while(true)` only in TaskService.cs:114 and WaitPveTaskCmdlet.cs:69 (the implementations themselves, with timeout) |
| D002 — SecureString passwords | Compliant | All 7 password cmdlet params use SecureString with Marshal try/finally |
| D003 — Uri.EscapeDataString | Compliant | All service path parameters properly escaped |
| D004 — No bare catches | **Resolved** | F039 regression fixed. All catches are specific or filtered. |
| D005 — OutputType required | Compliant | All cmdlets have [OutputType] |
| D006 — ConfirmImpact.High | Compliant | All destructive cmdlets have ConfirmImpact.High |
| D007 — Sealed cmdlets | Compliant | All cmdlet classes are sealed |
| D008 — Newtonsoft only | Compliant | No [JsonPropertyName] in source |
| D009 — netstandard2.0 | Compliant | Both .csproj target netstandard2.0; test targets net10.0+net48 |
| D010 — VmId ValidateRange | Compliant | All VmId params have [ValidateRange(100, 999999999)] |
| D011 — Verb class constants | Compliant | No string literal verbs found |
| D012 — Magic strings | Compliant | Auth constants extracted |
| D013 — No Newtonsoft in public API | Compliant | F085 resolved; JObject/JArray only used internally for parsing |

### Code Quality Findings

| Finding ID | File | Severity | Status | Description |
|---|---|---|---|---|
| F039 | VmService.cs, ImportPveOvaCmdlet.cs | Medium | **Resolved** | Bare catch regression fixed. VmService:554 catches PveApiException; ImportPveOvaCmdlet:108 catches Exception+ThrowTerminatingError |
| F048 | src/PSProxmoxVE.Core/ | Medium | Wont_fix | ~216 sync-over-async via GetAwaiter().GetResult() — accepted pattern for PS 5.1 compat |

**Note on JObject usage**: JObject/JArray/JToken are used extensively in services and cmdlets for internal JSON parsing (response deserialization). This is acceptable per D013 — the restriction is on **public API types** (model properties, OutputType), not internal implementation. No Newtonsoft types appear in model properties or OutputType attributes.

### HttpClient Lifecycle (F045)

Resolved in prior scan. IPveHttpClient interface extracted; all 14 services accept shared client via constructor injection.

---

## Phase 4 — Testing Coverage Analysis

### Test Infrastructure

| Component | Framework | Target |
|---|---|---|
| PSProxmoxVE.Core.Tests | xUnit 2.9.3 | net10.0, net48 |
| PSProxmoxVE.Tests | Pester 5 | PowerShell 7.x |
| Integration tests | Pester 5 | Live PVE 8 + PVE 9 |

### Test Coverage by Area

| Area | xUnit (Models/Services) | Pester (Unit) | Integration | Notes |
|---|---|---|---|---|
| Connection | — | Yes | Yes (00_Connection) | |
| Nodes | NodeModelTests | Yes | Yes (01_Nodes) | |
| Users | UserModelTests, UserServiceTests | Yes | Yes (02_Users) | |
| Storage | StorageModelTests, StorageServiceTests | Yes | Yes (03_Storage, 03a_Shared) | |
| Network | NetworkModelTests | Yes | Yes (04_Network) | |
| SDN | SdnModelTests | Yes | Yes (05_SDN) | |
| VMs | VmModelTests | Yes | Yes (06_VMs) | |
| Snapshots | SnapshotModelTests, SnapshotServiceTests | Yes | Yes (07_Snapshots) | |
| Templates | TemplateServiceTests | Yes | Yes (08_Templates) | |
| CloudInit | CloudInitServiceTests | Yes | Yes (09_CloudInit) | |
| Containers | ContainerModelTests | Yes | Yes (10_Containers) | |
| Firewall | FirewallModelTests | Yes | Yes (13_Firewall) | |
| Backup | BackupModelTests, BackupServiceTests | Yes | Yes (14_Backup) | |
| Tasks | TaskModelTests, TaskServiceTests | Yes | Yes (15_Tasks) | |
| Cluster | ClusterModelTests, ClusterServiceTests, ClusterConfigServiceTests | Yes | Yes (16_Cluster) | |
| HA | HaServiceTests | Yes (HaCmdlets.Tests.ps1) | Yes (16_Cluster) | Full CRUD for groups, rules, resources, status |
| Pools | PoolServiceTests | Yes (PoolCmdlets.Tests.ps1) | No | F046 — no integration tests |

### xUnit Test Stats

- 12 model test files, 12 service test files
- ~382 total xUnit tests (196 service tests added in recent remediation)

### Test Quality Assessment

- **Structure**: Arrange/Act/Assert pattern used consistently
- **Isolation**: Service tests use Moq-based IPveHttpClient mocks
- **Fixtures**: JSON fixture files for PVE 8 and 9 response deserialization
- **Integration**: 18 integration test files (00–16 + 99_Cleanup) covering happy paths
- **Edge cases**: Model tests cover null/missing fields; service tests verify URL construction

### Finding F046: Integration Test Gaps

Still open. HA cmdlets now have integration coverage in 16_Cluster.Tests.ps1 (status, groups, rules, resources). Key areas still without integration tests: pool management, some newer SDN/firewall operations. Estimated 55-65 of ~172 cmdlets lack integration coverage.

---

## Phase 4b — CI Integration Test Results

| Field | Value |
|---|---|
| Workflow | integration-tests.yml |
| Latest completed run | 23571920298 |
| Conclusion | **success** |
| Date | 2026-03-26T00:51:58Z |
| Branch | main |
| SHA | d375695 |

**Last integration run: PASSED** (both PVE 8 and PVE 9 matrices). No CI findings to generate.

A newer run (23596586594) is currently in_progress.

The most recent failure (23566610350, 2026-03-25) was a provisioning infrastructure issue (Docker/Terraform), not a test logic failure.

---

## Phase 5 — Security Review

| Finding ID | Area | File | Severity | Status | Description |
|---|---|---|---|---|---|
| F084 | Credential exposure | PveSession.cs, format.ps1xml | Medium | **Resolved** | format.ps1xml now hides Ticket/ApiToken/CsrfToken from default output |
| — | Credential handling | Cmdlets/ | — | Pass | All 7 password params use SecureString with Marshal try/finally |
| — | TLS/HTTPS | PveHttpClient.cs | — | Pass | HTTPS enforced; SkipCertificateCheck opt-in with WriteWarning |
| — | URL encoding | Services/ | — | Pass | Uri.EscapeDataString on all dynamic path segments |
| — | Secret scanning | All files | — | Pass | No hardcoded credentials in tracked files; .env.test gitignored |
| — | Dependencies | .csproj files | — | Pass | Newtonsoft.Json 13.0.3, SharpCompress 0.38.0, PowerShellStandard.Library 5.1.1 |

No new security findings this scan.

---

## Phase 6 — PSGallery Publication Readiness

| Finding ID | Check | Pass/Fail | Notes |
|---|---|---|---|
| — | ModuleVersion | Pass | 0.1.0 |
| — | GUID | Pass | a3f7c2d1-84e5-4b9f-a061-3e2d8c5f1a7b |
| — | Author/CompanyName | Pass | goodolclint / Worklab |
| — | Description | Pass | Comprehensive, mentions PVE 8.x and 9.x |
| — | PowerShellVersion | Pass | 5.1 |
| — | CompatiblePSEditions | Pass | Desktop, Core |
| — | Tags | Pass | 8 tags including Proxmox, PVE, IaC |
| — | LicenseUri | Pass | Points to LICENSE on main |
| — | ProjectUri | Pass | GitHub repo URL |
| F021 | IconUri | **Fail** | Missing — cosmetic only |
| — | ReleaseNotes | Pass | Preview release description |
| — | CmdletsToExport | Pass | 172 cmdlets listed |
| — | RequiredAssemblies | Pass | PSProxmoxVE.Core.dll, Newtonsoft.Json.dll |
| — | FormatsToProcess | Pass | PSProxmoxVE.format.ps1xml |
| — | netstandard2.0 target | Pass | Both projects target only netstandard2.0 |
| — | Publish workflow | Pass | Tag-triggered, PS 5.1 smoke test, threshold >= 150 |
| — | DotNetFrameworkVersion | Pass | 4.8 |
| — | HelpInfoUri | Pass | Points to docs/cmdlets/ |

---

## Phase 7 — Community & Repo Maintenance

| Finding ID | Check | Pass/Fail | Notes |
|---|---|---|---|
| — | Issue templates | Pass | Bug report + feature request (YAML) + config.yml |
| — | PR template | Pass | Structured checklist |
| — | CONTRIBUTING.md | Pass | .NET 10.0+ SDK, build/test, coding standards, PR process |
| — | CODE_OF_CONDUCT.md | Pass | Contributor Covenant v2.1 |
| — | SECURITY.md | Pass | Vulnerability disclosure with 48h response SLA |
| — | CODEOWNERS | Pass | Single maintainer |
| — | LICENSE | Pass | MIT |
| — | .editorconfig | Pass | C# and PS rules |
| — | .gitattributes | Pass | Line ending normalization |
| — | Branch protection | Pass | Documented in CLAUDE.md |
| — | Commit conventions | Pass | Conventional commits |
| — | DECISIONS.md | Pass | 13 active decisions, linked from CLAUDE.md |
| — | Dependabot | Pass | NuGet + GitHub Actions weekly |
| — | CHANGELOG | Pass | 0.1.0-preview entry |
| — | Release process | Pass | Tag-triggered publish.yml with GitHub Releases |

All community standards met. No findings.

---

## Phase 9 — Prioritized Recommendations

### 🟡 Medium

| Finding ID | What | Where | Why | Fix |
|---|---|---|---|---|
| F046 | Integration test coverage gaps | tests/Integration/ | ~55-65 cmdlets lack end-to-end integration tests | Add integration tests for pools, newer SDN/firewall operations |
| F061 | PVE 9.0 endpoints partially covered (5/42) | — | 37 new PVE 9.0 endpoints unimplemented (SDN fabrics, bulk actions, OCI registry) | Prioritize SDN fabrics (14 endpoints) and bulk actions (6 endpoints) |

### 🟢 Low

| Finding ID | What | Where | Why | Fix |
|---|---|---|---|---|
| F021 | No IconUri in manifest PSData | PSProxmoxVE.psd1 | Cosmetic — improves PSGallery listing appearance | Add icon to repo and reference in manifest |
| F054 | Ceph subsystem 0% coverage | — | 40 endpoints for OSD, MON, pools, status. Critical for hyperconverged setups | Implement Ceph cmdlets when demand warrants |
| F067 | Disk management 0% coverage | — | 18 endpoints for LVM, ZFS, SMART. Needed for storage provisioning | Implement disk cmdlets |
| F068 | Notifications 0% coverage | — | 32 cluster notification endpoints (PVE 8.1+) | Implement notification cmdlets |
| F069 | ACME/Certificates 0% coverage | — | 23 combined endpoints for TLS certificate management | Implement ACME/cert cmdlets |
