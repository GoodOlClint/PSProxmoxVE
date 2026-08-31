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
  description = "Map of PVE instances to provision. Key is a node label (e.g. '9a'), value defines the VM."
  type = map(object({
    pve_version = string
    vm_id       = number
    vm_name     = string
    mac_address = string
  }))
}

variable "pve_isos" {
  description = "Map of PVE version to the local path of the generic HTTP auto-install ISO."
  type        = map(string)
  default     = {}
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
  description = "Proxmox storage pool for uploads (must accept the iso AND import content types — import is not enabled by default on most storages)"
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

variable "storage_vmid" {
  description = "VMID for the shared storage VM (must be inside the CI pool's reserved range)"
  type        = number
  default     = 5080
}

variable "storage_vm_ssh_public_key" {
  description = "SSH public key granted to the storage VM's ubuntu user (cloud images refuse password SSH; required for provision, unused on destroy)"
  type        = string
  default     = ""
}

variable "cloud_image_path" {
  description = "Local path to the Ubuntu cloud image imported as the storage VM's disk (required for provision; unused on destroy)"
  type        = string
  default     = ""
}

variable "pool_id" {
  description = "Resource pool the nested VMs are created in (a pool-scoped API token can only allocate here)"
  type        = string
  default     = null
}
