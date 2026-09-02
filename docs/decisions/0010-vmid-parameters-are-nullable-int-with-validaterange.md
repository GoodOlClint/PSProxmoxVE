# ADR 0010 — VmId parameters are nullable int with ValidateRange

- **Status:** Accepted
- **Date:** 2026-03-22
- **Deciders:** unrecorded; adopted during review scans 2026-03-21 (ValidateRange) and 2026-03-22 (nullable)
- **Context source:** `docs/review/findings.json` F012, F038

## Context

PVE accepts VMIDs in the range 100–999999999. Without a `ValidateRange`, an out-of-range value reached the API and came back as a confusing server-side error rather than a parameter-binding failure the user could act on.

Separately, the firewall cmdlets operate at cluster, node or VM level, and used a non-nullable `int` defaulting to 0 for the optional VmId. That made "not specified" and "VM 0" the same value, so the cmdlet could not tell which scope the caller meant.

## Decision

- `[ValidateRange(100, 999999999)]` on every VmId parameter, mandatory or optional.
- `int?` when the parameter is optional, so absence is representable.
- `int` only when VmId is mandatory.

```csharp
[Parameter(Mandatory = true)]
[ValidateRange(100, 999999999)]
public int VmId { get; set; }

[Parameter()]
[ValidateRange(100, 999999999)]
public int? VmId { get; set; }
```

## Rejected alternatives

A non-nullable `int` for optional VmId, using 0 as the sentinel for "not supplied". Rejected because 0 is indistinguishable from a supplied value, and because it silently defeats `ValidateRange` — the default sits outside the valid range and never trips it.

## Consequences

The range check is on the parameter, not in the service, so a service called directly from another service is not covered by it.

`Get-PveTaskList` was found later still missing the attribute on its optional `int?` VmId, which is the failure mode this rule invites: adding `int?` is the visible half and it is easy to stop there.
