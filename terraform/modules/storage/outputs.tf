output "storage_account_id" {
  description = "Resource ID of the storage account, used to scope RBAC role assignments (e.g. VNet-rule / private-network firewalling)."
  value       = azurerm_storage_account.this.id
}

output "storage_account_name" {
  description = "Name of the storage account."
  value       = azurerm_storage_account.this.name
}

output "primary_blob_endpoint" {
  description = "Primary Blob service endpoint, e.g. \"https://sthumiditydevelopmentxxxx.blob.core.windows.net/\". Used for the app's ClimateStorage__ServiceUri and to build the Flex Consumption deployment storage container's endpoint URL."
  value       = azurerm_storage_account.this.primary_blob_endpoint
}

output "container_name" {
  description = "Name of the climate-readings blob container."
  value       = azurerm_storage_container.climate_readings.name
}
