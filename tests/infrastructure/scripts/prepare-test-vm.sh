#!/usr/bin/env bash
# Prepares a Debian cloud image VM with qemu-guest-agent on the nested PVE.
#
# Uses PSProxmoxVE cmdlets where possible (we're integration testing the module
# after all), SSH/SCP only for operations the API doesn't expose:
#   - SCP the cloud-init snippet (no snippet upload API)
#   - pvesm set to enable snippets content type
#   - qm importdisk (no API equivalent)
#   - qm set for --scsi0, --cicustom, --boot, --agent (Set-PveVmConfig doesn't expose these)
#
# Usage: prepare-test-vm.sh <nested-pve-ip> <root-password> <vm-id> <node> <api-token>
#
# Outputs (to stdout, for capture by caller):
#   LINUX_VMID=<vm-id>

set -euo pipefail

NESTED_IP="${1:?Usage: prepare-test-vm.sh <ip> <password> <vm-id> <node> <api-token>}"
ROOT_PASS="$2"
VMID="$3"
NODE="$4"
API_TOKEN="$5"

CLOUD_IMAGE_URL="https://cloud-images.ubuntu.com/noble/current/noble-server-cloudimg-amd64.img"
CLOUD_IMAGE_FILENAME="noble-server-cloudimg-amd64.img"

SSH_OPTS="-o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o LogLevel=ERROR"
SSH_CMD="sshpass -p ${ROOT_PASS} ssh ${SSH_OPTS} root@${NESTED_IP}"
SCP_CMD="sshpass -p ${ROOT_PASS} scp ${SSH_OPTS}"

echo "=== Preparing test Linux VM (VMID ${VMID}) on ${NESTED_IP} ==="

# ── Step 1: Upload cloud-init snippet (SSH — no API for snippets) ────
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

${SSH_CMD} "mkdir -p /var/lib/vz/snippets && pvesm set local --content iso,vztmpl,snippets"
${SCP_CMD} "${USERDATA}" "root@${NESTED_IP}:/var/lib/vz/snippets/test-vm-userdata.yml"
rm -f "${USERDATA}"

# ── Step 2: Download cloud image (PSProxmoxVE cmdlet) ────────────────
echo "Downloading Ubuntu cloud image via Invoke-PveStorageDownload..."
pwsh -NoProfile -Command "
    Import-Module PSProxmoxVE
    Connect-PveServer -Server '${NESTED_IP}' -ApiToken '${API_TOKEN}' -SkipCertificateCheck
    Invoke-PveStorageDownload \
        -Node '${NODE}' \
        -Storage 'local' \
        -Url '${CLOUD_IMAGE_URL}' \
        -Filename '${CLOUD_IMAGE_FILENAME}' \
        -ContentType 'iso' \
        -Wait
"

# ── Step 3: Create VM (PSProxmoxVE cmdlet) ───────────────────────────
echo "Creating VM ${VMID} via New-PveVm..."
pwsh -NoProfile -Command "
    Import-Module PSProxmoxVE
    Connect-PveServer -Server '${NESTED_IP}' -ApiToken '${API_TOKEN}' -SkipCertificateCheck
    New-PveVm \
        -Node '${NODE}' \
        -VmId ${VMID} \
        -Name 'debian-test' \
        -Memory 512 \
        -Cores 1 \
        -OsType 'l26' \
        -Wait
"

# ── Step 4: Import disk and configure (SSH — no importdisk API) ──────
echo "Importing disk and configuring VM..."
${SSH_CMD} bash <<REMOTE
set -e

# Import the downloaded image as a disk (stored in local ISO dir by download-url API)
qm importdisk ${VMID} /var/lib/vz/template/iso/${CLOUD_IMAGE_FILENAME} local-lvm 2>&1 | tail -1

# Attach disk, enable agent, configure boot, add cloud-init
qm set ${VMID} \
    --scsihw virtio-scsi-single \
    --scsi0 local-lvm:vm-${VMID}-disk-0 \
    --boot order=scsi0 \
    --serial0 socket \
    --agent 1 \
    --net0 virtio,bridge=vmbr0 \
    --ide2 local-lvm:cloudinit \
    --cicustom "user=local:snippets/test-vm-userdata.yml"
REMOTE

# ── Step 5: Set cloud-init config (PSProxmoxVE cmdlet) ───────────────
echo "Setting cloud-init config via Set-PveCloudInitConfig..."
pwsh -NoProfile -Command "
    Import-Module PSProxmoxVE
    Connect-PveServer -Server '${NESTED_IP}' -ApiToken '${API_TOKEN}' -SkipCertificateCheck
    Set-PveCloudInitConfig \
        -Node '${NODE}' \
        -VmId ${VMID} \
        -CiUser 'root' \
        -Password (ConvertTo-SecureString '${ROOT_PASS}' -AsPlainText -Force) \
        -IpConfig0 'ip=dhcp'
    # Note: Invoke-PveCloudInitRegenerate has a bug (returns cloud-config
    # content as UPID). Skipping — PVE regenerates cloud-init on VM start.
"

# ── Step 6: Start VM (PSProxmoxVE cmdlet) ────────────────────────────
echo "Starting VM ${VMID} via Start-PveVm..."
pwsh -NoProfile -Command "
    Import-Module PSProxmoxVE
    Connect-PveServer -Server '${NESTED_IP}' -ApiToken '${API_TOKEN}' -SkipCertificateCheck
    Start-PveVm -Node '${NODE}' -VmId ${VMID} -Wait
"

# ── Step 7: Wait for guest agent ─────────────────────────────────────
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
