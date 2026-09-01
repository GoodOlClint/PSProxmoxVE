#!/usr/bin/env bash
# Pre-flight cleanup for CI runs.
# Ensures no leftover resources from a previous failed run before starting fresh.
#
# Usage: preflight-cleanup.sh <pve-endpoint> <api-token> <vm-id> <iso-filename> <terraform-dir>
set -uo pipefail
# Note: not using -e — we want to attempt all cleanup steps even if some fail

PVE_ENDPOINT="${1%/}"
API_TOKEN="$2"
VM_ID="$3"
ISO_FILENAME="$4"
TF_DIR="$5"
API_BASE="${PVE_ENDPOINT}/api2/json"

# Node: PVE_TARGET_NODE when set (a pool-scoped token cannot list /nodes), else the first node.
NODE="${PVE_TARGET_NODE:-}"
if [ -z "$NODE" ]; then
    NODES_JSON=$(curl -sk -H "Authorization: PVEAPIToken=${API_TOKEN}" "${API_BASE}/nodes" 2>/dev/null)
    NODE=$(echo "$NODES_JSON" | python3 -c "import json,sys; print(json.load(sys.stdin)['data'][0]['node'])" 2>/dev/null || echo "pve")
fi
ISO_STORAGE="${TF_VAR_iso_storage:-}"

echo "=== Pre-flight cleanup (node: ${NODE}, vmid: ${VM_ID}) ==="

# --- Clean up orphaned VM ---
VM_STATUS=$(curl -sk -H "Authorization: PVEAPIToken=${API_TOKEN}" \
    "${API_BASE}/nodes/${NODE}/qemu/${VM_ID}/status/current" 2>/dev/null \
    | python3 -c "import json,sys; print(json.load(sys.stdin).get('data',{}).get('status',''))" 2>/dev/null || true)

if [ -n "$VM_STATUS" ]; then
    echo "Found orphaned VM ${VM_ID} (status: ${VM_STATUS})"
    if [ "$VM_STATUS" = "running" ]; then
        echo "  Stopping VM..."
        curl -sk -X POST -H "Authorization: PVEAPIToken=${API_TOKEN}" \
            "${API_BASE}/nodes/${NODE}/qemu/${VM_ID}/status/stop" >/dev/null 2>&1
        # Wait for stop
        for i in $(seq 1 12); do
            sleep 5
            S=$(curl -sk -H "Authorization: PVEAPIToken=${API_TOKEN}" \
                "${API_BASE}/nodes/${NODE}/qemu/${VM_ID}/status/current" 2>/dev/null \
                | python3 -c "import json,sys; print(json.load(sys.stdin).get('data',{}).get('status',''))" 2>/dev/null || true)
            if [ "$S" = "stopped" ]; then break; fi
        done
    fi
    echo "  Deleting VM..."
    curl -sk -X DELETE -H "Authorization: PVEAPIToken=${API_TOKEN}" \
        "${API_BASE}/nodes/${NODE}/qemu/${VM_ID}?destroy-unreferenced-disks=1&purge=1" >/dev/null 2>&1
    sleep 3
    echo "  VM cleanup done"
else
    echo "No orphaned VM ${VM_ID} found"
fi

# --- Clean up orphaned ISO ---
if [ -z "$ISO_FILENAME" ]; then
    echo "No ISO filename specified, skipping ISO cleanup"
elif [ -z "$ISO_STORAGE" ]; then
    # force-cleanup is the only cleanup CI runs and it wipes Terraform state, so a
    # skipped ISO delete here strands the upload with nothing left to reclaim it.
    if [ "${GITHUB_ACTIONS:-}" = "true" ]; then
        echo "::warning::TF_VAR_iso_storage is unset — ISO cleanup skipped; the uploaded auto-install ISO is stranded"
    fi
    echo "WARNING: TF_VAR_iso_storage is unset — skipping ISO cleanup rather than guessing a storage pool"
else
ISO_EXISTS=$(curl -sk -H "Authorization: PVEAPIToken=${API_TOKEN}" \
    "${API_BASE}/nodes/${NODE}/storage/${ISO_STORAGE}/content" 2>/dev/null \
    | python3 -c "
import json, sys
data = json.load(sys.stdin).get('data', [])
for item in data:
    if item.get('volid', '').endswith('/${ISO_FILENAME}'):
        print(item['volid'])
        break
" 2>/dev/null || true)

if [ -n "$ISO_EXISTS" ]; then
    echo "Found orphaned ISO: ${ISO_EXISTS}"
    echo "  Deleting..."
    ENCODED=$(python3 -c "import urllib.parse; print(urllib.parse.quote('${ISO_EXISTS}', safe=''))")
    curl -sk -X DELETE -H "Authorization: PVEAPIToken=${API_TOKEN}" \
        "${API_BASE}/nodes/${NODE}/storage/${ISO_STORAGE}/content/${ENCODED}" >/dev/null 2>&1
    sleep 2
    echo "  ISO cleanup done"
else
    echo "No orphaned ISO found"
fi
fi  # end ISO_FILENAME check

# --- Clean up stale Terraform state ---
if [ -d "$TF_DIR" ]; then
    if [ -f "${TF_DIR}/.terraform.tfstate.lock.info" ]; then
        echo "Found stale Terraform lock, removing..."
        rm -f "${TF_DIR}/.terraform.tfstate.lock.info"
    fi
    if [ -f "${TF_DIR}/terraform.tfstate" ]; then
        echo "Found stale Terraform state, removing..."
        rm -f "${TF_DIR}/terraform.tfstate" "${TF_DIR}/terraform.tfstate.backup"
    fi
else
    echo "Terraform dir not found (clean checkout)"
fi

echo "=== Pre-flight cleanup complete ==="
