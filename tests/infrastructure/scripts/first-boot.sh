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

# No upgrade here on purpose. `apt-get upgrade` holds back packages that need
# new dependencies, which produced pve-cluster 9.1.6 against
# libpve-cluster-api-perl 9.1.0 — a combination no real install ever has, and
# one whose cluster join silently leaves the node unclustered. The ISO is the
# pin; the currency lane is where upgrades get exercised.
apt-get update -qq
apt-get install -y -qq --no-install-recommends qemu-guest-agent open-iscsi
systemctl start qemu-guest-agent
systemctl enable open-iscsi
