terraform {
  required_version = ">= 1.5.0"
  required_providers {
    proxmox = {
      source  = "bpg/proxmox"
      # disk.import_from and content_type "import" need >= 0.79.0
      version = ">= 0.79.0"
    }
  }
}

provider "proxmox" {
  endpoint  = var.proxmox_endpoint
  api_token = var.proxmox_api_token
  insecure  = var.proxmox_insecure
}

resource "proxmox_virtual_environment_file" "auto_iso" {
  for_each     = var.pve_isos
  content_type = "iso"
  datastore_id = var.iso_storage
  node_name    = var.target_node
  overwrite    = true

  source_file {
    path = each.value
  }
}

# ── Nested PVE VMs ────────────────────────────────────────────────────

resource "proxmox_virtual_environment_vm" "nested_pve" {
  for_each  = var.pve_instances
  name      = each.value.vm_name
  node_name = var.target_node
  vm_id     = each.value.vm_id
  pool_id   = var.pool_id

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
    file_id   = proxmox_virtual_environment_file.auto_iso[each.value.pve_version].id
    interface = "ide2"
  }

  network_device {
    bridge      = var.network_bridge
    model       = "virtio"
    mac_address = each.value.mac_address
  }

  operating_system {
    type = "l26"
  }

  agent {
    enabled = true
  }

  started = true

  # VMs must not boot until the storage VM exists — it serves the HTTP answer
  # files the PVE auto-installer fetches. run-integration.sh additionally
  # configures the storage VM (phase one) before applying these resources.
  depends_on = [proxmox_virtual_environment_vm.storage]

  lifecycle {
    ignore_changes = [started, cdrom]
  }
}
