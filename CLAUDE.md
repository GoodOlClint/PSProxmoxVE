# PSProxmoxVE — Claude Code Instructions

## Project Overview

C# binary PowerShell module for managing Proxmox VE (PVE) infrastructure. Two projects:
- `src/PSProxmoxVE/` — Cmdlets and module surface (targets netstandard2.0)
- `src/PSProxmoxVE.Core/` — Services, models, HTTP client (targets netstandard2.0)

Tests: xUnit (`tests/PSProxmoxVE.Core.Tests/`) and Pester 5 (`tests/PSProxmoxVE.Tests/`).

## Development Workflow

**All changes go through pull requests.** The `main` branch has branch protection enabled
(required build checks, required review, admin enforced). Never push directly to main.

```bash
# Create a feature branch
git checkout -b feat/my-feature

# ... make changes ...

# Commit using conventional commits
git commit -m "feat: add new cmdlet"

# Push and create PR
git push -u origin feat/my-feature
gh pr create
```

### Dev container (recommended)

A Docker-based dev environment replicates the full CI setup locally. Works on ARM Macs
(build + test) and x86 (full provisioning flow).

```bash
./tests/dev.sh              # Open pwsh shell in dev container
./tests/dev.sh build        # Build the module
./tests/dev.sh test         # Run unit tests
./tests/dev.sh integration  # Run integration tests against existing PVE
./tests/dev.sh provision    # Full CI flow: provision → test → cleanup (x86 only)
```

Configure integration test targets by copying `tests/.env.test.example` to `tests/.env.test`.

### Build & test without container

```bash
# Build
dotnet build PSProxmoxVE.sln

# xUnit tests
dotnet test tests/PSProxmoxVE.Core.Tests/

# Pester tests (requires pwsh)
pwsh -Command "Invoke-Pester tests/PSProxmoxVE.Tests/ -Output Detailed"

# Run all tests via helper
pwsh -File tools/Invoke-Tests.ps1
```

## Key Conventions

- All cmdlets use `Pve` noun prefix
- All cmdlet classes must be `sealed`
- All cmdlets must have `[OutputType]` attribute
- Destructive cmdlets must set `ConfirmImpact = ConfirmImpact.High`
- VmId parameters: `[ValidateRange(100, 999999999)]`, nullable when optional
- JSON: Newtonsoft.Json only (`[JsonProperty]`), no System.Text.Json attributes
- Task polling: always use `TaskService.WaitForTask`, never inline loops
- Passwords: `SecureString` type, never plain `string`
- URL paths: `Uri.EscapeDataString()` on all dynamic path segments
- No bare `catch {}` blocks — use specific or filtered exceptions
- Verb class constants required (`VerbsCommon.Get`, not `"Get"`)

## Review System

This repo uses a structured review system to track findings and prevent regressions.

### Key files
- `docs/review/findings.json` — stable findings database. IDs are permanent (F001, F002...).
  Never renumber. Read this before any coding session to understand open issues.
- `docs/review/REVIEW_REPORT.md` — latest full review report (scan-7, 2026-03-23)
- `DECISIONS.md` — architectural decisions and anti-patterns. **Read this before writing
  any new code.** It documents patterns that were deliberately chosen or changed and must
  not be reintroduced.

### Before starting a coding session
1. Read `DECISIONS.md` to understand established patterns
2. Check `docs/review/findings.json` for open findings relevant to the area you're working in
3. Do not introduce patterns listed as anti-patterns in DECISIONS.md

### Finding ID stability
Finding IDs (F001, F002...) are permanent. A resolved finding is never deleted from
findings.json — it is marked `resolved` with evidence of the fix. If a finding reappears,
it is marked `regressed` and retains its original ID.
