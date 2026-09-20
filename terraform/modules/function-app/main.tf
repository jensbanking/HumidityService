# Flex Consumption plan: bills per-execution like the old Consumption (Y1) plan - no idle
# reserved-instance cost - while still supporting regional VNet Integration, which this app
# needs to reach Azure SQL through the VNet-firewalled path (see modules/sql). Elastic Premium
# (EP1/EP2/...) was the previous choice here; it was replaced because it bills for at least one
# always-on pre-warmed instance 24/7 regardless of actual traffic, which is wasteful for an
# app that only runs once an hour.
resource "azurerm_service_plan" "this" {
  name                = "asp-${var.name_prefix}"
  resource_group_name = var.resource_group_name
  location            = var.location
  os_type             = "Linux"
  sku_name            = var.sku_name

  tags = var.tags
}

# Flex Consumption requires its own dedicated deployment package container, separate from any
# data containers the app writes to (see modules/storage for those).
resource "azurerm_storage_container" "deployment_package" {
  name                  = "deployment-${var.name_prefix}"
  storage_account_id    = var.storage_account_id
  container_access_type = "private"
}

resource "azurerm_function_app_flex_consumption" "this" {
  name                = "func-${var.name_prefix}"
  resource_group_name = var.resource_group_name
  location            = var.location
  service_plan_id     = azurerm_service_plan.this.id

  # Identity-based deployment storage connection (Managed Identity, preferred per project
  # standards) rather than an access-key connection string.
  storage_container_type      = "blobContainer"
  storage_container_endpoint  = "${var.storage_primary_blob_endpoint}${azurerm_storage_container.deployment_package.name}"
  storage_authentication_type = "SystemAssignedIdentity"

  runtime_name    = "dotnet-isolated"
  runtime_version = var.runtime_version

  maximum_instance_count = var.maximum_instance_count
  instance_memory_in_mb  = var.instance_memory_in_mb

  virtual_network_subnet_id = var.function_subnet_id

  identity {
    type = "SystemAssigned"
  }

  site_config {
    application_insights_connection_string = var.app_insights_connection_string
    vnet_route_all_enabled                 = true
  }

  # Feature 3 requirement: Blob Storage connection string is wired into the Function App's
  # environment automatically via Terraform - as a Key Vault reference, not a plaintext
  # value, so the secret itself never lands in source control or the app setting itself.
  app_settings = {
    "ClimateStorage__ConnectionString" = "@Microsoft.KeyVault(SecretUri=${var.key_vault_uri}secrets/${var.storage_connection_string_secret_name}/)"
    "ClimateStorage__ContainerName"    = var.storage_container_name

    "DanfossApi__BaseUrl"      = var.danfoss_api_base_url
    "DanfossApi__ClientId"     = "@Microsoft.KeyVault(SecretUri=${var.key_vault_uri}secrets/${var.danfoss_client_id_secret_name}/)"
    "DanfossApi__ClientSecret" = "@Microsoft.KeyVault(SecretUri=${var.key_vault_uri}secrets/${var.danfoss_client_secret_secret_name}/)"

    "OpenMeteoApi__BaseUrl" = var.open_meteo_api_base_url

    "SqlConnectionString" = "@Microsoft.KeyVault(SecretUri=${var.key_vault_uri}secrets/${var.sql_connection_string_secret_name}/)"

    "Locations" = var.locations_json
  }

  tags = var.tags
}

# Required for storage_authentication_type = "SystemAssignedIdentity": both the deployment
# package container above and the Functions host's own runtime state need blob data access
# via the identity.
resource "azurerm_role_assignment" "function_storage_access" {
  scope                = var.storage_account_id
  role_definition_name = "Storage Blob Data Owner"
  principal_id         = azurerm_function_app_flex_consumption.this.identity[0].principal_id
}
