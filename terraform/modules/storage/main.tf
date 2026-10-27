# Storage account name must be globally unique, lowercase alphanumeric, <= 24 chars.
resource "random_string" "suffix" {
  keepers = {
    name_prefix = var.name_prefix
  }

  length  = 6
  special = false
  upper   = false
}

resource "azurerm_storage_account" "this" {
  name                     = substr("st${replace(var.name_prefix, "-", "")}${random_string.suffix.result}", 0, 24)
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  min_tls_version          = "TLS1_2"

  blob_properties {
    versioning_enabled = true
  }

  tags = var.tags
}

# Raw indoor/outdoor climate JSON blobs (Feature 1 & 2). Named "yyyy/MM/dd/HH/apiraw_..." and "yyyy/MM/dd/HH/{location}/{indoor|outdoor|subdevices}_{location}_..."
# by the Function code itself; Terraform only needs to guarantee the container exists.
resource "azurerm_storage_container" "climate_readings" {
  name                  = var.container_name
  storage_account_id    = azurerm_storage_account.this.id
  container_access_type = "private"
}

# Small state blobs, e.g. the date a humidity alert was last sent per location (Feature 5), kept
# apart from the raw readings so ETL/backfill never has to skip them.
resource "azurerm_storage_container" "alert_state" {
  name                  = "alert-state"
  storage_account_id    = azurerm_storage_account.this.id
  container_access_type = "private"
}

# Destination for corrupt/unparseable source files (Feature 10). Created now so app settings
# referencing it are valid from day one, even though nothing writes to it until Feature 10.
resource "azurerm_storage_container" "dead_letter" {
  name                  = "dead-letter"
  storage_account_id    = azurerm_storage_account.this.id
  container_access_type = "private"
}
