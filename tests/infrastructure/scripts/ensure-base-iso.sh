#!/usr/bin/env bash
# Downloads a PVE base ISO to the cache directory if not already present.
#
# Usage: ensure-base-iso.sh <iso-filename> <cache-dir>
#   iso-filename: e.g. proxmox-ve_9.1-1.iso
#   cache-dir:    e.g. /opt/pve-isos
set -euo pipefail

ISO_FILENAME="${1:?Usage: ensure-base-iso.sh <iso-filename> <cache-dir>}"
CACHE_DIR="${2:?Cache directory required}"

CACHED_PATH="${CACHE_DIR}/${ISO_FILENAME}"

if [ -f "${CACHED_PATH}" ] && [ -s "${CACHED_PATH}" ]; then
    echo "Base ISO already cached: ${CACHED_PATH} ($(du -h "${CACHED_PATH}" | cut -f1))"
    exit 0
fi

# Ensure cache directory exists and is writable
mkdir -p "${CACHE_DIR}"

DOWNLOAD_URL="http://download.proxmox.com/iso/${ISO_FILENAME}"
TMP_PATH="${CACHED_PATH}.downloading"

echo "Downloading PVE base ISO: ${DOWNLOAD_URL}"
echo "  Target: ${CACHED_PATH}"

# Download to temp file, then atomic move
if curl -fSL --progress-bar -o "${TMP_PATH}" "${DOWNLOAD_URL}"; then
    mv "${TMP_PATH}" "${CACHED_PATH}"
    echo "Downloaded: ${CACHED_PATH} ($(du -h "${CACHED_PATH}" | cut -f1))"
else
    rm -f "${TMP_PATH}"
    echo "ERROR: Failed to download ${DOWNLOAD_URL}" >&2
    exit 1
fi
