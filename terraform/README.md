# Terraform: HumidityService infrastructure

Two layers:

- **`bootstrap/`** - the one shared resource group, the Terraform state storage account/container,
  and the central Key Vault. Applied once per subscription (state stays local by design - see the
  comment in `bootstrap/versions.tf` - it creates the very storage account every other config uses
  as a remote backend, so it can't depend on that backend existing yet).
- **`environments/{development,test,staging,production}/`** - one Function App, Storage account,
  Azure SQL Database, and VNet per environment, all wired to the shared Key Vault from bootstrap.
  Each composes the reusable building blocks in `modules/` (`networking`, `storage`, `sql`,
  `function-app`) via `modules/environment`.

## First-time setup

1. **Bootstrap** (once):
   ```powershell
   cd terraform/bootstrap
   terraform init
   terraform apply
   terraform output          # note tfstate_storage_account_name, shared_resource_group_name,
                              # key_vault_id, key_vault_uri - the next step needs all four
   ```

2. **Per environment** (repeat for development/test/staging/production):
   ```powershell
   cd terraform/environments/development
   terraform init `
     -backend-config="resource_group_name=<shared_resource_group_name>" `
     -backend-config="storage_account_name=<tfstate_storage_account_name>"

   Copy-Item terraform.tfvars.example terraform.tfvars   # then fill in real, non-secret values
   $env:TF_VAR_danfoss_client_id     = "<client id>"
   $env:TF_VAR_danfoss_client_secret = "<client secret>"

   terraform plan
   terraform apply
   ```

   Before the first `apply` for an environment, edit that environment's `main.tf` and replace the
   placeholder `locations` block (`danfossDeviceId = "REPLACE_WITH_REAL_DANFOSS_DEVICE_ID"`) with
   the real location list for that environment.

3. After `apply`, follow `modules/sql/README.md` to grant the Function App's managed identity a
   contained database user (skipped by default - `manage_database_user = false` - since it needs
   network line-of-sight to the VNet-firewalled SQL server that a normal apply doesn't have).

## What's still open

- **CI/CD** (Feature 4) doesn't exist yet, so all of the above is run by hand. Once it does,
  revisit `manage_database_user` and the backend-config secrets/tokens above so the pipeline can
  run `terraform apply` non-interactively.
- The Function App's own deployment (the compiled artifact) isn't part of this Terraform - it
  provisions the empty Function App; Feature 4 deploys code into it.
