# ADR 0004 — No bare catch blocks

- **Status:** Accepted
- **Date:** 2026-03-22
- **Deciders:** unrecorded; adopted during review scan 2026-03-22
- **Context source:** `docs/review/findings.json` F039

## Context

Bare catches in `PveHttpClient`, `PveCmdletBase`, `VmService`, `ContainerService` and `GetPveVmCmdlet` swallowed every error, including ones that had nothing to do with the transient failure the catch was written for. A misconfigured endpoint and a stalled task presented identically: as silence.

## Decision

No `catch { }` and no unfiltered `catch (Exception) { }`. Every catch either names a specific exception type, or filters with a `when` clause that excludes fatal exceptions.

```csharp
catch (PveApiException ex) { WriteWarning(ex.Message); }

catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
{
    WriteVerbose($"Status poll failed: {ex.Message}");
}
```

## Rejected alternatives

Catching everything and continuing, on the theory that a status poll failing is never worth surfacing:

```csharp
try { ... }
catch { }

try { ... }
catch (Exception) { /* ignore */ }
```

Rejected because it also swallows `OutOfMemoryException` and `StackOverflowException`, and because "this particular call is allowed to fail quietly" is a claim that has to be re-checked whenever the body of the `try` grows.

## Consequences

Filtered catches still need somewhere for the message to go — `WriteVerbose` at minimum — or the filter merely moves the silence. This regressed once: F039 was reopened after bare catches reappeared in `VmService.PingGuestAgent` and `Import-PveOva`'s VM-retrieval fallback, and was fixed again.
