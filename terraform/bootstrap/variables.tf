variable "location" {
  description = "Azure region for the shared bootstrap resources."
  type        = string
  default     = "westeurope"
}

variable "name_prefix" {
  description = "Naming prefix for the shared bootstrap resources."
  type        = string
  default     = "humidity"
}

variable "tags" {
  description = "Common tags applied to all shared bootstrap resources."
  type        = map(string)
  default = {
    project     = "HumidityService"
    environment = "shared"
    managed_by  = "terraform"
  }
}
