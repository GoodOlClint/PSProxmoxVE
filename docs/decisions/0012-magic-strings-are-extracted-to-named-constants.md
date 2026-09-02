# ADR 0012 — Magic strings are extracted to named constants

- **Status:** Accepted
- **Date:** 2026-03-22
- **Deciders:** unrecorded; adopted during review scan 2026-03-22
- **Context source:** `docs/review/findings.json` F049

## Context

The auth header names `PVEAPIToken=` and `CSRFPreventionToken` appeared as inline literals at several call sites. A typo in one of them fails at runtime as an authentication error, which is a long way from the cause.

## Decision

String literals used in more than one place — auth header names, token prefixes and the like — are `const string` fields with names, such as `ApiTokenPrefix` and `CsrfHeaderName`.

## Rejected alternatives

None recorded. This is a maintainability convention adopted during review scan 2026-03-22, not a choice between competing designs.

## Consequences

The rule is about repeated literals with protocol meaning. It is not an instruction to hoist every string in the module into a constants class, and it has no mechanical test — a new inline literal is caught by review or not at all.
