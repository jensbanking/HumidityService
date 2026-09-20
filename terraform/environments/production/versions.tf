terraform {
  required_version = ">= 1.7.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }

  # Partial config: run
  #   terraform init -backend-config="storage_account_name=<tfstate_storage_account_name from bootstrap output>" -backend-config="resource_group_name=<shared_resource_group_name>"
  # so the storage account's globally-unique generated name never has to be hardcoded here.
  backend "azurerm" {
    container_name = "tfstate"
    key            = "production.tfstate"
  }
}

provider "azurerm" {
  # Deliberately left at the provider default (features.resource_group.prevent_deletion_if_contains_resources
  # = true): unlike development/test/staging, this resource group should never be silently
  # wiped out by a `terraform destroy` just because it contains something Terraform doesn't
  # track (e.g. Azure's auto-created "Application Insights Smart Detection" action group).
  features {}
}
