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

**Status**: Active, rescoped 2026-09-01 — it guards the config lock only, never the flock
**Finding refs**: (none — found via integration runs 183/184, 2026-09-01; rescoped for #113)
**Resolved in scan**: n/a

> **This entry was written for a failure it does not prevent.** Two different things in PVE are
> called "lock": the **config lock** (the `lock:` property — `migrate`, `backup`, `clone`,
> `snapshot` — a persisted config field, exposed as `lock` in `status/current`) and the
> **flock** on `/var/lock/qemu-server/lock-<vmid>.conf` taken by `PVE::QemuConfig->lock_config`,
> which is not exposed through the API at all. The integration failures were the flock; this
> entry guards the config lock, which an ordinary start/stop never sets. Run 186 confirmed the
> check never fired — `Restart-PveVm` took 4.11 s, unchanged. The flock is handled by **D016**
> (serialise server-side where an endpoint exists) and **D020** (retry where none does). The
> guard below is correct for the config lock and stays, but must never be described as covering
> the flock.

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
WaitForStatusTransition(session, node, () => vmService.ShutdownVm(session, node, vmid, timeout),
    vmid, "stopped", timeout);
WaitForStatusTransition(session, node, () => vmService.StartVm(session, node, vmid),
    vmid, "running", timeout);
```

### Correct pattern
```csharp
PveTask Issue() => vmService.RebootVm(session, node, vmid, timeout);

var task = Wait.IsPresent
    ? WaitForStatusTransition(session, node, Issue, vmid, "running", timeout)
    : Issue();
```

---

## D017 — CI runs two lanes: a pinned gating lane and a report-only currency lane

**Status**: Active
**Finding refs**: (none — arose from integration runs 176–180, root-caused 2026-09-01)
**Resolved in scan**: n/a

### Decision
CI provisions nested PVE nodes in two distinct modes, and they must not be merged into one:

- **Lane 1, `integration-tests.yml`** — nodes stay pinned to what the ISO ships. `first-boot.sh`
  must never run `apt-get upgrade` or `dist-upgrade`. This lane gates merges.
- **Lane 2, `package-currency.yml`** — nodes are `dist-upgrade`d to current PVE and the suite runs
  against them. **Report-only**: test failures do not fail the job.

Both declare `concurrency: group: integration-tests`. They drive the same nested VMIDs on the same
parent node, so they must never run at once.

### Rationale
`apt-get upgrade` holds back packages that need new dependencies. On the nested nodes that produced
`pve-cluster` 9.1.6 against `libpve-cluster-api-perl` 9.1.0 — a combination no real install ever
has — and its symptom was not a package error. The node's pmxcfs came back in local mode after a
cluster join, `/etc/pve/corosync.conf` never appeared, and the node reported `online=0` while
corosync itself had healthy 2-node membership. Three CI runs went into diagnosing that, and removing
the upgrade was the entire fix: run 180 was the first fully green integration run.

So the pin is what makes lane 1 a trustworthy merge gate. But a permanently pinned CI never
exercises the module against a current PVE, and that gap is exactly where an upstream regression
would hide. Lane 2 closes it without putting the instability back into the gating path.

Report-only is deliberate. A scheduled job that goes red on an upstream change nobody has chosen to
chase becomes noise, and a noisy cron gets ignored — which is the failure mode that makes a canary
worthless. The signal is the rolling issue and the recorded package set, not the check colour.

**Consequence accepted**: a module genuinely broken against current PVE shows a green weekly check
plus an updated issue. Operator ruling 2026-09-01, to be revisited after a few releases.

A failure of the lane's own machinery — provisioning, the upgrade, the reboot, an unreachable node —
still fails the job. `run-integration.sh` returns 3 for a genuine test failure and 4 when it cannot
reach or authenticate to a node; only 3 is suppressed. Suppressing both would let a botched reboot
report success while the lane learned nothing.

### Amendment 2026-09-01 — the pin covers the test tooling, not just the nested PVE packages

The gating lane's own tooling is pinned by exact version for the same reason its nested PVE packages
are: `Pester` in `tests/Dockerfile.test` (`ARG PESTER_VERSION`) and in `.github/workflows/unit-tests.yml`
(`env.PESTER_VERSION`), installed and imported with `-RequiredVersion` at every site, including the
suite's own import inside the container. The Dockerfile promotes the ARG to `ENV` so the version is
discoverable at runtime. Both files must name the same version, and `shell-selfchecks` asserts it.

Before this, both sites used `-MinimumVersion 5.0` with no ceiling. The image is rebuilt on every CI
run and Pester is installed fresh on every unit-test run, so PSGallery decided the version — a new
major could reach the merge gate with no commit to this repository, surfacing as unexplained test
breakage on whichever PR happened to run next. That is the same class of moving input the lane split
exists to eliminate; the difference is only that it moves in the test runner rather than in PVE.

It had already happened silently: steps named "Install Pester 5" were resolving 6.1.0 on both the
PowerShell 5.1 and 7.x legs, because Pester 6 declares `PowerShellVersion 5.1` and so installs on
Windows PowerShell too. Nothing broke — the suite uses only constructs common to 5 and 6 — but
nobody chose it.

Bumping is a deliberate commit that changes both files together.

### Anti-pattern (do not reintroduce)
```bash
# NEVER in first-boot.sh — this is the mismatch that left a node unclustered
apt-get update -qq
apt-get -y upgrade
```

### Correct pattern
```bash
# first-boot.sh installs only what provisioning needs; the ISO is the pin
apt-get update -qq
apt-get install -y -qq --no-install-recommends qemu-guest-agent open-iscsi
```
```yaml
# package-currency.yml opts in explicitly; lane 1 never sets this
env:
  PVE_DIST_UPGRADE: '1'
```

---

## D018 — The currency lane reboots after dist-upgrade, and proves it rebooted

**Status**: Active
**Finding refs**: (none — found in pre-push review of PR #106, 2026-09-01)
**Resolved in scan**: n/a

### Decision
After `dist-upgrade`, `prepare-test-environment.sh` reboots the node **unconditionally** and then
**verifies the reboot happened** by comparing `/proc/sys/kernel/random/boot_id` before and after.
An unchanged boot id is fatal. The reboot lives here, not in `first-boot.sh`.

### Rationale
A PVE `dist-upgrade` pulls `proxmox-kernel-*`. Without a reboot the node runs new userspace on the
old kernel, so the lane records a package set it never actually ran and is blind to kernel
regressions — it would report "current PVE" while testing something that never booted.

The reboot cannot live in `first-boot.sh`: that runs `ordering = "fully-up"` while the parent is
still polling, so `wait-for-pve.sh` can discover the IP, see the API, pass auth, and then have the
node reboot out from under provisioning. That presents as an intermittent network fault.

It is unconditional rather than gated on `/var/run/reboot-required`, because that file comes from
`update-notifier-common`, which is not guaranteed present on a PVE node.

Verification is the part that is easy to omit and was omitted in the first draft. `ssh … reboot`
returns non-zero when the connection dies, so it needs `|| true` — which swallows *every* ssh
failure, including the reboot never being issued. `wait-for-api.sh` then matches the
**still-running pre-reboot** pveproxy on its first poll and returns `responsive after 0s`. The
script exits 0 having proved nothing. A blind `sleep` before polling does not fix this; it is wrong
in both directions and verifies nothing either way.

Order matters: prove the boot id changed first (ssh returns before pveproxy does), then wait for the
API, then wait for pmxcfs — `pvesm set` writes `/etc/pve/storage.cfg`, which needs `/etc/pve`
mounted, and on a fresh boot that lags the API by seconds.

### Anti-pattern (do not reintroduce)
```bash
# NEVER — `|| true` hides a reboot that never happened, and the poll then
# matches the pre-reboot node and returns immediately
${SSH_CMD} "systemctl reboot" || true
sleep 30
bash "${SCRIPT_DIR}/wait-for-api.sh" "${NESTED_IP}" 8006 600
```

### Correct pattern
```bash
boot_before="$(${SSH_CMD} "cat /proc/sys/kernel/random/boot_id")"
${SSH_CMD} "systemctl reboot" || true
boot_after=""
for _ in $(seq 1 60); do
    boot_after="$(${SSH_CMD} "cat /proc/sys/kernel/random/boot_id" 2>/dev/null || true)"
    [[ -n "${boot_after}" && "${boot_after}" != "${boot_before}" ]] && break
    sleep 5
done
if [[ -z "${boot_after}" || "${boot_after}" == "${boot_before}" ]]; then
    echo "ERROR: ${NESTED_IP} did not reboot (boot_id unchanged)" >&2
    exit 1
fi
bash "${SCRIPT_DIR}/wait-for-api.sh" "${NESTED_IP}" 8006 600
```

---

## D019 — Local dev calls run-integration.sh directly; there is no wrapper script

**Status**: Active
**Finding refs**: (none — found auditing the local dev path against the post-ARC CI, 2026-09-01)
**Resolved in scan**: n/a

### Decision
`tests/infrastructure/scripts/run-integration.sh` is the only entry point to the
provision → test → cleanup lifecycle, for CI and for local development alike. Local runs
invoke it inside the `dev-infra` container — the same image CI runs its jobs in. Do not add
a convenience wrapper around it.

Build and unit tests need no container at all; they run natively against the solution.

### Rationale
`tests/dev.ps1` was a 291-line PowerShell wrapper over roughly six `docker compose` and
`docker exec` calls. Every capability it had was already available elsewhere: build and unit
tests are plain `dotnet` and `Invoke-Pester` invocations, and the module build it performed
is duplicated inside `run-integration.sh` itself, which publishes and installs the module
before running the suite.

Being a second entry point, it drifted from the script it wrapped and from the CI it claimed
to replicate. By the time it was removed it still offered a `-Version 8` leg retired in #88,
mounted the Docker socket for storage containers replaced by the storage VM in #87, and
defaulted its remote-host examples to a runner decommissioned in the ARC migration.

Four documentation files described a positional calling convention (`./tests/dev.ps1 test`)
that did not do what it read as. The script took its actions from switches (`-Test`), but
also declared `[string[]] $Tests`, so the bare word bound to `-Tests` — the integration-area
filter. With no action switch set, the script then fell through to its `-Shell` default and
silently opened an interactive container shell. Every documented command was wrong, and
wrong in the quietest possible way: it succeeded at something nobody asked for.

A wrapper that must be kept in sync with the thing it wraps earns its place only when it
removes real friction. This one removed none.

### Anti-pattern (do not reintroduce)
A `dev.ps1`, `Makefile` target, or shell function that re-implements provisioning steps,
module installation, or test invocation. If a local flow is awkward, fix it in
`run-integration.sh` so CI gets the fix too.

---

## D020 — The qemu-server flock is retried, never predicted

**Status**: Active
**Finding refs**: (none — issue #113, reproduced 2026-09-01 on a Rosetta-emulated client)
**Resolved in scan**: n/a

### Decision
An operation PVE rejects with `can't lock file '<guest lock path>' - got timeout` is reissued
for a bounded window (`GuestLockRetry.DefaultWindow`, 45 s). Nothing in the module may attempt
to *detect* that the flock is held before acting.

Two seams implement it, and both are required:

- **`PveHttpClient.SendAsync`** — retries the request itself. This covers every operation PVE
  serialises inside the API handler, where the failure arrives as a 500: `Set-PveVmConfig`,
  `Resize-PveVmDisk`'s config writes, and every future call that goes through the client.
  The private send takes a `Func<HttpRequestMessage>` rather than a request because an
  `HttpRequestMessage` cannot be sent twice.
- **`PveCmdletBase.InvokeGuestTask`** — reissues the API call *and* re-waits its task. PVE takes
  the flock inside the forked worker for most guest operations (`qmreset`, `qmclone`), so the
  POST returns 200 with a UPID and the failure only appears in the task's exit status. The HTTP
  layer cannot see it and cannot retry it. `WaitForStatusTransition` routes through this helper,
  which is why it takes a `Func<PveTask>` instead of an already-issued `PveTask`.

Reissuing is safe **only** for a failure to *enter* `lock_config`, which PVE raises before the
operation does any work. `GuestLockRetry.IsLockTimeout` must keep both properties that establish
this, and no failure may be added to it without them:

- **Path-specific.** `PVE::Tools::lock_file` emits the identical wording for storage, LVM, HA,
  backup and firewall locks. Those are taken mid-worker and carry no such guarantee, so the match
  names the two guest config paths (`/var/lock/qemu-server/lock-<vmid>.conf`,
  `/run/lock/lxc/pve-config-<vmid>.lock`) rather than the generic phrasing.
- **Anchored at the start of what PVE said.** `qmclone` is the operation that makes this matter:
  its worker creates and locks the target config, allocates disks, then re-locks. A timeout at one
  of those later points reads the same as one at entry, and reissuing it would hit
  `check_vmid_unused` — "VM <newid> already exists" — leaving an orphaned guest behind. PVE
  prefixes the late form with its own context (`clone failed: ...`), so anchoring rejects it.
  `Resize-PveVmDisk -Size '+1G'` is the case where getting this wrong is irreversible rather than
  merely messy.

The anchor only works against the raw text, so the predicate reads
`PveTaskFailedException.ExitStatus` and `PveApiException.ApiMessage` — never `Exception.Message`,
which both types prefix with their own context. `ApiMessage` exists for this.

### Rationale
D015 tried to predict the lock and guarded the wrong one (see its note). D016 removed the race
for `Restart-PveVm` by handing the ordering to PVE, but that only works where a server-side
serialised endpoint exists. `Set-PveVmConfig`, `Resize-PveVmDisk` and clone have none, so for
them the choice is retry or nothing.

The window is 45 s because `qm cleanup` holds the flock while polling `vm_running_locally` for
up to 30 s, and each rejected attempt first burns PVE's own 10 s `lock_config` timeout. It bounds
when a *new* attempt may start, not total wall clock: an attempt beginning just inside the window
still runs to its own conclusion, so the real ceiling is roughly one attempt longer.

Two consequences are deliberate, and both are load-bearing enough to state rather than discover:

- **`-Timeout` does not bound the retry.** It is documented as the budget for the status
  transition, and `WaitForStatusTransition` starts counting it only after the operation's task
  completes. Binding the retry to it would defeat the fix at exactly the values that need it —
  `Reset-PveVm -Wait -Timeout 30` needed ~31 s of retrying in the run that verified this change.
- **The two seams nest.** A cmdlet operation rejected synchronously burns the HTTP layer's window
  inside `InvokeGuestTask`'s. The overlap costs a longer wait before the same failure, never a
  different outcome, so it is not worth threading a shared budget through both layers.

CI never showed this. Runs 189–200 were green because the CI client is fast enough to win the
race. It reproduces on a client roughly 40% slower — the CI container image run under Docker
Desktop's Rosetta emulation on Apple Silicon — which failed `Should hard-reset a running VM`,
`Should clone a VM` and `Should resize a VM disk (Resize-PveVmDisk)` on the same commit CI
passed. **A green CI run is not evidence about this class of bug.**

### Not yet adopted
`InvokeGuestTask` is the correct seam for every cmdlet that issues a guest operation and waits
on its task. `Remove-PveVm`, `Move-PveVm`, the snapshot and template cmdlets, and the container
equivalents still call `TaskService.WaitForTask` directly and remain exposed to the same race.
They adopt the helper as they are next touched.

### Anti-pattern (do not reintroduce)
```csharp
// NEVER try to observe the flock — PVE does not expose it in status/current or anywhere else
if (!snapshot.Locked)
    return task;   // reads the config `lock:` property; says nothing about the flock
```

### Correct pattern
```csharp
PveTask Issue() => vmService.CloneVm(session, sourceNode, vmid, newid, name, targetNode, full);

var task = Wait.IsPresent
    ? InvokeGuestTask(session, sourceNode, Issue)
    : Issue();
```

---

## D021 — Integration tests prove server semantics; payloads are proven offline

**Status**: Active
**Issue refs**: #120 (coverage), #92/#118 (the case that motivated it)
**Decided**: 2026-09-02

### Decision
An integration test must earn its place by testing something only a live PVE can answer.
Request-payload correctness — which keys a cmdlet sends, and with what values — is verified
offline against the mock `IPveHttpClient` harness.

Concretely:
- New cmdlets route through a `*Service` that accepts `IPveHttpClient`, so their payload is
  reachable from `PSProxmoxVE.Core.Tests` without a cluster.
- The 37 cmdlets that construct `PveHttpClient` directly are converted to that seam before the
  next large coverage push, and opportunistically when otherwise touched. Measured against 194
  concrete cmdlet files (`src/PSProxmoxVE/Cmdlets/**/*.cs` less the `PveCmdletBase` base class):
  155 reach the API only through a `*Service`, 25 only through their own client, 12 do both, and
  2 do neither. The service and direct-client sets overlap, so they do not sum to 194.
- The integration suite is tiered: a PR exercises smoke plus the areas its diff touches
  (`run-integration.sh test <ver> <Area>` already supports this); the full suite runs on merge
  to `main`.
- Areas whose dependencies cannot exist in CI (ACME needs a CA plus DNS or HTTP reachability)
  are covered by mock/contract tests asserting request shape, not by a live lane.
- Areas needing a differently-shaped cluster (Ceph needs dedicated block devices per node and
  wants three monitors) live behind an opt-in provisioning profile, so ordinary runs do not pay
  for them.

### Rationale
`Set-PveNetwork` sent `bridge_vlan_aware=0` to clear a VLAN-aware bridge. The API schema
advertises a plain optional boolean, so the request is valid and returns success — and PVE
merges supplied keys onto the stored stanza and ignores the `0`. The flag never cleared. Only
`delete=bridge_vlan_aware` works, and only a run against a real PVE 9 revealed it.

That is what a live cluster is for: server behaviour the schema misdescribes. It is not for
checking that a dictionary has the right keys, which a mock proves in milliseconds.

The distinction matters because it decides whether the suite scales. Coverage is ~36% of 678
endpoints; the remaining surface is large enough that "every new endpoint gets a live test" puts
the integration suite on a growth curve the CI budget cannot absorb. Growth is linear in
*live-only* surface, and how much surface is live-only is a design choice, not a given.

### Anti-pattern (do not reintroduce)
```csharp
// A cmdlet that builds its own form and owns its own client has no offline seam:
// every field below is verifiable only by provisioning a cluster.
using var client = new PveHttpClient(session);
var data = new Dictionary<string, string> { ["type"] = Type };
if (!string.IsNullOrEmpty(Address)) data["address"] = Address!;
client.PutAsync($"nodes/{node}/network/{iface}", data).GetAwaiter().GetResult();
```

### Correct pattern
This is the target shape, not a form the tree already takes. `Set-PveNetwork` is one of the 37,
and `NetworkService.SetNetwork` has no callers anywhere in the repository.

```csharp
// Service takes IPveHttpClient, so PSProxmoxVE.Core.Tests can assert the emitted form
// without a cluster; the integration test then covers only what PVE alone can tell us.
var service = new NetworkService();
service.SetNetwork(session, Node, Iface, config);
```
