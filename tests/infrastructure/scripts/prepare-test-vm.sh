#!/usr/bin/env bash
# Prepares an Ubuntu cloud image VM with qemu-guest-agent on the nested PVE.
#
# Uses PSProxmoxVE cmdlets for all supported operations:
#   - Invoke-PveStorageDownload (cloud image download)
#   - New-PveVm (VM creation)
#   - Import-PveVmDisk (disk import from storage)
#   - Set-PveVmConfig -AdditionalConfig (boot/agent/cloud-init config)
#   - Set-PveCloudInitConfig (user/password/IP)
#   - Start-PveVm (boot)
#   - Test-PveVmGuestAgent (agent ping)
#
# SSH/SCP only for operations without API support:
#   - pvesm set (enable snippets content type)
#   - SCP snippet upload (no snippet upload API — PVE limitation, not even the web UI supports this)
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

CONNECT_CMD="Connect-PveServer -Server '${NESTED_IP}' -ApiToken '${API_TOKEN}' -SkipCertificateCheck"

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

# ── Step 2: Download cloud image (module cmdlet) ─────────────────────
echo "Downloading Ubuntu cloud image via Invoke-PveStorageDownload..."
pwsh -NoProfile -Command "
    Import-Module PSProxmoxVE; ${CONNECT_CMD}
    Invoke-PveStorageDownload \
        -Node '${NODE}' -Storage 'local' \
        -Url '${CLOUD_IMAGE_URL}' -Filename '${CLOUD_IMAGE_FILENAME}' \
        -ContentType 'iso' -Wait
"

# ── Step 3: Create VM (module cmdlet) ────────────────────────────────
echo "Creating VM ${VMID} via New-PveVm..."
pwsh -NoProfile -Command "
    Import-Module PSProxmoxVE; ${CONNECT_CMD}
    New-PveVm -Node '${NODE}' -VmId ${VMID} -Name 'ubuntu-test' \
        -Memory 512 -Cores 1 -OsType 'l26' -Wait
"

# ── Step 4: Import disk (module cmdlet) ────────────────────────────
echo "Importing disk image via Import-PveVmDisk..."
pwsh -NoProfile -Command "
    Import-Module PSProxmoxVE; ${CONNECT_CMD}
    Import-PveVmDisk -Node '${NODE}' -VmId ${VMID} -Disk 'scsi0' \
        -TargetStorage 'local-lvm' \
        -Source 'local:iso/${CLOUD_IMAGE_FILENAME}' -Wait
"

# ── Step 5: Configure VM (module cmdlet — AdditionalConfig) ──────────
echo "Configuring VM via Set-PveVmConfig -AdditionalConfig..."
pwsh -NoProfile -Command "
    Import-Module PSProxmoxVE; ${CONNECT_CMD}
    Set-PveVmConfig -Node '${NODE}' -VmId ${VMID} -AdditionalConfig @{
        scsihw   = 'virtio-scsi-single'
        boot     = 'order=scsi0'
        serial0  = 'socket'
        agent    = '1'
        net0     = 'virtio,bridge=vmbr0'
        ide2     = 'local-lvm:cloudinit'
        cicustom = 'user=local:snippets/test-vm-userdata.yml'
    }
"

# ── Step 6: Set cloud-init config (module cmdlet) ────────────────────
echo "Setting cloud-init config via Set-PveCloudInitConfig..."
pwsh -NoProfile -Command "
    Import-Module PSProxmoxVE; ${CONNECT_CMD}
    Set-PveCloudInitConfig -Node '${NODE}' -VmId ${VMID} \
        -CiUser 'root' \
        -Password (ConvertTo-SecureString '${ROOT_PASS}' -AsPlainText -Force) \
        -IpConfig0 'ip=dhcp'
"

# ── Step 7: Start VM (module cmdlet) ─────────────────────────────────
echo "Starting VM ${VMID} via Start-PveVm..."
pwsh -NoProfile -Command "
    Import-Module PSProxmoxVE; ${CONNECT_CMD}
    Start-PveVm -Node '${NODE}' -VmId ${VMID} -Wait
"

# ── Step 8: Wait for guest agent (module cmdlet) ─────────────────────
echo "Waiting for guest agent on VM ${VMID} (cloud-init installing packages)..."
pwsh -NoProfile -Command "
    Import-Module PSProxmoxVE; ${CONNECT_CMD}
    \$timeout = 300; \$elapsed = 0
    while (\$elapsed -lt \$timeout) {
        if (Test-PveVmGuestAgent -Node '${NODE}' -VmId ${VMID}) {
            Write-Host 'Guest agent responding on VM ${VMID}'
            exit 0
        }
        Start-Sleep -Seconds 10
        \$elapsed += 10
        Write-Host \"  Waiting... (\${elapsed}s / \${timeout}s)\"
    }
    throw 'Timeout waiting for guest agent on VM ${VMID}'
"

echo "LINUX_VMID=${VMID}"
