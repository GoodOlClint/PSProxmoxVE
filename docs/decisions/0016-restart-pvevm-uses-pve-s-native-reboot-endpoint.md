# ADR 0016 — Restart-PveVm uses PVE's native reboot endpoint

- **Status:** Accepted
- **Date:** 2026-09-01
- **Deciders:** operator + agent
- **Context source:** integration runs 183/185/186, root-caused on a live PVE 9.2.2 node 2026-09-01. No finding ID.

## Context

Composing a restart client-side races Proxmox's own post-stop cleanup for the guest's config flock:

1. The shutdown completes and the QEMU process exits.
2. `qmeventd` forks `/usr/sbin/qm cleanup <vmid> ...`.
3. The client sees `status == stopped` and immediately posts `status/start`. `vm_start` takes the flock, wins the race, starts a **new** QEMU, releases.
4. `qm cleanup` then takes the flock with a **60 s** timeout and polls `vm_running_locally` for up to **30 s**, holding it the whole time, because it sees the new PID as the old one failing to exit. PVE's own warning names this: `QEMU process $pid for VM $vmid still running (or newly started)`.
5. Every subsequent call fails: `lock_config` defaults to **10 s**, so the client gets `can't lock file '/var/lock/qemu-server/lock-<vmid>.conf' - got timeout`.

Measured on a reproduction (integration run 187), three distinct source constants matching:

```
qmstart    ends  t+3    <- qm cleanup takes the flock, sees the NEW pid
qmstop #1  FAIL  t+14   10 s = lock_config default
qmstop #2  FAIL  t+24   10 s = lock_config default
qmclone    FAIL  t+25    1 s = qmclone's separate source-VM lock timeout
qmstop #3  OK    t+33   <- released; hold was t+3..t+33 = 30 s = cleanup's wait loop
```

This surfaced on PVE 9.2 and not 9.1 because of two May 2026 qemu-server changes: cleanup deduplication, shipped for 9.1.13, and the 30 s cleanup wait. Neither touches the REST surface, so the API changelog showed nothing. **"The API did not change, therefore behaviour did not" is not a valid inference for this class of bug.**

## Decision

`Restart-PveVm` calls `POST /nodes/{node}/qemu/{vmid}/status/reboot` (`VmService.RebootVm`).

`vm_reboot` holds the config lock across the entire shutdown and lets `qm cleanup` perform the restart while it already holds that same lock, so there is no window for a client call to interleave.

```csharp
PveTask Issue() => vmService.RebootVm(session, node, vmid, timeout);

var task = Wait.IsPresent
    ? WaitForStatusTransition(session, node, Issue, vmid, "running", timeout)
    : Issue();
```

## Rejected alternatives

Composing the restart from two client calls — `status/shutdown` then `status/start`. This is what the cmdlet did, and it is what races `qmeventd`'s cleanup:

```csharp
WaitForStatusTransition(session, node, () => vmService.ShutdownVm(session, node, vmid, timeout),
    vmid, "stopped", timeout);
WaitForStatusTransition(session, node, () => vmService.StartVm(session, node, vmid),
    vmid, "running", timeout);
```

## Consequences

**Containers are not affected.** `/nodes/{node}/lxc/{vmid}/status/reboot` does not exist, so `Restart-PveContainer` necessarily keeps shutdown + start and keeps the exposure.

This removes the race only where PVE offers a server-side serialised endpoint. `Set-PveVmConfig`, `Resize-PveVmDisk` and clone have none, which is what [ADR 0020](0020-the-qemu-server-flock-is-retried-never-predicted.md) exists to handle.
