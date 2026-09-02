# Lane 2 — PVE package-currency CI lane: change plan

Status: **approved by the operator 2026-09-01.** Report-only accepted for now — revisit once a few releases are out.

## 1. Problem

Lane 1 pins the nested PVE nodes to what the ISO ships and deliberately does not upgrade. `first-boot.sh` says so in a comment: `apt-get upgrade` held packages back and produced `pve-cluster` 9.1.6 against `libpve-cluster-api-perl` 9.1.0 — a combination no real install ever has, whose cluster join silently left the node unclustered. Removing it was the whole fix for integration run 180.

That pin buys a stable signal and costs currency: nothing in CI ever exercises the module against a *current* PVE. Lane 2 is the canary that does, without putting that instability back into the lane that gates merges.

## 2. What is already decided

From the cluster/CI workstream tracker, and not reopened here:

- Lane 2 does `dist-upgrade`, records the resulting package set, and is **report-only** — it never auto-bumps lane 1's pin.
- Weekly cron plus manual dispatch.
- It **must reboot** after the `dist-upgrade`. A PVE `dist-upgrade` pulls `proxmox-kernel-*`; with no reboot the node runs new userspace on the old kernel, so the lane records a package set it never actually ran and is blind to kernel regressions.
- The reboot goes in `prepare-test-environment.sh`, **not** `first-boot.sh`. `first-boot.sh` runs `ordering = "fully-up"` while the parent is still polling, so `wait-for-pve.sh` can discover the IP, see the API, pass auth, and then have the node reboot out from under provisioning — intermittent, and looks like a network fault.
- Wait for the node to come back with `wait-for-api.sh`, which already exists and is currently unused by `run-integration.sh`.
- Reboot **unconditionally**, not gated on `/var/run/reboot-required` — that file comes from `update-notifier-common`, which is not guaranteed on a PVE node.
- Failures raise **one** rolling GitHub issue, updated in place, carrying the `dpkg` diff against the last good run — not a new issue per tick.
- Share the `integration-tests` concurrency group, or two runs fight over the nested VMIDs.

## 3. Decisions taken in this gate

| # | Fork | Choice |
|---|---|---|
| 1 | Lane home | New `.github/workflows/package-currency.yml` |
| 2 | Scope | Full Pester suite; test failures reported, **not** fatal |
| 3 | Baseline store | **Superseded during build** — see 3.3. An unprotected `ci/package-baseline` data branch, not a committed file in a PR |

### 3.1 Why a separate workflow

`integration-tests.yml` is already the most complex workflow in the repo and its trigger comment explicitly reasons about fork-PR safety. The currency lane needs `issues: write` (to maintain the rolling issue) and `contents: write` + `pull-requests: write` (to raise the baseline PR); `integration-tests.yml` needs none of those. Putting the lane in its own file keeps those permissions off every normal integration run. Both files declare `concurrency: group: integration-tests` so they serialise against each other.

### 3.2 Why the suite runs but does not fail the job

Package drift that doesn't break anything is not interesting. The thing worth knowing is *the current PVE breaks the module* — that only surfaces by running the tests. But a scheduled job that goes red on an upstream change we have not chosen to chase becomes noise, and a noisy cron gets ignored, which is the failure mode that makes canaries worthless. So the suite runs, results land in the issue, and the job stays green unless the lane's own machinery fails.

**Consequence to accept:** a genuinely broken module against current PVE is a green check with an updated issue. The issue is the signal, not the check colour.

### 3.3 Baseline: superseded during implementation

The original choice was a committed baseline file updated by an auto-merging PR. **That cannot work on this repo**, and the reason only surfaced in review:

