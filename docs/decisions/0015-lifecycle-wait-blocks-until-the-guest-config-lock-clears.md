# ADR 0015 — Lifecycle -Wait blocks until the guest config lock clears

- **Status:** Accepted, rescoped 2026-09-01 — covers the config lock only. The flock this was written for is [ADR 0016](0016-restart-pvevm-uses-pve-s-native-reboot-endpoint.md) and [ADR 0020](0020-the-qemu-server-flock-is-retried-never-predicted.md).
- **Date:** 2026-09-01
- **Deciders:** operator + agent
- **Context source:** integration runs 183/184, 2026-09-01; rescoped for issue #113. No finding ID.

## Context

**This entry was written for a failure it does not prevent.** Two different things in PVE are called "lock":

- the **config lock** — the `lock:` property (`migrate`, `backup`, `clone`, `snapshot`), a persisted config field exposed as `lock` in `status/current`;
- the **flock** on `/var/lock/qemu-server/lock-<vmid>.conf` taken by `PVE::QemuConfig->lock_config`, which is not exposed through the API at all.

The integration failures were the flock. This decision guards the config lock, which an ordinary start or stop never sets. Run 186 confirmed the check never fired: `Restart-PveVm` took 4.11 s, unchanged. The guard below is correct for the config lock and stays, but must never be described as covering the flock.

The original observation still stands as motivation. Integration run 183 failed four tests from one cause: `Restart-PveVm -Wait` returned after 4.1 s having observed `running`, and the following `Stop-PveVm` spent exactly 10.0 s failing to acquire the lock, cascading into the template convert, clone and remove tests. Run 184 — the same commit, re-run — passed, because its status poll happened to take 10.1 s by which point the lock had cleared. The same settling happens either way; the only variable is whether the wait absorbs it or the next caller does.

## Decision

`WaitForStatusTransition` returns only when the guest reports the expected status **and** its config lock has cleared.

`lock` is read from the `status/current` response the poll already fetches. It is present on both `qemu` and `lxc` and has been since PVE 5.4, well below this module's 7.0 floor, so this costs no extra request.

If the guest still reports the expected status on the final poll but the lock outlasts `-Timeout`, the cmdlet returns success rather than throwing: the waited-for operation did complete, and only the settling ran long. That fallback tests the **most recent** observation, not "matched at some point during the wait" — a guest that reached the expected status and then drifted away has not satisfied the wait and still raises `PveTaskTimeoutException`. A poll that fails outright leaves the previous observation standing, so a single API blip is not read as divergence.

```csharp
var snapshot = GuestStatusSnapshot.Evaluate(json, expectedStatus);
if (snapshot.StatusMatched && !snapshot.Locked)
    return task;
```

The check lives in `WaitForStatusTransition` rather than in each cmdlet because all nine lifecycle call sites route through it.

## Rejected alternatives

Treating the status transition alone as "ready for the next operation":

```csharp
if (string.Equals(effectiveStatus, expectedStatus, StringComparison.OrdinalIgnoreCase))
    return task;
```

Also rejected, and this is the important one: **attempting to detect the flock before acting**. `snapshot.Locked` reads the config `lock:` property and says nothing about the flock. There is no API surface that does. See [ADR 0020](0020-the-qemu-server-flock-is-retried-never-predicted.md).

## Consequences

Same family as [ADR 0014](0014-new-pvecluster-wait-blocks-until-the-cluster-is-quorate.md): a PVE task completing does not mean the resource is ready for the next operation. 0014 is the cluster-quorum instance, this is the guest config-lock instance.

The flock race that motivated this entry was left unfixed by it, and needed two further decisions: serialise server-side where an endpoint exists (0016), and retry where none does (0020).
