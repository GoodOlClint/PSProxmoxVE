# ADR 0025 — Review instructions come from the default branch, and review-governing PRs cannot self-approve

- **Status:** Accepted
- **Date:** 2026-09-02
- **Deciders:** operator + agent
- **Context source:** PR #131 blocked by the anti-tamper gate; security review of the fix, 2026-09-02

## Context

`claude-code-action` submits a binding review verdict, and on this repository a `claude[bot]` **APPROVED** satisfies branch protection. An approval merges code, which makes the reviewer a high-value target.

The action carries an anti-tamper gate: it self-skips when the PR's copy of `.github/workflows/claude-code-review.yml` differs from the default branch, and the fail-closed step turns that into a red check. That is correct, but it made the review prompt effectively uneditable — PR #131 had 38 files of ADR migration blocked from review by a four-line prompt change.

Moving the prompt into `.github/review-prompt.md` and materializing it from the default branch fixes that. A security review of the first draft found the naive form of that materialization was itself exploitable, and that the guarantee's scope was narrower than its comments claimed.

## Decision

**Instructions come from the default branch, addressed by SHA.** The materialize step resolves `refs/remotes/origin/<default>` to a commit and reads both `.github/review-prompt.md` and `CLAUDE.md` from it.

The ref is fully qualified deliberately. `git show "origin/main:<path>"` is an **unqualified** refname, and `gitrevisions(7)` resolves `refs/tags/<name>` *before* `refs/remotes/<name>`. `actions/checkout` with `fetch-depth: 0` fetches all tags, so a tag literally named `origin/main` supplies the review instructions for every subsequent PR — exiting 0 with only a stderr warning, so it does not fail closed. Reproduced end to end on 2026-09-02.

**`CLAUDE.md` is materialized too.** The prompt judges convention compliance against the "Key Conventions" list. Read from the PR checkout, a PR could edit that list to permit its own violation, so the reviewer is pointed at the default branch's copy.

**A PR that governs review cannot auto-approve itself.** A step diffs against the default branch for `.github/review-prompt.md`, `.github/workflows/**` and `CLAUDE.md`. On a match, any `claude[bot]` APPROVED review is dismissed through the API and the check fails. `CHANGES_REQUESTED` and `COMMENTED` are left standing — they do not unblock a merge, and their content is still useful.

**The sentinel is checked mechanically.** `grep -qx PSPROXMOXVE-REVIEW-V1` runs in the materialize step, before Claude starts, rather than only being asserted by the model the sentinel exists to protect.

**No build or test tools.** The reviewer verifies build and test claims by reading the check runs for the same SHA (`gh pr checks`, `gh run view`, with `actions: read`), never by running the suite.

## Rejected alternatives

**`pull_request_target`.** It runs the workflow from the base branch, which would satisfy "always use main" directly. Rejected: it hands a write-scoped token and secrets to a context where PR-authored code is checked out, and the action can run Bash. It is the single most catastrophic misconfiguration in this class.

**Granting `dotnet build`, `dotnet test` and `pwsh -Command Invoke-Pester`,** which the reviewer asked for on PR #131 so it could verify the test counts itself. Rejected: `dotnet test` executes test code from the branch under review and `dotnet build` runs MSBuild targets that can `Exec` arbitrary commands, in a job holding a token that can approve the PR. That converts probabilistic prompt-injection influence into deterministic control. CI already ran those suites on the same SHA; reading the result is both safer and better evidence.

**Prose alone for the governance rule.** The first draft relied on a "defer to the operator" instruction in the prompt. A soft control cannot protect the root of trust: one successful injection, or plain non-compliance, would convert into persistent control of every future review.

**Leaving the prompt inline in the workflow.** Maximally tamper-proof — the anti-tamper gate covers it — but it blocks unrelated work, which is what prompted this.

## Consequences

Prompt edits no longer trip the anti-tamper gate, but they do trip the governance gate: a PR changing `review-prompt.md` gets a red check and needs operator review. That is the intended trade — routine work is unblocked, changes to the reviewer are not.

**Bootstrap:** the PR introducing this fails its own materialize step, because `review-prompt.md` is not yet on the default branch. It is red regardless, since it also edits the workflow. Confirm the next PR after merge goes green.

Fork PRs are skipped rather than failed. Previously they would have gone red with a message blaming the anti-tamper gate, and a required check that is always red on outside contributions trains the operator to override red checks — the habit the governance gate depends on not existing.

Two limits remain, and are not closed by this decision:

- `Bash(gh api:*)` is broad enough to reach endpoints the narrower `gh pr *` grants exclude, so the tool allowlist is a guardrail for a cooperating agent, not a sandbox for a hijacked one. The real bound is the Claude App installation's permissions.
- `docs/decisions/` is still read from the PR checkout. The prompt instructs the reviewer to treat an ADR added or edited by the PR under review as a claim, not as settled precedent.

The same unqualified-ref defect exists in `~/Source/Athena/.github/workflows/claude-code-review.yml`, which this workflow was adapted from.
