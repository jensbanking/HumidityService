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
   terraform init -backend-config="storage_account_name=<tfstate_storage_account_name>"

   Copy-Item terraform.tfvars.example terraform.tfvars   # then fill in real, non-secret values
   $env:TF_VAR_danfoss_client_id     = "<client id>"
   $env:TF_VAR_danfoss_client_secret = "<client secret>"

   terraform plan
   terraform apply
   ```

   The backend authenticates to the state blob as your own Azure AD identity (`use_azuread_auth
   = true` in each environment's `versions.tf` - no storage account key ever leaves Azure). This
   needs the `Storage Blob Data Contributor` role on the tfstate storage account itself; grant it
   to your own user (or rely on a broader role like Owner that already includes it) before the
   first `terraform init` for an environment. The same role is required for the CI service
   principal - see "CI/CD authentication" below.

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

### CI/CD authentication (Feature 4 / Feature 7 prerequisite)

GitHub Actions authenticates to Azure via OIDC federated credentials - no client secret is
stored anywhere. One App Registration per environment (not one shared registration), each with a
single federated credential scoped to that environment's GitHub Environment:

| Environment | App registration name | Federated credential name | GitHub Environment |
|---|---|---|---|
| development | `spn-humidity-development` | `github-actions-development` | `development` |
| test        | `spn-humidity-test`        | `github-actions-test`        | `test`        |
| staging     | `spn-humidity-staging`     | `github-actions-staging`     | `staging`     |
| production  | `spn-humidity-production`  | `github-actions-production`  | `production`  |

Naming follows the same `name_prefix` (`humidity`) used everywhere else in this Terraform (see
`bootstrap/variables.tf`), so App Registrations line up visually with the resource groups they
deploy into (`rg-humidity-<env>`).

Setup per environment:
1. Entra ID → App registrations → New registration → name per the table above.
2. App → *Certificates & secrets* → *Federated credentials* → *Add credential* → scenario
   "GitHub Actions deploying Azure resources" → entity type "Environment" → environment name
   matches the GitHub Environment above.
3. Grant the app's service principal `Contributor` on that environment's resource group
   (`rg-humidity-<env>`), plus two roles on the shared Key Vault and its storage account (both in
   `rg-humidity-shared`, from bootstrap):
   - `Key Vault Secrets Officer` on `kv-humidity-shared` - the pipeline doesn't just read secrets,
     it creates/updates them via the `azurerm_key_vault_secret` resources in
     `modules/environment/main.tf` (storage connection string, Danfoss client id/secret, SQL
     connection string), which needs write (`Set`) access - `Key Vault Secrets User` is read-only
     and fails even on `terraform plan`, which has to read the existing secret value to diff
     against, with a 403 `Forbidden` on `Microsoft.KeyVault/vaults/secrets/getSecret/action`.
   - `Storage Blob Data Contributor`, scoped to the tfstate storage account, so `terraform init`'s
     `use_azuread_auth` backend can read/write the state blob - without this the pipeline fails at
     `terraform init` with "Either an Access Key / SAS Token or the Resource Group for the Storage
     Account must be specified - or Azure AD Authentication must be enabled", since the service
     principal otherwise has no permission on `rg-humidity-shared` at all.
4. In GitHub: Settings → Environments → create/confirm `development`/`test`/`staging`/`production`.
   `staging` and `production` require a *Required reviewer* (manual approval before deploy runs).
   Add environment variables `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` (plain
   variables, not secrets - OIDC means there's no password to protect).
5. Workflow jobs need `permissions: id-token: write` and use `azure/login@v2` with those three
   variables.

References:
- https://learn.microsoft.com/en-us/azure/developer/github/connect-from-azure-openid-connect
- https://docs.github.com/en/actions/security-for-github-actions/security-hardening-your-deployments/configuring-openid-connect-in-azure
