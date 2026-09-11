# SQL module: manual database user fallback

`manage_database_user = true` runs `Invoke-Sqlcmd` from the machine applying Terraform to
create the contained database user for the Function App's managed identity. That only works
if the applying machine is inside (or temporarily granted access to) the VNet the SQL server
firewalls to - it will fail from a normal developer laptop or an unmodified GitHub Actions
hosted runner. Revisit once Feature 4 (CI/CD) decides how the pipeline reaches the VNet
(self-hosted runner, `az sql server firewall-rule` scoped to the runner's IP for the duration
of the job, or an Azure DevOps/GitHub-hosted VNet-injected runner).

Until then, leave `manage_database_user = false` and run this once per environment from a
machine with network access (e.g. via `az sql db` or the SQL Server AAD admin's connection),
authenticated as the AAD administrator:

```sql
CREATE USER [<function-app-name>] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [<function-app-name>];
ALTER ROLE db_datawriter ADD MEMBER [<function-app-name>];
```

`<function-app-name>` must match the Function App's name exactly - Azure SQL resolves
`FROM EXTERNAL PROVIDER` users against Entra ID by that display name.
