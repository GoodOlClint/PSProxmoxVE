# ADR 0017 — CI runs two lanes: a pinned gating lane and a report-only currency lane

- **Status:** Accepted. Extended by [ADR 0022](0022-the-gating-lane-pins-its-own-test-tooling-by-exact-version.md), which applies the same pin to the lane's own test tooling.
- **Date:** 2026-09-01
- **Deciders:** operator + agent
- **Context source:** integration runs 176–180, root-caused 2026-09-01. No finding ID.

## Context

`apt-get upgrade` holds back packages that need new dependencies. On the nested nodes that produced `pve-cluster` 9.1.6 against `libpve-cluster-api-perl` 9.1.0 — a combination no real install ever has — and its symptom was not a package error. The node's pmxcfs came back in local mode after a cluster join, `/etc/pve/corosync.conf` never appeared, and the node reported `online=0` while corosync itself had healthy 2-node membership. Three CI runs went into diagnosing that, and removing the upgrade was the entire fix: run 180 was the first fully green integration run.

So the pin is what makes the gating lane trustworthy. But a permanently pinned CI never exercises the module against a current PVE, and that gap is exactly where an upstream regression would hide.

## Decision

CI provisions nested PVE nodes in two distinct modes, and they are not merged into one:

- **Lane 1, `integration-tests.yml`** — nodes stay pinned to what the ISO ships. `first-boot.sh` never runs `apt-get upgrade` or `dist-upgrade`. This lane gates merges.
- **Lane 2, `package-currency.yml`** — nodes are `dist-upgrade`d to current PVE and the suite runs against them. **Report-only**: test failures do not fail the job.

Both declare `concurrency: group: integration-tests`. They drive the same nested VMIDs on the same parent node, so they must never run at once.

```bash
# first-boot.sh installs only what provisioning needs; the ISO is the pin
apt-get update -qq
apt-get install -y -qq --no-install-recommends qemu-guest-agent open-iscsi
```

```yaml
# package-currency.yml opts in explicitly; lane 1 never sets this
env:
  PVE_DIST_UPGRADE: '1'
```

Report-only is deliberate. A scheduled job that goes red on an upstream change nobody has chosen to chase becomes noise, and a noisy cron gets ignored — the failure mode that makes a canary worthless. The signal is the rolling issue and the recorded package set, not the check colour.

A failure of the lane's own machinery — provisioning, the upgrade, the reboot, an unreachable node — still fails the job. `run-integration.sh` returns 3 for a genuine test failure and 4 when it cannot reach or authenticate to a node; only 3 is suppressed. Suppressing both would let a botched reboot report success while the lane learned nothing.

## Rejected alternatives

Upgrading packages in the gating lane's `first-boot.sh`, so one lane covers both currency and gating:

```bash
apt-get update -qq
apt-get -y upgrade
```

This is the mismatch that left a node unclustered and cost three CI runs to diagnose. `upgrade` rather than `dist-upgrade` is what produces the impossible combination, but the deeper problem is that a moving input cannot sit in the merge gate at all.

Also rejected: making lane 2 fail the build. See the consequence below.

## Consequences

**Accepted risk:** a module genuinely broken against current PVE shows a green weekly check plus an updated issue. Operator ruling 2026-09-01, to be revisited after a few releases.

Any moving input to the gating lane is the same defect in a different place, which is what [ADR 0022](0022-the-gating-lane-pins-its-own-test-tooling-by-exact-version.md) addresses for the test tooling.

A node-versus-node package comparison is reported even when the set is otherwise unchanged, because a mismatch *between* the two nested nodes is the failure that cost those three runs.
