# ADR 0014 — New-PveCluster -Wait blocks until the cluster is quorate

- **Status:** Accepted
- **Date:** 2026-09-01
- **Deciders:** operator + agent
- **Context source:** integration run 172, 2026-09-01. No finding ID.

## Context

PVE's cluster-create task returns before corosync converges. Until the node is quorate it rejects a join with `cluster not ready - no quorum?`, so the natural sequence `New-PveCluster -Wait` then `Add-PveClusterMember` failed intermittently for every caller.

Observed on node A in integration run 172: the create task returned, corosync started about a second later, and `node has quorum` appeared about six seconds after that. The integration test had guarded this with `Start-Sleep -Seconds 5` — a fixed sleep against a longer, variable convergence — which is why the cluster tests had never passed.

## Decision

`New-PveCluster -Wait` returns only once the cluster reports quorum, not when the create task completes. `ClusterConfigService.WaitForQuorum` polls `GET /cluster/status` for the `cluster` entry with `quorate = 1`, tolerating transient API errors while corosync and pmxcfs restart.

The wait is bounded and throws `TimeoutException` on expiry, following the `-Wait` timeout convention already used by `Stop-PveContainer`, `Reset-PveVm` and `New-PveBackup`: `[ValidateRange(1, 3600)] public int Timeout`, default 60, **no `0 = infinite`**. A single-node cluster reaches quorum in seconds, so a node still not quorate after 60 s is broken rather than slow.

`-Wait` on every other cmdlet still means "wait for the task". Cluster creation is the exception because the task completing does not make the cluster usable.

```powershell
New-PveCluster -ClusterName 'c1' -Wait
Add-PveClusterMember ...
```

## Rejected alternatives

A fixed sleep between create and join, which is what the integration test did:

```powershell
New-PveCluster -ClusterName 'c1' -Wait
Start-Sleep -Seconds 5
Add-PveClusterMember ...
```

It is wrong in both directions — too short for a slow convergence, wasted time on a fast one — and it puts the workaround in every caller instead of in the cmdlet.

## Consequences

There are now two distinct timeout conventions in the module and they must not be mixed:

- **`-Wait` waits** — `Timeout`, `int` with a default, range 1–3600, no infinite. Task and state waits.
- **HTTP client timeouts** — `TimeoutSeconds`, `int?`, range 0–`int.MaxValue`, `0 = infinite`. `Connect-PveServer`, `Send-PveFile`, `Invoke-PveStorageDownload`, which set `HttpClient.Timeout`.

This is the first instance of a general problem: a PVE task completing does not mean the resource is ready for the next operation. [ADR 0015](0015-lifecycle-wait-blocks-until-the-guest-config-lock-clears.md) is the guest-lock instance of the same thing.
