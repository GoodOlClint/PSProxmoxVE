#!/usr/bin/env bash
# Configures the shared services VM inside the CI sandbox VLAN: NFS export,
# iSCSI target, and the HTTP answer server the PVE auto-installers fetch
# from. Idempotent — safe to re-run against an already-configured VM.
#
# Usage: setup-storage-server.sh <storage-vm-ip> <ssh-private-key-path> <iscsi-iqn> <answer-dir>
#   <answer-dir> is the local directory holding default.toml and answers/.

set -euo pipefail

STORAGE_IP="${1:?Usage: setup-storage-server.sh <ip> <ssh-key> <iqn> <answer-dir>}"
SSH_KEY="${2:?missing SSH private key path}"
ISCSI_IQN="${3:?missing iSCSI IQN}"
ANSWER_DIR="${4:?missing answer directory}"

# Same pinned image the Terraform-managed runner container used previously.
ANSWER_SERVER_IMAGE="slothcroissant/proxmox-auto-installer-server@sha256:0f45d7bfe6e3cc76aa00fc578e40b80b9054e377db18a79122866fe5522bc7ed"

SSH_OPTS="-i ${SSH_KEY} -o IdentitiesOnly=yes -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o LogLevel=ERROR"

echo "=== Configuring storage services on ${STORAGE_IP} ==="

# Cloud-init may still be running on first boot; wait for SSH to accept the
# key login it installs. The last attempt's stderr is kept so an auth failure
# is distinguishable from an unreachable host.
for i in $(seq 1 60); do
    if ssh ${SSH_OPTS} -o ConnectTimeout=5 "ubuntu@${STORAGE_IP}" true 2>/tmp/storage-ssh-err; then
        break
    fi
    if [[ $i -eq 60 ]]; then
        echo "ERROR: storage VM at ${STORAGE_IP} not reachable over SSH" >&2
        cat /tmp/storage-ssh-err >&2
        exit 1
    fi
    sleep 5
done

# sshd accepts logins before cloud-init's first-boot work (and its apt/dpkg
# locks) is finished; a degraded cloud-init result is fine for our purposes,
# and a hung cloud-init must fail fast, not eat the job timeout.
echo "Waiting for cloud-init to finish (up to 300s)..."
timeout 300 ssh ${SSH_OPTS} "ubuntu@${STORAGE_IP}" \
    "sudo cloud-init status --wait" \
    || echo "WARNING: cloud-init did not finish cleanly within 300s — continuing"

echo "Network probe from the storage VM:"
ssh ${SSH_OPTS} "ubuntu@${STORAGE_IP}" '
    ip -4 -brief addr; ip route show default
    ping -c1 -W2 "$(ip route show default | awk "{print \$3; exit}")" >/dev/null 2>&1 \
        && echo "gateway ping:  OK" || echo "gateway ping:  FAIL"
    ping -c1 -W2 1.1.1.1 >/dev/null 2>&1 \
        && echo "internet ping: OK" || echo "internet ping: FAIL"
    getent hosts archive.ubuntu.com >/dev/null 2>&1 \
        && echo "DNS resolve:   OK" || echo "DNS resolve:   FAIL (resolv.conf: $(grep ^nameserver /etc/resolv.conf | tr "\n" " "))"
' || true

echo "Copying answer files..."
ssh ${SSH_OPTS} "ubuntu@${STORAGE_IP}" \
    "sudo mkdir -p /opt/answer-server && sudo rm -rf /opt/answer-server/answers && sudo chown ubuntu /opt/answer-server"
scp ${SSH_OPTS} -r \
    "${ANSWER_DIR}/default.toml" "${ANSWER_DIR}/answers" \
    "ubuntu@${STORAGE_IP}:/opt/answer-server/"

ssh ${SSH_OPTS} "ubuntu@${STORAGE_IP}" \
    "sudo ISCSI_IQN='${ISCSI_IQN}' ANSWER_SERVER_IMAGE='${ANSWER_SERVER_IMAGE}' bash -s" <<'REMOTE'
set -euo pipefail
export DEBIAN_FRONTEND=noninteractive

# cloud-init's first-boot apt activity can hold the dpkg lock briefly;
# a network/DNS failure must fail fast with the real apt error, not fall
# through to a misleading "package not found" from empty lists.
echo "Updating apt package lists..."
for i in $(seq 1 12); do
    if apt_out=$(apt-get update -qq 2>&1); then
        break
    fi
    echo "apt-get update attempt $i/12 failed"
    if [ "$i" -eq 12 ]; then
        echo "$apt_out" >&2
        echo "ERROR: apt-get update never succeeded — check CI VLAN egress/DNS" >&2
        exit 1
    fi
    sleep 5
done
echo "Installing nfs-kernel-server, tgt, docker.io..."
apt-get install -y -qq nfs-kernel-server tgt docker.io >/dev/null

# Ubuntu's nfs-kernel-server package does not create /etc/exports.d
mkdir -p /etc/exports.d /srv/nfs/shared /srv/iscsi
echo '/srv/nfs/shared *(rw,sync,no_subtree_check,no_root_squash)' > /etc/exports.d/pvetest.exports
exportfs -ra

if [ ! -f /srv/iscsi/lun0.img ]; then
    truncate -s 10G /srv/iscsi/lun0.img
fi
cat > /etc/tgt/conf.d/pvetest.conf <<CONF
<target ${ISCSI_IQN}>
    backing-store /srv/iscsi/lun0.img
</target>
CONF

systemctl enable --now nfs-kernel-server tgt >/dev/null || {
    journalctl -u nfs-kernel-server -u tgt --no-pager -n 30 >&2
    exit 1
}
systemctl restart tgt

echo "Starting answer server container..."
docker rm -f pvetest-answer-server >/dev/null 2>&1 || true
docker run -d --name pvetest-answer-server --restart unless-stopped \
    --network host \
    -v /opt/answer-server/answers:/app/answers \
    -v /opt/answer-server/default.toml:/app/default.toml \
    "${ANSWER_SERVER_IMAGE}" >/dev/null

# Ordering requirement: the PVE installers fetch answers as soon as this
# script returns, so :8000 must accept connections before exit (any HTTP
# status counts as listening).
for i in $(seq 1 20); do
    code=$(curl -s -o /dev/null -w '%{http_code}' --max-time 2 http://127.0.0.1:8000/ || true)
    [ "$code" != "000" ] && break
    if [ "$i" -eq 20 ]; then
        echo "ERROR: answer server did not start listening on :8000" >&2
        docker logs pvetest-answer-server 2>&1 | tail -20 >&2
        exit 1
    fi
    sleep 3
done

echo "Services ready: NFS /srv/nfs/shared, iSCSI ${ISCSI_IQN}, answer server :8000"
REMOTE

echo "Storage server configuration complete."
