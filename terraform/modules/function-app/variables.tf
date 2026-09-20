variable "name_prefix" {
  description = "Naming prefix used to derive the Function App and plan names."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group the Function App is created in."
  type        = string
}

variable "location" {
  description = "Azure region for the Function App."
  type        = string
}

variable "sku_name" {
  description = "App Service Plan SKU. \"FC1\" (Flex Consumption) is the only SKU this module's Flex Consumption Function App resource supports - it bills per-execution (no idle/reserved-instance cost) while still supporting regional VNet Integration, unlike the older Consumption (Y1) plan."
  type        = string
  default     = "FC1"
}

variable "runtime_version" {
  description = "dotnet-isolated runtime version for the Flex Consumption Function App, matching the FunctionHost project's TargetFramework (e.g. \"10.0\")."
  type        = string
  default     = "10.0"
}

variable "maximum_instance_count" {
  description = "Upper bound on concurrent Flex Consumption instances."
  type        = number
  default     = 40
}

variable "instance_memory_in_mb" {
  description = "Memory per Flex Consumption instance in MB (2048 or 4096)."
  type        = number
  default     = 2048
}

variable "function_subnet_id" {
  description = "Resource ID of the delegated subnet (see modules/networking) to regionally VNet-integrate the Function App into."
  type        = string
}

variable "storage_account_name" {
  description = "Name of the storage account backing AzureWebJobsStorage and ClimateStorage."
  type        = string
}

variable "storage_account_id" {
  description = "Resource ID of the storage account, used to scope the RBAC role assignment for the Function App's identity-based storage connections."
  type        = string
}

variable "storage_primary_blob_endpoint" {
  description = "Primary Blob service endpoint of the storage account (module.storage output primary_blob_endpoint), used to build the Flex Consumption deployment storage container's endpoint URL."
  type        = string
}

variable "storage_container_name" {
  description = "Blob container name for ClimateStorage__ContainerName."
  type        = string
}

variable "key_vault_uri" {
  description = "URI of the shared central Key Vault, used to build Key Vault reference app settings."
  type        = string
}

variable "danfoss_client_id_secret_name" {
  description = "Name of the Key Vault secret holding DanfossApi__ClientId."
  type        = string
}

variable "danfoss_client_secret_secret_name" {
  description = "Name of the Key Vault secret holding DanfossApi__ClientSecret."
  type        = string
}

variable "storage_connection_string_secret_name" {
  description = "Name of the Key Vault secret holding the storage account's connection string (AzureWebJobsStorage / ClimateStorage__ConnectionString)."
  type        = string
}

variable "sql_connection_string_secret_name" {
  description = "Name of the Key Vault secret holding SqlConnectionString."
  type        = string
}

variable "danfoss_api_base_url" {
  description = "DanfossApi__BaseUrl."
  type        = string
  default     = "https://api.danfoss.com"
}

variable "open_meteo_api_base_url" {
  description = "OpenMeteoApi__BaseUrl."
  type        = string
  default     = "https://api.open-meteo.com"
}

variable "locations_json" {
  description = "JSON-encoded array of configured locations (slug, danfossDeviceId, latitude, longitude), matching the \"Locations\" app setting consumed by ConfigurationLocationProvider."
  type        = string
}

variable "app_insights_connection_string" {
  description = "Application Insights connection string for OpenTelemetry export."
  type        = string
  sensitive   = true
}

variable "tags" {
  description = "Common tags applied to Function App resources."
  type        = map(string)
  default     = {}
}
