#!/usr/bin/env bash
# Downloads a PVE base ISO to the cache directory if not already present,
# and verifies it against the upstream SHA256SUMS on every run — including a
# cache hit, since the cache lives on a persistent mount nothing else audits.
#
# Usage: ensure-base-iso.sh <iso-filename> <cache-dir>
#   iso-filename: e.g. proxmox-ve_9.1-1.iso
#   cache-dir:    e.g. /opt/pve-isos
set -euo pipefail

ISO_FILENAME="${1:?Usage: ensure-base-iso.sh <iso-filename> <cache-dir>}"
CACHE_DIR="${2:?Cache directory required}"

CACHED_PATH="${CACHE_DIR}/${ISO_FILENAME}"

# download.proxmox.com's own TLS cert does not list download.proxmox.com in
# its SAN — only the regional *.cdn.proxmox.com aliases and
# enterprise.proxmox.com — so https to that name fails certificate
# validation. enterprise.proxmox.com serves byte-identical ISOs and
# SHA256SUMS over a valid cert.
BASE_URL="https://enterprise.proxmox.com/iso"
DOWNLOAD_URL="${BASE_URL}/${ISO_FILENAME}"
SUMS_URL="${BASE_URL}/SHA256SUMS"

mkdir -p "${CACHE_DIR}"

verify_checksum() {
    local filepath="$1"
    local sums_file hash dir base

    sums_file="$(mktemp)"

    if ! curl -fsSL -o "${sums_file}" "${SUMS_URL}"; then
        echo "ERROR: failed to download ${SUMS_URL}" >&2
        rm -f "${sums_file}"
        return 1
    fi

    hash="$(awk -v f="${ISO_FILENAME}" '$2 == f || $2 == "*" f {print $1; exit}' "${sums_file}")"
    rm -f "${sums_file}"

    if [ -z "${hash}" ]; then
        echo "ERROR: ${ISO_FILENAME} not listed in ${SUMS_URL}" >&2
        return 1
    fi

    dir="$(dirname "${filepath}")"
    base="$(basename "${filepath}")"

    if ! printf '%s  %s\n' "${hash}" "${base}" | (cd "${dir}" && sha256sum -c -); then
        echo "ERROR: checksum mismatch for ${filepath}" >&2
        return 1
    fi
}

if [ -f "${CACHED_PATH}" ] && [ -s "${CACHED_PATH}" ]; then
    echo "Base ISO already cached: ${CACHED_PATH} ($(du -h "${CACHED_PATH}" | cut -f1))"
    if verify_checksum "${CACHED_PATH}"; then
        echo "Checksum verified: ${CACHED_PATH}"
        exit 0
    fi
    echo "Cached ISO failed checksum verification; removing and re-downloading." >&2
    rm -f "${CACHED_PATH}"
fi

TMP_PATH="${CACHED_PATH}.downloading"

echo "Downloading PVE base ISO: ${DOWNLOAD_URL}"
echo "  Target: ${CACHED_PATH}"

# Download to a temp file, verify it, then atomic move — never let unverified
# bytes sit at the canonical cache path, where a concurrent run's cache-hit
# check could pick them up.
if curl -fSL --progress-bar -o "${TMP_PATH}" "${DOWNLOAD_URL}"; then
    if ! verify_checksum "${TMP_PATH}"; then
        rm -f "${TMP_PATH}"
        echo "ERROR: downloaded ISO failed checksum verification; removed" >&2
        exit 1
    fi
    mv "${TMP_PATH}" "${CACHED_PATH}"
    echo "Downloaded: ${CACHED_PATH} ($(du -h "${CACHED_PATH}" | cut -f1))"
else
    rm -f "${TMP_PATH}"
    echo "ERROR: Failed to download ${DOWNLOAD_URL}" >&2
    exit 1
fi

echo "Checksum verified: ${CACHED_PATH}"
