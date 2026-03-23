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
