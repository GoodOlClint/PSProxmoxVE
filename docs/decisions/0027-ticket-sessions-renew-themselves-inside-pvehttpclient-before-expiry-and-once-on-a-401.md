# ADR 0027 — Ticket sessions renew themselves inside PveHttpClient before expiry and once on a 401

- **Status:** Proposed
- **Date:** 2026-09-03
- **Deciders:** operator + agent
- **Context source:** issue #143; the 2026-09-02 whole-repo review; wave 4 of the remediation

## Context

`PveAuthenticator.AuthenticateWithCredentials` stamps `TicketExpiry = UtcNow + 2h` and nothing ever moves it. A backup, migration or full clone waited on past that point starts getting 401s from the status poll, which surface as a raw `PveApiException`. `PveCmdletBase.GetSession()` throws `PveSessionExpiredException` once the clock passes the stamp, with no recovery path. API-token sessions are unaffected; only ticket sessions age.

An earlier note (recorded before this ADR system existed) put the fix in `TaskService.WaitForTask`, because the cluster-join cmdlet had grown a bespoke 401 retry loop and the next long wait would grow another. Since [ADR 0021](0021-integration-tests-prove-server-semantics-payloads-are-proven-offline.md) and #151, every service call goes through `PveServiceBase.Invoke`, which builds one `PveHttpClient` per call, and `WaitForTask` holds one client for the whole wait. The client is now the single place every request passes through.

PVE's ticket endpoint accepts a still-valid ticket in place of the password. The published spec for `POST /access/ticket` describes the `password` parameter as "The secret password. This can also be a valid ticket." ([pve9 OpenAPI at 1530d5a, byte offset 4446761 in the one-line file](https://github.com/GoodOlClint/Proxmox_API/blob/1530d5a0b0dbf248159e7265acc617d22c200888/pve/openapi/pve-openapi.pve9.json#L1)). A ticket session can therefore renew itself without holding the password, as long as it renews while the current ticket is still alive.

## Decision

Renewal lives in `PveHttpClient`, not in `WaitForTask` and not in any cmdlet.

- **Proactive renewal.** Before sending a request on a `Ticket`-mode session whose ticket is past half its lifetime (one hour of the two), the client POSTs `/access/ticket` with the session's user name and the current ticket as `password`, then swaps the session's `Ticket`, `CsrfToken` and `TicketExpiry` for the new values. Half the lifetime is chosen so that a poll interval, a slow upload or a stalled request cannot push the next request past the hard expiry.
- **Reactive renewal.** A 401 on a `Ticket`-mode request triggers one renewal and one retry of the original request. If the renewal itself fails, the client throws `PveSessionExpiredException` with the 401 as its inner exception, so the cmdlet base maps it to a `PermissionDenied`-class error rather than the anonymous `NotSpecified` wrapper (#155).
- **Single flight.** Renewal is serialised per session. Concurrent requests on the same session share one renewal; none observe a half-updated credential. `PveSession` therefore gains a lock and `internal` setters for the three ticket fields, and records the user name it was issued to.
- **API-token sessions never renew.** A 401 on one is surfaced as-is; there is nothing to renew.
- **`GetSession()` keeps its expiry check.** It is the pre-flight guard for a session whose ticket already died while idle, where no renewal is possible. The window it guards shrinks from "two hours after connect" to "two hours after the last request".

Offline tests, per [ADR 0021](0021-integration-tests-prove-server-semantics-payloads-are-proven-offline.md): a mock handler that answers 401 then 200 asserts exactly one `/access/ticket` POST whose `password` is the old ticket, and that the retried request carries the new cookie and CSRF token; a session constructed with a near-past issue time asserts the proactive path fires once and does not fire again until the next half-life. The live behaviour (that PVE really accepts the ticket as a password) is proven by the integration run after merge; the spec citation above is what the reviewer verifies before that.

## Rejected alternatives

**In `WaitForTask` only**, as the earlier note proposed. It leaves every other path exposed: a script that connects, sleeps two hours between steps, then calls `Get-PveVm` dies with the same raw 401, and `WaitForTask` no longer owns its client since #151. The note's real point, that the retry must not live per cmdlet, is kept.

**Storing the password on the session and re-authenticating.** Keeps a secret in managed memory for the life of the session, against the spirit of [ADR 0002](0002-password-parameters-must-use-securestring.md), and buys nothing the ticket-as-password renewal does not.

**Per-cmdlet retry**, the shape the cluster-join cmdlet once had. Each copy drifts, and there are 194 cmdlets.

**A background timer on the session.** Runspace and thread lifetime become the module's problem; renewal at request time needs no timer and cannot fire after the session is discarded.

## Consequences

- `PveSession.Ticket`, `CsrfToken` and `TicketExpiry` become mutable from inside `PSProxmoxVE.Core`; anything that cached them (nothing does today) would go stale.
- `PveSession` gains the user name it was issued to. `PveAuthenticator` already has it at login.
- The hard two-hour wall becomes "a session used at least once an hour lives indefinitely". An idle session still dies after two hours; `Connect-PveServer` help says so.
- `PveSessionExpiredException` can now originate inside the client as well as in `GetSession()`; #155 maps both.
- This is a live-only behaviour change. The integration run after the merge is the proof, and a login or ticket test failing on that run is a regression, not a flake.
