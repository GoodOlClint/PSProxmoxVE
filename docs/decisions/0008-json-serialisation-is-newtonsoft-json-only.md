# ADR 0008 — JSON serialisation is Newtonsoft.Json only

- **Status:** Accepted
- **Date:** 2026-03-22
- **Deciders:** unrecorded; adopted during review scan 2026-03-22
- **Context source:** `docs/review/findings.json` F044

## Context

Model classes carried both `[JsonProperty]` (Newtonsoft) and `[JsonPropertyName]` (System.Text.Json) attributes. Only Newtonsoft runs at runtime, so the second set was inert — but a reader could not tell which one the deserialiser honoured, and changing one without the other would look correct and do nothing.

## Decision

Newtonsoft.Json is the only JSON library. Model classes carry `[JsonProperty]` and nothing else.

```csharp
[JsonProperty("status")]
public string Status { get; set; }
```

## Rejected alternatives

Carrying both attribute sets so a future migration to System.Text.Json is already half-done:

```csharp
[JsonProperty("status")]
[JsonPropertyName("status")]
public string Status { get; set; }
```

Rejected because the unused set is never exercised, so it rots silently, and it makes every property read as though two serialisers are in play.

## Consequences

All `[JsonPropertyName]` attributes were removed. A `System.Text.Json` package reference survived the removal for the netstandard2.0 and net48 targets with no source using it — an unused dependency on the published surface, filed separately.
