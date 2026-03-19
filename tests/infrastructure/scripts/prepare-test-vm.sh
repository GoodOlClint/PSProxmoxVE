#!/usr/bin/env bash
# Deploys the pre-built Alpine test image to a nested PVE instance and creates
# a VM with guest agent enabled. Waits for the guest agent to respond.
#
# The VM gets:
#   - The Alpine cloud image imported as its boot disk
#   - Cloud-init drive for initial configuration
#   - Guest agent enabled (agent=1)
#   - DHCP networking on vmbr0
#
# Usage: prepare-test-vm.sh <nested-pve-ip> <root-password> <image-path> <vm-id>
#
# Outputs (to stdout, for capture by caller):
#   LINUX_VMID=<vm-id>

set -euo pipefail

NESTED_IP="${1:?Usage: prepare-test-vm.sh <ip> <password> <image-path> <vm-id>}"
ROOT_PASS="$2"
IMAGE_PATH="$3"
VMID="$4"

SSH_OPTS="-o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o LogLevel=ERROR"
SSH_CMD="sshpass -p ${ROOT_PASS} ssh ${SSH_OPTS} root@${NESTED_IP}"
SCP_CMD="sshpass -p ${ROOT_PASS} scp ${SSH_OPTS}"

echo "=== Preparing test Linux VM (VMID ${VMID}) on ${NESTED_IP} ==="

# Copy image to nested PVE
echo "Uploading Alpine image to nested PVE..."
${SCP_CMD} "${IMAGE_PATH}" "root@${NESTED_IP}:/tmp/alpine-test.qcow2"

# Create VM and import disk
echo "Creating VM and importing disk..."
${SSH_CMD} bash <<REMOTE
set -e

# Create the VM shell
qm create ${VMID} \
    --name alpine-test \
    --memory 256 \
    --cores 1 \
    --net0 virtio,bridge=vmbr0 \
    --agent 1 \
    --ostype l26 \
    --scsihw virtio-scsi-single

# Import the disk image to local-lvm
qm importdisk ${VMID} /tmp/alpine-test.qcow2 local-lvm 2>&1 | tail -1

# Attach the imported disk and configure boot
qm set ${VMID} \
    --scsi0 local-lvm:vm-${VMID}-disk-0 \
    --boot order=scsi0 \
    --serial0 socket

# Add cloud-init drive
qm set ${VMID} --ide2 local-lvm:cloudinit

# Set cloud-init config (root user with password)
qm set ${VMID} \
    --ciuser root \
    --cipassword "${ROOT_PASS}" \
    --ipconfig0 ip=dhcp

# Start the VM
qm start ${VMID}

# Clean up uploaded image
rm -f /tmp/alpine-test.qcow2
REMOTE

# Wait for guest agent to respond
echo "Waiting for guest agent on VM ${VMID}..."
TIMEOUT=120
ELAPSED=0
while [ $ELAPSED -lt $TIMEOUT ]; do
    if ${SSH_CMD} "qm agent ${VMID} ping" 2>/dev/null; then
        echo "Guest agent responding on VM ${VMID}"
        echo "LINUX_VMID=${VMID}"
        exit 0
    fi
    sleep 5
    ELAPSED=$((ELAPSED + 5))
    echo "  Waiting... (${ELAPSED}s / ${TIMEOUT}s)"
done

echo "ERROR: Timeout waiting for guest agent on VM ${VMID}" >&2
exit 1
