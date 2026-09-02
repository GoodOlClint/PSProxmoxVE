# ADR 0018 — The currency lane reboots after dist-upgrade, and proves it rebooted

- **Status:** Accepted
- **Date:** 2026-09-01
- **Deciders:** operator + agent
- **Context source:** pre-push review of PR #106, 2026-09-01. No finding ID.

## Context

A PVE `dist-upgrade` pulls `proxmox-kernel-*`. Without a reboot the node runs new userspace on the old kernel, so the currency lane records a package set it never actually ran and is blind to kernel regressions — it would report "current PVE" while testing something that never booted.

The verification half is the part that is easy to omit, and it was omitted in the first draft. `ssh … reboot` returns non-zero when the connection dies, so it needs `|| true` — which swallows *every* ssh failure, including the reboot never being issued. `wait-for-api.sh` then matches the **still-running pre-reboot** pveproxy on its first poll and returns `responsive after 0s`. The script exits 0 having proved nothing. A blind `sleep` before polling does not fix this; it is wrong in both directions and verifies nothing either way.

## Decision

After `dist-upgrade`, `prepare-test-environment.sh` reboots the node **unconditionally** and then **verifies the reboot happened** by comparing `/proc/sys/kernel/random/boot_id` before and after. An unchanged boot id is fatal.

```bash
boot_before="$(${SSH_CMD} "cat /proc/sys/kernel/random/boot_id")"
${SSH_CMD} "systemctl reboot" || true
boot_after=""
for _ in $(seq 1 60); do
    boot_after="$(${SSH_CMD} "cat /proc/sys/kernel/random/boot_id" 2>/dev/null || true)"
    [[ -n "${boot_after}" && "${boot_after}" != "${boot_before}" ]] && break
    sleep 5
done
if [[ -z "${boot_after}" || "${boot_after}" == "${boot_before}" ]]; then
    echo "ERROR: ${NESTED_IP} did not reboot (boot_id unchanged)" >&2
    exit 1
fi
bash "${SCRIPT_DIR}/wait-for-api.sh" "${NESTED_IP}" 8006 600
```

Order matters: prove the boot id changed first (ssh returns before pveproxy does), then wait for the API, then wait for pmxcfs. `pvesm set` writes `/etc/pve/storage.cfg`, which needs `/etc/pve` mounted, and on a fresh boot that lags the API by seconds.

## Rejected alternatives

Reboot and sleep, without proving anything:

```bash
${SSH_CMD} "systemctl reboot" || true
sleep 30
bash "${SCRIPT_DIR}/wait-for-api.sh" "${NESTED_IP}" 8006 600
```

Gating the reboot on `/var/run/reboot-required`. That file comes from `update-notifier-common`, which is not guaranteed present on a PVE node, so the gate silently never fires.

Putting the reboot in `first-boot.sh`. That runs `ordering = "fully-up"` while the parent is still polling, so `wait-for-pve.sh` can discover the IP, see the API, pass auth, and then have the node reboot out from under provisioning — presenting as an intermittent network fault.

## Consequences

The lane costs one reboot and up to five minutes of waiting per node, paid on every currency run. That is the price of the package set it records being the one it actually tested.

This is the general shape of the [ADR 0017](0017-ci-runs-two-lanes-a-pinned-gating-lane-and-a-report-only-currency-lane.md) machinery rule: a failure of the lane's own plumbing fails the job, even though test failures in that lane do not.
