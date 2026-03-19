#!/bin/bash
# First-boot script for nested PVE test instances.
# Runs once after auto-install completes and the system reboots.
# Installs qemu-guest-agent so the parent PVE can discover the VM's IP via the guest agent API.

set -e

# Disable enterprise repos (no subscription) and enable the no-subscription repo
# PVE 8.x uses .list files, PVE 9.x uses .sources (DEB822 format)
rm -f /etc/apt/sources.list.d/pve-enterprise.list /etc/apt/sources.list.d/pve-enterprise.sources
rm -f /etc/apt/sources.list.d/ceph.list /etc/apt/sources.list.d/ceph.sources

# Detect Debian suite from os-release (works on both PVE 8/bookworm and PVE 9/trixie)
SUITE=$(. /etc/os-release && echo "$VERSION_CODENAME")
if [ -z "$SUITE" ]; then
    # Fallback: try parsing apt sources
    SUITE=$(grep -oP 'Suites:\s*\K\S+' /etc/apt/sources.list.d/debian.sources 2>/dev/null | head -1 || echo "bookworm")
fi
echo "deb http://download.proxmox.com/debian/pve ${SUITE} pve-no-subscription" > /etc/apt/sources.list.d/pve-no-subscription.list

apt-get update -qq
apt-get install -y -qq qemu-guest-agent
systemctl start qemu-guest-agent
