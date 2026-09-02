# ADR 0021 — Integration tests prove server semantics; payloads are proven offline

- **Status:** Accepted
- **Date:** 2026-09-02
- **Deciders:** operator + agent
- **Context source:** issue #120 (coverage); #92/#118 (the case that motivated it)

## Context

`Set-PveNetwork` sent `bridge_vlan_aware=0` to clear a VLAN-aware bridge. The API schema advertises a plain optional boolean, so the request is valid and returns success — and PVE merges supplied keys onto the stored stanza and ignores the `0`. The flag never cleared. Only `delete=bridge_vlan_aware` works, and only a run against a real PVE 9 revealed it.

That is what a live cluster is for: server behaviour the schema misdescribes. It is not for checking that a dictionary has the right keys, which a mock proves in milliseconds.

The distinction decides whether the suite scales. Coverage is about 36% of 678 endpoints; the remaining surface is large enough that "every new endpoint gets a live test" puts the integration suite on a growth curve the CI budget cannot absorb. Growth is linear in *live-only* surface, and how much surface is live-only is a design choice, not a given.

## Decision

An integration test must earn its place by testing something only a live PVE can answer. Request-payload correctness — which keys a cmdlet sends, and with what values — is verified offline against the mock `IPveHttpClient` harness.

Concretely:

- New cmdlets route through a `*Service` that accepts `IPveHttpClient`, so their payload is reachable from `PSProxmoxVE.Core.Tests` without a cluster.
- The 37 cmdlets that construct `PveHttpClient` directly are converted to that seam before the next large coverage push, and opportunistically when otherwise touched. Measured against 194 concrete cmdlet files (`src/PSProxmoxVE/Cmdlets/**/*.cs` less the `PveCmdletBase` base class): 155 reach the API only through a `*Service`, 25 only through their own client, 12 do both, and 2 do neither. The service and direct-client sets overlap, so they do not sum to 194.
- The integration suite is tiered: a PR exercises smoke plus the areas its diff touches (`run-integration.sh test <ver> <Area>` already supports this); the full suite runs on merge to `main`.
- Areas whose dependencies cannot exist in CI (ACME needs a CA plus DNS or HTTP reachability) are covered by mock/contract tests asserting request shape, not by a live lane.
- Areas needing a differently-shaped cluster (Ceph needs dedicated block devices per node and wants three monitors) live behind an opt-in provisioning profile, so ordinary runs do not pay for them.

The target shape, which is not yet how the tree reads — `Set-PveNetwork` is one of the 37, and `NetworkService.SetNetwork` has no callers anywhere in the repository:

```csharp
var service = new NetworkService();
service.SetNetwork(session, Node, Iface, config);
```

## Rejected alternatives

A cmdlet that builds its own form and owns its own client. Every field it emits is then verifiable only by provisioning a cluster:

```csharp
using var client = new PveHttpClient(session);
var data = new Dictionary<string, string> { ["type"] = Type };
if (!string.IsNullOrEmpty(Address)) data["address"] = Address!;
client.PutAsync($"nodes/{node}/network/{iface}", data).GetAwaiter().GetResult();
```

Also rejected: a live integration test for every newly covered endpoint. It is the status quo and the reason the question arose — Ceph and certificates alone would add about 66.

## Consequences

The seam conversion (#126) is a precondition for the Ceph (#128) and certificates (#129) work, not a parallel task. Sequencing agreed with the operator 2026-09-02: quick wins (#121–#124), then the seam conversion, then Ceph and certificates. Suite tiering is #127.

`PSProxmoxVE.Core.Tests` references only `PSProxmoxVE.Core`, not the cmdlet assembly, so a payload that stays in the cmdlet has no offline path even in principle.

A tiered PR run is not full validation; merge to `main` remains the gate.
