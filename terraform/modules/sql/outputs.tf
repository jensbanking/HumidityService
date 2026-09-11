output "server_id" {
  description = "Resource ID of the SQL server."
  value       = azurerm_mssql_server.this.id
}

output "server_fqdn" {
  description = "Fully qualified domain name of the SQL server, used to build the Function App's connection string."
  value       = azurerm_mssql_server.this.fully_qualified_domain_name
}

output "database_name" {
  description = "Name of the SQL database."
  value       = azurerm_mssql_database.this.name
}
