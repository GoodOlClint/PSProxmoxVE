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

# Create a minimal test OVA for Import-PveOva testing
# OVA = TAR containing an OVF descriptor + a small VMDK/raw disk
OVA_PATH="${OUTPUT_DIR}/test-appliance.ova"
if [ ! -f "${OVA_PATH}" ]; then
    echo "Creating minimal test OVA..."
    OVA_TMPDIR=$(mktemp -d)

    # Create a 1MB raw disk image and convert to vmdk-stream (flat)
    dd if=/dev/zero of="${OVA_TMPDIR}/test-disk.vmdk" bs=1M count=1 2>/dev/null

    # Create OVF descriptor
    cat > "${OVA_TMPDIR}/test-appliance.ovf" <<'OVF'
<?xml version="1.0" encoding="UTF-8"?>
<Envelope xmlns="http://schemas.dmtf.org/ovf/envelope/1"
          xmlns:rasd="http://schemas.dmtf.org/wbem/wscim/1/cim-schema/2/CIM_ResourceAllocationSettingData"
          xmlns:vssd="http://schemas.dmtf.org/wbem/wscim/1/cim-schema/2/CIM_VirtualSystemSettingData"
          xmlns:ovf="http://schemas.dmtf.org/ovf/envelope/1">
  <References>
    <File ovf:id="file1" ovf:href="test-disk.vmdk" ovf:size="1048576"/>
  </References>
  <DiskSection>
    <Info>Virtual disk information</Info>
    <Disk ovf:diskId="vmdisk1" ovf:fileRef="file1" ovf:capacity="1073741824" ovf:format="http://www.vmware.com/interfaces/specifications/vmdk.html#streamOptimized"/>
  </DiskSection>
  <VirtualSystem ovf:id="test-appliance">
    <Info>A minimal test appliance</Info>
    <OperatingSystemSection ovf:id="101">
      <Info>Linux 64-bit</Info>
      <Description>Linux</Description>
    </OperatingSystemSection>
    <VirtualHardwareSection>
      <Info>Virtual hardware requirements</Info>
      <System>
        <vssd:ElementName>Virtual Hardware Family</vssd:ElementName>
        <vssd:InstanceID>0</vssd:InstanceID>
        <vssd:VirtualSystemIdentifier>test-appliance</vssd:VirtualSystemIdentifier>
        <vssd:VirtualSystemType>vmx-13</vssd:VirtualSystemType>
      </System>
      <Item>
        <rasd:Description>Number of Virtual CPUs</rasd:Description>
        <rasd:ElementName>1 virtual CPU(s)</rasd:ElementName>
        <rasd:InstanceID>1</rasd:InstanceID>
        <rasd:ResourceType>3</rasd:ResourceType>
        <rasd:VirtualQuantity>1</rasd:VirtualQuantity>
      </Item>
      <Item>
        <rasd:AllocationUnits>byte * 2^20</rasd:AllocationUnits>
        <rasd:Description>Memory Size</rasd:Description>
        <rasd:ElementName>256MB of memory</rasd:ElementName>
        <rasd:InstanceID>2</rasd:InstanceID>
        <rasd:ResourceType>4</rasd:ResourceType>
        <rasd:VirtualQuantity>256</rasd:VirtualQuantity>
      </Item>
      <Item>
        <rasd:ElementName>SCSI Controller</rasd:ElementName>
        <rasd:InstanceID>3</rasd:InstanceID>
        <rasd:ResourceSubType>lsilogic</rasd:ResourceSubType>
        <rasd:ResourceType>6</rasd:ResourceType>
      </Item>
      <Item>
        <rasd:ElementName>Hard Disk 1</rasd:ElementName>
        <rasd:HostResource>ovf:/disk/vmdisk1</rasd:HostResource>
        <rasd:InstanceID>4</rasd:InstanceID>
        <rasd:Parent>3</rasd:Parent>
        <rasd:ResourceType>17</rasd:ResourceType>
      </Item>
      <Item>
        <rasd:Connection>VM Network</rasd:Connection>
        <rasd:ElementName>Ethernet adapter 1</rasd:ElementName>
        <rasd:InstanceID>5</rasd:InstanceID>
        <rasd:ResourceSubType>E1000</rasd:ResourceSubType>
        <rasd:ResourceType>10</rasd:ResourceType>
      </Item>
    </VirtualHardwareSection>
  </VirtualSystem>
</Envelope>
OVF

    # Pack as OVA (TAR, OVF first per spec)
    (cd "${OVA_TMPDIR}" && tar cf "${OVA_PATH}" test-appliance.ovf test-disk.vmdk)
    rm -rf "${OVA_TMPDIR}"
    echo "Created test OVA at ${OVA_PATH} ($(du -h "${OVA_PATH}" | cut -f1))"
else
    echo "Test OVA already cached at ${OVA_PATH}"
fi

echo "CLOUD_IMAGE_PATH=${CLOUD_IMAGE_PATH}"
echo "OVA_PATH=${OVA_PATH}"
echo "Environment preparation complete."
