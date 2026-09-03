# ADR 0028 — Connect-PveServer -ApiToken is a SecureString and the session exposes no credential material

- **Status:** Accepted
- **Date:** 2026-09-03
- **Deciders:** operator + agent
- **Context source:** issue #147; the 2026-09-02 whole-repo review; wave 4 of the remediation

## Context

[ADR 0002](0002-password-parameters-must-use-securestring.md) made every password parameter a `SecureString`, and `Connect-PveServer` takes a `PSCredential` for the password path. The API token, a credential that never expires (`TicketExpiry = DateTime.MaxValue`), is the one exception: `-ApiToken` is `string?`, and `PveSession` exposes `ApiToken`, `Ticket` and `CsrfToken` as public getters.

`Connect-PveServer -ApiToken 'root@pam!ci=...'` is written verbatim to PSReadLine history and to any `Start-Transcript` log. `PSProxmoxVE.format.ps1xml` hides the three properties from the default table view only; `Format-List *`, `ConvertTo-Json`, `Export-Clixml`, and any `ErrorRecord` carrying the session as `TargetObject` still render them.

Changing a parameter's type is a public-surface change. Scripts that pass a string literal today would fail at parameter binding if the type simply became `SecureString`, so the change needs a deprecation path.

## Decision

- **`-ApiToken` is a `SecureString`.** The whole `USER@REALM!TOKENID=UUID` string is the secret. The token id alone is not sensitive, but users copy the token as one string from the PVE UI and every PVE document shows it that way; splitting it would make the parameter differ from its own documentation.
- **One minor release of tolerance.** In the release that ships this, a plain string argument is still accepted: an `ArgumentTransformationAttribute` on the parameter converts a `string` to `SecureString` at binding time and the cmdlet emits a deprecation warning naming the replacement (`ConvertTo-SecureString -AsPlainText -Force`, `Read-Host -AsSecureString`, or a secret vault). The next major release removes the transformation, at which point a string argument fails at binding with PowerShell's own type-conversion error.
- **The session exposes no credential material.** `PveSession.ApiToken`, `Ticket` and `CsrfToken` lose their public getters. The raw material lives in private fields, and `PveHttpClient` (same assembly) obtains the authentication headers through an `internal` accessor. `Format-List *`, `ConvertTo-Json` and `Export-Clixml` therefore render a session without secrets, and the `format.ps1xml` hiding becomes belt-and-braces rather than the only defence.
- **The Core seam stays `string`.** `PveAuthenticator.AuthenticateWithApiToken(string apiToken)` keeps its signature. As ADR 0002 records for passwords, the guarantee is about the module's PowerShell surface and the window of exposure; the conversion happens at the cmdlet boundary, and the header value has to exist as a string for the duration of each request regardless.

## Rejected alternatives

**`PSCredential` with the token id as user name and the secret as password** (`-ApiTokenCredential`). Idiomatic for `Get-Credential` and SecretManagement, but it splits the token at the `=` into two halves the user never sees separately, adds a third parameter set, and makes the parameter unlike every PVE document that shows the token. A user who wants a vault can store the whole token as a `SecureString` today.

**An `object`-typed parameter with a runtime type check.** Keeps the name and accepts both shapes without a transformation attribute, at the cost of a parameter whose declared type says nothing, in help and in tab completion, for as long as it exists.

**A hard break with no deprecation release.** Smaller diff; breaks every existing script at binding time with no warning in the release before.

**Leaving the type alone and relying on `format.ps1xml`.** The status quo. It hides the secret from one view and none of the others, and it does nothing for shell history or transcripts.

## Consequences

- The release that ships this is a minor version (a new accepted type plus a warning), and the removal of the string tolerance is the next major. Both go in `CHANGELOG.md` under the versions that carry them.
- `PveSession`'s public shape shrinks. `Disconnect-PveServer`'s warning text and the `format.ps1xml` entries keep working; the Pester tests that assert on the three properties are rewritten to assert their absence.
- Generated help changes for `Connect-PveServer`; the `help-current` check requires the regenerated help in the same PR.
- The integration scripts connect with a `PSCredential`, not a token, so no test infrastructure changes; the offline tests cover the transformation, the warning and the session's public surface.
