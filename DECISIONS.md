# Architectural Decisions

This file documents patterns that were deliberately chosen or changed, anti-patterns that
were explicitly removed, and constraints that must be maintained. **Read this before writing
any new code.**

---

## D001 — Task polling must use TaskService.WaitForTask

**Status**: Active
**Finding refs**: F032, F033, F036, F058
**Resolved in scan**: 2026-03-22 (for VM/network cmdlets); container snapshot + storage cmdlets still open

### Decision
All task-polling loops must use `TaskService.WaitForTask(upid, session, timeout, progress)`.
Never implement inline `while(true)` or `do/while` polling loops in cmdlet files.

### Rationale
Four VM/network cmdlets (InvokePveNetworkApply, NewPveSnapshot, RestorePveSnapshot,
RemovePveSnapshot) and one guest exec cmdlet had copy-pasted polling loops with no timeout,
causing cmdlets to hang indefinitely if a PVE task stalled. TaskService.WaitForTask has
timeout enforcement, failure detection, and WriteProgress support.

Five additional cmdlets (3 container snapshot + 2 storage) still have this anti-pattern as
of scan 2026-03-22 (F058).

### Anti-pattern (do not reintroduce)
```csharp
// NEVER do this in a cmdlet
while (true)
{
    var status = taskService.GetTask(upid, session);
    if (status.IsFinished) break;
    Thread.Sleep(1000);
}
```

### Correct pattern
```csharp
// Always use this
TaskService.WaitForTask(upid, session, TimeoutSeconds, this);
```

---

## D002 — Password parameters must use SecureString

**Status**: Active
**Finding refs**: F051
**Resolved in scan**: 2026-03-22

### Decision
All cmdlet parameters that accept passwords must use `SecureString` type with
`Marshal.SecureStringToGlobalAllocUnicode` + `ZeroFreeGlobalAllocUnicode` in a
try/finally block for extraction.

### Rationale
Set-PveVmGuestPassword originally accepted a plain `string` password parameter, leaving
the credential in managed memory indefinitely. SecureString minimizes the window of
exposure and is consistent with Connect-PveServer's PSCredential handling.

### Anti-pattern (do not reintroduce)
```csharp
// NEVER accept passwords as plain strings
[Parameter(Mandatory = true)]
public string Password { get; set; }
```

### Correct pattern
```csharp
[Parameter(Mandatory = true)]
public SecureString Password { get; set; }

// In ProcessRecord:
IntPtr ptr = IntPtr.Zero;
try
{
    ptr = Marshal.SecureStringToGlobalAllocUnicode(Password);
    string plainText = Marshal.PtrToStringUni(ptr);
    // Use plainText for API call
}
finally
{
    if (ptr != IntPtr.Zero)
        Marshal.ZeroFreeGlobalAllocUnicode(ptr);
}
```

---

## D003 — URL encoding required for all path parameters

**Status**: Active
**Finding refs**: F050
**Resolved in scan**: 2026-03-22

### Decision
All user-supplied or dynamic values interpolated into API URL paths must be wrapped in
`Uri.EscapeDataString()`. This applies to all service classes.

### Rationale
Snapshot names, node names, user IDs, and other identifiers could theoretically contain
characters that break URL path segments. While most values come from validated sources,
defense-in-depth requires consistent encoding. Applied across all 14 service classes.

### Anti-pattern (do not reintroduce)
```csharp
// NEVER interpolate raw strings into URL paths
var resource = $"nodes/{node}/qemu/{vmid}/snapshot/{snapshotName}";
```

### Correct pattern
```csharp
var resource = $"nodes/{Uri.EscapeDataString(node)}/qemu/{vmid}/snapshot/{Uri.EscapeDataString(snapshotName)}";
```

---

## D004 — No bare catch blocks

**Status**: Active
**Finding refs**: F039
**Resolved in scan**: 2026-03-22

### Decision
No bare `catch { }` or `catch (Exception) { }` blocks. All catch blocks must either:
1. Use a specific exception type (`catch (PveApiException ex)`), or
2. Use a filtered catch with `when` clause that excludes fatal exceptions
   (`catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)`)

### Rationale
Bare catches in PveHttpClient, PveCmdletBase, VmService, ContainerService, and
GetPveVmCmdlet silently swallowed errors, making debugging impossible. Replacing with
filtered or specific catches preserves error visibility while still handling expected
transient failures.

