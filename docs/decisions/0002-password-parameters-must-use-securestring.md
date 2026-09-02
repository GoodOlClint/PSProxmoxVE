# ADR 0002 — Password parameters must use SecureString

- **Status:** Accepted
- **Date:** 2026-03-22
- **Deciders:** unrecorded; adopted during review scan 2026-03-22
- **Context source:** `docs/review/findings.json` F051

## Context

`Set-PveVmGuestPassword` accepted a plain `string` password parameter, leaving the credential in managed memory indefinitely.

`Connect-PveServer` already took a `PSCredential`, so the module was inconsistent with itself about how sensitive input arrives.

## Decision

Every cmdlet parameter that accepts a password is a `SecureString`, extracted with `Marshal.SecureStringToGlobalAllocUnicode` and freed with `ZeroFreeGlobalAllocUnicode` in a `finally`.

```csharp
[Parameter(Mandatory = true)]
public SecureString Password { get; set; }

IntPtr ptr = IntPtr.Zero;
try
{
    ptr = Marshal.SecureStringToGlobalAllocUnicode(Password);
    string plainText = Marshal.PtrToStringUni(ptr);
}
finally
{
    if (ptr != IntPtr.Zero)
        Marshal.ZeroFreeGlobalAllocUnicode(ptr);
}
```

## Rejected alternatives

A plain `string` parameter. It is simpler to write and to test, and it leaves the credential recoverable from a memory dump for the lifetime of the process:

```csharp
[Parameter(Mandatory = true)]
public string Password { get; set; }
```

## Consequences

The service layer below the cmdlet still receives a plain `string` — the conversion happens at the cmdlet boundary, and `ClusterConfigService.JoinCluster` documents that it expects the converted value. The guarantee is about the module's public surface and the window of exposure, not about the credential never existing in managed memory.

A TLS private key is at least as sensitive as a password; anything accepting one is covered by the same rule.
