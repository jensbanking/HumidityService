locals {
  name_prefix = "humidity-${var.environment}"
}

resource "azurerm_resource_group" "this" {
  name     = "rg-${local.name_prefix}"
  location = var.location
  tags     = var.tags
}

resource "azurerm_log_analytics_workspace" "this" {
  name                = "log-${local.name_prefix}"
  resource_group_name = azurerm_resource_group.this.name
  location            = azurerm_resource_group.this.location
  sku                 = "PerGB2018"
  retention_in_days   = 30

  tags = var.tags
}

resource "azurerm_application_insights" "this" {
  name                = "appi-${local.name_prefix}"
  resource_group_name = azurerm_resource_group.this.name
  location            = azurerm_resource_group.this.location
  workspace_id        = azurerm_log_analytics_workspace.this.id
  application_type    = "other"

  tags = var.tags
}

module "networking" {
  source = "../networking"

  name_prefix            = local.name_prefix
  resource_group_name    = azurerm_resource_group.this.name
  location               = azurerm_resource_group.this.location
  address_space          = var.vnet_address_space
  function_subnet_prefix = var.function_subnet_prefix
  tags                   = var.tags
}

module "storage" {
  source = "../storage"

  name_prefix         = local.name_prefix
  resource_group_name = azurerm_resource_group.this.name
  location            = azurerm_resource_group.this.location
  tags                = var.tags
}

module "sql" {
  source = "../sql"

  name_prefix                    = local.name_prefix
  resource_group_name            = azurerm_resource_group.this.name
  location                       = azurerm_resource_group.this.location
  database_sku_name              = var.database_sku_name
  aad_admin_login_name           = var.sql_aad_admin_login_name
  aad_admin_object_id            = var.sql_aad_admin_object_id
  tenant_id                      = var.tenant_id
  function_subnet_id             = module.networking.function_subnet_id
  function_identity_principal_id = module.function_app.principal_id
  function_identity_name         = module.function_app.function_app_name
  manage_database_user           = var.manage_database_user
  tags                           = var.tags
}

# Secrets live in the shared central Key Vault (from terraform/bootstrap), namespaced per
# environment, rather than a per-environment vault - matches CLAUDE.md's single central KV.
resource "azurerm_key_vault_secret" "storage_connection_string" {
  name         = "${var.environment}-storage-connection-string"
  value        = module.storage.primary_connection_string
  key_vault_id = var.shared_key_vault_id
}

resource "azurerm_key_vault_secret" "danfoss_client_id" {
  name         = "${var.environment}-danfoss-client-id"
  value        = var.danfoss_client_id
  key_vault_id = var.shared_key_vault_id
}

resource "azurerm_key_vault_secret" "danfoss_client_secret" {
  name         = "${var.environment}-danfoss-client-secret"
  value        = var.danfoss_client_secret
  key_vault_id = var.shared_key_vault_id
}

resource "azurerm_key_vault_secret" "sql_connection_string" {
  name = "${var.environment}-sql-connection-string"
  # Active Directory Managed Identity auth: no password/secret embedded, but the connection
  # string is centralized here like the others so the Function App only ever needs one
  # Key Vault reference per setting.
  value        = "Server=tcp:${module.sql.server_fqdn},1433;Database=${module.sql.database_name};Authentication=Active Directory Managed Identity;"
  key_vault_id = var.shared_key_vault_id
}

module "function_app" {
  source = "../function-app"

  name_prefix            = local.name_prefix
  resource_group_name    = azurerm_resource_group.this.name
  location               = azurerm_resource_group.this.location
  sku_name               = var.function_app_sku_name
  function_subnet_id     = module.networking.function_subnet_id
  storage_account_name   = module.storage.storage_account_name
  storage_account_id     = module.storage.storage_account_id
  storage_container_name = module.storage.container_name
  key_vault_uri          = var.shared_key_vault_uri

  storage_connection_string_secret_name = azurerm_key_vault_secret.storage_connection_string.name
  danfoss_client_id_secret_name         = azurerm_key_vault_secret.danfoss_client_id.name
  danfoss_client_secret_secret_name     = azurerm_key_vault_secret.danfoss_client_secret.name
  sql_connection_string_secret_name     = "${var.environment}-sql-connection-string"

  locations_json                 = jsonencode(var.locations)
  app_insights_connection_string = azurerm_application_insights.this.connection_string

  tags = var.tags
}

# Lets the Function App's managed identity resolve the @Microsoft.KeyVault(...) app setting
# references above at runtime.
resource "azurerm_role_assignment" "function_kv_secrets_user" {
  scope                = var.shared_key_vault_id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = module.function_app.principal_id
}
