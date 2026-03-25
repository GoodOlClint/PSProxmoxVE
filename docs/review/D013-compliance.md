# D013 Compliance Report — Native Type Emission

Scan date: 2026-03-25 (scan-2, broadened scope)
Directive: D013 — Cmdlets must emit only native or module-defined types
Scope: All third-party dependencies (not Newtonsoft-only)
Findings DB: docs/review/findings.json

## Third-Party Type Inventory

| Library | Version | Violation Types Scanned | Permitted Internal Use |
|---|---|---|---|
| Newtonsoft.Json | 13.0.3 | `JObject`, `JArray`, `JToken`, `JValue`, `JProperty`, `JConstructor`, `JRaw`, `JContainer` | `[JsonProperty]`, `[JsonConverter]`, `[JsonIgnore]` attributes; `JsonConvert.*` methods; local vars in private methods |
| SharpCompress | 0.47.3 | `IArchive`, `IArchiveEntry`, `IReader`, `ReaderFactory`, `ArchiveFactory` | Local vars in private extraction methods (fully-qualified inline usage) |
| PowerShellStandard.Library | 5.1.1 | *(none — build-time SDK reference, PrivateAssets="all")* | All types (PSCmdlet, etc.) are PowerShell SDK types, expected in public APIs |

### SharpCompress Usage Detail

SharpCompress is used in exactly one location: `OvfMetadata.ExtractOvfFromTar()` (line 86–103 of
`src/PSProxmoxVE.Core/Models/Vms/OvfMetadata.cs`). This is a `private static` method that uses
fully-qualified type references (`SharpCompress.Readers.ReaderFactory.OpenReader`) — no `using`
directive, no SharpCompress type in any return value, parameter, property, or field. **Clean.**

## Summary

| Category | Files Scanned | Violations | Clean |
|---|---|---|---|
| Cmdlet files | 195 | 0 | 195 |
| Service files | 16 | 0 | 16 |
| Model files | 51 | 1 | 50 |
| Other Core files | 16 | 0 | 16 |
| Test files (informational) | 26 | 0 | 26 |
| **Total** | **304** | **1** | **303** |

## Verdict

**NON-COMPLIANT** — 1 violation found (2 properties in 1 model class). See findings below.

All violations are from Newtonsoft.Json. SharpCompress has zero public API surface exposure.

## Violations

| Finding ID | Severity | Library | File | Line | Violation Type | Description |
|---|---|---|---|---|---|---|
| F085 | High | Newtonsoft.Json | src/PSProxmoxVE.Core/Models/Cluster/PveClusterJoinInfo.cs | 23 | Model property typed as `JArray` | `Nodelist` property typed as `JArray?` — emitted via `Get-PveClusterJoinInfo` |
| F085 | High | Newtonsoft.Json | src/PSProxmoxVE.Core/Models/Cluster/PveClusterJoinInfo.cs | 35 | Model property typed as `JObject` | `Totem` property typed as `JObject?` — emitted via `Get-PveClusterJoinInfo` |

### Detail: PveClusterJoinInfo (F085)

**File**: `src/PSProxmoxVE.Core/Models/Cluster/PveClusterJoinInfo.cs`
**Cmdlet**: `GetPveClusterJoinInfoCmdlet` → `[OutputType(typeof(PveClusterJoinInfo))]`
**Service**: `ClusterConfigService.GetJoinInfo()` returns `PveClusterJoinInfo`

The model has two Newtonsoft-typed properties:

```csharp
// CURRENT (violates D013)
[JsonProperty("nodelist")]
public JArray? Nodelist { get; set; }

[JsonProperty("totem")]
public JObject? Totem { get; set; }
```

These flow to the PowerShell pipeline via:
```csharp
var joinInfo = service.GetJoinInfo(session, Node);
WriteObject(joinInfo);  // JArray and JObject properties exposed
```

## Scan Passes Performed

