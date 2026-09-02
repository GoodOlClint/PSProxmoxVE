#!/usr/bin/env bash
# Prepares the test environment on the nested PVE node.
# Only performs operations that have no PVE API equivalent.
#
# Usage: prepare-test-environment.sh <nested-pve-ip> <root-password> [dist-upgrade] [pkg-out]
#
# Operations:
#   - Optionally dist-upgrade the node, record its package set, and reboot
#     (currency lane only; off unless <dist-upgrade> is 1)
#   - Enable snippets+import content types on local storage (pvesm set)
#   - Upload cloud-init user-data snippet (SCP — no snippet upload API)

set -euo pipefail

NESTED_IP="${1:?Usage: prepare-test-environment.sh <ip> <password> [dist-upgrade] [pkg-out]}"
ROOT_PASS="$2"
DIST_UPGRADE="${3:-0}"
PKG_OUT="${4:-}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

SSH_OPTS="-o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o LogLevel=ERROR"
export SSHPASS="${ROOT_PASS}"
SSH_CMD="sshpass -e ssh ${SSH_OPTS} root@${NESTED_IP}"
SCP_CMD="sshpass -e scp ${SSH_OPTS}"

echo "=== Preparing test environment on ${NESTED_IP} ==="

if [[ "${DIST_UPGRADE}" == "1" ]]; then
    echo "Running dist-upgrade (currency lane)..."
    ${SSH_CMD} "DEBIAN_FRONTEND=noninteractive apt-get update -qq && \
        DEBIAN_FRONTEND=noninteractive apt-get -y \
            -o Dpkg::Options::=--force-confold \
            -o Dpkg::Options::=--force-confdef \
            dist-upgrade"

    if [[ -n "${PKG_OUT}" ]]; then
        echo "Recording package set to ${PKG_OUT}..."
        # pipefail must be set in the REMOTE shell. ssh returns the remote
        # pipeline's status, which is sort's — and sort succeeds on the empty
        # input a failed dpkg-query produces, so a broken query would otherwise
        # leave a zero-byte file and still exit 0.
        ${SSH_CMD} "set -o pipefail; dpkg-query -W -f='\${binary:Package}\t\${Version}\n' | sort" > "${PKG_OUT}"
        if [[ ! -s "${PKG_OUT}" ]]; then
            echo "ERROR: empty package set from ${NESTED_IP}" >&2
            exit 1
        fi
    fi

    # A PVE dist-upgrade pulls proxmox-kernel-*; without a reboot the node runs
    # new userspace on the old kernel. Reboot unconditionally rather than
    # testing /var/run/reboot-required — that file comes from
    # update-notifier-common, which is not guaranteed on a PVE node.
    #
    # boot_id is the evidence that the reboot happened. Without it the `|| true`
    # below swallows every ssh failure, the node stays up, and wait-for-api.sh
    # matches the still-running pre-reboot pveproxy on its first poll.
    boot_before="$(${SSH_CMD} "cat /proc/sys/kernel/random/boot_id")"

    echo "Rebooting after dist-upgrade..."
    ${SSH_CMD} "systemctl reboot" || true

    # Order matters: prove the reboot first (ssh returns before pveproxy does),
    # then wait for the API, then for pmxcfs.
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

    # wait-for-api.sh only proves pveproxy answers. `pvesm set` below writes
    # /etc/pve/storage.cfg, which needs pmxcfs to have mounted /etc/pve — on a
    # freshly rebooted node those are seconds apart.
    for _ in $(seq 1 30); do
        ${SSH_CMD} "test -f /etc/pve/storage.cfg" 2>/dev/null && break
        sleep 5
    done

    # printf, not echo: bash's builtin echo does not interpret \t without -e,
    # which would make this the one row in the file without a real tab.
    if [[ -n "${PKG_OUT}" ]]; then
        ${SSH_CMD} "printf '# running-kernel\t%s\n' \"\$(uname -r)\"" >> "${PKG_OUT}"
    fi
fi

# Enable snippets and import content types on local storage
echo "Configuring local storage content types..."
${SSH_CMD} "mkdir -p /var/lib/vz/snippets && pvesm set local --content images,iso,vztmpl,snippets,import"

# Upload cloud-init user-data snippet (no API for snippet upload)
echo "Uploading cloud-init user-data snippet..."
USERDATA=$(mktemp)
cat > "${USERDATA}" <<'YAML'
#cloud-config
package_update: true
packages:
  - qemu-guest-agent
runcmd:
  - systemctl enable --now qemu-guest-agent
YAML

${SCP_CMD} "${USERDATA}" "root@${NESTED_IP}:/var/lib/vz/snippets/test-vm-userdata.yml"
rm -f "${USERDATA}"

echo "Environment preparation complete."
