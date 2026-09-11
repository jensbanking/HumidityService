variable "name_prefix" {
  description = "Naming prefix (lowercase, alphanumeric only) used to derive the storage account name."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group the storage account is created in."
  type        = string
}

variable "location" {
  description = "Azure region for the storage account."
  type        = string
}

variable "container_name" {
  description = "Blob container that holds the raw indoor/outdoor climate JSON, matching ClimateStorage__ContainerName."
  type        = string
  default     = "climate-readings"
}

variable "tags" {
  description = "Common tags applied to the storage account."
  type        = map(string)
  default     = {}
}
