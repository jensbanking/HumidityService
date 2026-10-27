# Project Roadmap: HumidityService

# Global Engineering Standards

## Development Environment & OS
- OS: Windows 11 / Windows 10.
- Shell: PowerShell (v7 or Windows PowerShell).
- Local Storage Emulator: Azurite running locally (`UseDevelopmentStorage=true`).
- Terminal Commands: Always use Windows/PowerShell compatible commands (e.g., use `\` for paths or ensure cross-platform dotnet tools).

## Architecture

- Follow Clean Architecture strictly.
- Dependencies may only point inward.
- Domain must not reference Application, Infrastructure, or FunctionHost.

## Coding Standards

- Nullable enabled.
- Implicit usings enabled.
- File-scoped namespaces.
- XML documentation on public APIs.
- Async/await everywhere appropriate.

## Dependency Injection

- Use constructor injection only.
- No service locator pattern.

## Error Handling

- Never swallow exceptions.
- Log all failures with context.

## Testing

- xUnit
- NSubstitute
- High coverage for Application layer

## Security

- No secrets in source control.
- Managed Identity preferred.
- Key Vault for secrets.

## Location Configuration

- A single deployed environment supports **multiple locations** (e.g., multiple houses/devices), configured statically per environment — no dynamic discovery from the Danfoss API and no runtime location resolution.
- Each location is defined as a static config entry containing:
  - `slug` — a URL/path-safe identifier (lowercase, no spaces, e.g. `aarhus-house1`), used identically for both indoor and outdoor naming so readings can be correlated.
  - `danfossGatewayId` — the Danfoss Ally gateway ID (from the `/ally/devices` response) identifying the location's house.
  - `latitude` / `longitude` — the coordinate pair used for the Open-Meteo outdoor query.
  - `humidityThresholds` (optional) — per-room humidity alert limits keyed by room name, overriding the global default (see Feature 5).
- The full set of locations is stored as a list (e.g., a JSON app setting, config file, or small config table) rather than as individual scalar environment variables, since cardinality is >1 per environment.
- Indoor and outdoor ingestion (Features 1 & 2) iterate over this list on every run, producing one blob per location per run for each of indoor/outdoor.
- Each location's fetch/write is isolated from the others — a failure processing one location must not prevent the others from being fetched and persisted in the same run.

## Architecture & Code Quality Requirements

### Clean Architecture Foundation
* Implement **Domain** layer containing core entities, value objects, and business logic.
* Implement **Application** layer containing interfaces, DTOs, CQRS handlers, and validators.
* Implement **Infrastructure** layer handling database, API clients, storage, and external services.
* Implement **Function Host** layer containing Azure Function triggers and Dependency Injection setup.

### Testing & Quality Assurance
* Achieve at least 80% test coverage across Application and Infrastructure layers.
* Use **NSubstitute** for isolating dependencies and mocking external interfaces.
* Enforce extensive XML documentation comments on all public classes, methods, and interfaces.

### Observability & Security
* Configure **OpenTelemetry** logging, metrics, and tracing.
* Export data to **Azure Application Insights** for deep analysis.
* Utilize **Azure Key Vault** for all application secrets.
* Strictly enforce zero secrets in the source code repositories.

### Resilience & Retry Policies
* Implement Polly retry policies for all **OAuth requests**.
* Implement Polly retry policies for all **REST API requests**.
* Implement Polly retry policies for all **Azure Blob Storage uploads**.
* Implement Polly retry policies for all **Azure SQL Database calls**.

---

## Technical Features Blueprint

### - [x] Feature 1: Data Ingestion indoor climate (Azure Function & Storage)
* Develop a timer-triggered Azure Function running at the top of every hour.
* Iterate over all configured locations (see Location Configuration); process each location independently so a failure on one does not block the others in the same run.
* Fetch OAuth tokens from `https://api.danfoss.com/oauth2/token`.
* Fetch climate data from `https://api.danfoss.com/ally/devices`, using each location's `danfossGatewayId`.
* Persist raw JSON responses directly to Azure Blob Storage, one blob per location.
* Enforce naming format: `{location}/yyyy/MM/dd/{location}_indoor_yyyy_MM_dd_hour.json`, where `{location}` is the location's `slug`.
* Fallback to local emulator storage (Azurite) as default configuration.

### - [x] Feature 2: Expand the Azure function with Outdoor Climate (Azure Function & Storage)
* Fetch meteorological data from Open-Meteo api.
* Iterate over all configured locations (see Location Configuration); process each location independently so a failure on one does not block the others in the same run.
* Query specific parameters (`temperature_2m,relative_humidity_2m`) filtered by each location's `latitude/longitude`.
* Persist raw JSON responses directly to Azure Blob Storage, one blob per location.
* Enforce naming format: `{location}/yyyy/MM/dd/{location}_outdoor_yyyy_MM_dd_hour.json`, where `{location}` is the same `slug` used in Feature 1, so indoor and outdoor readings correlate.
* Fallback to local emulator storage (Azurite) as default configuration.

### - [x] Feature 3: Infrastructure as Code (Terraform)
* Author Terraform configurations to support 4 environments (`development`, `test`, `staging`, `production`) and 1 `shared` resource group.
* Provision an environment-specific Azure Function, Blob Storage, Azure SQL Database, and Virtual Network (VNet) for SQL firewalling.
* Automate wiring Blob Storage into the Azure Function's environment variables: the blob service endpoint (`ClimateStorage__ServiceUri`), reached with the Function App's Managed Identity - no storage connection string or account key in app settings or Key Vault.
* Enforce **Managed Identity** for securing Azure SQL Database access.
* Provision a Shared Resource Group containing the Terraform State Blob Storage Container and the central Azure Key Vault.

### - [x] Feature 4: CI/CD Pipeline (GitHub Actions)
* Configure GitHub Actions workflows for automated continuous integration and delivery.
* Orchestrate Terraform infrastructure deployment steps.
* Build and deploy the .NET 10 Function app artifact to Azure.
* Database migrations are deliberately out of scope here — see Feature 8, which depends on the EF Core schema (Feature 6) existing first.
* Covers the `development` environment only (`deploy-development.yml`); `test`, `staging` and `production` are deferred to Feature 11.

### - [x] Feature 5: Humidity Alert Email Notifications (Azure Communication Services)
* Right after each location's Danfoss Ally response is persisted (hourly), check the humidity of every room with a humidity sensor (`humidity_value`, scaled by 10 like `temp_current`).
* A room alerts when its humidity is `>=` its limit: a global default (`70` %), or an optional per-room override (`humidityThresholds` in the location config, keyed case-insensitively by Danfoss room name).
* Send at most one email per location per day (`Europe/Copenhagen`) via **Azure Communication Services (ACS) Email** to a configurable recipient list; persist the last-sent date so the limit survives restarts.
* Subject `Fugtigheds alarm fra {location}`; body starts `Fugtighedsmålinger for {location} {Danish date and time}`, then a table (`Rum`, `Temp`, `Fugt %`) of all rooms with a humidity sensor, rooms over their limit in **bold red**.
* An alert failure is logged and never affects persisted data or the other locations.
* Managed Identity to ACS, Polly retry on send; Terraform provisions ACS, its email domain and the `HumidityAlert__*` / `CommunicationServices__Endpoint` app settings.

### - [ ] Feature 6: Data Warehouse Design (EF Core)
* Model a Relational Star Schema using Entity Framework Core code-first approach.
* Implement a **Date Dimension** (`DimDate`).
* Implement a **Time Dimension** (`DimTime`).
* Implement a **Location & Room Dimension** (`DimLocationRoom`).
* Implement a **Temperature and Humidity Fact Table** (`FactIndoorClimateReading`).
* Implement a **Temperature and Humidity Fact Table** (`FactOutdoorClimateReading`).

### - [ ] Feature 7: Data Processing & ETL Logic
* Build a core process to parse raw JSON source structures using the Danfoss OpenAPI specification.
* Chain execution within the Azure Function immediately following successful raw data ingestion.
* Support an argument parameter to trigger processing independently without pulling fresh API data.
* Enable batch processing to backfill and catch up on unprocessed blobs from storage, keyed by both location and date so backfill can target a specific location without reprocessing others.
* Guarantee strict **Idempotency** for all data entries written to the Data Warehouse.
* **Open decision (revisit when building this feature):** "chain immediately following ingestion" above would hit Azure SQL every hour, which works against the serverless database's auto-pause (Feature 3 sets `auto_pause_delay_in_minutes = 60`, Azure's minimum). Consider decoupling the DWH sync onto its own, less frequent schedule (e.g. every few hours or once daily) instead of running it right after every hourly ingestion. Decide the actual cadence here, not earlier - it has no impact on the Feature 3 infrastructure.

