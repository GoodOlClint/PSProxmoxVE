# ── Answer server container ──────────────────────────────────────────

resource "docker_container" "answer_server" {
  name     = "pvetest-answer-server"
  image    = "slothcroissant/proxmox-auto-installer-server:latest"
  restart  = "unless-stopped"
  must_run = true
  start    = true

  network_mode = "host"

  volumes {
    host_path      = var.answer_files_dir
    container_path = "/app/answers"
  }

  volumes {
    host_path      = var.default_answer_file
    container_path = "/app/default.toml"
  }
}

# ── Docker images & volumes ──────────────────────────────────────────

resource "docker_image" "ubuntu" {
  name = "ubuntu:24.04"
}

resource "docker_volume" "iscsi_data" {
  name = "pvetest-iscsi-data"
}

resource "docker_volume" "nfs_data" {
  name = "pvetest-nfs-data"
}

# ── iSCSI target container ──────────────────────────────────────────

resource "docker_container" "iscsi_target" {
  name       = "pvetest-iscsi"
  image      = docker_image.ubuntu.image_id
  privileged = true
  restart    = "unless-stopped"

  network_mode = "host"

  volumes {
    volume_name    = docker_volume.iscsi_data.name
    container_path = "/srv/iscsi"
  }

  env = [
    "ISCSI_IQN=${var.storage_iscsi_iqn}",
    "ISCSI_LUN_SIZE=${var.storage_iscsi_lun_size}",
  ]

  entrypoint = ["/bin/bash", "-c"]
  command = [<<-EOT
    set -e
    apt-get update -qq && apt-get install -y -qq tgt >/dev/null 2>&1
    mkdir -p /srv/iscsi
    if [ ! -f /srv/iscsi/lun0.img ]; then
      truncate -s $${ISCSI_LUN_SIZE} /srv/iscsi/lun0.img
    fi
    tgtd --foreground &
    sleep 2
    if ! tgtadm --lld iscsi --op show --mode target | grep -q "Target 1: $${ISCSI_IQN}"; then
      tgtadm --lld iscsi --op new --mode target --tid 1 -T $${ISCSI_IQN}
    fi
    if ! tgtadm --lld iscsi --op show --mode logicalunit --tid 1 2>/dev/null | grep -qE "LUN:[[:space:]]*1($$|[^0-9])"; then
      tgtadm --lld iscsi --op new --mode logicalunit --tid 1 --lun 1 --backing-store /srv/iscsi/lun0.img
    fi
    if ! tgtadm --lld iscsi --op show --mode target --tid 1 2>/dev/null | grep -q "Initiator-address: ALL"; then
      tgtadm --lld iscsi --op bind --mode target --tid 1 -I ALL
    fi
    echo "iSCSI target ready: $${ISCSI_IQN} (port 3260)"
    wait
  EOT
  ]
}

# ── NFS server container ────────────────────────────────────────────

resource "docker_container" "nfs_server" {
  name       = "pvetest-nfs"
  image      = "erichough/nfs-server:2.2.1"
  privileged = true
  restart    = "unless-stopped"

  network_mode = "host"

  volumes {
    volume_name    = docker_volume.nfs_data.name
    container_path = "/srv/nfs/shared"
  }

  volumes {
    host_path      = "/lib/modules"
    container_path = "/lib/modules"
    read_only      = true
  }

  env = [
    "NFS_EXPORT_0=/srv/nfs/shared *(rw,sync,no_subtree_check,no_root_squash)",
  ]
}
