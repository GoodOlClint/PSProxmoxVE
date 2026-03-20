#!/usr/bin/env bash
# Prepares the test environment on the nested PVE node.
# Only performs operations that have no PVE API equivalent, plus
# downloads test artifacts for the integration tests to upload.
#
# Usage: prepare-test-environment.sh <nested-pve-ip> <root-password> <output-dir>
#
# Operations:
#   - Enable snippets+import content types on local storage (pvesm set)
#   - Upload cloud-init user-data snippet (SCP — no snippet upload API)
#   - Download Ubuntu cloud image to <output-dir> for upload tests

set -euo pipefail

NESTED_IP="${1:?Usage: prepare-test-environment.sh <ip> <password> <output-dir>}"
ROOT_PASS="$2"
OUTPUT_DIR="${3:?Output directory required}"

CLOUD_IMAGE_URL="https://cloud-images.ubuntu.com/noble/current/noble-server-cloudimg-amd64.img"
# PVE upload API validates extensions per content type — content=import
# does not accept .img. The Ubuntu cloud image is qcow2 format, so we
# rename it to .qcow2 for compatibility with the upload endpoint.
CLOUD_IMAGE_FILENAME="noble-server-cloudimg-amd64.qcow2"

SSH_OPTS="-o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o LogLevel=ERROR"
SSH_CMD="sshpass -p ${ROOT_PASS} ssh ${SSH_OPTS} root@${NESTED_IP}"
SCP_CMD="sshpass -p ${ROOT_PASS} scp ${SSH_OPTS}"

echo "=== Preparing test environment on ${NESTED_IP} ==="

# Enable snippets and import content types on local storage
echo "Configuring local storage content types..."
${SSH_CMD} "mkdir -p /var/lib/vz/snippets && pvesm set local --content iso,vztmpl,snippets,import"

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

# Download cloud image for integration tests to upload via Send-PveFile
CLOUD_IMAGE_PATH="${OUTPUT_DIR}/${CLOUD_IMAGE_FILENAME}"
if [ ! -f "${CLOUD_IMAGE_PATH}" ]; then
    echo "Downloading Ubuntu cloud image..."
    curl -fSL -o "${CLOUD_IMAGE_PATH}" "${CLOUD_IMAGE_URL}"
else
    echo "Cloud image already cached at ${CLOUD_IMAGE_PATH}"
fi

echo "CLOUD_IMAGE_PATH=${CLOUD_IMAGE_PATH}"
echo "Environment preparation complete."
