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
# Generated auto-install ISOs carry a hash of first-boot.sh in the name, so each
# change to that script mints a new filename. Deleting only the current name
# strands every earlier one on the storage, and force-cleanup wipes the
# Terraform state that could otherwise reclaim them. Match the whole family.
ISO_MATCHES=$(curl -sk -H "Authorization: PVEAPIToken=${API_TOKEN}" \
    "${API_BASE}/nodes/${NODE}/storage/${ISO_STORAGE}/content" 2>/dev/null \
    | ISO_FILENAME="$ISO_FILENAME" ISO_STORAGE="$ISO_STORAGE" python3 -c '
import json, os, re, sys

name = os.environ["ISO_FILENAME"]
storage = os.environ["ISO_STORAGE"]
# Generated names are <base>-auto-<storage-vm-fqdn-dashed>-<12 hex of first-boot.sh>.iso.
# Siblings differ only in the hash, so sweep the family by rebuilding the full
# shape — a prefix test alone would also match a longer FQDN or a hand-uploaded
# "-manual-backup.iso", and this script deletes what it matches.
family = re.match(r"^(.+-auto-.+-)[0-9a-f]{12}\.iso$", name)

try:
    data = json.load(sys.stdin).get("data", [])
except Exception:
    sys.exit(0)

def candidates():
    for item in data:
        volid = item.get("volid", "")
        # The channel to the shell is newline-delimited, so a volid carrying a
        # newline would arrive as two lines and the tail would be deleted without
        # ever having matched. The anchored sibling pattern below already
        # excludes such a volid, so this is unreachable today and no test can
        # cover it — it is here so loosening that pattern cannot silently
        # reintroduce the split.
        if any(c in volid for c in "\r\n\0"):
            continue
        # A volid names its own storage. Deleting one through a different
        # storage endpoint is never right.
        if not volid.startswith(storage + ":"):
            continue
        yield volid, volid.rsplit("/", 1)[-1]

if family:
    sibling = re.compile(r"^" + re.escape(family.group(1)) + r"[0-9a-f]{12}\.iso$")
    for volid, base in candidates():
        if sibling.match(base):
            print(volid)
else:
    # Anything else (the storage VM cloud image) keeps the original one-shot
    # behaviour: a basename can repeat across content namespaces, and a
    # non-generated name carries nothing that identifies a family.
    for volid, base in candidates():
        if base == name:
            print(volid)
            break
' 2>/dev/null || true)

if [ -n "$ISO_MATCHES" ]; then
    while IFS= read -r volid; do
        [ -n "$volid" ] || continue
        echo "Found orphaned ISO: ${volid}"
        echo "  Deleting..."
        ENCODED=$(python3 -c 'import sys, urllib.parse; print(urllib.parse.quote(sys.argv[1], safe=""))' "$volid")
        if [ -z "$ENCODED" ]; then
            echo "  WARNING: could not encode ${volid} — skipping rather than issuing a bare DELETE" >&2
            continue
        fi
        code=$(curl -sk -o /dev/null -w '%{http_code}' -X DELETE \
            -H "Authorization: PVEAPIToken=${API_TOKEN}" \
            "${API_BASE}/nodes/${NODE}/storage/${ISO_STORAGE}/content/${ENCODED}" 2>/dev/null || echo 000)
        sleep 2
        case "$code" in
            2*) echo "  ISO cleanup done" ;;
            *)  echo "  WARNING: DELETE of ${volid} returned ${code} — it is still on ${ISO_STORAGE}" >&2
                if [ "${GITHUB_ACTIONS:-}" = "true" ]; then
                    echo "::warning::ISO ${volid} was not deleted (HTTP ${code}); it will accumulate on ${ISO_STORAGE}"
                fi ;;
        esac
    done <<EOF
$ISO_MATCHES
EOF
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
