#!/usr/bin/env bash
# Wait for a fresh nested PVE instance to boot, discover its IP via the QEMU guest agent,
# then wait for the PVE API and create an API token.
#
# Usage: create-api-token.sh <parent-pve-endpoint> <parent-api-token> <vm-id> <root-password> [max-wait-seconds]
#   parent-pve-endpoint: Full URL e.g. https://pve.example.com:8006
#   Outputs two lines:
#     IP=<discovered-ip>
#     TOKEN=root@pam!integration=<secret>
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

# --- Phase 3: Authenticate and create API token ---
echo "Authenticating as root@pam on nested PVE..."
AUTH_RESPONSE=$(curl -sk -d "username=root@pam&password=${ROOT_PASSWORD}" \
    "${NESTED_API}/access/ticket")
TICKET=$(echo "$AUTH_RESPONSE" | python3 -c "import json,sys; print(json.load(sys.stdin)['data']['ticket'])" 2>/dev/null || true)
CSRF=$(echo "$AUTH_RESPONSE" | python3 -c "import json,sys; print(json.load(sys.stdin)['data']['CSRFPreventionToken'])" 2>/dev/null || true)

if [ -z "$TICKET" ] || [ -z "$CSRF" ]; then
    echo "ERROR: Authentication failed. Response: $AUTH_RESPONSE" >&2
    exit 1
fi

echo "Creating API token root@pam!integration..."
TOKEN_RESPONSE=$(curl -sk \
    -b "PVEAuthCookie=${TICKET}" \
    -H "CSRFPreventionToken: ${CSRF}" \
    -d "privsep=0" \
    "${NESTED_API}/access/users/root@pam/token/integration")

TOKEN_VALUE=$(echo "$TOKEN_RESPONSE" | python3 -c "import json,sys; print(json.load(sys.stdin)['data']['value'])" 2>/dev/null || true)

if [ -z "$TOKEN_VALUE" ]; then
    echo "ERROR: Token creation failed. Response: $TOKEN_RESPONSE" >&2
    exit 1
fi

echo "IP=${VM_IP}"
echo "TOKEN=root@pam!integration=${TOKEN_VALUE}"
