# PSProxmoxVE — Copilot Instructions

## Project Overview

C# binary PowerShell module for managing Proxmox VE (PVE) infrastructure. Two projects:
- `src/PSProxmoxVE/` — Cmdlets and module surface (targets `netstandard2.0`)
- `src/PSProxmoxVE.Core/` — Services, models, HTTP client (targets `netstandard2.0`)

Tests: xUnit (`tests/PSProxmoxVE.Core.Tests/`) and Pester 5 (`tests/PSProxmoxVE.Tests/`).

## Development Workflow

**All changes go through pull requests.** The `main` branch has branch protection enabled
(required build checks, required review, admin enforced). Never push directly to main.

Use [Conventional Commits](https://www.conventionalcommits.org/) for all commit messages
(e.g. `feat: add new cmdlet`, `fix: handle null node name`, `chore: update deps`).

### Build & test

```bash
# Build
dotnet build PSProxmoxVE.sln

# xUnit tests
dotnet test tests/PSProxmoxVE.Core.Tests/

# Pester tests (requires pwsh)
pwsh -Command "Invoke-Pester tests/PSProxmoxVE.Tests/ -Output Detailed"
```

A Docker-based dev container replicates the full CI setup locally:

```powershell
./tests/dev.ps1              # Open pwsh shell in dev container
./tests/dev.ps1 build        # Build the module
./tests/dev.ps1 test         # Run unit tests
./tests/dev.ps1 integration  # Provision nested PVE, run integration tests, cleanup (x86 only)
```

## Key Coding Conventions

### Cmdlet rules
- All cmdlets use the `Pve` noun prefix (e.g. `Get-PveVm`, `New-PveSnapshot`).
- All cmdlet classes must be `sealed`.
- All cmdlets must have an `[OutputType(typeof(...))]` attribute.
- Destructive cmdlets (`Remove-*`, `Stop-*`, `Reset-*`, `Restart-*`, `Suspend-*`, restore ops) must set `ConfirmImpact = ConfirmImpact.High`.
- Use verb class constants — `VerbsCommon.Get`, not the string literal `"Get"`.

### Parameters
- `VmId` parameters: `[ValidateRange(100, 999999999)]`; use `int?` (nullable) when optional.
- Passwords must use `SecureString`, never plain `string`. Extract with `Marshal.SecureStringToGlobalAllocUnicode` inside a `try/finally` that calls `Marshal.ZeroFreeGlobalAllocUnicode`.

### URL paths
- All user-supplied or dynamic values interpolated into API URL paths must be wrapped in `Uri.EscapeDataString()`.

### Task polling
- Always use `TaskService.WaitForTask(upid, session, TimeoutSeconds, this)`.
- Never implement inline `while (true)` / `do/while` polling loops in cmdlet files.

### JSON serialization
- Use only `Newtonsoft.Json` (`[JsonProperty]`). Do **not** add `System.Text.Json` (`[JsonPropertyName]`) attributes.

### Error handling
- No bare `catch { }` or `catch (Exception) { }` blocks.
- Use a specific exception type (`catch (PveApiException ex)`) or a filtered catch with a `when` clause that excludes fatal exceptions.

### Framework targeting
- Publishable projects (`PSProxmoxVE`, `PSProxmoxVE.Core`): `netstandard2.0`.
- Test projects: `net10.0` + `net48`.

## Key Reference Files

Before writing new code, read these files to understand established patterns and open issues:

- [`DECISIONS.md`](../DECISIONS.md) — architectural decisions and anti-patterns that must not be reintroduced.
- [`docs/review/findings.json`](../docs/review/findings.json) — stable findings database (IDs F001… are permanent; resolved findings are marked, never deleted).
- [`docs/review/REVIEW_REPORT.md`](../docs/review/REVIEW_REPORT.md) — latest full review report.
