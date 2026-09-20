variable "environment" {
  description = "Environment name: \"development\", \"test\", \"staging\", or \"production\". Used as a naming/secret-naming prefix."
  type        = string

  validation {
    condition     = contains(["development", "test", "staging", "production"], var.environment)
    error_message = "environment must be one of: development, test, staging, production."
  }
}

variable "location" {
  description = "Azure region for this environment's resources."
  type        = string
  default     = "westeurope"
}

variable "vnet_address_space" {
  description = "CIDR address space for this environment's VNet, e.g. \"10.10.0.0/16\". Must not overlap other environments."
  type        = list(string)
}

variable "function_subnet_prefix" {
  description = "CIDR prefix for the Function App's delegated subnet, e.g. \"10.10.1.0/24\"."
  type        = string
}

variable "function_app_sku_name" {
  description = "App Service Plan SKU for the Function App - \"FC1\" (Flex Consumption) is the only supported value; see modules/function-app."
  type        = string
  default     = "FC1"
}

variable "database_sku_name" {
  description = "Azure SQL Database SKU."
  type        = string
  default     = "GP_S_Gen5_1"
}

variable "manage_database_user" {
  description = "Whether to attempt automatic contained-database-user creation (see modules/sql/README.md)."
  type        = bool
  default     = false
}

variable "tenant_id" {
  description = "Entra ID tenant ID."
  type        = string
}

variable "sql_aad_admin_login_name" {
  description = "Display name of the Entra ID group/user that administers this environment's SQL server."
  type        = string
}

variable "sql_aad_admin_object_id" {
  description = "Object ID of the Entra ID group/user that administers this environment's SQL server."
  type        = string
}

variable "shared_key_vault_id" {
  description = "Resource ID of the shared central Key Vault (bootstrap output shared_key_vault_id / azurerm_key_vault.shared.id)."
  type        = string
}

variable "shared_key_vault_uri" {
  description = "URI of the shared central Key Vault (bootstrap output key_vault_uri)."
  type        = string
}

variable "danfoss_client_id" {
  description = "Danfoss API OAuth client ID. Pass via TF_VAR_danfoss_client_id / -var at apply time - never commit to a tfvars file."
  type        = string
  sensitive   = true
}

variable "danfoss_client_secret" {
  description = "Danfoss API OAuth client secret. Pass via TF_VAR_danfoss_client_secret / -var at apply time - never commit to a tfvars file."
  type        = string
  sensitive   = true
}

variable "locations" {
  description = "Statically configured locations for this environment (see CLAUDE.md Location Configuration): slug, danfossDeviceId, latitude, longitude."
  type = list(object({
    slug            = string
    danfossDeviceId = string
    latitude        = number
    longitude       = number
  }))
}

variable "tags" {
  description = "Common tags applied to all resources in this environment."
  type        = map(string)
  default     = {}
}
