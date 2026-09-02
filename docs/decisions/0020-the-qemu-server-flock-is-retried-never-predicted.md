# ADR 0020 — The qemu-server flock is retried, never predicted

- **Status:** Accepted
- **Date:** 2026-09-01
- **Deciders:** operator + agent
- **Context source:** issue #113, reproduced 2026-09-01 on a Rosetta-emulated client. No finding ID.

## Context

[ADR 0015](0015-lifecycle-wait-blocks-until-the-guest-config-lock-clears.md) tried to predict the lock and guarded the wrong one. [ADR 0016](0016-restart-pvevm-uses-pve-s-native-reboot-endpoint.md) removed the race for `Restart-PveVm` by handing the ordering to PVE, but that only works where a server-side serialised endpoint exists. `Set-PveVmConfig`, `Resize-PveVmDisk` and clone have none, so for them the choice is retry or nothing.

CI never showed this. Runs 189–200 were green because the CI client is fast enough to win the race. It reproduces on a client roughly 40% slower — the CI container image run under Docker Desktop's Rosetta emulation on Apple Silicon — which failed `Should hard-reset a running VM`, `Should clone a VM` and `Should resize a VM disk (Resize-PveVmDisk)` on the same commit CI passed. **A green CI run is not evidence about this class of bug.**

## Decision

An operation PVE rejects with `can't lock file '<guest lock path>' - got timeout` is reissued for a bounded window (`GuestLockRetry.DefaultWindow`, 45 s).

Two seams implement it, and both are required:

- **`PveHttpClient.SendAsync`** — retries the request itself. This covers every operation PVE serialises inside the API handler, where the failure arrives as a 500: `Set-PveVmConfig`, `Resize-PveVmDisk`'s config writes, and every future call through the client. The private send takes a `Func<HttpRequestMessage>` rather than a request, because an `HttpRequestMessage` cannot be sent twice.
- **`PveCmdletBase.InvokeGuestTask`** — reissues the API call *and* re-waits its task. PVE takes the flock inside the forked worker for most guest operations (`qmreset`, `qmclone`), so the POST returns 200 with a UPID and the failure appears only in the task's exit status. The HTTP layer cannot see it and cannot retry it. `WaitForStatusTransition` routes through this helper, which is why it takes a `Func<PveTask>` rather than an already-issued `PveTask`.

```csharp
PveTask Issue() => vmService.CloneVm(session, sourceNode, vmid, newid, name, targetNode, full);

var task = Wait.IsPresent
    ? InvokeGuestTask(session, sourceNode, Issue)
    : Issue();
```

Reissuing is safe **only** for a failure to *enter* `lock_config`, which PVE raises before the operation does any work. `GuestLockRetry.IsLockTimeout` must keep both properties that establish this, and no failure may be added to it without them:

- **Path-specific.** `PVE::Tools::lock_file` emits identical wording for storage, LVM, HA, backup and firewall locks. Those are taken mid-worker and carry no such guarantee, so the match names the two guest config paths (`/var/lock/qemu-server/lock-<vmid>.conf`, `/run/lock/lxc/pve-config-<vmid>.lock`) rather than the generic phrasing.
- **Anchored at the start of what PVE said.** `qmclone` is the operation that makes this matter: its worker creates and locks the target config, allocates disks, then re-locks. A timeout at one of those later points reads the same as one at entry, and reissuing it would hit `check_vmid_unused` — "VM `<newid>` already exists" — leaving an orphaned guest behind. PVE prefixes the late form with its own context (`clone failed: ...`), so anchoring rejects it. `Resize-PveVmDisk -Size '+1G'` is the case where getting this wrong is irreversible rather than merely messy.

The anchor only works against the raw text, so the predicate reads `PveTaskFailedException.ExitStatus` and `PveApiException.ApiMessage` — never `Exception.Message`, which both types prefix with their own context. `ApiMessage` exists for this.

The window is 45 s because `qm cleanup` holds the flock while polling `vm_running_locally` for up to 30 s, and each rejected attempt first burns PVE's own 10 s `lock_config` timeout.

## Rejected alternatives

Observing the flock before acting. PVE does not expose it in `status/current` or anywhere else, so there is nothing to observe:

```csharp
if (!snapshot.Locked)
    return task;   // reads the config `lock:` property; says nothing about the flock
```

Binding the retry to the cmdlet's `-Timeout`. See the consequence below — it defeats the fix at exactly the values that need it.

Threading a shared retry budget through both seams. See below: the overlap costs a longer wait before the same failure, never a different outcome.

## Consequences

- **`-Timeout` does not bound the retry.** It is documented as the budget for the status transition, and `WaitForStatusTransition` starts counting only after the operation's task completes. `Reset-PveVm -Wait -Timeout 30` needed about 31 s of retrying in the run that verified this change.
- **The two seams nest.** A cmdlet operation rejected synchronously burns the HTTP layer's window inside `InvokeGuestTask`'s.
- The window bounds when a *new* attempt may start, not total wall clock: an attempt beginning just inside the window still runs to its own conclusion, so the real ceiling is roughly one attempt longer.

**Not yet adopted.** `InvokeGuestTask` is the correct seam for every cmdlet that issues a guest operation and waits on its task. `Remove-PveVm`, `Move-PveVm`, the snapshot and template cmdlets, and the container equivalents still call `TaskService.WaitForTask` directly and remain exposed to the same race. They adopt the helper as they are next touched.
