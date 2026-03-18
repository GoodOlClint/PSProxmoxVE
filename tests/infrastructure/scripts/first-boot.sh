#!/bin/bash
# First-boot script for nested PVE test instances.
# Runs once after auto-install completes and the system reboots.
# Installs qemu-guest-agent so the parent PVE can discover the VM's IP via the guest agent API.

set -e

apt-get update -qq
apt-get install -y -qq qemu-guest-agent
systemctl enable --now qemu-guest-agent
