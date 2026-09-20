output "function_app_name" {
  description = "Name of the Function App. Also used as the contained database user's name in the SQL module (must match exactly for `FROM EXTERNAL PROVIDER` to resolve)."
  value       = azurerm_function_app_flex_consumption.this.name
}

output "function_app_id" {
  description = "Resource ID of the Function App."
  value       = azurerm_function_app_flex_consumption.this.id
}

output "principal_id" {
  description = "Object (principal) ID of the Function App's system-assigned managed identity."
  value       = azurerm_function_app_flex_consumption.this.identity[0].principal_id
}
