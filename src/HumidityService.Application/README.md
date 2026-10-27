# HumidityService.Application

Interfaces, DTOs, CQRS handlers, and validators. Orchestrates ELT flows.

References: `HumidityService.Domain` only.

Contents:
- `Services/ClimateIngestionService` - the hourly ingestion run: per location outdoor, then
  indoor, then the humidity alert, each step isolated; one timestamp for the whole run; fails the
  run if any step failed.
- `Services/OutdoorClimateIngestionService` - one location's outdoor step: fetch, persist the raw
  JSON, read the current values (`OpenMeteoResponseParser`).
- `Services/HumidityAlertService`, `HumidityAlertEmailBuilder` - the humidity alert check and
  email, including the outdoor line (Features 5 and 6).
- `Services/DanfossDeviceListParser` - reads device ids and room readings from the raw Danfoss
  `/ally/devices` response.
- `Services/ClimateBlobPathBuilder` - the blob paths: `yyyy/MM/dd/HH/apiraw_...` and
  `yyyy/MM/dd/HH/{location}/{indoor|outdoor|subdevices}_{location}_...` (`Models/LocationBlobType`).
- `Interfaces/` - the ports Infrastructure implements (`IDanfossAuthClient`,
  `IDanfossClimateApiClient`, `IOpenMeteoApiClient`, `IRawClimateBlobStorage`,
  `IHumidityAlertStateStore`, `IEmailSender`, `ILocationProvider`).

Planned:
- ETL orchestration handlers (Feature 8).
- Payload validation (Feature 11).
