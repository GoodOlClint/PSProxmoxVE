#!/usr/bin/env bash
# Prepare a PVE auto-install ISO with the answer file and first-boot script baked in.
# Uses --fetch-from iso mode so only a single CD-ROM is needed, no HTTP server required.
#
# Usage: prepare-auto-iso.sh <base-iso> <answer-file> [first-boot-script] [output-iso]
set -euo pipefail

BASE_ISO="$1"
ANSWER_FILE="$2"
FIRST_BOOT="${3:-}"
OUTPUT_ISO="${4:-${BASE_ISO%.iso}-auto.iso}"

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

echo "Preparing auto-install ISO..."
echo "  Base ISO:     $BASE_ISO"
echo "  Answer file:  $ANSWER_FILE"
echo "  First boot:   ${FIRST_BOOT:-none}"
echo "  Output:       $OUTPUT_ISO"

proxmox-auto-install-assistant prepare-iso \
    --fetch-from iso \
    --answer-file "$ANSWER_FILE" \
    "${FIRST_BOOT_ARGS[@]}" \
    --output "$OUTPUT_ISO" \
    "$BASE_ISO"

echo "Created: $OUTPUT_ISO"
