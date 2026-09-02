# ADR 0024 — The findings ledger is retired; open work lives in GitHub issues

- **Status:** Accepted
- **Date:** 2026-09-02
- **Deciders:** operator + agent
- **Context source:** audit of `docs/review/` during the ADR migration, [ADR 0023](0023-decisions-live-in-docs-decisions-in-house-adr-format.md)

## Context

`docs/review/` held a structured review system: `findings.json`, a ledger of 91 findings with permanent IDs, resolution evidence and regression history; `REVIEW_REPORT.md`, the last full scan report; and `PLAN-integration-refactor.md`, a planning document. `CLAUDE.md` instructed every session to read the ledger before starting work.

It had already stopped being used. The last substantive commit to `docs/review/` was 2026-05-22, more than three months before this decision, while the repository kept moving — decisions D014 through D021 were all recorded without a corresponding scan.

The parity check is what settled it. Of 91 findings, 83 were resolved and one was `wont_fix`. Of the seven still open, **six were already filed as GitHub issues**: F046 as #127 and #120, F054 as #128, F061, F067 and F068 as #120, F069 as #129. Only F021 — no `IconUri` in the manifest, severity low — existed solely in the ledger, and was filed as #130 before this decision took effect.

So the ledger was not a second source of truth. It was a stale copy of one.

The two supporting documents were worse than stale. `REVIEW_REPORT.md` is a dated snapshot whose decision-compliance table covered D001–D013 while the repository held D021. `PLAN-integration-refactor.md`, marked "Planned (not started)", proposed parallel provisioning, ISO caching and zero-touch runner setup — all since delivered by the Terraform and ARC work — and still planned around the PVE 8 leg retired in #88.

## Decision

`docs/review/` is deleted. `docs/lane2-change-plan.md` is deleted with it, on the same reasoning: its only live content was a ruling now superseded by [ADR 0023](0023-decisions-live-in-docs-decisions-in-house-adr-format.md), and the rest is a change plan for work that shipped.

Open work is tracked in GitHub issues. Decisions are recorded as ADRs in `docs/decisions/`. Conventions live in `CLAUDE.md` § "Key Conventions". There is no fourth store.

A planning document that needs to be public becomes an issue, not a file in `docs/`.

## Rejected alternatives

**Keeping `findings.json` as a historical archive.** It would still be listed in `CLAUDE.md` and in `.github/copilot-instructions.md` as a thing to read before coding, so every future session would read a ledger that has been wrong since May. An archive nobody is told to ignore is not an archive.

**Keeping the ledger and retiring only the two stale documents.** This was the narrower option and it fails the same test: the ledger's remaining value was its seven open findings, six of which were already duplicated in issues. Maintaining both means reconciling them, and nothing had reconciled them for three months.

**Migrating the 83 resolved findings into issues as closed records.** High volume, no reader. The resolution evidence that mattered was already absorbed into the ADRs during the migration, and git history holds the rest.

## Consequences

Fourteen ADRs cite finding IDs in their **Context source** line, such as `docs/review/findings.json F032, F033, F036, F058`. Those citations stay. They record what prompted the decision, which remains true, and this ADR is where a reader learns the ledger was retired deliberately rather than lost. The file is recoverable from git history at any commit before this one.

`CLAUDE.md` loses its "Review System" and "Finding ID stability" sections. The session checklist no longer directs a reader to a findings file.

The F-numbers are retired as identifiers, the same way [ADR 0023](0023-decisions-live-in-docs-decisions-in-house-adr-format.md) retired the D-numbers. Unlike the D-numbers there is no redirect table, because there is nothing to redirect to.

`.github/workflows/package-currency.yml` carried a header comment pointing at the deleted change plan; it now points at [ADR 0017](0017-ci-runs-two-lanes-a-pinned-gating-lane-and-a-report-only-currency-lane.md), which is where that lane's reasoning lives.
