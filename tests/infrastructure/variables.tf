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
  default     = "local-lvm"
}

variable "iso_storage" {
  description = "Proxmox storage pool for uploading the PVE ISO"
  type        = string
  default     = "local"
}

variable "iso_file" {
  description = "Local path to the Proxmox VE installation ISO file"
  type        = string
}

variable "network_bridge" {
  description = "Network bridge on the host to attach the nested PVE VM to"
  type        = string
  default     = "vmbr0"
}

variable "test_vm_ip" {
  description = "Static IP address to assign to the nested PVE instance"
  type        = string
}

variable "test_vm_gateway" {
  description = "Default gateway for the nested PVE instance"
  type        = string
}

variable "test_vm_netmask_bits" {
  description = "CIDR prefix length for the nested PVE network (e.g. 24 for /24)"
  type        = string
  default     = "24"
}

variable "test_vm_dns" {
  description = "DNS server for the nested PVE instance"
  type        = string
  default     = "1.1.1.1"
}

variable "test_vm_password" {
  description = "Root password for the nested PVE instance"
  type        = string
  sensitive   = true
  default     = "Testpass123!"
}

variable "answer_file_storage" {
  description = "Proxmox storage pool for the answer file snippet (must support snippets content type)"
  type        = string
  default     = "local"
}
