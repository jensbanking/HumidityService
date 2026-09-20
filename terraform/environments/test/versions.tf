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
    key            = "test.tfstate"
  }
}

provider "azurerm" {
  features {
    resource_group {
      # This environment is expected to be torn down and rebuilt for testing. Without this,
      # `terraform destroy` refuses to delete the resource group if anything not tracked in
      # state remains in it (e.g. the "Application Insights Smart Detection" action group
      # Azure creates automatically alongside Application Insights) - it would otherwise have
      # to be removed by hand before every destroy. Deliberately left at the default (true,
      # safer) for production - see that environment's versions.tf.
      prevent_deletion_if_contains_resources = false
    }
  }
}