### Phase 1 — Third-Party Inventory
- **Newtonsoft.Json 13.0.3**: Referenced by both PSProxmoxVE and PSProxmoxVE.Core. Namespaces `Newtonsoft.Json` and `Newtonsoft.Json.Linq` are imported across 82 source files.
- **SharpCompress 0.47.3**: Referenced by PSProxmoxVE.Core only. Used via fully-qualified names in 1 private method. No `using` directive anywhere.
- **PowerShellStandard.Library 5.1.1**: Build-time SDK reference (`PrivateAssets="all"`). Not a third-party runtime dependency.
- **No other third-party packages** found. No vendored DLLs outside bin/obj.
- **No non-standard using directives** found beyond `Newtonsoft.*`, `SharpCompress.*` (inline-only), `System.*`, `Microsoft.*`, and `PSProxmoxVE.*`.

### Phase 2a — Return type violations (services and public methods)
**Patterns**: `public ... J* methodName(`, `Task<J*>`, `List<J*>`, `IEnumerable<J*>`
**Result**: No matches across all third-party types. All service methods return native or module-defined types.

### Phase 2b — Model property type violations
**Pattern**: `public J*? PropertyName {` (and collection/array variants for all third-party types)
**Result**: 2 matches in PveClusterJoinInfo.cs (lines 23, 35). **Confirmed violation (F085).**
No SharpCompress types found in any model properties.

### Phase 2c — OutputType attribute violations
**Pattern**: `[OutputType(typeof(J*` (and all SharpCompress types)
**Result**: No matches.

### Phase 2d — WriteObject violations
**Pattern**: `WriteObject( (new)? <third-party-type>`
**Result**: No matches. No cmdlets pass third-party typed values directly to WriteObject.

### Phase 2e — Local variable third-party type assignments in cmdlets
**Pattern**: `<third-party-type>? varName =` in cmdlet files
**Result**: No matches for any third-party type in cmdlet files.

### Phase 2f — Using directive audit

| Namespace | Cmdlet files | Service files | Model files | Notes |
|---|---|---|---|---|
| `Newtonsoft.Json` | 26 | 16 | 50 | All use for attributes or internal deserialization only, except PveClusterJoinInfo (F085) |
| `Newtonsoft.Json.Linq` | (subset of above) | (subset of above) | 1 | Only PveClusterJoinInfo imports `.Linq` in models |
| `SharpCompress.*` | 0 | 0 | 0 | Used only via fully-qualified names in 1 private method |

### Phase 3 — Data flow inspection
26 cmdlet files import Newtonsoft namespaces. All were verified:
- None declare local variables of J* types
- None pass J* values to WriteObject
- All use Newtonsoft only for `[JsonProperty]` attributes or internal JObject parsing with native value extraction
- **All 26 confirmed clean**

### Phase 4 — Model property deep scan
51 model files scanned. All public properties use native .NET types (`string`, `int`, `long`, `bool`, `double`, `DateTime`, `List<T>`, `Dictionary<string, object?>`) or module-defined `Pve*` types, **except**:
- `PveClusterJoinInfo.Nodelist` → `JArray?` (violation)
- `PveClusterJoinInfo.Totem` → `JObject?` (violation)

No private fields typed as third-party types were found that back public properties.

| File | Class | Member | Declared Type | Third-Party Library | Violation? | Notes |
|---|---|---|---|---|---|---|
| PveClusterJoinInfo.cs | PveClusterJoinInfo | Nodelist | `JArray?` | Newtonsoft.Json | **Yes** | F085 |
| PveClusterJoinInfo.cs | PveClusterJoinInfo | Totem | `JObject?` | Newtonsoft.Json | **Yes** | F085 |
| OvfMetadata.cs | OvfMetadata | *(all public)* | native types | — | No | SharpCompress confined to private method |

## Clean Files (confirmed compliant)

All 303 non-violating source files were confirmed clean through static pattern scanning across all third-party type patterns. The 26 cmdlet files importing Newtonsoft.Json were individually verified to use it only for attributes or internal deserialization, with no third-party types flowing to `WriteObject`.

