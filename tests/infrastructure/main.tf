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

resource "proxmox_virtual_environment_vm" "nested_pve" {
  name      = var.vm_name
  node_name = var.target_node
  vm_id     = var.vm_id

  machine = "q35"
  bios    = "ovmf"

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
    file_id   = var.iso_file_id
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