### Anti-pattern (do not reintroduce)
```csharp
// NEVER use bare catches
try { ... }
catch { }

// NEVER catch all exceptions unfiltered
try { ... }
catch (Exception) { /* ignore */ }
```

### Correct pattern
```csharp
// Catch specific exceptions
try { ... }
catch (PveApiException ex) { WriteWarning(ex.Message); }

// Or use filtered catch for status polling
try { ... }
catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
{
    WriteVerbose($"Status poll failed: {ex.Message}");
}
```

---

## D005 — OutputType required on all cmdlets

**Status**: Active
**Finding refs**: F037
**Resolved in scan**: 2026-03-22

### Decision
Every cmdlet must have an `[OutputType(typeof(...))]` attribute declaring its return type.

### Rationale
~54 cmdlets were missing OutputType, degrading IntelliSense, pipeline type inference, and
`Get-Command -OutputType` queries. All 169 cmdlets now have the attribute.

### Correct pattern
```csharp
[Cmdlet(VerbsCommon.Get, "PveVm")]
[OutputType(typeof(VmInfo))]
public sealed class GetPveVmCmdlet : PveCmdletBase
```

---

## D006 — ConfirmImpact.High required for destructive operations

**Status**: Active
**Finding refs**: F011, F034, F042, F043, F062, F063
**Resolved in scan**: 2026-03-22 (for VM cmdlets); container Restart/Suspend still open

### Decision
All cmdlets that perform destructive or disruptive operations must set
`ConfirmImpact = ConfirmImpact.High` in the `[Cmdlet]` attribute. This includes:
- All `Remove-*` cmdlets
- All `Stop-*` cmdlets
- All `Reset-*` cmdlets
- All `Restart-*` cmdlets
- All `Suspend-*` cmdlets
- `Restore-PveSnapshot` and `Restore-PveContainerSnapshot`
- `New-PveTemplate` (irreversible conversion)

### Rationale
Stop-PveVm, Reset-PveVm, Suspend-PveVm, Restart-PveVm, and Remove-PveRole were missing
ConfirmImpact.High, meaning users could accidentally perform disruptive operations without
being prompted. Container counterparts (Restart/Suspend) remain inconsistent as of F062/F063.

### Correct pattern
```csharp
[Cmdlet(VerbsLifecycle.Stop, "PveVm", SupportsShouldProcess = true,
    ConfirmImpact = ConfirmImpact.High)]
```

---

## D007 — All cmdlet classes must be sealed

**Status**: Active
**Finding refs**: F041
**Resolved in scan**: 2026-03-22

### Decision
All cmdlet classes must be declared `sealed`. Cmdlets are not designed for inheritance
and sealing prevents unintended extension.

### Rationale
~95 cmdlets were not sealed. Sealing all 169 cmdlets makes the design intent explicit
and enables potential JIT optimizations.

### Correct pattern
```csharp
public sealed class GetPveVmCmdlet : PveCmdletBase
```

---

## D008 — JSON serialization: Newtonsoft.Json only

**Status**: Active
**Finding refs**: F044
**Resolved in scan**: 2026-03-22

### Decision
Use only `Newtonsoft.Json` (`[JsonProperty]`) for JSON serialization attributes on model
classes. Do not add `System.Text.Json` (`[JsonPropertyName]`) attributes.

### Rationale
The module uses Newtonsoft.Json for all API response deserialization. Having both
`[JsonProperty]` and `[JsonPropertyName]` attributes was redundant and confusing —
System.Text.Json is not used at runtime. All `[JsonPropertyName]` attributes were removed.

### Anti-pattern (do not reintroduce)
```csharp
// NEVER add System.Text.Json attributes alongside Newtonsoft
[JsonProperty("status")]
[JsonPropertyName("status")]  // Don't add this
public string Status { get; set; }
```

### Correct pattern
```csharp
[JsonProperty("status")]
public string Status { get; set; }
```

---

## D009 — Framework targeting: netstandard2.0 for publishable, net10.0+net48 for tests

**Status**: Active
**Finding refs**: F047, F064
**Status note**: net9.0 → net10.0 migration still pending

### Decision
- Publishable projects (`PSProxmoxVE`, `PSProxmoxVE.Core`): Target `netstandard2.0` for
  maximum compatibility (PS 5.1 Desktop + PS 7.x Core).
- Test projects: Target `net10.0` (LTS) and `net48` (Windows PowerShell 5.1 validation).

