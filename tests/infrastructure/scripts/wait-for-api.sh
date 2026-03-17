#!/usr/bin/env bash
set -euo pipefail

HOST="$1"
PORT="${2:-8006}"
MAX_WAIT="${3:-600}"
INTERVAL=10

echo "Waiting for PVE API at https://${HOST}:${PORT}..."
elapsed=0
while [ $elapsed -lt $MAX_WAIT ]; do
    if curl -sk --connect-timeout 5 "https://${HOST}:${PORT}/api2/json/version" 2>/dev/null | grep -q '"version"'; then
        echo "PVE API is responsive after ${elapsed}s"
        exit 0
    fi
    echo "  Not ready yet (${elapsed}s elapsed)..."
    sleep $INTERVAL
    elapsed=$((elapsed + INTERVAL))
done

echo "ERROR: PVE API not responsive after ${MAX_WAIT}s"
exit 1
