# HumidityService.Application

Interfaces, DTOs, CQRS handlers, and validators. Orchestrates ELT flows.

References: `HumidityService.Domain` only.

Contents:
- `Services/IndoorClimateIngestionService`, `OutdoorClimateIngestionService` - the hourly
  ingestion runs: fetch per location, persist the raw JSON, fail the run if any location failed.
- `Services/HumidityAlertService`, `HumidityAlertEmailBuilder` - the humidity alert check and
  email (Feature 5).
- `Services/DanfossDeviceListParser` - reads device ids and room readings from the raw Danfoss
  `/ally/devices` response.
- `Services/ClimateBlobPathBuilder` - the blob paths: `yyyy/MM/dd/HH/apiraw_...` and
  `yyyy/MM/dd/HH/{location}/{indoor|outdoor|subdevices}_{location}_...` (`Models/LocationBlobType`).
- `Interfaces/` - the ports Infrastructure implements (`IDanfossAuthClient`,
  `IDanfossClimateApiClient`, `IOpenMeteoApiClient`, `IRawClimateBlobStorage`,
  `IHumidityAlertStateStore`, `IEmailSender`, `ILocationProvider`).

Planned:
- ETL orchestration handlers (Feature 7).
- Payload validation (Feature 10).
