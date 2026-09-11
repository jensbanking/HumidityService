output "resource_group_name" {
  description = "Name of this environment's resource group."
  value       = azurerm_resource_group.this.name
}

output "function_app_name" {
  description = "Name of this environment's Function App."
  value       = module.function_app.function_app_name
}

output "storage_account_name" {
  description = "Name of this environment's storage account."
  value       = module.storage.storage_account_name
}

output "sql_server_fqdn" {
  description = "Fully qualified domain name of this environment's SQL server."
  value       = module.sql.server_fqdn
}

output "sql_database_name" {
  description = "Name of this environment's SQL database."
  value       = module.sql.database_name
}

output "application_insights_connection_string" {
  description = "Application Insights connection string for this environment."
  value       = azurerm_application_insights.this.connection_string
  sensitive   = true
}
