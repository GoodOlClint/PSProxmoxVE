# ADR 0013 — Cmdlets must emit only native or module-defined types

- **Status:** Accepted
- **Date:** 2026-03-25
- **Deciders:** unrecorded; adopted during review scan 2026-03-25
- **Context source:** `docs/review/findings.json` F085

## Context

PowerShell enumerates a Newtonsoft `JArray` in ways the user does not expect, and `JObject` properties are invisible to `Get-Member` and to tab completion. Piping module output into `Format-Table`, `Select-Object` or `Where-Object` therefore behaved differently depending on whether the underlying value happened to be a Newtonsoft container — a distinction the user has no way to see.

Native dictionaries and lists work naturally in the pipeline, so the fix is to stop the third-party types at the module boundary.

## Decision

Cmdlet output types and public model properties are native .NET types (`string`, `int`, `bool`, `Dictionary<string, object?>`, `List<T>`, `PSObject`, `void`) or types the module defines itself. No `JObject`, `JArray` or `JToken` on the public surface.

```csharp
public Dictionary<string, object?> GetNodeConfig(...) { ... }

[JsonProperty("members")]
[JsonConverter(typeof(NativeListConverter))]
public List<Dictionary<string, object?>>? Members { get; set; }

[OutputType(typeof(Dictionary<string, object>))]
```

## Rejected alternatives

Returning the parsed Newtonsoft object directly. It is the shortest path from response to output and it pushes the enumeration problem onto every user:

```csharp
public JObject GetNodeConfig(...) { ... }

[JsonProperty("members")]
public JArray? Members { get; set; }

[OutputType(typeof(JObject))]
```

## Consequences

The restriction is on the **public** surface. `JObject`, `JArray` and `JToken` are still used freely inside services and cmdlets for response parsing, and that is intended — a review that flags internal parsing use is reading this rule too broadly.

Conversion has to happen somewhere: models that deserialise a nested structure need a converter (`NativeListConverter`, `JsonHelper.ToNative`) rather than the default binding. A `[JsonExtensionData]` catch-all must land in a private field and be exposed as a native dictionary, or it reintroduces `JToken` through the back door.

This is the rule most at risk on a large nested response. `GET /nodes/{node}/ceph/status` returns raw `ceph status` output and is the most likely place in the module to leak a `JObject` into public view.
