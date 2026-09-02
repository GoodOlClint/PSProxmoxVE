# ADR 0006 — ConfirmImpact.High required for destructive operations

- **Status:** Accepted
- **Date:** 2026-03-22
- **Deciders:** unrecorded; adopted during review scan 2026-03-22
- **Context source:** `docs/review/findings.json` F011, F034, F042, F043, F062, F063

## Context

`Stop-PveVm`, `Reset-PveVm`, `Suspend-PveVm`, `Restart-PveVm` and `Remove-PveRole` did not set `ConfirmImpact.High`, so a user could perform a disruptive operation without being prompted — including against the wrong guest.

## Decision

Every cmdlet performing a destructive or disruptive operation sets `ConfirmImpact = ConfirmImpact.High`. That covers all `Remove-*`, `Stop-*`, `Reset-*`, `Restart-*` and `Suspend-*` cmdlets, plus `Restore-PveSnapshot`, `Restore-PveContainerSnapshot`, and `New-PveTemplate` because the conversion is irreversible.

```csharp
[Cmdlet(VerbsLifecycle.Stop, "PveVm", SupportsShouldProcess = true,
    ConfirmImpact = ConfirmImpact.High)]
```

## Rejected alternatives

None recorded. The rule states which verbs qualify rather than choosing between options; the open question at the time was only which cmdlets had been missed.

## Consequences

The container counterparts `Restart-PveContainer` and `Suspend-PveContainer` remained inconsistent with their VM equivalents at scan 2026-03-22 (F062, F063).

A cmdlet whose danger is not obvious from its verb needs the same treatment. The module has no cmdlet for HA `disarm-ha` yet; when one is added it will be a `Disable-`/`Invoke-` verb that this rule's verb list does not cover, while releasing every watchdog in the cluster.
