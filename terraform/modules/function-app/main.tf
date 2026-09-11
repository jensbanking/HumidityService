# Elastic Premium plan: Consumption does not support regional VNet Integration, which this
# app needs to reach Azure SQL through the VNet-firewalled path (see modules/sql).
resource "azurerm_service_plan" "this" {
  name                = "asp-${var.name_prefix}"
  resource_group_name = var.resource_group_name
  location            = var.location
  os_type             = "Linux"
  sku_name            = var.sku_name

  tags = var.tags
}

resource "azurerm_linux_function_app" "this" {
  name                = "func-${var.name_prefix}"
  resource_group_name = var.resource_group_name
  location            = var.location
  service_plan_id     = azurerm_service_plan.this.id

  # Identity-based storage connection (Managed Identity, preferred per project standards)
  # rather than an access-key connection string for the runtime's own storage account.
  storage_account_name          = var.storage_account_name
  storage_uses_managed_identity = true

  virtual_network_subnet_id = var.function_subnet_id

  identity {
    type = "SystemAssigned"
  }

  site_config {
    application_stack {
      # NOTE: requires an azurerm provider version whose validation accepts "10.0" as a
      # supported Functions .NET-isolated stack; bump versions.tf if `terraform plan` rejects it.
      dotnet_version              = "10.0"
      use_dotnet_isolated_runtime = true
    }

    application_insights_connection_string = var.app_insights_connection_string
    vnet_route_all_enabled                 = true
  }

  # Feature 3 requirement: Blob Storage connection string is wired into the Function App's
  # environment automatically via Terraform - as a Key Vault reference, not a plaintext
  # value, so the secret itself never lands in source control or the app setting itself.
  # storage_account_name + storage_uses_managed_identity above already configure the
  # AzureWebJobsStorage__accountName identity-based connection; it does not need restating here.
  app_settings = {
    FUNCTIONS_WORKER_RUNTIME = "dotnet-isolated"

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

  lifecycle {
    # The platform stamps these onto app_settings/site_config after deploys; ignoring them
    # stops every `terraform plan` from showing a spurious diff.
    ignore_changes = [
      app_settings["WEBSITE_RUN_FROM_PACKAGE"],
      app_settings["FUNCTIONS_EXTENSION_VERSION"],
    ]
  }
}

# Required for storage_uses_managed_identity: the Functions host itself (triggers, bindings,
# lease blobs) needs blob/queue/table access to its own storage account via the identity.
resource "azurerm_role_assignment" "function_storage_access" {
  scope                = var.storage_account_id
  role_definition_name = "Storage Blob Data Owner"
  principal_id         = azurerm_linux_function_app.this.identity[0].principal_id
}
