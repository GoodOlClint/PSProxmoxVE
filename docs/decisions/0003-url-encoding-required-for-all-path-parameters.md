# ADR 0003 — URL encoding required for all path parameters

- **Status:** Accepted
- **Date:** 2026-03-22
- **Deciders:** unrecorded; adopted during review scan 2026-03-22
- **Context source:** `docs/review/findings.json` F050

## Context

Snapshot names, node names, user IDs and similar identifiers were interpolated raw into API URL paths. Most reach the module from validated sources, but nothing in the code enforced that, and a value carrying `/` or `?` would silently change which endpoint was called.

## Decision

Every user-supplied or dynamic value interpolated into an API URL path is wrapped in `Uri.EscapeDataString()`. This applies to all service classes without exception.

```csharp
var resource = $"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/snapshot/{Uri.EscapeDataString(snapshotName)}";
```

## Rejected alternatives

Encoding only the parameters that can plausibly carry a separator, and trusting validation upstream for the rest. Rejected because the audit then has to be redone on every new call site, and the reader cannot tell a deliberate omission from an oversight:

```csharp
var resource = $"nodes/{node}/qemu/{vmid}/snapshot/{snapshotName}";
```

## Consequences

Applied across all 14 service classes at the time of the decision. Form-encoded *bodies* are a separate matter — PVE does not URL-decode form values in some internal consumers, so the cluster-join path deliberately sends minimally encoded values.
