terraform {
  required_version = ">= 1.7.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }

  # Intentionally local state: this module creates the storage account that
  # every other Terraform config (per-environment) will use as its remote
  # backend, so it cannot depend on that backend existing yet.
}

provider "azurerm" {
  features {}
}
