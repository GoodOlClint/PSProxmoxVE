output "pve_test_vm_id" {
  value = var.vm_id
}

output "pve_test_node_name" {
  value       = "pve"
  description = "Default node name inside a fresh PVE install"
}
