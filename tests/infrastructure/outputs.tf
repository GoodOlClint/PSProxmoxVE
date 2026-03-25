output "pve_vm_ids" {
  description = "Map of instance key to VM ID"
  value       = { for k, v in proxmox_virtual_environment_vm.nested_pve : k => v.vm_id }
}

output "pve_test_node_name" {
  value       = "pve"
  description = "Default node name inside a fresh PVE install"
}

output "storage_ip" {
  description = "IP address where storage services are reachable"
  value       = var.docker_host_ip
}

output "storage_iscsi_iqn" {
  description = "iSCSI target IQN"
  value       = var.storage_iscsi_iqn
}

output "storage_nfs_export" {
  description = "NFS export path"
  value       = "${var.docker_host_ip}:/srv/nfs/shared"
}
