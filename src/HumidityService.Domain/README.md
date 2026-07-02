# HumidityService.Domain

Core entities, value objects, and business logic.

Per the Clean Architecture rules in `claude.md`:
- No references to Application, Infrastructure, or FunctionHost.
- No external package dependencies.

Planned contents (see roadmap Features 1, 2, 5):
- `ClimateReading` (indoor/outdoor value objects)
- `Location` value object (`slug`, `danfossDeviceId`, `latitude`, `longitude`)
- Validation logic for incoming readings