- A PR opened with `GITHUB_TOKEN` never triggers `pull_request` workflows — GitHub suppresses them to prevent recursion. `build.yml`, `unit-tests.yml` and `claude-code-review.yml` are all `pull_request`-triggered, so the baseline PR would report **zero required checks** and be permanently unmergeable under branch protection. Not "waits for a human to merge it" — cannot be merged.
- Auto-merge is enabled on the repo, but auto-merge waits for required checks that will never report, so it does not help.
- `pve_api` solves this by pushing straight to `main` as `github-actions[bot]` with `contents: write`. That works because its `main` is unprotected. This repo's is (required checks, required review, admin enforced), so the same action is rejected.
- Using a GitHub App installation token would fire the checks — but it puts output from a machine that just ran `dist-upgrade` against an upstream repo in front of the `claude-review` agent, which holds `pull-requests: write` and is instructed to end with `--approve`. That is a known [required-reviews bypass](https://medium.com/cider-sec/bypassing-required-reviews-using-github-actions-6e1b29135cc7) shape, and not worth opening for a generated data file.

**Adopted instead: an unprotected data branch**, `ci/package-baseline`, holding exactly one file. This is the established pattern for generated data against a protected main — [github-archive-action](https://github.com/githubocto/github-archive-action) writes to an orphan branch for the same reasons. Branch protection covers `main` only, so `contents: write` plus `GITHUB_TOKEN` is sufficient: no PR, no checks, no protection conflict, and no path from node output to the review agent.

It is written with git plumbing (`hash-object` → `mktree` → `commit-tree` → `push <sha>:refs/heads/…`) so the job's checkout is never touched and no local branch is created, which also makes a re-run unable to collide with itself.

Artifacts alone were considered — the operator's own suggestion — and rejected on the stated goal: artifacts expire (90 days by default), so a weekly cadence would retain roughly 13 data points and lose the history. `git log ci/package-baseline` keeps it indefinitely.

## 4. Change plan

Small, test-pinned commits; each is its own PR.

### Commit 1 — `prepare-test-environment.sh` learns to upgrade

Add an opt-in third argument (default off), so lane 1's behaviour is byte-identical when it isn't passed:

- `dist-upgrade` non-interactively.
- Capture `dpkg-query -W -f='${binary:Package}\t${Version}\n'`, sorted, to a file the caller collects.
- Reboot unconditionally.
- Return; the caller waits.

`run-integration.sh` gains a `PVE_DIST_UPGRADE` env var that it forwards, and calls `wait-for-api.sh` after `prepare-test-environment.sh` when it is set. `wait-for-api.sh` is used for the first time here.

*Check:* `bash -n` on both scripts, plus a lane-1 run showing the provisioning path unchanged when the flag is absent.

### Commit 2 — `package-currency.yml`

Schedule + `workflow_dispatch`; `concurrency: group: integration-tests`; provisions with `PVE_DIST_UPGRADE=1`; runs the full suite with `continue-on-error` on the test step; uploads the package set as an artifact.

### Commit 3 — reporting *(landed as #108, revised)*

Diff the reference node against the baseline on `ci/package-baseline`. On difference: update the data branch, then upsert the rolling issue (`pve-currency`). On no difference: exit quietly.

Two additions beyond the plan:

- **Node-vs-node comparison.** A package mismatch *between* the two nested nodes is the failure that left a node unclustered and cost three CI runs to diagnose (see [ADR 0017](decisions/0017-ci-runs-two-lanes-a-pinned-gating-lane-and-a-report-only-currency-lane.md)). It is reported even when the set is otherwise unchanged.
- **Input validation.** The package files come from a machine that just installed from an upstream repo and their contents reach a GitHub issue body, so anything that is not a dpkg name/version pair fails the run.

### Commit 4 — `DECISIONS.md`

[ADR 0017](decisions/0017-ci-runs-two-lanes-a-pinned-gating-lane-and-a-report-only-currency-lane.md) (two-lane CI: pinned gating lane + report-only currency lane, and why `first-boot.sh` must never upgrade) and [ADR 0018](decisions/0018-the-currency-lane-reboots-after-dist-upgrade-and-proves-it-rebooted.md) (the currency lane reboots after `dist-upgrade` **and proves it rebooted** — the verification was missing from the first draft and is the part easiest to omit).

## 5. Convention conflict, surfaced

~~The global instruction routes architectural decisions to `docs/decisions/` in house ADR format. This repo has no `docs/decisions/` — it records decisions in `DECISIONS.md` as D001–D016, and its own `CLAUDE.md` names that file as the thing to read before writing code. **Repo convention wins:** Lane 2's decisions go in `DECISIONS.md` as D017/D018, not a new `docs/decisions/` tree. Introducing a parallel decision store in a repo that already has one is exactly the defect the gate warns about.~~

**Superseded 2026-09-02 by [ADR 0023](decisions/0023-decisions-live-in-docs-decisions-in-house-adr-format.md).** The ruling above was correct while the CI lane work was in flight — a second decision store mid-change is the defect the gate warns about — and it deferred the migration rather than refusing it. That work has landed, so `DECISIONS.md` D001–D021 became ADR 0001–0021 and `DECISIONS.md` is now a stub with a redirect table. Lane 2's decisions are ADR 0017, 0018 and 0022.

## 6. Definition of Done

Discriminating, end-to-end — fails before, passes after:

1. A manual `workflow_dispatch` of `package-currency.yml` provisions a nested PVE, and the run log shows `dist-upgrade` installing at least one package, a reboot, and `wait-for-api.sh` reporting the API responsive afterwards. *Before the change there is no such workflow.*
2. `pveversion` captured after the reboot reports a kernel matching the upgraded `proxmox-kernel-*` package, not the ISO's. This is the check that proves the reboot is real; it fails if the reboot is dropped.
3. The full Pester suite runs and its result is recorded. A deliberately failed test does **not** turn the job red.
4. With the baseline file absent or stale, the run opens exactly one baseline PR and updates exactly one issue. Re-running with the baseline current opens neither.
5. A lane-1 integration run on the same commit is unchanged — no `dist-upgrade`, no reboot — proving the opt-in default holds.

## 7. Risks

- **The lane's own instability.** Upgrading is what broke cluster join before. It is contained here: lane 2 never gates a merge, and lane 1 keeps the pin.
- **Concurrency starvation.** Sharing the group means a long lane-2 run delays a post-merge lane-1 run. Weekly cadence makes collisions rare; `cancel-in-progress: false` is already the setting, so nothing is lost, only delayed.
- **Auto-merge blast radius.** "Allow auto-merge" is repo-wide, not scoped to this lane — any PR can be set to auto-merge. Already enabled, operator-confirmed.
- **Report-only hides a real break.** A module genuinely broken against current PVE shows a green weekly check plus an updated issue. Accepted deliberately; revisit after a few releases.
