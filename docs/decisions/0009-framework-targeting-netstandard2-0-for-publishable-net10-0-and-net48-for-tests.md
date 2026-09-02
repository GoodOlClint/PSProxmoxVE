# ADR 0009 — Framework targeting: netstandard2.0 for publishable, net10.0 and net48 for tests

- **Status:** Accepted
- **Date:** 2026-03-22
- **Deciders:** unrecorded; adopted during review scan 2026-03-22
- **Context source:** `docs/review/findings.json` F047, F064. The date is the scan that recorded it; no decision date was captured.

## Context

.NET 9.0 reached end of life in May 2025, and the test projects still targeted it.

The publishable module has a harder constraint than the tests do: it must load in both Windows PowerShell 5.1 (Desktop) and PowerShell 7.x (Core), which only `netstandard2.0` satisfies.

## Decision

- Publishable projects (`PSProxmoxVE`, `PSProxmoxVE.Core`) target `netstandard2.0` and nothing else.
- Test projects target `net10.0` (current LTS) and `net48` (to validate the Windows PowerShell 5.1 path).

## Rejected alternatives

Multi-targeting the publishable projects as `netstandard2.0;net10.0;net48`. It was briefly in place and inflates the published module with framework-specific assemblies PowerShell will not use, for no compatibility gain over `netstandard2.0` alone.

## Consequences

The net9.0 → net10.0 move on the test projects was still outstanding when this was recorded, alongside two related dependency pins the same decision governs: the `System.Management.Automation` pin (F064) and the workflow SDK versions (F073, F079). All are now resolved.

Anything the module needs that `netstandard2.0` lacks has to be polyfilled or avoided; that constraint does not apply to test code, which is why the split exists.
