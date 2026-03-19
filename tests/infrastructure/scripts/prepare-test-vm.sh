#!/usr/bin/env bash
# Downloads a Debian cloud image, installs qemu-guest-agent via
# virt-customize (which is already available on the nested PVE host),
# creates a VM with the customized disk, and waits for the guest agent.
#
# All heavy lifting happens on the nested PVE via SSH — no libguestfs
# or large dependencies needed in the CI container.
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

echo "=== Preparing test Linux VM (VMID ${VMID}) on ${NESTED_IP} ==="

# Download image, customize, create VM — all on the nested PVE
echo "Downloading and customizing Debian cloud image on nested PVE..."
${SSH_CMD} bash <<REMOTE
set -e

# Ensure libguestfs-tools is available (not installed by default on fresh PVE)
if ! command -v virt-customize &>/dev/null; then
    echo "Installing libguestfs-tools..."
    apt-get update -qq && apt-get install -y -qq libguestfs-tools
fi

# Download the cloud image
echo "Downloading Debian cloud image..."
curl -sL -o /tmp/debian-cloud.qcow2 "${CLOUD_IMAGE_URL}"

# Install qemu-guest-agent into the image
echo "Installing qemu-guest-agent into image..."
virt-customize -a /tmp/debian-cloud.qcow2 \
    --install qemu-guest-agent \
    --run-command 'systemctl enable qemu-guest-agent'

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

# Import the customized disk
echo "Importing disk..."
qm importdisk ${VMID} /tmp/debian-cloud.qcow2 local-lvm 2>&1 | tail -1

# Attach disk and configure boot
qm set ${VMID} \
    --scsi0 local-lvm:vm-${VMID}-disk-0 \
    --boot order=scsi0 \
    --serial0 socket

# Add cloud-init drive
qm set ${VMID} --ide2 local-lvm:cloudinit

# Set cloud-init config
qm set ${VMID} \
    --ciuser root \
    --cipassword "${ROOT_PASS}" \
    --ipconfig0 ip=dhcp

# Start the VM
echo "Starting VM ${VMID}..."
qm start ${VMID}

# Clean up
rm -f /tmp/debian-cloud.qcow2
REMOTE

# Wait for guest agent to respond
echo "Waiting for guest agent on VM ${VMID}..."
TIMEOUT=180
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
