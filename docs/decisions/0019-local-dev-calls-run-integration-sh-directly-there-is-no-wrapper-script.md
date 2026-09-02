# ADR 0019 — Local dev calls run-integration.sh directly; there is no wrapper script

- **Status:** Accepted
- **Date:** 2026-09-01
- **Deciders:** operator + agent
- **Context source:** audit of the local dev path against the post-ARC CI, 2026-09-01. No finding ID.

## Context

`tests/dev.ps1` was a 291-line PowerShell wrapper over roughly six `docker compose` and `docker exec` calls. Every capability it had was already available elsewhere: build and unit tests are plain `dotnet` and `Invoke-Pester` invocations, and the module build it performed is duplicated inside `run-integration.sh`, which publishes and installs the module before running the suite.

Being a second entry point, it drifted from the script it wrapped and from the CI it claimed to replicate. By the time it was removed it still offered a `-Version 8` leg retired in #88, mounted the Docker socket for storage containers replaced by the storage VM in #87, and defaulted its remote-host examples to a runner decommissioned in the ARC migration.

Four documentation files described a positional calling convention (`./tests/dev.ps1 test`) that did not do what it read as. The script took its actions from switches (`-Test`), but also declared `[string[]] $Tests`, so the bare word bound to `-Tests` — the integration-area filter. With no action switch set, the script fell through to its `-Shell` default and silently opened an interactive container shell. Every documented command was wrong, and wrong in the quietest possible way: it succeeded at something nobody asked for.

## Decision

`tests/infrastructure/scripts/run-integration.sh` is the only entry point to the provision → test → cleanup lifecycle, for CI and for local development alike. Local runs invoke it inside the `dev-infra` container — the same image CI runs its jobs in.

Build and unit tests need no container at all; they run natively against the solution.

## Rejected alternatives

A `dev.ps1`, a `Makefile` target, or a shell function that re-implements provisioning steps, module installation or test invocation.

A wrapper that must be kept in sync with the thing it wraps earns its place only when it removes real friction. This one removed none, and its drift was invisible because a wrong invocation still exited zero.

## Consequences

If a local flow is awkward, the fix goes in `run-integration.sh` so CI gets it too.

Local runs on Apple Silicon pay for this in emulation: the image is amd64-only, and under Rosetta the suite runs roughly 40% slower. That is a documented consequence of using the CI image rather than a local shortcut — and it turned out to be load-bearing, because it is the client speed that reproduced the flock race in [ADR 0020](0020-the-qemu-server-flock-is-retried-never-predicted.md) that CI never showed.
