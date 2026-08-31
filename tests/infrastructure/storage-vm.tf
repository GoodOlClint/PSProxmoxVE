# ── Shared storage VM ───────────────────────────────────────────────
# One small Ubuntu guest in the CI pool serves NFS + iSCSI to every nested
# PVE node. It lives on the same isolated VLAN as the nodes under test, so
# no firewall path out of the CI sandbox is needed. Package install and
# service configuration happen over SSH (setup-storage-server.sh) after boot.

resource "proxmox_virtual_environment_file" "storage_cloud_image" {
  content_type = "import"
  datastore_id = var.iso_storage
  node_name    = var.target_node
  overwrite    = true

  source_file {
    path = var.cloud_image_path
  }
}

resource "proxmox_virtual_environment_vm" "storage" {
  name      = "pvetest-storage"
  node_name = var.target_node
  vm_id     = var.storage_vmid
  pool_id   = var.pool_id

  cpu {
    type    = "host"
    cores   = 2
    sockets = 1
  }

  memory {
    dedicated = 2048
  }

  disk {
    datastore_id = var.disk_storage
    interface    = "scsi0"
    size         = 32
    import_from  = proxmox_virtual_environment_file.storage_cloud_image.id
  }

  initialization {
    datastore_id = var.disk_storage

    ip_config {
      ipv4 {
        address = var.storage_vm_ip
        gateway = var.storage_vm_gateway
      }
    }

    dns {
      servers = [var.storage_vm_gateway]
    }

    user_account {
      username = "ubuntu"
      # Password is console-only: Ubuntu cloud images refuse SSH password auth
      # and PVE's cloud-init never sets ssh_pwauth. SSH uses the key.
      password = var.test_vm_password
      keys     = [trimspace(var.storage_vm_ssh_public_key)]
    }
  }

  network_device {
    bridge = var.network_bridge
    model  = "virtio"
  }

  operating_system {
    type = "l26"
  }

  # Resized Ubuntu cloud images kernel-panic on boot without a serial console
  # (bpg/terraform-provider-proxmox documented issue).
  serial_device {
    device = "socket"
  }

  started = true
}
