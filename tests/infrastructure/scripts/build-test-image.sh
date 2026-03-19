#!/usr/bin/env bash
# Builds a small Alpine Linux cloud image with qemu-guest-agent pre-installed.
# The image is cached on the runner so it only needs to be built once.
#
# Prerequisites: libguestfs-tools (virt-customize), curl, qemu-utils
# Install on Debian/Ubuntu: apt-get install -y libguestfs-tools curl qemu-utils
#
# Usage: build-test-image.sh <output-path>
#   e.g.: build-test-image.sh /opt/pve-images/alpine-guest-agent.qcow2

set -euo pipefail

OUTPUT="${1:?Usage: build-test-image.sh <output-path>}"

# Skip if image already exists
if [ -f "$OUTPUT" ]; then
    echo "Image already exists at $OUTPUT, skipping build"
    exit 0
fi

ALPINE_VERSION="3.21"
ALPINE_RELEASE="3.21.3"
IMAGE_URL="https://dl-cdn.alpinelinux.org/alpine/v${ALPINE_VERSION}/releases/cloud/nocloud_alpine-${ALPINE_RELEASE}-x86_64-bios-cloudinit-r0.qcow2"

TEMP_DIR=$(mktemp -d)
TEMP_IMAGE="${TEMP_DIR}/alpine-cloud.qcow2"

cleanup() {
    rm -rf "$TEMP_DIR"
}
trap cleanup EXIT

echo "=== Building Alpine test image ==="
echo "Downloading Alpine ${ALPINE_RELEASE} cloud image..."
curl -sL -o "$TEMP_IMAGE" "$IMAGE_URL"

echo "Customizing image (installing qemu-guest-agent)..."
# virt-customize modifies the image in place:
#   - Install qemu-guest-agent package
#   - Enable the service on boot (OpenRC)
#   - Ensure the virtio serial device is available for guest agent communication
virt-customize -a "$TEMP_IMAGE" \
    --install qemu-guest-agent \
    --run-command 'rc-update add qemu-guest-agent default' \
    --run-command 'echo "GA_PATH=/dev/vport2p1" >> /etc/conf.d/qemu-guest-agent'

# Move to final location
mkdir -p "$(dirname "$OUTPUT")"
mv "$TEMP_IMAGE" "$OUTPUT"
echo "Image ready at $OUTPUT ($(du -h "$OUTPUT" | cut -f1))"
