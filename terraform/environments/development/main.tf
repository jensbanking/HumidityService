module "environment" {
  source = "../../modules/environment"

  environment = "development"
  location    = "westeurope"

  vnet_address_space     = ["10.10.0.0/16"]
  function_subnet_prefix = "10.10.1.0/24"

  function_app_sku_name = "EP1"
  database_sku_name     = "GP_S_Gen5_1" # serverless: cheapest option, fine for a non-production environment
  manage_database_user  = false         # see modules/sql/README.md for the manual fallback

  tenant_id                = var.tenant_id
  sql_aad_admin_login_name = var.sql_aad_admin_login_name
  sql_aad_admin_object_id  = var.sql_aad_admin_object_id

  shared_key_vault_id  = var.shared_key_vault_id
  shared_key_vault_uri = var.shared_key_vault_uri

  danfoss_client_id     = var.danfoss_client_id
  danfoss_client_secret = var.danfoss_client_secret

  # TODO: replace with the real Danfoss device ID(s) for this location before first apply.
  locations = [
    {
      slug            = "aarhus-house1"
      danfossDeviceId = "REPLACE_WITH_REAL_DANFOSS_DEVICE_ID"
      latitude        = 56.1496
      longitude       = 10.2134
    },
  ]

  tags = {
    project     = "HumidityService"
    environment = "development"
    managed_by  = "terraform"
  }
}
