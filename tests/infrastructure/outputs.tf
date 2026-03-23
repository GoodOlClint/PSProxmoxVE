output "pve_vm_ids" {
  description = "Map of instance key to VM ID"
  value       = { for k, v in proxmox_virtual_environment_vm.nested_pve : k => v.vm_id }
}

output "pve_test_node_name" {
  value       = "pve"
  description = "Default node name inside a fresh PVE install"
}
