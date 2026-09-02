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

**A PR that governs review cannot auto-approve itself.** A step diffs against the default branch for `.github/review-prompt.md`, `.github/workflows/**`, `CLAUDE.md`, `DECISIONS.md` and `docs/decisions/**`. On a match, the review still runs and its findings are still wanted — the only thing withheld is the power to approve.

The expected verdict on such a PR is `COMMENTED`, which the prompt asks for and which does not satisfy branch protection, so merge waits for the operator regardless. The gate **passes** in that case: the reviewer behaved correctly. It fails only when `claude[bot]` actually submitted `APPROVED`, which means either a successful injection or plain non-compliance — that approval is dismissed through the API so it cannot satisfy branch protection, and the red check records that it happened.

Operator ruling 2026-09-02: withhold approval, not review. An earlier draft failed the check unconditionally on these PRs, which threw away a review that was wanted and made red the normal outcome for a whole class of PR — training the merge-past-red habit that the fork-notice change exists to avoid.

`docs/decisions/` is in that list deliberately, and it has a cost: **every ADR now needs operator approval.** That follows from what the ADRs became in [ADR 0023](0023-decisions-live-in-docs-decisions-in-house-adr-format.md) — the reviewer is told to defer to them as recorded precedent, so a PR that adds an ADR and then leans on it is the fabricated-precedent attack. The prompt separately instructs the reviewer to treat an ADR added by the PR under review as a claim rather than settled precedent; the dismissal makes that mechanical instead of advisory. `CHANGES_REQUESTED` and `COMMENTED` are left standing — they do not unblock a merge, and their content is still useful.

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

The trade is that GitHub reports a skipped required check as **passing**, so a fork PR shows green `claude-review` with nothing reviewed. A `fork-notice` job runs in its place and puts that in the run summary, but the green check is real and misleading on its own. Fork PRs are operator-reviewed by policy.

**The premise this all rests on: code-owner review is not enforced.** `CODEOWNERS` is `* @goodolclint`, and branch protection's "Require review from Code Owners" is **off** — verified on PR #100, where `mergeable_state` reached `clean` on `claude[bot]`'s approval alone with the operator's review still pending. Turning it on would make every bypass in this area inert, because `claude[bot]` is not a code owner and its approval could never satisfy protection.

It is deliberately left off. Enabling it would end the verdict-gated merge loop entirely — every PR would need the operator, not just the governance-path ones — which is a different project from this one. The consequence is that the dismissal gate is load-bearing rather than defence-in-depth, and it should be read that way: it is the only thing standing between an automated approval and a merge on a governance PR.

Three limits remain, and are not closed by this decision:

- **`.github/workflows/claude.yml` is a second, ungated path to a binding approval.** It runs the same action on any `@claude` mention with `contents: write` and no `allowedTools` restriction, no materialized prompt, and no governance detection — so its standing instructions are the repository `CLAUDE.md` *from the PR's checkout*, the exact file this workflow materializes from the default branch to avoid. An approval produced there is never dismissed, because the gate here is a one-shot check inside this job and `pull_request` does not fire on review submission. Filed separately; not fixed here.

- `Bash(gh api:*)` is broad enough to reach endpoints the narrower `gh pr *` grants exclude, so the tool allowlist is a guardrail for a cooperating agent, not a sandbox for a hijacked one. The real bound is the Claude App installation's permissions.
- `docs/decisions/` is still read from the PR checkout rather than materialized. The prompt instructs the reviewer to treat an ADR added or edited by the PR under review as a claim, not as settled precedent, and the dismissal gate above backs that mechanically.
- The formal-review check is scoped to the head SHA, so a re-run cannot pass on a verdict about an earlier commit. It had counted any historical `claude[bot]` review; that predates this decision and was fixed alongside it.

The same unqualified-ref defect exists in `~/Source/Athena/.github/workflows/claude-code-review.yml`, which this workflow was adapted from.
