# HumidityService.Infrastructure

Database, API clients, storage, and external services.

References: `HumidityService.Application` (and `Domain` transitively).

Planned contents:
- `DanfossApiClient`, `OpenMeteoApiClient` (Polly retry policies applied here)
- `BlobStorageWriter` (raw JSON persistence, dead-letter routing)
- `HumidityDwhDbContext` (EF Core, Star Schema — Feature 5)
- Managed Identity-based SQL and Key Vault access
- OpenTelemetry configuration
