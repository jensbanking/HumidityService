variable "name_prefix" {
  description = "Naming prefix for networking resources, e.g. \"humidity-development\"."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group the VNet is created in."
  type        = string
}

variable "location" {
  description = "Azure region for the VNet."
  type        = string
}

variable "address_space" {
  description = "CIDR address space for the VNet, e.g. \"10.10.0.0/16\". Kept distinct per environment so the VNets never overlap if peered later."
  type        = list(string)
}

variable "function_subnet_prefix" {
  description = "CIDR prefix for the subnet the Function App uses for regional VNet Integration."
  type        = string
}

variable "tags" {
  description = "Common tags applied to all networking resources."
  type        = map(string)
  default     = {}
}