### Rationale
.NET 9.0 reached EOL in May 2025. The test projects should use the current LTS release
(net10.0). The publishable module must remain on netstandard2.0 to support both Desktop
and Core editions.

---

## D010 — VmId parameters: nullable int with ValidateRange

**Status**: Active
**Finding refs**: F012, F038
**Resolved in scan**: 2026-03-21 (ValidateRange), 2026-03-22 (nullable)

### Decision
VmId parameters must:
1. Use `int?` (nullable) when the parameter is optional (e.g., firewall cmdlets that
   operate at cluster, node, or VM level)
2. Include `[ValidateRange(100, 999999999)]` to match PVE's VMID constraints
3. Use `int` (non-nullable) only when VmId is mandatory

### Rationale
PVE requires VMIDs in range 100-999999999. Without ValidateRange, invalid IDs reach the
API and return confusing errors. Using non-nullable int with default 0 for optional VmId
made it impossible to distinguish "not specified" from "VM 0" in firewall cmdlets.

### Correct pattern
```csharp
// Mandatory VmId
[Parameter(Mandatory = true)]
[ValidateRange(100, 999999999)]
public int VmId { get; set; }

// Optional VmId (e.g., firewall cmdlets)
[Parameter()]
[ValidateRange(100, 999999999)]
public int? VmId { get; set; }
```

---

## D011 — Verb class constants required for cmdlet attributes

**Status**: Active
**Finding refs**: F009
**Resolved in scan**: 2026-03-21

### Decision
All `[Cmdlet]` attributes must use verb class constants (`VerbsCommon.Get`,
`VerbsLifecycle.Start`, etc.) instead of hardcoded string literals.

### Rationale
Reset-PveVm used `[Cmdlet("Reset", ...)]` instead of `VerbsCommon.Reset`. While "Reset"
is an approved verb, using the constant ensures compile-time verification and consistency
with all other cmdlets.

### Anti-pattern (do not reintroduce)
```csharp
[Cmdlet("Reset", "PveVm")]  // Don't use string literals
```

### Correct pattern
```csharp
[Cmdlet(VerbsCommon.Reset, "PveVm")]
```

---

## D012 — Magic strings: extract to named constants

**Status**: Active
**Finding refs**: F049
**Resolved in scan**: 2026-03-22

### Decision
Frequently used string literals (auth header names, token prefixes, etc.) should be
extracted to `const string` fields for maintainability.

### Rationale
Auth header names (`PVEAPIToken=`, `CSRFPreventionToken`) were inline string literals
used in multiple places. Extracting to `const string ApiTokenPrefix` and
`CsrfHeaderName` fields improves maintainability and reduces typo risk.

---

## D013 — Cmdlets must emit only native or module-defined types

**Status**: Active
**Finding refs**: F085
**Resolved in scan**: 2026-03-25

### Decision
All cmdlet output types and public model properties must be native .NET types (`string`,
`int`, `bool`, `Dictionary<string, object?>`, `List<T>`, `PSObject`, `void`) or types
defined within the module itself (`Pve*` classes). Never expose third-party types like
Newtonsoft's `JObject`, `JArray`, or `JToken` in public APIs.

### Rationale
PowerShell enumerates `JArray` unexpectedly and `JObject` properties are not discoverable
via `Get-Member` or tab completion. Users piping module output into `Format-Table`,
`Select-Object`, or `Where-Object` get confusing behavior when the underlying type is a
Newtonsoft container. Native dictionaries and lists work naturally in PowerShell pipelines.

### Anti-pattern (do not reintroduce)
```csharp
// Service returning Newtonsoft type
public JObject GetNodeConfig(...) { ... }

// Model exposing Newtonsoft type
[JsonProperty("members")]
public JArray? Members { get; set; }

// Cmdlet OutputType referencing Newtonsoft type
[OutputType(typeof(JObject))]
```

### Correct pattern
```csharp
// Service returns native dictionary
public Dictionary<string, object?> GetNodeConfig(...) { ... }

// Model uses native type with converter for deserialization
[JsonProperty("members")]
[JsonConverter(typeof(NativeListConverter))]
public List<Dictionary<string, object?>>? Members { get; set; }

// Cmdlet OutputType uses native or module type
[OutputType(typeof(Dictionary<string, object>))]
// or
[OutputType(typeof(PSObject))]
```

---

## D014 — New-PveCluster -Wait blocks until the cluster is quorate

