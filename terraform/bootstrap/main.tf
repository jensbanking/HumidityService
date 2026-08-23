data "azurerm_client_config" "current" {}

resource "azurerm_resource_group" "shared" {
  name     = "rg-${var.name_prefix}-shared"
  location = var.location
  tags     = var.tags
}

# Storage account name must be globally unique, lowercase alphanumeric, <= 24 chars.
resource "random_string" "state_storage_suffix" {
  length  = 6
  special = false
  upper   = false
}

resource "azurerm_storage_account" "tfstate" {
  name                     = "st${var.name_prefix}state${random_string.state_storage_suffix.result}"
  resource_group_name      = azurerm_resource_group.shared.name
  location                 = azurerm_resource_group.shared.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  min_tls_version          = "TLS1_2"

  blob_properties {
    versioning_enabled = true
  }

  tags = var.tags
}

resource "azurerm_storage_container" "tfstate" {
  name                  = "tfstate"
  storage_account_id    = azurerm_storage_account.tfstate.id
  container_access_type = "private"
}

resource "azurerm_key_vault" "shared" {
  name                       = "kv-${var.name_prefix}-shared"
  resource_group_name        = azurerm_resource_group.shared.name
  location                   = azurerm_resource_group.shared.location
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  rbac_authorization_enabled = true
  purge_protection_enabled   = true
  soft_delete_retention_days = 90

  tags = var.tags
}

# Grants the identity running this bootstrap admin rights so it can create
# secrets immediately after apply. Per-environment access (Function App
# Managed Identities) is granted in the per-environment Terraform configs.
resource "azurerm_role_assignment" "bootstrap_identity_kv_admin" {
  scope                = azurerm_key_vault.shared.id
  role_definition_name = "Key Vault Administrator"
  principal_id         = data.azurerm_client_config.current.object_id
}