SharpCompress usage in `OvfMetadata.cs` is fully confined to a private static method with no type leakage.

## Informational — Test File Findings

| File | Matches | Classification |
|---|---|---|
| tests/PSProxmoxVE.Core.Tests/Models/NodeModelTests.cs | 8 | Expected/legitimate — `JObject.Parse()` for fixture construction |
| tests/PSProxmoxVE.Core.Tests/Models/StorageModelTests.cs | 12 | Expected/legitimate — `JObject.Parse()` for fixture construction |
| tests/PSProxmoxVE.Core.Tests/Models/FirewallModelTests.cs | 22 | Expected/legitimate — `JObject.Parse()` for fixture construction |
| tests/PSProxmoxVE.Core.Tests/Models/SdnModelTests.cs | 20 | Expected/legitimate — `JObject.Parse()` for fixture construction |
| tests/PSProxmoxVE.Core.Tests/Models/BackupModelTests.cs | 9 | Expected/legitimate — `JObject.Parse()` for fixture construction |
| tests/PSProxmoxVE.Core.Tests/Models/ContainerModelTests.cs | 11 | Expected/legitimate — `JObject.Parse()` for fixture construction |
| tests/PSProxmoxVE.Core.Tests/Models/ClusterModelTests.cs | 12 | Expected/legitimate — `JObject.Parse()` for fixture construction |
| tests/PSProxmoxVE.Core.Tests/Models/VmModelTests.cs | 14 | Expected/legitimate — `JObject.Parse()` for fixture construction |
| tests/PSProxmoxVE.Core.Tests/Models/UserModelTests.cs | 13 | Expected/legitimate — `JObject.Parse()` for fixture construction |
| tests/PSProxmoxVE.Core.Tests/Models/NetworkModelTests.cs | 9 | Expected/legitimate — `JObject.Parse()` for fixture construction |
| tests/PSProxmoxVE.Core.Tests/Models/TaskModelTests.cs | 8 | Expected/legitimate — `JObject.Parse()` for fixture construction |
| tests/PSProxmoxVE.Core.Tests/Models/SnapshotModelTests.cs | 9 | Expected/legitimate — `JObject.Parse()` for fixture construction |

All test file J* usage is legitimate: `JObject.Parse(json)["data"]` to extract fixture data for deserialization tests. No tests assert against third-party type properties on cmdlet output. No SharpCompress references in test files.

## Remediation Guidance

### F085 — PveClusterJoinInfo model property violations

Replace `JArray` and `JObject` properties with native types and add `[JsonConverter]` attributes:

**Before:**
```csharp
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class PveClusterJoinInfo
{
    [JsonProperty("nodelist")]
    public JArray? Nodelist { get; set; }

    [JsonProperty("totem")]
    public JObject? Totem { get; set; }
}
```

**After:**
```csharp
using System.Collections.Generic;
using Newtonsoft.Json;
using PSProxmoxVE.Core.Utilities;

public class PveClusterJoinInfo
{
    [JsonProperty("nodelist")]
    [JsonConverter(typeof(NativeListConverter))]
    public List<Dictionary<string, object?>>? Nodelist { get; set; }

    [JsonProperty("totem")]
    [JsonConverter(typeof(NativeDictionaryConverter))]
    public Dictionary<string, object?>? Totem { get; set; }
}
```

The `ToString()` override needs no change — `List<T>.Count` works identically to `JArray.Count`.

No changes needed to `GetPveClusterJoinInfoCmdlet` or `ClusterConfigService.GetJoinInfo()` — the fix is entirely in the model class.

## Notes

- **F049** `decisions_ref` was corrected from `D013` to `D012` prior to this scan (F049 is about magic string constants, not type emission).
- **F085** was created during D013-scan-1 and confirmed still open during D013-scan-2 (this scan).
- No files require manual review — all ambiguous cases resolved to **Clean**.
