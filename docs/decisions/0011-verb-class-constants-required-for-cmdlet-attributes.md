# ADR 0011 — Verb class constants required for cmdlet attributes

- **Status:** Accepted
- **Date:** 2026-03-21
- **Deciders:** unrecorded; adopted during review scan 2026-03-21
- **Context source:** `docs/review/findings.json` F009

## Context

`Reset-PveVm` declared `[Cmdlet("Reset", ...)]` with a string literal while every other cmdlet used the verb constants. "Reset" is an approved verb, so nothing was broken — but a typo in that position produces a cmdlet with an unapproved verb, which surfaces only as a module-load warning.

## Decision

Every `[Cmdlet]` attribute names its verb through the verb classes — `VerbsCommon`, `VerbsLifecycle` and the rest — never as a string literal.

```csharp
[Cmdlet(VerbsCommon.Reset, "PveVm")]
```

## Rejected alternatives

The string literal. It compiles, reads identically, and moves verb validation from the compiler to a runtime warning nobody reads:

```csharp
[Cmdlet("Reset", "PveVm")]
```

## Consequences

The noun half is still a string literal and gets no such protection; the `Pve` prefix convention is enforced by review, not by the compiler.
