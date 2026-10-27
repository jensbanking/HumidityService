# HumidityService.Infrastructure

Database, API clients, storage, and external services.

References: `HumidityService.Application` (and `Domain` transitively).

Contents:
- `ExternalApis/` - `DanfossAuthClient`, `DanfossClimateApiClient`, `OpenMeteoApiClient`
  (typed `HttpClient`s with a Polly retry policy on transient HTTP errors).
- `Storage/` - `BlobRawClimateStorage` (raw JSON readings), `BlobHumidityAlertStateStore`
  (last alert date per location) and `BlobServiceClientFactory` (Managed Identity via
  `ClimateStorage:ServiceUri`, or a connection string - Azurite by default).
- `Email/AcsEmailSender` - Azure Communication Services Email with Managed Identity.
- `Resilience/AzureRetryPipeline` - the Polly pipeline for Azure SDK calls; the SDK clients' own
  retries are turned off so it's the only retry layer.
- `Configuration/` - options and parsing/validation of the `Locations` and `HumidityAlert`
  app settings.
- `DependencyInjection/` - `AddHumidityInfrastructure`.

Planned:
- `HumidityDwhDbContext` (EF Core, star schema - Feature 7) with Managed Identity SQL access.
- Dead-letter routing for unparseable source files (Feature 11).
- OpenTelemetry configuration (a project-wide requirement in `claude.md`, not yet wired up).
