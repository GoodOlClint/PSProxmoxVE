variable "proxmox_endpoint" {
  description = "URL of the existing Proxmox VE API (e.g. https://pve.example.com:8006)"
  type        = string
}

variable "proxmox_api_token" {
  description = "API token for authenticating with the existing Proxmox host (user@realm!tokenid=secret)"
  type        = string
  sensitive   = true
}

variable "proxmox_insecure" {
  description = "Whether to skip TLS verification when connecting to the Proxmox API"
  type        = bool
  default     = true
}

variable "target_node" {
  description = "Name of the Proxmox node where the nested PVE VM will be created"
  type        = string
}

variable "vm_id" {
  description = "VMID to assign to the nested PVE virtual machine"
  type        = number
  default     = 99900
}

variable "vm_name" {
  description = "Name for the nested PVE virtual machine"
  type        = string
  default     = "pve-test-nested"
}

variable "cores" {
  description = "Number of CPU cores to allocate to the nested PVE VM"
  type        = number
  default     = 4
}

variable "memory" {
  description = "Amount of memory in MB to allocate to the nested PVE VM"
  type        = number
  default     = 8192
}

variable "disk_size" {
  description = "Size of the primary disk in GB for the nested PVE VM"
  type        = number
  default     = 64
}

variable "disk_storage" {
  description = "Proxmox storage pool for VM disks (must support raw format)"
  type        = string
  default     = "nas-iscsi-lvm"
}

variable "iso_local_path" {
  description = "Local path to the prepared auto-install PVE ISO on the runner"
  type        = string
}

variable "iso_storage" {
  description = "Proxmox storage pool for uploading the ISO (must accept ISO content type)"
  type        = string
  default     = "local"
}

variable "network_bridge" {
  description = "Network bridge on the host to attach the nested PVE VM to"
  type        = string
  default     = "Core"
}

variable "test_vm_password" {
  description = "Root password for the nested PVE instance"
  type        = string
  sensitive   = true
  default     = "Testpass123!"
}
