# HumidityService.Domain

Core entities, value objects, and business logic.

Per the Clean Architecture rules in `claude.md`:
- No references to Application, Infrastructure, or FunctionHost.
- No external package dependencies.

Contents:
- `Location` - a statically configured location (`Slug`, `DanfossGatewayId`, `Latitude`,
  `Longitude`, optional per-room `HumidityThresholds`).
- `RoomClimateReading` - a room's current temperature and humidity.
- `ClimateReadingType` - indoor (Danfoss) or outdoor (Open-Meteo).

Planned:
- Data warehouse entities for the star schema (Feature 6).
