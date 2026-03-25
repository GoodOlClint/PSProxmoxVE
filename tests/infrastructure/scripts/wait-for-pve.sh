#!/usr/bin/env bash
# Wait for a fresh nested PVE instance to boot, discover its IP via the QEMU guest agent,
# then wait for the PVE API to become responsive.
#
# Usage: wait-for-pve.sh <parent-pve-endpoint> <parent-api-token> <vm-id> <root-password> [max-wait-seconds]
#   Outputs:
#     IP=<discovered-ip>
#     NODE=<pve-hostname>
set -euo pipefail

PARENT_ENDPOINT="${1%/}"
PARENT_TOKEN="$2"
VM_ID="$3"
ROOT_PASSWORD="$4"
MAX_WAIT="${5:-600}"
INTERVAL=10
PARENT_API="${PARENT_ENDPOINT}/api2/json"
NODES_JSON=$(curl -sk -H "Authorization: PVEAPIToken=${PARENT_TOKEN}" \
    "${PARENT_API}/nodes")
PARENT_NODE=$(echo "$NODES_JSON" | python3 -c "import json,sys; print(json.load(sys.stdin)['data'][0]['node'])")

# --- Phase 1: Discover IP via QEMU guest agent ---
echo "Waiting for guest agent on VM ${VM_ID} (node: ${PARENT_NODE})..."
VM_IP=""
elapsed=0
while [ $elapsed -lt $MAX_WAIT ]; do
    AGENT_RESPONSE=$(curl -sk \
        -H "Authorization: PVEAPIToken=${PARENT_TOKEN}" \
        "${PARENT_API}/nodes/${PARENT_NODE}/qemu/${VM_ID}/agent/network-get-interfaces" 2>/dev/null || true)

    VM_IP=$(echo "$AGENT_RESPONSE" | python3 -c "
import json, sys
try:
    data = json.load(sys.stdin).get('data', {}).get('result', [])
    for iface in data:
        if iface.get('name') == 'lo':
            continue
        for addr in iface.get('ip-addresses', []):
            if addr.get('ip-address-type') == 'ipv4' and not addr['ip-address'].startswith('127.'):
                print(addr['ip-address'])
                sys.exit(0)
except:
    pass
" 2>/dev/null || true)

    if [ -n "$VM_IP" ]; then
        echo "Discovered VM IP: $VM_IP (after ${elapsed}s)"
        break
    fi
    echo "  Guest agent not ready yet (${elapsed}s elapsed)..."
    sleep $INTERVAL
    elapsed=$((elapsed + INTERVAL))
done

if [ -z "$VM_IP" ]; then
    echo "ERROR: Could not discover VM IP via guest agent after ${MAX_WAIT}s" >&2
    exit 1
fi

# --- Phase 2: Wait for PVE API on the nested instance ---
NESTED_API="https://${VM_IP}:8006/api2/json"
echo "Waiting for nested PVE API at ${NESTED_API}..."
while [ $elapsed -lt $MAX_WAIT ]; do
    if curl -sk --connect-timeout 5 "${NESTED_API}/access/domains" 2>/dev/null | grep -q '"realm"'; then
        echo "Nested PVE API is responsive after ${elapsed}s"
        break
    fi
    echo "  API not ready yet (${elapsed}s elapsed)..."
    sleep $INTERVAL
    elapsed=$((elapsed + INTERVAL))
done

if [ $elapsed -ge $MAX_WAIT ]; then
    echo "ERROR: Nested PVE API not responsive after ${MAX_WAIT}s" >&2
    exit 1
fi

# --- Phase 3: Verify authentication works ---
echo "Verifying root@pam authentication..."
AUTH_RESPONSE=$(curl -sk -d "username=root@pam&password=${ROOT_PASSWORD}" \
    "${NESTED_API}/access/ticket" 2>/dev/null || true)
TICKET=$(echo "$AUTH_RESPONSE" | python3 -c "import json,sys; print(json.load(sys.stdin)['data']['ticket'])" 2>/dev/null || true)

if [ -z "$TICKET" ]; then
    echo "ERROR: root@pam authentication failed on ${VM_IP}. Response: $AUTH_RESPONSE" >&2
    exit 1
fi
echo "Authentication verified."

# Discover the nested node's hostname
NODE_NAME=$(curl -sk -b "PVEAuthCookie=${TICKET}" \
    "${NESTED_API}/nodes" 2>/dev/null \
    | python3 -c "import json,sys; print(json.load(sys.stdin)['data'][0]['node'])" 2>/dev/null || echo "unknown")

echo "IP=${VM_IP}"
echo "NODE=${NODE_NAME}"
