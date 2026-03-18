#!/usr/bin/env bash
# Upload a file to PVE storage via the REST API.
#
# Usage: upload-to-pve.sh <pve-host> <api-token> <node> <storage> <file-path> <content-type>
#   content-type: "iso" or "vztmpl"
set -euo pipefail

PVE_HOST="$1"
API_TOKEN="$2"
NODE="$3"
STORAGE="$4"
FILE_PATH="$5"
CONTENT_TYPE="${6:-iso}"

FILENAME=$(basename "$FILE_PATH")
API_URL="https://${PVE_HOST}/api2/json/nodes/${NODE}/storage/${STORAGE}/upload"

# Check if file already exists on storage
echo "Checking if ${FILENAME} already exists on ${STORAGE}..."
EXISTING=$(curl -sk \
    -H "Authorization: PVEAPIToken=${API_TOKEN}" \
    "https://${PVE_HOST}/api2/json/nodes/${NODE}/storage/${STORAGE}/content" \
    | python3 -c "
import json, sys
data = json.load(sys.stdin).get('data', [])
for item in data:
    if item.get('volid', '').endswith('/${FILENAME}'):
        print(item['volid'])
        break
" 2>/dev/null || true)

if [ -n "$EXISTING" ]; then
    echo "Already exists: $EXISTING (skipping upload)"
    echo "$EXISTING"
    exit 0
fi

echo "Uploading ${FILENAME} ($(du -h "$FILE_PATH" | cut -f1)) to ${STORAGE}:${CONTENT_TYPE}/..."
RESPONSE=$(curl -sk --progress-bar \
    -H "Authorization: PVEAPIToken=${API_TOKEN}" \
    -F "content=${CONTENT_TYPE}" \
    -F "filename=@${FILE_PATH}" \
    "$API_URL")

UPID=$(echo "$RESPONSE" | python3 -c "import json,sys; print(json.load(sys.stdin)['data'])" 2>/dev/null || true)
if [ -z "$UPID" ]; then
    echo "ERROR: Upload failed. Response: $RESPONSE" >&2
    exit 1
fi

echo "Upload started: $UPID"

# Wait for upload task to complete
echo "Waiting for upload task..."
for i in $(seq 1 60); do
    STATUS=$(curl -sk \
        -H "Authorization: PVEAPIToken=${API_TOKEN}" \
        "https://${PVE_HOST}/api2/json/nodes/${NODE}/tasks/${UPID}/status" \
        | python3 -c "import json,sys; d=json.load(sys.stdin)['data']; print(d.get('status','unknown'))" 2>/dev/null || echo "unknown")
    if [ "$STATUS" = "stopped" ]; then
        echo "Upload complete: ${STORAGE}:${CONTENT_TYPE}/${FILENAME}"
        echo "${STORAGE}:${CONTENT_TYPE}/${FILENAME}"
        exit 0
    fi
    sleep 2
done

echo "ERROR: Upload task did not complete within timeout" >&2
exit 1
