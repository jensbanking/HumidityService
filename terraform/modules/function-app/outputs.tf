output "function_app_name" {
  description = "Name of the Function App. Also used as the contained database user's name in the SQL module (must match exactly for `FROM EXTERNAL PROVIDER` to resolve)."
  value       = azurerm_linux_function_app.this.name
}

output "function_app_id" {
  description = "Resource ID of the Function App."
  value       = azurerm_linux_function_app.this.id
}

output "principal_id" {
  description = "Object (principal) ID of the Function App's system-assigned managed identity."
  value       = azurerm_linux_function_app.this.identity[0].principal_id
}
