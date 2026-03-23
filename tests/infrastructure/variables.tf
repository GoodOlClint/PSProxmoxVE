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
  description = "Name of the Proxmox node where the nested PVE VMs will be created"
  type        = string
}

variable "pve_instances" {
  description = "Map of PVE instances to provision. Key is a label (e.g. 'pve9'), value defines the VM."
  type = map(object({
    iso_local_path = string
    vm_id          = number
    vm_name        = string
  }))
}

variable "cores" {
  description = "Number of CPU cores to allocate to each nested PVE VM"
  type        = number
  default     = 4
}

variable "memory" {
  description = "Amount of memory in MB to allocate to each nested PVE VM"
  type        = number
  default     = 8192
}

variable "disk_size" {
  description = "Size of the primary disk in GB for each nested PVE VM"
  type        = number
  default     = 64
}

variable "disk_storage" {
  description = "Proxmox storage pool for VM disks (must support raw format)"
  type        = string
  default     = "nas-iSCSI-lvm"
}

variable "iso_storage" {
  description = "Proxmox storage pool for uploading the ISO (must accept ISO content type)"
  type        = string
  default     = "local"
}

variable "network_bridge" {
  description = "Network bridge on the host to attach the nested PVE VMs to"
  type        = string
  default     = "Core"
}

variable "test_vm_password" {
  description = "Root password for the nested PVE instances. Set via TF_VAR_test_vm_password env var."
  type        = string
  sensitive   = true
}
