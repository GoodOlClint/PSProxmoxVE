output "pve_test_host" {
  value = var.test_vm_ip
}

output "pve_test_port" {
  value = 8006
}

output "pve_test_url" {
  value = "https://${var.test_vm_ip}:8006"
}

output "pve_test_vm_id" {
  value = var.vm_id
}

output "pve_test_node_name" {
  value       = "pve"
  description = "Default node name inside a fresh PVE install"
}

output "pve_test_api_token" {
  value     = fileexists("${path.module}/.api-token") ? trimspace(file("${path.module}/.api-token")) : "not-yet-created"
  sensitive = true
}
