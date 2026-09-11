# Azure SQL Database for the data warehouse (Feature 5). Access is Managed-Identity-only:
# SQL native authentication is disabled server-wide (azuread_authentication_only), and the
# only network path in is the VNet the Function App integrates into (no public firewall rules).

locals {
  # Serverless SKUs (e.g. "GP_S_Gen5_1") require min_capacity/auto_pause_delay_in_minutes to be
  # set explicitly - the provider's zero-value default is rejected by Azure ("does not support
  # min capacity '0'"). Provisioned SKUs (e.g. "GP_Gen5_2") reject those same arguments outright.
  database_is_serverless = can(regex("_S_", var.database_sku_name))
}

resource "azurerm_mssql_server" "this" {
  name                = "sql-${var.name_prefix}"
  resource_group_name = var.resource_group_name
  location            = var.location
  version             = "12.0"

  # No administrator_login/administrator_login_password: AAD-only auth below removes SQL
  # native authentication entirely, so there is no local admin credential to manage or leak.
  azuread_administrator {
    login_username              = var.aad_admin_login_name
    object_id                   = var.aad_admin_object_id
    tenant_id                   = var.tenant_id
    azuread_authentication_only = true
  }

  minimum_tls_version = "1.2"

  # No public firewall rules are defined; the VNet rule below is the only allowed path in.
  public_network_access_enabled = true

  tags = var.tags
}

resource "azurerm_mssql_database" "this" {
  name           = "sqldb-${var.name_prefix}"
  server_id      = azurerm_mssql_server.this.id
  sku_name       = var.database_sku_name
  zone_redundant = false

  min_capacity                = local.database_is_serverless ? var.serverless_min_capacity : null
  auto_pause_delay_in_minutes = local.database_is_serverless ? var.serverless_auto_pause_delay_in_minutes : null

  tags = var.tags
}

# Firewalls the SQL server to the Function App's subnet only (see modules/networking).
resource "azurerm_mssql_virtual_network_rule" "function_subnet" {
  name      = "vnet-rule-function-subnet"
  server_id = azurerm_mssql_server.this.id
  subnet_id = var.function_subnet_id
}

# Best-effort: creates the contained database user mapped to the Function App's managed
# identity and grants it db_datareader/db_datawriter. See variables.tf for why this is
# gated behind manage_database_user and modules/sql/README.md for the manual fallback.
resource "null_resource" "function_identity_db_user" {
  count = var.manage_database_user ? 1 : 0

  triggers = {
    server_fqdn = azurerm_mssql_server.this.fully_qualified_domain_name
    database    = azurerm_mssql_database.this.name
    identity    = var.function_identity_principal_id
  }

  provisioner "local-exec" {
    interpreter = ["PowerShell", "-Command"]
    command     = <<-EOT
      $ErrorActionPreference = "Stop"
      $sql = @"
      IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'${var.function_identity_name}')
      BEGIN
          CREATE USER [${var.function_identity_name}] FROM EXTERNAL PROVIDER;
      END
      ALTER ROLE db_datareader ADD MEMBER [${var.function_identity_name}];
      ALTER ROLE db_datawriter ADD MEMBER [${var.function_identity_name}];
"@
      Invoke-Sqlcmd -ServerInstance "${azurerm_mssql_server.this.fully_qualified_domain_name}" `
        -Database "${azurerm_mssql_database.this.name}" `
        -AccessToken (Get-AzAccessToken -ResourceUrl "https://database.windows.net/").Token `
        -Query $sql
    EOT
  }

  depends_on = [azurerm_mssql_virtual_network_rule.function_subnet]
}
