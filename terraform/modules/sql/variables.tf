variable "name_prefix" {
  description = "Naming prefix used to derive the SQL server/database names."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group the SQL server is created in."
  type        = string
}

variable "location" {
  description = "Azure region for the SQL server."
  type        = string
}

variable "database_sku_name" {
  description = "SKU of the Azure SQL Database, e.g. \"GP_S_Gen5_1\" (serverless) or \"GP_Gen5_2\"."
  type        = string
  default     = "GP_S_Gen5_1"
}

variable "serverless_min_capacity" {
  description = "Minimum vCores when database_sku_name is a serverless (\"..._S_...\") SKU. Ignored for provisioned SKUs."
  type        = number
  default     = 0.5
}

variable "serverless_auto_pause_delay_in_minutes" {
  description = "Minutes of inactivity before a serverless database auto-pauses (billing drops to storage-only). Ignored for provisioned SKUs. Set to -1 to disable auto-pause."
  type        = number
  default     = 60
}

variable "aad_admin_login_name" {
  description = "Display name of the Entra ID group or user that is Azure AD administrator for this SQL server. Should be a group of engineers, not a service principal."
  type        = string
}

variable "aad_admin_object_id" {
  description = "Object ID of the Entra ID group or user that is Azure AD administrator for this SQL server."
  type        = string
}

variable "tenant_id" {
  description = "Entra ID tenant ID."
  type        = string
}

variable "function_subnet_id" {
  description = "Resource ID of the Function App's delegated/service-endpoint-enabled subnet. The only network path allowed to reach this SQL server."
  type        = string
}

variable "function_identity_principal_id" {
  description = "Object (principal) ID of the Function App's system-assigned managed identity. Granted a contained database user via a post-provision script, since the azurerm provider cannot run T-SQL directly."
  type        = string
}

variable "function_identity_name" {
  description = "Name to give the contained database user created for the Function App's managed identity (typically the Function App name)."
  type        = string
}

variable "manage_database_user" {
  description = <<-EOT
    Whether Terraform should attempt to create the contained database user for the Function
    App's managed identity via sqlcmd (local-exec). Requires the machine running `terraform
    apply` to have network line-of-sight to the SQL server (temporarily allowed through the
    VNet rule, e.g. a GitHub Actions self-hosted runner inside the VNet, or a jump host) and
    the `sqlcmd`/Az.Sql PowerShell tooling installed. Set to false to skip and run the
    CREATE USER / ALTER ROLE statements manually instead - see modules/sql/README.md.
  EOT
  type        = bool
  default     = false
}

variable "tags" {
  description = "Common tags applied to SQL resources."
  type        = map(string)
  default     = {}
}
