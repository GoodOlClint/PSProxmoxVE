#!/usr/bin/env bash
# Downloads a Debian cloud image to the nested PVE, creates a VM, and uses
# cloud-init custom user-data to install qemu-guest-agent on first boot.
#
# No virt-customize or libguestfs needed — cloud-init handles the package
# installation. A custom user-data snippet is SCP'd to the nested PVE's
# snippets directory and referenced via --cicustom.
#
# Usage: prepare-test-vm.sh <nested-pve-ip> <root-password> <vm-id>
#
# Outputs (to stdout, for capture by caller):
#   LINUX_VMID=<vm-id>

set -euo pipefail

NESTED_IP="${1:?Usage: prepare-test-vm.sh <ip> <password> <vm-id>}"
ROOT_PASS="$2"
VMID="$3"

CLOUD_IMAGE_URL="https://cloud.debian.org/images/cloud/bookworm/latest/debian-12-genericcloud-amd64.qcow2"

SSH_OPTS="-o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o LogLevel=ERROR"
SSH_CMD="sshpass -p ${ROOT_PASS} ssh ${SSH_OPTS} root@${NESTED_IP}"
SCP_CMD="sshpass -p ${ROOT_PASS} scp ${SSH_OPTS}"

echo "=== Preparing test Linux VM (VMID ${VMID}) on ${NESTED_IP} ==="

# Create cloud-init user-data snippet locally
USERDATA=$(mktemp)
cat > "${USERDATA}" <<'YAML'
#cloud-config
package_update: true
packages:
  - qemu-guest-agent
runcmd:
  - systemctl enable --now qemu-guest-agent
YAML

# Upload snippet to nested PVE
echo "Uploading cloud-init user-data snippet..."
${SSH_CMD} "mkdir -p /var/lib/vz/snippets"
${SCP_CMD} "${USERDATA}" "root@${NESTED_IP}:/var/lib/vz/snippets/test-vm-userdata.yml"
rm -f "${USERDATA}"

# Download image and create VM — all on the nested PVE
echo "Downloading Debian cloud image and creating VM on nested PVE..."
${SSH_CMD} bash <<REMOTE
set -e

# Enable snippets content type on local storage (disabled by default)
pvesm set local --content iso,vztmpl,snippets

# Download the cloud image
echo "Downloading Debian cloud image..."
curl -sL -o /tmp/debian-cloud.qcow2 "${CLOUD_IMAGE_URL}"

# Create the VM
echo "Creating VM ${VMID}..."
qm create ${VMID} \
    --name debian-test \
    --memory 512 \
    --cores 1 \
    --net0 virtio,bridge=vmbr0 \
    --agent 1 \
    --ostype l26 \
    --scsihw virtio-scsi-single

# Import the disk
echo "Importing disk..."
qm importdisk ${VMID} /tmp/debian-cloud.qcow2 local-lvm 2>&1 | tail -1

# Attach disk and configure boot
qm set ${VMID} \
    --scsi0 local-lvm:vm-${VMID}-disk-0 \
    --boot order=scsi0 \
    --serial0 socket

# Add cloud-init drive
qm set ${VMID} --ide2 local-lvm:cloudinit

# Set cloud-init config with custom user-data for guest-agent install
qm set ${VMID} \
    --ciuser root \
    --cipassword "${ROOT_PASS}" \
    --ipconfig0 ip=dhcp \
    --cicustom "user=local:snippets/test-vm-userdata.yml"

# Start the VM
echo "Starting VM ${VMID}..."
qm start ${VMID}

# Clean up
rm -f /tmp/debian-cloud.qcow2
REMOTE

# Wait for guest agent to respond (cloud-init needs time to install the package)
echo "Waiting for guest agent on VM ${VMID} (cloud-init installing packages)..."
TIMEOUT=300
ELAPSED=0
while [ $ELAPSED -lt $TIMEOUT ]; do
    if ${SSH_CMD} "qm agent ${VMID} ping" 2>/dev/null; then
        echo "Guest agent responding on VM ${VMID}"
        echo "LINUX_VMID=${VMID}"
        exit 0
    fi
    sleep 10
    ELAPSED=$((ELAPSED + 10))
    echo "  Waiting... (${ELAPSED}s / ${TIMEOUT}s)"
done

echo "ERROR: Timeout waiting for guest agent on VM ${VMID}" >&2
exit 1
