output "storage_account_id" {
  description = "Resource ID of the storage account, used to scope RBAC role assignments (e.g. VNet-rule / private-network firewalling)."
  value       = azurerm_storage_account.this.id
}

output "storage_account_name" {
  description = "Name of the storage account."
  value       = azurerm_storage_account.this.name
}

output "primary_connection_string" {
  description = "Primary connection string for the storage account. Sensitive: written to a Key Vault secret by the environment module rather than passed around in plain app settings."
  value       = azurerm_storage_account.this.primary_connection_string
  sensitive   = true
}

output "container_name" {
  description = "Name of the climate-readings blob container."
  value       = azurerm_storage_container.climate_readings.name
}
