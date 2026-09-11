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
