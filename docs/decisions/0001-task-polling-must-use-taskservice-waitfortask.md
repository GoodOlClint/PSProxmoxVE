# ADR 0001 — Task polling must use TaskService.WaitForTask

- **Status:** Accepted
- **Date:** 2026-03-22
- **Deciders:** unrecorded; adopted during review scan 2026-03-22
- **Context source:** `docs/review/findings.json` F032, F033, F036, F058

## Context

Four VM/network cmdlets (`Invoke-PveNetworkApply`, `New-PveSnapshot`, `Restore-PveSnapshot`, `Remove-PveSnapshot`) and one guest exec cmdlet had copy-pasted polling loops with no timeout, so a cmdlet hung indefinitely if a PVE task stalled.

`TaskService.WaitForTask` already has timeout enforcement, failure detection and `WriteProgress` support. Every inline loop was a worse reimplementation of it.

## Decision

All task-polling loops use `TaskService.WaitForTask(upid, session, timeout, progress)`. No cmdlet file implements its own `while(true)` or `do`/`while` polling.

```csharp
TaskService.WaitForTask(upid, session, TimeoutSeconds, this);
```

## Rejected alternatives

An inline poll in the cmdlet. It carries no timeout, no failure detection and no progress reporting, and each copy drifts from the others:

```csharp
while (true)
{
    var status = taskService.GetTask(upid, session);
    if (status.IsFinished) break;
    Thread.Sleep(1000);
}
```

## Consequences

Five cmdlets — three container snapshot, two storage — still carried the inline form at scan 2026-03-22 (F058). They adopt `WaitForTask` as they are next touched.

`WaitForTask` is also where re-authentication on a 401 belongs, rather than in each caller.
