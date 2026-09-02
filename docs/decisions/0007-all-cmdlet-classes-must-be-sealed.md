# ADR 0007 — All cmdlet classes must be sealed

- **Status:** Accepted
- **Date:** 2026-03-22
- **Deciders:** unrecorded; adopted during review scan 2026-03-22
- **Context source:** `docs/review/findings.json` F041

## Context

Around 95 of the module's 169 cmdlet classes were not `sealed`. Cmdlets in this module are leaves — they derive from `PveCmdletBase` and nothing derives from them — but the code did not say so.

## Decision

Every cmdlet class is declared `sealed`.

```csharp
public sealed class GetPveVmCmdlet : PveCmdletBase
```

## Rejected alternatives

None recorded. Adopted as a convention during review scan 2026-03-22. Beyond making the design intent explicit, sealing enables potential JIT devirtualisation. All 169 cmdlets are now sealed.

## Consequences

Applies to cmdlets only. `PveCmdletBase` is the shared base and is deliberately not sealed; a rule stated as "all cmdlet classes" has to exclude it, and a mechanical check that does not will produce a false positive on every run.
