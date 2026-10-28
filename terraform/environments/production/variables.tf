variable "tenant_id" {
  description = "Entra ID tenant ID."
  type        = string
}

variable "shared_key_vault_id" {
  description = "Resource ID of the shared central Key Vault. From `terraform output key_vault_id` in terraform/bootstrap (add that output if missing, or build it from `key_vault_name`)."
  type        = string
}

variable "shared_key_vault_uri" {
  description = "URI of the shared central Key Vault. From `terraform output key_vault_uri` in terraform/bootstrap."
  type        = string
}

variable "sql_aad_admin_login_name" {
  description = "Display name of the Entra ID group/user administering this environment's SQL server (e.g. \"sql-admins-development\")."
  type        = string
}

variable "sql_aad_admin_object_id" {
  description = "Object ID of the Entra ID group/user administering this environment's SQL server."
  type        = string
}

variable "humidity_alert_recipients" {
  description = "Email addresses that receive humidity alerts. Supply via TF_VAR_humidity_alert_recipients (JSON list, e.g. [\"you@example.com\"]) so addresses stay out of source control; empty disables sending."
  type        = list(string)
  default     = []
}

variable "danfoss_client_id" {
  description = "Danfoss API OAuth client ID for this environment. Supply via TF_VAR_danfoss_client_id - never commit to terraform.tfvars."
  type        = string
  sensitive   = true
}

variable "danfoss_client_secret" {
  description = "Danfoss API OAuth client secret for this environment. Supply via TF_VAR_danfoss_client_secret - never commit to terraform.tfvars."
  type        = string
  sensitive   = true
}

variable "locations" {
  description = "Locations for this environment (see CLAUDE.md Location Configuration). Supply via TF_VAR_locations (JSON list) or the gitignored terraform.tfvars, so gateway ids and home coordinates stay out of source control."
  type = list(object({
    slug             = string
    danfossGatewayId = string
    latitude         = number
    longitude        = number
    # Per-room humidity alert limits keyed by Danfoss room name, overriding the global default.
    humidityThresholds = optional(map(number))
  }))
  sensitive = true
}
