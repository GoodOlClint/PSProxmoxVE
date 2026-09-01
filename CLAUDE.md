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

### Review before pushing, not after

**Write the code, then review it, then commit and push.** Not the other way round.
Before `git add`/`git commit` on any non-trivial change, spawn reviews of the working
tree and act on what they find:

- **Codex** (`codex:codex-rescue`) — a second opinion from a different model.
- **Subagent reviewers** — pick for the change: `correctness-reviewer`,
  `security-reviewer`, `test-reviewer`, `architecture-reviewer`, `api-compat-reviewer`,
  `performance-reviewer`, `ai-smell-reviewer`.

Run them in parallel in one message; they are read-only and independent.

The point is to cut review churn. A defect found before the push costs one edit; the same
defect found by the PR reviewer costs a review cycle, a force-push and a re-review, and on
CI-infrastructure changes a runner iteration is roughly 45 minutes. Reviewers are not
infallible — verify a finding against the code before acting on it, and say so when you
judge one wrong rather than silently ignoring it.

When a reviewer criticises a test, mutation-test it: break the behaviour the test claims to
cover and confirm the test fails. A suite that passes against a deliberately broken
implementation is not evidence of anything.

Trivial edits skip this: a typo, a version bump, a one-line doc change.

### Agent pushes and commit identity

**Default: the `github` MCP tools.** Agent-authored branches go up with `create_branch` +
`push_files`, which commits as the `goodolclint-claude` App and produces a **verified**
commit. `push_files` re-uploads full file contents, so byte-verify before opening the PR:
commit the identical change locally, `git fetch`, and `git diff <local-commit>
origin/<branch> --` must be empty. `push_files` cannot express a removal — use
`delete_file` for deletions, and a rename is `push_files` of the new path plus
`delete_file` of the old.

**Fallback: local `git push`, for large pushes only.** Re-uploading full contents inline
is impractical past a certain size. `.claude/settings.json` sets `GIT_AUTHOR_*` /
`GIT_COMMITTER_*` to `goodolclint-claude[bot]` so those commits are still attributed to the
App — but they are **not verified**, because the signature comes from committing through
the API, not from the author name. Use this path when needed, not by default.

Claude Code picks the env block up immediately — the session that adds it already commits
as the bot, no restart needed. A `Co-Authored-By` trailer is redundant once it is in
effect, since the App is the commit author.

### Local dev environment

`tests/infrastructure/scripts/run-integration.sh` is the single source of truth for the
provision → test → cleanup lifecycle. CI calls it directly, and so should you — there is
no wrapper script.

Build and unit tests run natively, no container needed:

```bash
dotnet build PSProxmoxVE.sln
dotnet test tests/PSProxmoxVE.Core.Tests/
pwsh -Command "Invoke-Pester tests/PSProxmoxVE.Tests/ -ExcludeTagFilter Integration -Output Detailed"
```

An installed `PSProxmoxVE` in `~/.local/share/powershell/Modules/` shadows the local build,
because `_TestHelper.ps1` tries `Import-Module PSProxmoxVE` by name first. Force the local
build with `Import-Module ./src/PSProxmoxVE/bin/Debug/netstandard2.0/PSProxmoxVE.psd1 -Force`,
or delete the installed copy. (`dotnet build` writes there; only `dotnet publish -o
./publish/netstandard2.0`, which CI runs, creates `publish/`.)

The integration flow needs the `dev-infra` container — the same image CI runs its jobs in
(`tests/Dockerfile.test`, target `dev-infra`). On x86 Linux, compose builds and runs it:

```bash
pve() {
    docker compose -f tests/docker-compose.test.yml --profile infra run --rm dev-infra \
        bash tests/infrastructure/scripts/run-integration.sh "$@"
}

pve provision 9
pve test 9 Cluster,VMs   # the area filter is optional
pve force-cleanup
```

### Running it on macOS (Apple Silicon)

