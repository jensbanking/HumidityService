output "shared_resource_group_name" {
  description = "Name of the shared resource group. Used by per-environment configs that reference shared resources."
  value       = azurerm_resource_group.shared.name
}

output "tfstate_storage_account_name" {
  description = "Storage account holding Terraform state. Reference this in each environment's backend \"azurerm\" block."
  value       = azurerm_storage_account.tfstate.name
}

output "tfstate_container_name" {
  description = "Blob container holding Terraform state files."
  value       = azurerm_storage_container.tfstate.name
}

output "key_vault_name" {
  description = "Name of the shared central Key Vault."
  value       = azurerm_key_vault.shared.name
}

output "key_vault_uri" {
  description = "URI of the shared central Key Vault."
  value       = azurerm_key_vault.shared.vault_uri
}