### - [ ] Feature 8: CI/CD Database Migration Pipeline
* Extend the Feature 4 GitHub Actions pipeline to execute Entity Framework Core database migrations against target databases, once Feature 6 (Data Warehouse schema) and Feature 7 (ETL logic) exist.
* Resolve the `manage_database_user = false` gap in the Terraform SQL module (see `terraform/README.md`, "What's still open") so the Function App's managed identity can authenticate to Azure SQL non-interactively.
* Decide and implement migration cadence/safety — e.g. run only on schema-changing deploys. Applies the same manual-approval gate as Feature 11 for `staging`/`production`.
* Resolves the open decision under Feature 7 about DWH sync cadence vs. serverless SQL auto-pause: migrations/sync run on this pipeline's own schedule, decoupled from the hourly ingestion trigger.

### - [ ] Feature 9: Monitoring & Alerting
* Design operational dashboards inside Azure Application Insights.
* Configure metric alerts for failed ingestion cycles.
* Configure metric alerts for ETL processing errors and database sync failures.

### - [ ] Feature 10: Data Quality & Governance
* Enforce request payload validation on incoming API schemas.
* Intercept and log invalid or malformed data records without halting the application pipeline.
* Route corrupt or unparseable source files to a dedicated blob storage container called `dead-letter`.

### - [ ] Feature 11: CI/CD for Remaining Environments (GitHub Actions)
* Extend the Feature 4 pipeline (today `development` only) to the `test`, `staging` and `production` environments.
* Refactor `deploy-development.yml` into a reusable workflow called once per environment, rather than duplicating it four times.
* `development`/`test` deploy automatically; `staging`/`production` require manual approval (e.g. a GitHub Environments protection rule) before the deployment job runs.
* Provision the per-environment GitHub Environments, secrets and Azure federated credentials/role assignments each deployment needs.