The image is amd64-only — `proxmox-auto-install-assistant` and the HashiCorp apt repo publish no
arm64 — so it runs under emulation. **Turn on Docker Desktop's "Use Rosetta for x86_64/amd64
emulation" (Settings → General) first.** Under the default qemu translation `pwsh` starts and
reports its version, then segfaults on module discovery (`uncaught target signal 11`). That fails
the image build at `Install-Module Pester`, and would fail Pester at test time. The build exits 1
with no diagnostic output, so it reads as a Dockerfile defect rather than an emulation problem.

With Rosetta on, the same Dockerfile builds to within 150 bytes of the image CI pushes.

Two ways to get the image. Pulling what CI built is faster and is the exact artifact CI ran:

```bash
# Needs a CLASSIC PAT with read:packages — GHCR does not accept fine-grained tokens.
read -rs PAT && echo "$PAT" | docker login ghcr.io -u <user> --password-stdin && unset PAT
docker pull --platform linux/amd64 ghcr.io/goodolclint/psproxmoxve-integration:latest

# or build it locally
docker build --platform linux/amd64 --target dev-infra -f tests/Dockerfile.test -t pve-dev .
```

Then drive `run-integration.sh` directly. Compose is not used here: its `dev-infra` service builds
rather than pulls, and bind-mounts `/opt/pve-integration`, which does not exist on a Mac.

```bash
pve() {
    docker run --rm --platform linux/amd64 \
        --env-file tests/.env.test \
        -v "$HOME/pve-integration:/opt/pve-integration" \
        -v "$PWD:/repo" -w /repo \
        ghcr.io/goodolclint/psproxmoxve-integration:latest \
        bash tests/infrastructure/scripts/run-integration.sh "$@"
}

mkdir -p ~/pve-integration
pve provision 9
pve test 9
pve force-cleanup          # always run this — see below
```

**Expect lifecycle-test failures that CI does not see.** Emulation runs the suite roughly 40%
slower, which widens the `qemu-server` flock race in #113 — typically `Reset-PveVm`, clone and
`Set-PveVmConfig` failing with `can't lock file '/var/lock/qemu-server/lock-<vmid>.conf'`. Those
are the emulated client losing a race CI wins, not regressions. Provisioning and cleanup are
unaffected.

### Before any integration run, on any host

Copy `tests/.env.test.example` to `tests/.env.test` — it lists every required variable,
including the Terraform storage pools CI supplies from repository variables.

**The nested VMIDs are fixed constants** (storage VM 5080 at `run-integration.sh:80`; nodes 5091
and 5092 in `pve_vmid()` at `:112`) and
are shared with CI on the same parent cluster. Never start a local run while a CI integration run
is in flight, and always finish with `force-cleanup`: leftover guests fail the next run's
headroom guard.

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
- `docs/review/REVIEW_REPORT.md` — latest full review report (scan-9, 2026-03-26, F001–F085)
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

## Releasing to PSGallery

Tag-driven: pushing a `v*` tag to `main` triggers `.github/workflows/publish.yml` (build →
PS 5.1 smoke test → publish to PSGallery → create GitHub Release with auto-generated notes).

Each release PR must update **three** things in lockstep before the tag is cut:

1. `ModuleVersion` in `src/PSProxmoxVE/PSProxmoxVE.psd1` (semver patch for bug-fix-only;
   minor for new features; major for breaking changes).
2. `ReleaseNotes` in the same psd1 — this is what PSGallery surfaces on the version page.
   Replace the previous version's notes; do not append.
3. `CHANGELOG.md` — cut the `[Unreleased]` section into a new `[X.Y.Z] - YYYY-MM-DD`
   block and reset `[Unreleased]` to empty.

After merge, tag `main` with `vX.Y.Z` and push the tag. The publish workflow rewrites
the psd1 `ModuleVersion` in the build artifact from the tag, so the tag and the source
version must match.
