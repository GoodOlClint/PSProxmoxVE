# Architectural Decisions

**This file has moved.** Architectural decisions now live in [`docs/decisions/`](docs/decisions/) as house-format ADRs, one decision per file. See [ADR 0023](docs/decisions/0023-decisions-live-in-docs-decisions-in-house-adr-format.md) for why.

New decisions are generated from the repository root with `~/.claude/templates/new-adr.sh "Title in plain words"`, which stamps the next number, the filename and the section skeleton. Do not add entries to this file.

The convention checklist — the rules to follow when writing a cmdlet — is `CLAUDE.md` § "Key Conventions". The ADRs carry the rationale behind those rules.

## D-number redirects

The old `DNNN` identifiers are retired. They appear in released `CHANGELOG.md` entries and in GitHub issues written before 2026-09-02, neither of which is rewritten. This table maps them.

| Old | ADR |
|---|---|
| D001 | [0001 — Task polling must use TaskService.WaitForTask](docs/decisions/0001-task-polling-must-use-taskservice-waitfortask.md) |
| D002 | [0002 — Password parameters must use SecureString](docs/decisions/0002-password-parameters-must-use-securestring.md) |
| D003 | [0003 — URL encoding required for all path parameters](docs/decisions/0003-url-encoding-required-for-all-path-parameters.md) |
| D004 | [0004 — No bare catch blocks](docs/decisions/0004-no-bare-catch-blocks.md) |
| D005 | [0005 — OutputType required on all cmdlets](docs/decisions/0005-outputtype-required-on-all-cmdlets.md) |
| D006 | [0006 — ConfirmImpact.High required for destructive operations](docs/decisions/0006-confirmimpact-high-required-for-destructive-operations.md) |
| D007 | [0007 — All cmdlet classes must be sealed](docs/decisions/0007-all-cmdlet-classes-must-be-sealed.md) |
| D008 | [0008 — JSON serialisation is Newtonsoft.Json only](docs/decisions/0008-json-serialisation-is-newtonsoft-json-only.md) |
| D009 | [0009 — Framework targeting](docs/decisions/0009-framework-targeting-netstandard2-0-for-publishable-net10-0-and-net48-for-tests.md) |
| D010 | [0010 — VmId parameters are nullable int with ValidateRange](docs/decisions/0010-vmid-parameters-are-nullable-int-with-validaterange.md) |
| D011 | [0011 — Verb class constants required for cmdlet attributes](docs/decisions/0011-verb-class-constants-required-for-cmdlet-attributes.md) |
| D012 | [0012 — Magic strings are extracted to named constants](docs/decisions/0012-magic-strings-are-extracted-to-named-constants.md) |
| D013 | [0013 — Cmdlets must emit only native or module-defined types](docs/decisions/0013-cmdlets-must-emit-only-native-or-module-defined-types.md) |
| D014 | [0014 — New-PveCluster -Wait blocks until the cluster is quorate](docs/decisions/0014-new-pvecluster-wait-blocks-until-the-cluster-is-quorate.md) |
| D015 | [0015 — Lifecycle -Wait blocks until the guest config lock clears](docs/decisions/0015-lifecycle-wait-blocks-until-the-guest-config-lock-clears.md) |
| D016 | [0016 — Restart-PveVm uses PVE's native reboot endpoint](docs/decisions/0016-restart-pvevm-uses-pve-s-native-reboot-endpoint.md) |
| D017 | [0017 — CI runs two lanes: a pinned gating lane and a report-only currency lane](docs/decisions/0017-ci-runs-two-lanes-a-pinned-gating-lane-and-a-report-only-currency-lane.md) |
| D017 (amendment) | [0022 — The gating lane pins its own test tooling by exact version](docs/decisions/0022-the-gating-lane-pins-its-own-test-tooling-by-exact-version.md) |
| D018 | [0018 — The currency lane reboots after dist-upgrade, and proves it rebooted](docs/decisions/0018-the-currency-lane-reboots-after-dist-upgrade-and-proves-it-rebooted.md) |
| D019 | [0019 — Local dev calls run-integration.sh directly](docs/decisions/0019-local-dev-calls-run-integration-sh-directly-there-is-no-wrapper-script.md) |
| D020 | [0020 — The qemu-server flock is retried, never predicted](docs/decisions/0020-the-qemu-server-flock-is-retried-never-predicted.md) |
| D021 | [0021 — Integration tests prove server semantics; payloads are proven offline](docs/decisions/0021-integration-tests-prove-server-semantics-payloads-are-proven-offline.md) |
