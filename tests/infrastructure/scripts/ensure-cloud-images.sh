#!/usr/bin/env bash
# Downloads cloud image and OVA to the cache directory if not already present
# or if the cached copy is older than 7 days. Verifies both against the
# upstream Ubuntu SHA256SUMS on every check, including a fresh cache hit.
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
CLOUD_IMAGE_SUMS_URL="https://cloud-images.ubuntu.com/noble/current/SHA256SUMS"
CLOUD_IMAGE_FILENAME="noble-server-cloudimg-amd64.qcow2"

OVA_URL="https://cloud-images.ubuntu.com/releases/24.04/release/ubuntu-24.04-server-cloudimg-amd64.ova"
OVA_SUMS_URL="https://cloud-images.ubuntu.com/releases/24.04/release/SHA256SUMS"
OVA_FILENAME="ubuntu-24.04-server-cloudimg-amd64.ova"

mkdir -p "${CACHE_DIR}"

# Ubuntu's SHA256SUMS lists the upstream filename, which is not always the
# name we cache under (the cloud image is published as .img and cached as
# .qcow2 — Ubuntu's .img is already qcow2-formatted). Match by upstream name,
# then check the bytes under the name they actually have on disk.
verify_checksum() {
    local filepath="$1" sums_url="$2" upstream_name="$3"
    local sums_file hash dir base

    sums_file="$(mktemp)"

    if ! curl -fsSL -o "${sums_file}" "${sums_url}"; then
        echo "  ERROR: failed to download ${sums_url}" >&2
        rm -f "${sums_file}"
        return 1
    fi

    hash="$(awk -v f="${upstream_name}" '$2 == f || $2 == "*" f {print $1; exit}' "${sums_file}")"
    rm -f "${sums_file}"

    if [ -z "${hash}" ]; then
        echo "  ERROR: ${upstream_name} not listed in ${sums_url}" >&2
        return 1
    fi

    dir="$(dirname "${filepath}")"
    base="$(basename "${filepath}")"

    if ! printf '%s  %s\n' "${hash}" "${base}" | (cd "${dir}" && sha256sum -c -); then
        echo "  ERROR: checksum mismatch for ${filepath}" >&2
        return 1
    fi
}

download_if_stale() {
    local url="$1"
    local filepath="$2"
    local description="$3"
    local sums_url="$4"
    local upstream_name
    upstream_name="$(basename "${url}")"

    if [ -f "${filepath}" ] && [ -s "${filepath}" ]; then
        # Check age
        local age_days
        age_days=$(( ( $(date +%s) - $(stat -c %Y "${filepath}" 2>/dev/null || stat -f %m "${filepath}" 2>/dev/null) ) / 86400 ))
        if [ "${age_days}" -lt "${MAX_AGE_DAYS}" ]; then
            if verify_checksum "${filepath}" "${sums_url}" "${upstream_name}"; then
                echo "${description} cached and fresh (${age_days}d old): ${filepath}"
                return 0
            fi
            # Remove it now, not just on a redownload's own failure below —
            # otherwise a redownload that then fails falls through to the
            # "keep the stale copy" branch and hands back these same
            # known-bad bytes with exit 0.
            echo "${description} cached copy failed checksum verification, removing and re-downloading..." >&2
            rm -f "${filepath}"
        else
            echo "${description} is ${age_days}d old, re-downloading..."
        fi
    else
        echo "Downloading ${description}..."
    fi

    local tmp_path="${filepath}.downloading"
    if curl -fSL --progress-bar -o "${tmp_path}" "${url}"; then
        if ! verify_checksum "${tmp_path}" "${sums_url}" "${upstream_name}"; then
            rm -f "${tmp_path}"
            echo "ERROR: ${description} failed checksum verification" >&2
            return 1
        fi
        mv "${tmp_path}" "${filepath}"
        echo "Downloaded ${description}: $(du -h "${filepath}" | cut -f1)"
    else
        rm -f "${tmp_path}"
        # A copy that failed verification above is already gone by this
        # point; a copy that's here because it was merely stale-by-age was
        # never re-verified this run. Verify it now, at the point we'd
        # actually hand it back — checking only here, not proactively before
        # the redownload attempt, avoids deleting a copy the redownload was
        # about to replace anyway.
        if [ -f "${filepath}" ] && verify_checksum "${filepath}" "${sums_url}" "${upstream_name}"; then
            echo "WARNING: Download failed, using stale cached copy" >&2
            return 0
        fi
        rm -f "${filepath}"
        echo "ERROR: Failed to download ${description}" >&2
        return 1
    fi
}

download_if_stale "${CLOUD_IMAGE_URL}" "${CACHE_DIR}/${CLOUD_IMAGE_FILENAME}" "Ubuntu cloud image" "${CLOUD_IMAGE_SUMS_URL}"
download_if_stale "${OVA_URL}" "${CACHE_DIR}/${OVA_FILENAME}" "Ubuntu OVA" "${OVA_SUMS_URL}"

echo "CLOUD_IMAGE_PATH=${CACHE_DIR}/${CLOUD_IMAGE_FILENAME}"
echo "OVA_PATH=${CACHE_DIR}/${OVA_FILENAME}"