**Status**: Active
**Finding refs**: (none — found via integration run 172, 2026-09-01)
**Resolved in scan**: n/a

### Decision
`New-PveCluster -Wait` returns only after the cluster reports quorum, not merely when the
create task completes. `ClusterConfigService.WaitForQuorum` implements the wait; it polls
`GET /cluster/status` for the `cluster` entry with `quorate = 1` and tolerates transient API
errors while corosync and pmxcfs restart.

The wait is bounded and throws `TimeoutException` on expiry. It follows the `-Wait` timeout
convention already used by `Stop-PveContainer`, `Reset-PveVm` and `New-PveBackup`:
`[ValidateRange(1, 3600)] public int Timeout` with a default (60 here), **no `0 = infinite`**.

Note there are two distinct timeout conventions in this module; do not mix them:
- **`-Wait` waits** (`Timeout`, `int` with a default, range 1-3600, no infinite) — task/state waits.
- **HTTP client timeouts** (`TimeoutSeconds`, `int?`, range 0-int.MaxValue, `0 = infinite`) —
  `Connect-PveServer`, `Send-PveFile`, `Invoke-PveStorageDownload`, which set `HttpClient.Timeout`.

A single-node cluster reaches quorum in seconds (~6 s observed), so a node still not quorate
after 60 s is broken rather than slow.

`-Wait` on every other cmdlet still means "wait for the task". Cluster creation is the
exception because the task completing does not make the cluster usable.

### Rationale
PVE's cluster-create task returns before corosync converges. Until the node is quorate it
rejects a join with `cluster not ready - no quorum?`, so the natural sequence
`New-PveCluster -Wait` → `Add-PveClusterMember` fails intermittently for every caller.

Observed on node A in integration run 172: the create task returned, corosync started ~1 s
later, and `node has quorum` appeared ~6 s after that. The integration test had guarded this
with `Start-Sleep -Seconds 5` — a fixed sleep against a longer, variable convergence — which
is why the cluster tests had never passed.

### Anti-pattern (do not reintroduce)
```powershell
# NEVER guard cluster convergence with a fixed sleep
New-PveCluster -ClusterName 'c1' -Wait
Start-Sleep -Seconds 5
Add-PveClusterMember ...
```

### Correct pattern
```powershell
# -Wait already guarantees quorum; join immediately
New-PveCluster -ClusterName 'c1' -Wait
Add-PveClusterMember ...
```

---

## D015 — Lifecycle -Wait blocks until the guest config lock clears

**Status**: Superseded by D016 (2026-09-01) — the mechanism below is wrong
**Finding refs**: (none — found via integration runs 183/184, 2026-09-01)
**Resolved in scan**: n/a

> **This entry misdiagnosed the failure it was written for.** Two different things in PVE are
> called "lock": the **config lock** (the `lock:` property — `migrate`, `backup`, `clone`,
> `snapshot` — a persisted config field, exposed as `lock` in `status/current`) and the
> **flock** on `/var/lock/qemu-server/lock-<vmid>.conf` taken by `PVE::QemuConfig->lock_config`,
> which is not exposed through the API at all. The integration failures were the flock; this
> entry guards the config lock, which an ordinary start/stop never sets. Run 186 confirmed the
> check never fired — `Restart-PveVm` took 4.11 s, unchanged. The real cause and fix are in
> **D016**. The waiting behaviour described below is harmless and still applies when a genuine
> config lock is present, so the code stays.

### Decision
`WaitForStatusTransition` returns only when the guest reports the expected status **and**
its config lock has cleared. Reaching the status is not enough: PVE publishes the new
status while the operation still holds `/var/lock/qemu-server/lock-<vmid>.conf`, and the
next API call against that guest fails with `got timeout` trying to take the same lock.

`lock` is read from the `status/current` response the poll already fetches — it is present
on both `qemu` and `lxc` status/current and has been since PVE 5.4, well below this
module's 7.0 floor, so this costs no extra request.

If the guest still reports the expected status on the final poll but the lock outlasts
`-Timeout`, the cmdlet returns success rather than throwing. The waited-for operation did
complete; only the settling ran long. This keeps a call that succeeded before the change
from becoming an exception after it.

That fallback tests the **most recent** observation, not "matched at some point during the
wait". A guest that reached the expected status and then drifted away from it has not
satisfied the wait and still raises `PveTaskTimeoutException`. A poll that fails outright
leaves the previous observation standing, so a single API blip is not read as divergence.

