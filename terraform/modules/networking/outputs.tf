output "vnet_id" {
  description = "Resource ID of the environment's VNet."
  value       = azurerm_virtual_network.this.id
}

output "function_subnet_id" {
  description = "Resource ID of the delegated subnet the Function App VNet-integrates into, and that the SQL virtual-network-rule allows."
  value       = azurerm_subnet.function.id
}
