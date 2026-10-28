# HumidityService.Domain

Core entities, value objects, and business logic.

Per the Clean Architecture rules in `claude.md`:
- No references to Application, Infrastructure, or FunctionHost.
- No external package dependencies.

Contents:
- `Location` - a statically configured location (`Slug`, `DanfossGatewayId`, `Latitude`,
  `Longitude`, optional per-room `HumidityThresholds`), with its rules: `Validate()` (slug of
  a-z, 0-9 and inner hyphens; gateway id required; latitude -90..90, longitude -180..180; valid
  humidity limits, one per room), `ValidateAll()` (no shared slug or gateway between locations) and
  `GetHumidityThreshold()` (room names match case-insensitively, ignoring surrounding spaces).
- `HumidityThreshold` - the default humidity limit (70 %) and the valid range (above 0, at most 100).
- `RoomClimateReading` - a room's current temperature and humidity.
- `RoomHumidityStatus` - a reading against its limit (`IsAboveThreshold`: at or above it).
- `OutdoorClimateReading` - a location's current outdoor temperature and humidity (Open-Meteo).

Planned:
- Data warehouse entities for the star schema (Feature 7).
