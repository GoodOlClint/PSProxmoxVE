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
| 3 | Baseline store | Committed file, updated by an auto-merging PR, only when the package set actually changed |

### 3.1 Why a separate workflow

`integration-tests.yml` is already the most complex workflow in the repo and its trigger comment explicitly reasons about fork-PR safety. The currency lane needs `issues: write` (to maintain the rolling issue) and `contents: write` + `pull-requests: write` (to raise the baseline PR); `integration-tests.yml` needs none of those. Putting the lane in its own file keeps those permissions off every normal integration run. Both files declare `concurrency: group: integration-tests` so they serialise against each other.

### 3.2 Why the suite runs but does not fail the job

Package drift that doesn't break anything is not interesting. The thing worth knowing is *the current PVE breaks the module* — that only surfaces by running the tests. But a scheduled job that goes red on an upstream change we have not chosen to chase becomes noise, and a noisy cron gets ignored, which is the failure mode that makes canaries worthless. So the suite runs, results land in the issue, and the job stays green unless the lane's own machinery fails.

**Consequence to accept:** a genuinely broken module against current PVE is a green check with an updated issue. The issue is the signal, not the check colour.

### 3.3 Baseline: what works and what doesn't

The operator asked whether the baseline could go through a PR that auto-merges and skips CI, committing only when versions differ. Three parts, and they don't all hold:

**Commit only on change — yes.** The lane computes the set, diffs against the committed baseline, and does nothing when identical. On a pinned no-subscription repo most ticks are no-ops.

**Auto-merge — yes.** `enablePullRequestAutoMerge` merges once required checks pass and required reviews are satisfied. PR #100 established that `claude[bot]`'s APPROVED alone takes `mergeable_state` to `clean` on this repo, with the operator's code-owner review still pending. So the chain closes with no human: App opens the baseline PR → `claude-review` approves → build + unit tests go green → auto-merge fires. "Allow auto-merge" is already enabled on this repo (operator confirmed 2026-09-01).

**Skip CI — no, and it's worth being precise about why.** Both mechanisms deadlock against branch protection:

- `paths-ignore` on `build.yml` / `unit-tests.yml` → the workflow never runs → the required check never reports → the PR is blocked forever, and auto-merge waits forever.
- `[skip ci]` in the commit message → GitHub skips the whole run → identical deadlock.

The only way to skip the *work* while still satisfying protection is to keep the trigger and have each job short-circuit to an immediate `exit 0` on a baseline-only diff, so the check still reports success. That means editing `build.yml` and `unit-tests.yml` — two files with no other stake in this lane — to special-case it.

**Recommendation: let CI run.** Both workflows trigger on every PR to `main` with no path filters today. On PR #100 the full set settled in roughly two minutes, and a baseline PR only exists on ticks where versions actually moved. Paying two minutes a handful of times a year is cheaper than a permanent special case in the two workflows that gate every merge. If the baseline turns out to churn weekly, add the short-circuit then.

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

### Commit 3 — reporting

Diff the recorded set against `tests/infrastructure/pve-package-baseline.txt`. On difference: update the rolling issue (find by label, create if absent) with the diff and the suite result, and open the baseline-bump PR with auto-merge enabled. On no difference: exit quietly.

### Commit 4 — `DECISIONS.md`

D017 (two-lane CI: pinned gating lane + report-only currency lane, and why `first-boot.sh` must never upgrade) and D018 (currency lane reboots unconditionally after `dist-upgrade`).

## 5. Convention conflict, surfaced

The global instruction routes architectural decisions to `docs/decisions/` in house ADR format. This repo has no `docs/decisions/` — it records decisions in `DECISIONS.md` as D001–D016, and its own `CLAUDE.md` names that file as the thing to read before writing code. **Repo convention wins:** Lane 2's decisions go in `DECISIONS.md` as D017/D018, not a new `docs/decisions/` tree. Introducing a parallel decision store in a repo that already has one is exactly the defect the gate warns about.

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
