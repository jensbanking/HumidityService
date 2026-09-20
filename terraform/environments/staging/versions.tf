terraform {
  required_version = ">= 1.7.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }

  # Partial config: run
  #   terraform init -backend-config="storage_account_name=<tfstate_storage_account_name from bootstrap output>"
  # so the storage account's globally-unique generated name never has to be hardcoded here.
  # use_azuread_auth: authenticates to the state blob with the caller's own Azure AD identity
  # (interactively, or the CI service principal's OIDC token) instead of fetching/using a
  # storage account access key - no key-listing permission on the shared state storage account
  # needed, only "Storage Blob Data Contributor" scoped to it. See terraform/README.md.
  backend "azurerm" {
    container_name   = "tfstate"
    key              = "staging.tfstate"
    use_azuread_auth = true
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
