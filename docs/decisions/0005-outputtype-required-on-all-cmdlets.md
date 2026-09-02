# ADR 0005 — OutputType required on all cmdlets

- **Status:** Accepted
- **Date:** 2026-03-22
- **Deciders:** unrecorded; adopted during review scan 2026-03-22
- **Context source:** `docs/review/findings.json` F037

## Context

Around 54 of the module's 169 cmdlets had no `[OutputType]` attribute. PowerShell uses it for IntelliSense, for pipeline type inference, and to answer `Get-Command -OutputType`; without it, tooling cannot tell what a cmdlet emits until it runs.

## Decision

Every cmdlet declares its return type with `[OutputType(typeof(...))]`.

```csharp
[Cmdlet(VerbsCommon.Get, "PveVm")]
[OutputType(typeof(VmInfo))]
public sealed class GetPveVmCmdlet : PveCmdletBase
```

## Rejected alternatives

None recorded. This was adopted as a convention during review scan 2026-03-22 rather than chosen between competing options. All 169 cmdlets carry the attribute.

## Consequences

The attribute is only as useful as the type it names, which is what [ADR 0013](0013-cmdlets-must-emit-only-native-or-module-defined-types.md) constrains: an `[OutputType(typeof(JObject))]` satisfies this rule and still gives the user nothing discoverable.
