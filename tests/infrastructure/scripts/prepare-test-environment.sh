#!/usr/bin/env bash
# Prepares the test environment on the nested PVE node.
# Only performs operations that have no PVE API equivalent.
#
# Usage: prepare-test-environment.sh <nested-pve-ip> <root-password>
#
# Operations:
#   - Enable snippets+import content types on local storage (pvesm set)
#   - Upload cloud-init user-data snippet (SCP — no snippet upload API)

set -euo pipefail

NESTED_IP="${1:?Usage: prepare-test-environment.sh <ip> <password>}"
ROOT_PASS="$2"

SSH_OPTS="-o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o LogLevel=ERROR"
SSH_CMD="sshpass -p ${ROOT_PASS} ssh ${SSH_OPTS} root@${NESTED_IP}"
SCP_CMD="sshpass -p ${ROOT_PASS} scp ${SSH_OPTS}"

echo "=== Preparing test environment on ${NESTED_IP} ==="

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
