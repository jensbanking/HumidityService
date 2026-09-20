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
    key              = "production.tfstate"
    use_azuread_auth = true
  }
}

provider "azurerm" {
  # Deliberately left at the provider default (features.resource_group.prevent_deletion_if_contains_resources
  # = true): unlike development/test/staging, this resource group should never be silently
  # wiped out by a `terraform destroy` just because it contains something Terraform doesn't
  # track (e.g. Azure's auto-created "Application Insights Smart Detection" action group).
  features {}
}