This is the same family as D014: a PVE task completing does not mean the resource is ready
for the next operation. D014 is the cluster-quorum instance, D015 the guest-lock instance.

### Rationale
Integration run 183 failed four tests from one cause. `Restart-PveVm -Wait` returned after
4.1 s having observed `running`; the following `Stop-PveVm` spent exactly 10.0 s failing to
acquire the lock, which cascaded into the template convert, clone, and remove tests. Run 184,
the same commit re-run, passed: its status poll happened to take 10.1 s, by which point the
lock had cleared. The same settling happens either way — the only variable is whether the
wait absorbs it or the next caller does.

The check lives in `WaitForStatusTransition` rather than in each cmdlet because all nine
lifecycle call sites route through it.

### Anti-pattern (do not reintroduce)
```csharp
// NEVER treat the status transition alone as "ready for the next operation"
if (string.Equals(effectiveStatus, expectedStatus, StringComparison.OrdinalIgnoreCase))
    return task;
```

### Correct pattern
```csharp
var snapshot = GuestStatusSnapshot.Evaluate(json, expectedStatus);
if (snapshot.StatusMatched && !snapshot.Locked)
    return task;
```

---

## D016 — Restart-PveVm uses PVE's native reboot endpoint

**Status**: Active
**Finding refs**: (none — found via integration runs 183/185/186, root-caused on a live PVE 9.2.2 node 2026-09-01)
**Resolved in scan**: n/a

### Decision
`Restart-PveVm` calls `POST /nodes/{node}/qemu/{vmid}/status/reboot` (`VmService.RebootVm`).
It must **not** compose a restart client-side as `status/shutdown` followed by `status/start`.

### Rationale
Composing the restart races Proxmox's own post-stop cleanup for the guest's config flock:

1. The shutdown completes and the QEMU process exits.
2. `qmeventd` forks `/usr/sbin/qm cleanup <vmid> ...`.
3. The client sees `status == stopped` and immediately posts `status/start`. `vm_start` takes
   the flock, wins the race, starts a **new** QEMU, releases.
4. `qm cleanup` then takes the flock with a **60 s** timeout and polls `vm_running_locally`
   for up to **30 s**, holding it the whole time, because it sees the new PID as the old one
   failing to exit. PVE's own warning names this: `"QEMU process $pid for VM $vmid still
   running (or newly started)"`.
5. Every subsequent call fails: `lock_config` defaults to **10 s**, so the client gets
   `can't lock file '/var/lock/qemu-server/lock-<vmid>.conf' - got timeout`.

Measured on a reproduction (integration run 187), three distinct source constants matching:

```
qmstart    ends  t+3    <- qm cleanup takes the flock, sees the NEW pid
qmstop #1  FAIL  t+14   10 s = lock_config default
qmstop #2  FAIL  t+24   10 s = lock_config default
qmclone    FAIL  t+25    1 s = qmclone's separate source-VM lock timeout
qmstop #3  OK    t+33   <- released; hold was t+3..t+33 = 30 s = cleanup's wait loop
```

`vm_reboot` avoids all of it by holding the config lock across the entire shutdown and letting
`qm cleanup` perform the restart while it already holds that same lock — there is no window for
a client call to interleave.

This surfaced on PVE 9.2 and not 9.1 because of two May 2026 qemu-server changes (cleanup
deduplication, shipped for 9.1.13, and the 30 s cleanup wait). Neither touches the REST surface,
so the API changelog showed nothing — "the API did not change, therefore behaviour did not" is
not a valid inference for this class of bug.

**Containers are not affected by this decision**: `/nodes/{node}/lxc/{vmid}/status/reboot` does
not exist, so `Restart-PveContainer` necessarily keeps shutdown + start.

### Anti-pattern (do not reintroduce)
```csharp
// NEVER compose a VM restart from two client calls — it races qmeventd's cleanup
var shutdownTask = vmService.ShutdownVm(session, node, vmid, timeout);
WaitForStatusTransition(session, node, shutdownTask, vmid, "stopped", timeout);
var startTask = vmService.StartVm(session, node, vmid);
```

### Correct pattern
```csharp
var task = vmService.RebootVm(session, node, vmid, timeout);
if (Wait.IsPresent)
    task = WaitForStatusTransition(session, node, task, vmid, "running", timeout);
```
