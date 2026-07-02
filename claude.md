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
  - `danfossDeviceId` — the indoor device/house identifier used against the Danfoss API.
  - `latitude` / `longitude` — the coordinate pair used for the Open-Meteo outdoor query.
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

### - [ ] Feature 1: Data Ingestion indoor climate (Azure Function & Storage)
* Develop a timer-triggered Azure Function running at the top of every hour.
* Iterate over all configured locations (see Location Configuration); process each location independently so a failure on one does not block the others in the same run.
* Fetch OAuth tokens from `https://api.danfoss.com/oauth2/token`.
* Fetch climate data from `https://api.danfoss.com/ally/devices`, using each location's `danfossDeviceId`.
* Persist raw JSON responses directly to Azure Blob Storage, one blob per location.
* Enforce naming format: `{location}/yyyy/MM/dd/{location}_indoor_yyyy_MM_dd_hour.json`, where `{location}` is the location's `slug`.
* Fallback to local emulator storage (Azurite) as default configuration.

### - [ ] Feature 2: Expand the Azure function with Outdoor Climate (Azure Function & Storage)
* Fetch meteorological data from Open-Meteo api.
* Iterate over all configured locations (see Location Configuration); process each location independently so a failure on one does not block the others in the same run.
* Query specific parameters (`temp_dry,humidity`) filtered by each location's `latitude/longitude`.
* Persist raw JSON responses directly to Azure Blob Storage, one blob per location.
* Enforce naming format: `{location}/yyyy/MM/dd/{location}_outdoor_yyyy_MM_dd_hour.json`, where `{location}` is the same `slug` used in Feature 1, so indoor and outdoor readings correlate.
* Fallback to local emulator storage (Azurite) as default configuration.

### - [ ] Feature 3: Infrastructure as Code (Terraform)
* Author Terraform configurations to support 4 environments (`development`, `test`, `staging`, `production`) and 1 `shared` resource group.
* Provision an environment-specific Azure Function, Blob Storage, Azure SQL Database, and Virtual Network (VNet) for SQL firewalling.
* Automate copying Blob Storage connection strings to Azure Function environment variables.
* Enforce **Managed Identity** for securing Azure SQL Database access.
* Provision a Shared Resource Group containing the Terraform State Blob Storage Container and the central Azure Key Vault.

### - [ ] Feature 4: CI/CD Pipeline (GitHub Actions)
* Configure GitHub Actions workflows for automated continuous integration and delivery.
* Orchestrate Terraform infrastructure deployment steps.
* Execute Entity Framework Core database migrations against target databases.
* Build and deploy the .NET 10 Function app artifact to Azure.

### - [ ] Feature 5: Data Warehouse Design (EF Core)
* Model a Relational Star Schema using Entity Framework Core code-first approach.
* Implement a **Date Dimension** (`DimDate`).
* Implement a **Time Dimension** (`DimTime`).
* Implement a **Location & Room Dimension** (`DimLocationRoom`).
* Implement a **Temperature and Humidity Fact Table** (`FactIndoorClimateReading`).
* Implement a **Temperature and Humidity Fact Table** (`FactOutdoorClimateReading`).

### - [ ] Feature 6: Data Processing & ETL Logic
* Build a core process to parse raw JSON source structures using the Danfoss OpenAPI specification.
* Chain execution within the Azure Function immediately following successful raw data ingestion.
* Support an argument parameter to trigger processing independently without pulling fresh API data.
* Enable batch processing to backfill and catch up on unprocessed blobs from storage, keyed by both location and date so backfill can target a specific location without reprocessing others.
* Guarantee strict **Idempotency** for all data entries written to the Data Warehouse.

### - [ ] Feature 7: Monitoring & Alerting
* Design operational dashboards inside Azure Application Insights.
* Configure metric alerts for failed ingestion cycles.
* Configure metric alerts for ETL processing errors and database sync failures.

### - [ ] Feature 8: Data Quality & Governance
* Enforce request payload validation on incoming API schemas.
* Intercept and log invalid or malformed data records without halting the application pipeline.
* Route corrupt or unparseable source files to a dedicated blob storage container called `dead-letter`.
