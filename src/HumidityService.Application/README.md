# HumidityService.Application

Interfaces, DTOs, CQRS handlers, and validators. Orchestrates ELT flows.

References: `HumidityService.Domain` only.

Planned contents:
- `IClimateApiClient` (indoor/outdoor), `IBlobStorageWriter`, `ILocationProvider` interfaces
- DTOs for raw Danfoss / Open-Meteo payloads
- Validators (FluentValidation) for Feature 8 (Data Quality & Governance)
- ETL orchestration handlers (Feature 6)
