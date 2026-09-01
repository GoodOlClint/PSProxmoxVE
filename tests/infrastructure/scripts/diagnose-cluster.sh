#!/usr/bin/env bash
# Dump corosync state from both nested PVE nodes after a cluster test failure.
#
# Usage: diagnose-cluster.sh [8|9]
#
# The PVE API reports a joined-but-offline node as online=0 with no further
# detail; corosync's own view lives only on the nodes, which the cleanup job
# destroys minutes later. Best-effort: never fails the caller.
#
# Required env vars:
#   PVE_PASSWORD   Root password for the nested PVE instances
#
# Optional env vars:
#   CONFIG_FILE    Test config JSON (default: $CACHE_DIR/work/config.json)
#   CACHE_DIR      Shared cache mount (default: /opt/pve-integration)

VERSION="${1:-9}"
CACHE_DIR="${CACHE_DIR:-/opt/pve-integration}"
CONFIG_FILE="${CONFIG_FILE:-$CACHE_DIR/work/config.json}"

if [[ ! -f "$CONFIG_FILE" ]]; then
    echo "diagnose-cluster: no config at $CONFIG_FILE — nothing to inspect"
    exit 0
fi

if [[ -z "${PVE_PASSWORD:-}" ]]; then
    echo "diagnose-cluster: PVE_PASSWORD unset — cannot reach the nodes"
    exit 0
fi

SSH_OPTS=(-o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o LogLevel=ERROR -o ConnectTimeout=10)

dump_node() {
    local label="$1" ip="$2"
    echo
    echo "══════════ $label ($ip) ══════════"
    if [[ -z "$ip" || "$ip" == "null" ]]; then
        echo "  no address in $CONFIG_FILE"
        return
    fi

    sshpass -p "$PVE_PASSWORD" ssh "${SSH_OPTS[@]}" "root@${ip}" bash -s <<'REMOTE' 2>&1 || echo "  ssh to $ip failed (rc=$?)"
set +e
echo "--- hostname / resolution ---"
hostname -f
echo "hostname -i: $(hostname -i 2>&1)"
grep -vE '^\s*#' /etc/hosts | grep -vE '^\s*$'
echo
echo "--- addresses ---"
ip -4 -o addr show scope global
echo
echo "--- corosync.conf ---"
cat /etc/pve/corosync.conf 2>&1 || cat /etc/corosync/corosync.conf 2>&1
echo
echo "--- corosync-cfgtool -s ---"
corosync-cfgtool -s 2>&1
echo
echo "--- pvecm status ---"
pvecm status 2>&1
echo
echo "--- corosync service ---"
systemctl is-active corosync pve-cluster 2>&1
echo
echo "--- journalctl -u corosync (last 60) ---"
journalctl -u corosync -n 60 --no-pager 2>&1
echo
echo "--- journalctl -u pve-cluster (last 30) ---"
journalctl -u pve-cluster -n 30 --no-pager 2>&1
echo
echo "--- cluster task logs ---"
# "Cluster join aborted!" is generic; the reason is only in the task log.
find /var/log/pve/tasks -type f \( -name '*clusterjoin*' -o -name '*clustercreate*' \) \
    -exec echo "== {} ==" \; -exec cat {} \; 2>&1 | tail -80
REMOTE
}

echo "=== Cluster diagnostics for PVE $VERSION ==="
node_a="$(jq -r ".pve${VERSION}.nodes.a.host // empty" "$CONFIG_FILE")"
node_b="$(jq -r ".pve${VERSION}.nodes.b.host // empty" "$CONFIG_FILE")"

dump_node "node A" "$node_a"
dump_node "node B" "$node_b"

echo
echo "=== End cluster diagnostics ==="
exit 0
