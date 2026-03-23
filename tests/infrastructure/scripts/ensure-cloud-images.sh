#!/usr/bin/env bash
# Downloads cloud image and OVA to the cache directory if not already present
# or if the cached copy is older than 7 days.
#
# Usage: ensure-cloud-images.sh <cache-dir>
#
# Outputs (for use in GITHUB_OUTPUT):
#   CLOUD_IMAGE_PATH=<path>
#   OVA_PATH=<path>
set -euo pipefail

CACHE_DIR="${1:?Usage: ensure-cloud-images.sh <cache-dir>}"
MAX_AGE_DAYS=7

CLOUD_IMAGE_URL="https://cloud-images.ubuntu.com/noble/current/noble-server-cloudimg-amd64.img"
CLOUD_IMAGE_FILENAME="noble-server-cloudimg-amd64.qcow2"

OVA_URL="https://cloud-images.ubuntu.com/releases/24.04/release/ubuntu-24.04-server-cloudimg-amd64.ova"
OVA_FILENAME="ubuntu-24.04-server-cloudimg-amd64.ova"

mkdir -p "${CACHE_DIR}"

download_if_stale() {
    local url="$1"
    local filepath="$2"
    local description="$3"

    if [ -f "${filepath}" ] && [ -s "${filepath}" ]; then
        # Check age
        local age_days
        age_days=$(( ( $(date +%s) - $(stat -c %Y "${filepath}" 2>/dev/null || stat -f %m "${filepath}" 2>/dev/null) ) / 86400 ))
        if [ "${age_days}" -lt "${MAX_AGE_DAYS}" ]; then
            echo "${description} cached and fresh (${age_days}d old): ${filepath}"
            return 0
        fi
        echo "${description} is ${age_days}d old, re-downloading..."
    else
        echo "Downloading ${description}..."
    fi

    local tmp_path="${filepath}.downloading"
    if curl -fSL --progress-bar -o "${tmp_path}" "${url}"; then
        mv "${tmp_path}" "${filepath}"
        echo "Downloaded ${description}: $(du -h "${filepath}" | cut -f1)"
    else
        rm -f "${tmp_path}"
        # If we have a stale copy, keep using it
        if [ -f "${filepath}" ]; then
            echo "WARNING: Download failed, using stale cached copy" >&2
            return 0
        fi
        echo "ERROR: Failed to download ${description}" >&2
        return 1
    fi
}

download_if_stale "${CLOUD_IMAGE_URL}" "${CACHE_DIR}/${CLOUD_IMAGE_FILENAME}" "Ubuntu cloud image"
download_if_stale "${OVA_URL}" "${CACHE_DIR}/${OVA_FILENAME}" "Ubuntu OVA"

echo "CLOUD_IMAGE_PATH=${CACHE_DIR}/${CLOUD_IMAGE_FILENAME}"
echo "OVA_PATH=${CACHE_DIR}/${OVA_FILENAME}"
