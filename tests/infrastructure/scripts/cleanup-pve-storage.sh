#!/usr/bin/env bash
# Remove an uploaded ISO from PVE storage.
#
# Usage: cleanup-pve-storage.sh <pve-host> <api-token> <volume-id>
#   e.g.: cleanup-pve-storage.sh 172.16.100.150 "root@pam!tok=secret" "local:iso/proxmox-ve_9.1-1-auto.iso"
set -euo pipefail

PVE_HOST="$1"
API_TOKEN="$2"
VOLUME_ID="$3"

# Extract node name from API
NODE=$(curl -sk -H "Authorization: PVEAPIToken=${API_TOKEN}" \
    "https://${PVE_HOST}/api2/json/nodes" \
    | python3 -c "import json,sys; print(json.load(sys.stdin)['data'][0]['node'])")

echo "Deleting ${VOLUME_ID} from node ${NODE}..."
ENCODED_VOLID=$(python3 -c "import urllib.parse; print(urllib.parse.quote('${VOLUME_ID}', safe=''))")

RESPONSE=$(curl -sk -X DELETE \
    -H "Authorization: PVEAPIToken=${API_TOKEN}" \
    "https://${PVE_HOST}/api2/json/nodes/${NODE}/storage/local/content/${ENCODED_VOLID}")

echo "Response: $RESPONSE"
echo "Cleanup complete."
