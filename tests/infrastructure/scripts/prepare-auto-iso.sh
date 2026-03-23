#!/usr/bin/env bash
# Prepare a PVE auto-install ISO with the answer file and first-boot script baked in.
# Uses --fetch-from iso mode so only a single CD-ROM is needed, no HTTP server required.
#
# Usage: prepare-auto-iso.sh <base-iso> <answer-file> [first-boot-script] [output-iso] [--cache-dir <dir>]
#
# When --cache-dir is specified, the script caches the generated ISO keyed by a hash
# of the inputs (base ISO path + answer file content + first-boot script content).
# If the cache is warm and inputs haven't changed, the cached ISO is copied to the
# output path without regeneration.
set -euo pipefail

# Parse arguments
BASE_ISO="$1"
ANSWER_FILE="$2"
FIRST_BOOT="${3:-}"
OUTPUT_ISO="${4:-${BASE_ISO%.iso}-auto.iso}"
CACHE_DIR=""

# Check for --cache-dir flag in remaining args
shift 4 2>/dev/null || true
while [ $# -gt 0 ]; do
    case "$1" in
        --cache-dir)
            CACHE_DIR="$2"
            shift 2
            ;;
        *)
            shift
            ;;
    esac
done

for f in "$BASE_ISO" "$ANSWER_FILE"; do
    if [ ! -f "$f" ]; then
        echo "ERROR: File not found: $f" >&2
        exit 1
    fi
done

FIRST_BOOT_ARGS=()
if [ -n "$FIRST_BOOT" ] && [ -f "$FIRST_BOOT" ]; then
    FIRST_BOOT_ARGS=(--on-first-boot "$FIRST_BOOT")
fi

# --- Cache check ---
if [ -n "$CACHE_DIR" ]; then
    mkdir -p "$CACHE_DIR"

    # Build a hash of all inputs to detect changes
    INPUT_HASH=$(cat "$ANSWER_FILE" ${FIRST_BOOT:+"$FIRST_BOOT"} | sha256sum | cut -d' ' -f1)
    BASE_HASH=$(sha256sum "$BASE_ISO" | cut -d' ' -f1)
    CACHE_KEY="${BASE_HASH:0:16}_${INPUT_HASH:0:16}"
    CACHED_ISO="${CACHE_DIR}/$(basename "$OUTPUT_ISO")"
    CACHED_HASH_FILE="${CACHED_ISO}.inputhash"

    if [ -f "$CACHED_ISO" ] && [ -s "$CACHED_ISO" ] && [ -f "$CACHED_HASH_FILE" ]; then
        STORED_HASH=$(cat "$CACHED_HASH_FILE")
        if [ "$STORED_HASH" = "$CACHE_KEY" ]; then
            echo "Cached auto-install ISO is up to date (hash: ${CACHE_KEY})"
            if [ "$CACHED_ISO" != "$OUTPUT_ISO" ]; then
                cp "$CACHED_ISO" "$OUTPUT_ISO"
            fi
            echo "Using cached: $OUTPUT_ISO"
            exit 0
        fi
        echo "Cache stale (stored: ${STORED_HASH}, current: ${CACHE_KEY}), regenerating..."
    else
        echo "No cached auto-install ISO found, generating..."
    fi
fi

echo "Preparing auto-install ISO..."
echo "  Base ISO:     $BASE_ISO"
echo "  Answer file:  $ANSWER_FILE"
echo "  First boot:   ${FIRST_BOOT:-none}"
echo "  Output:       $OUTPUT_ISO"

# Use the output directory for staging to avoid permission issues with the source ISO directory
proxmox-auto-install-assistant prepare-iso \
    --fetch-from iso \
    --answer-file "$ANSWER_FILE" \
    "${FIRST_BOOT_ARGS[@]}" \
    --tmp "$(dirname "$OUTPUT_ISO")" \
    --output "$OUTPUT_ISO" \
    "$BASE_ISO"

echo "Created: $OUTPUT_ISO"

# --- Update cache ---
if [ -n "$CACHE_DIR" ]; then
    if [ "$CACHED_ISO" != "$OUTPUT_ISO" ]; then
        cp "$OUTPUT_ISO" "$CACHED_ISO"
    fi
    echo "$CACHE_KEY" > "$CACHED_HASH_FILE"
    echo "Cached auto-install ISO (hash: ${CACHE_KEY})"
fi
