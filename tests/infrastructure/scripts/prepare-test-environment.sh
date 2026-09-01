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
SSH_CMD="sshpass -p ${ROOT_PASS} ssh ${SSH_OPTS} root@${NESTED_IP}"
SCP_CMD="sshpass -p ${ROOT_PASS} scp ${SSH_OPTS}"

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
        ${SSH_CMD} "dpkg-query -W -f='\${binary:Package}\t\${Version}\n' | sort" > "${PKG_OUT}"
    fi

    # A PVE dist-upgrade pulls proxmox-kernel-*; without a reboot the node runs
    # new userspace on the old kernel. Reboot unconditionally rather than
    # testing /var/run/reboot-required — that file comes from
    # update-notifier-common, which is not guaranteed on a PVE node.
    echo "Rebooting after dist-upgrade..."
    ${SSH_CMD} "systemctl reboot" || true

    # The API stays up for a few seconds after the reboot is issued, so polling
    # immediately would match the pre-reboot node and return at once.
    sleep 30
    bash "${SCRIPT_DIR}/wait-for-api.sh" "${NESTED_IP}" 8006 600
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
