# ADR 0026 — A PVE behaviour claim documented against the published API spec is verified without the operator

- **Status:** Accepted
- **Date:** 2026-09-02
- **Deciders:** operator + agent
- **Context source:** wave 1 of the 2026-09-02 remediation; PRs #163 and #164; review of PR #170

## Context

The review prompt's "Reserved to the operator" list, as introduced by the commit [ADR 0025](0025-review-instructions-come-from-the-default-branch-and-review-governing-prs-cannot-self-approve.md) documents, reserves to the operator any PR that "claims live PVE behaviour that CI does not exercise". The rule exists because the offline suite proves payloads, not server semantics ([ADR 0021](0021-integration-tests-prove-server-semantics-payloads-are-proven-offline.md)), so a PR asserting what PVE does with a parameter is asserting something no check has verified.

In wave 1 that rule deferred two PRs that were correct and green: #163 (the shape of `/access/permissions`) and #164 (`skiplock` and `force` on the remove endpoints). Both claims came straight from PVE's published API schema. Most behaviour-changing issues in the remaining waves make the same kind of claim, so the rule as written routes most of the remediation through the operator for facts that are already written down.

The parsed PVE API specification lives in a public repository, `GoodOlClint/Proxmox_API`, as one OpenAPI file per PVE version under `pve/openapi/` plus a CHANGELOG of return-field history. The reviewer's tool allowlist already includes `gh api`, so it can read a file from that repository at a specific commit.

The review prompt also says, of the enum fixtures, that "a value the schema accepts is not necessarily a value PVE acts on". That caveat is true of the published spec as well: it is the contract PVE publishes, not the server's behaviour, and the two can differ.

## Decision

A server-behaviour claim in a PR is one of three things: observed, documented, or inferred.

- **Documented** means the PR cites a commit-anchored permalink into `GoodOlClint/Proxmox_API` (`.../blob/<sha>/pve/...`), to the version's OpenAPI spec file (a `#L` range locating the endpoint) or to the CHANGELOG entry for a return field. The reviewer fetches that file at that commit with `gh api` and reads it. If the spec supports the claim, the claim is verified and the review names the permalink it checked. Approval is not withheld for it.
- **Inferred** means anything else: no citation, a branch-reference link, a link the reviewer cannot fetch, or a cited file that does not say what the PR says it does. Inferred claims stay reserved to the operator, exactly as ADR 0025 has them.

The trade-off is taken with eyes open: the spec can lag or differ from the server. The check on that is the integration run at each wave end, which exercises main against a live cluster. When it fails on a spec-verified change, the named permalink shows the reviewer accepted the published contract, and the failure is a divergence between contract and server, which is itself worth knowing and reporting upstream. That is a better position than a guess that happened to be wrong.

## Rejected alternatives

**Keep every live-behaviour claim reserved to the operator.** Safe, and it is what ADR 0025 chose when there was no way for the reviewer to check the spec. It costs an operator approval per behaviour-changing PR, which in a 24-issue remediation is most of them, and it treats a claim read off the published schema the same as one made up.

**Narrow the skip to non-destructive operations only.** Considered on review of #170. It does not follow from the evidence: the spec is no more or less reliable for a `DELETE` than for a `GET`, and the destructive cmdlets already carry `ConfirmImpact.High` and the operator's integration run. A narrowing on operation type would add a second taxonomy for the reviewer to apply without changing what is actually known.

**Let the reviewer verify against the local `~/Source/pve_api` checkout.** Unreachable from the runner; only a GitHub-hosted copy is.

**Accept branch-reference links.** Mutable. The traceability the decision depends on holds only if the citation is pinned to a commit.

## Consequences

- The review prompt's "API correctness" item and the "Reserved to the operator" list carry the three-way taxonomy, the permalink form, and the `gh api` invocation.
- The agent contract for remediation waves requires the permalink in every PR body that makes a behaviour claim.
- `GoodOlClint/Proxmox_API` becomes part of the review oracle alongside the enum fixtures. A PR to this repository cannot alter it, which is the property the fixtures lack and why they are code-owned.
- Wave-end integration runs are load-bearing for this decision and stay in the plan; a failure on a spec-verified change is recorded against the permalink, not dismissed as a flake.
- ADR 0025 is not superseded. The review prompt's "claims live PVE behaviour" bullet, which that ADR's commit introduced, now reads as "claims live PVE behaviour that CI does not exercise and the PR does not document against the spec".
