variable "name_prefix" {
  description = "Naming prefix used to derive the Communication Services resource names."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group the Communication Services resources are created in."
  type        = string
}

variable "data_location" {
  description = "Geography where Communication Services stores data at rest, e.g. \"Europe\"."
  type        = string
  default     = "Europe"
}

variable "tags" {
  description = "Common tags applied to Communication Services resources."
  type        = map(string)
  default     = {}
}
