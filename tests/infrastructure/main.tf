terraform {
  required_version = ">= 1.5.0"
  required_providers {
    proxmox = {
      source  = "bpg/proxmox"
      version = ">= 0.70.0"
    }
  }
}

provider "proxmox" {
  endpoint  = var.proxmox_endpoint
  api_token = var.proxmox_api_token
  insecure  = var.proxmox_insecure
}

resource "proxmox_virtual_environment_file" "auto_iso" {
  for_each     = var.pve_instances
  content_type = "iso"
  datastore_id = var.iso_storage
  node_name    = var.target_node

  source_file {
    path = each.value.iso_local_path
  }
}

# ── Nested PVE VMs ────────────────────────────────────────────────────

resource "proxmox_virtual_environment_vm" "nested_pve" {
  for_each  = var.pve_instances
  name      = each.value.vm_name
  node_name = var.target_node
  vm_id     = each.value.vm_id

  machine    = "q35"
  bios       = "ovmf"
  boot_order = ["scsi0", "ide2"]

  cpu {
    type    = "host"
    cores   = var.cores
    sockets = 1
  }

  memory {
    dedicated = var.memory
  }

  efi_disk {
    datastore_id = var.disk_storage
    type         = "4m"
  }

  disk {
    datastore_id = var.disk_storage
    interface    = "scsi0"
    size         = var.disk_size
    file_format  = "raw"
  }

  cdrom {
    file_id   = proxmox_virtual_environment_file.auto_iso[each.key].id
    interface = "ide2"
  }

  network_device {
    bridge = var.network_bridge
    model  = "virtio"
  }

  operating_system {
    type = "l26"
  }

  agent {
    enabled = true
  }

  started = true

  lifecycle {
    ignore_changes = [started, cdrom]
  }
}
