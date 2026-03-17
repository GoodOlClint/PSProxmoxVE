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

  ssh {
    agent = true
  }
}

# Upload PVE ISO to the target node
resource "proxmox_virtual_environment_file" "pve_iso" {
  content_type = "iso"
  datastore_id = var.iso_storage
  node_name    = var.target_node

  source_file {
    path = var.iso_file
  }
}

# Answer file for unattended PVE installation
resource "proxmox_virtual_environment_file" "answer_file" {
  content_type = "snippets"
  datastore_id = var.answer_file_storage
  node_name    = var.target_node

  source_raw {
    data = templatefile("${path.module}/answer.toml.tftpl", {
      root_password = var.test_vm_password
      cidr          = "${var.test_vm_ip}/${var.test_vm_netmask_bits}"
      dns           = var.test_vm_dns
      gateway       = var.test_vm_gateway
    })
    file_name = "pve-test-answer.toml"
  }
}

# Nested PVE VM
resource "proxmox_virtual_environment_vm" "nested_pve" {
  name        = var.vm_name
  node_name   = var.target_node
  vm_id       = var.vm_id
  description = "Automated nested PVE test instance - safe to destroy"
  tags        = ["test", "nested-pve", "auto-managed"]

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
    enabled   = true
    file_id   = proxmox_virtual_environment_file.pve_iso.id
    interface = "ide2"
  }

  network_device {
    bridge = var.network_bridge
    model  = "virtio"
  }

  boot_order = ["scsi0", "ide2"]

  operating_system {
    type = "l26"
  }

  on_boot = true
  started = true

  lifecycle {
    ignore_changes = [cdrom]
  }
}

# Wait for PVE API to become responsive after installation
resource "null_resource" "wait_for_api" {
  depends_on = [proxmox_virtual_environment_vm.nested_pve]

  provisioner "local-exec" {
    command = "${path.module}/scripts/wait-for-api.sh ${var.test_vm_ip} 8006 600"
  }
}

# Create a test API token on the nested PVE for integration tests
resource "null_resource" "create_api_token" {
  depends_on = [null_resource.wait_for_api]

  provisioner "local-exec" {
    command = <<-EOT
      # Get a ticket first
      TICKET_DATA=$(curl -sk -d "username=root@pam&password=${var.test_vm_password}" \
        "https://${var.test_vm_ip}:8006/api2/json/access/ticket")
      TICKET=$(echo "$TICKET_DATA" | jq -r '.data.ticket')
      CSRF=$(echo "$TICKET_DATA" | jq -r '.data.CSRFPreventionToken')

      # Create API token
      TOKEN_DATA=$(curl -sk -X POST \
        -H "Cookie: PVEAuthCookie=$TICKET" \
        -H "CSRFPreventionToken: $CSRF" \
        -d "tokenid=integration" \
        -d "privsep=0" \
        "https://${var.test_vm_ip}:8006/api2/json/access/users/root@pam/token/integration")

      TOKEN_VALUE=$(echo "$TOKEN_DATA" | jq -r '.data.value')
      echo "root@pam!integration=$TOKEN_VALUE" > ${path.module}/.api-token
    EOT
  }
}
