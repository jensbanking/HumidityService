namespace HumidityService.Domain.Models;

/// <summary>
/// A single statically-configured location (e.g. a house) that indoor and outdoor
/// climate readings are collected for. The same <see cref="Slug"/> is used for both
/// indoor and outdoor blob naming so readings for a location can be correlated.
/// </summary>
/// <param name="Slug">URL/path-safe identifier (lowercase, no spaces), e.g. "aarhus-house1".</param>
/// <param name="DanfossGatewayId">Device id of the location's Danfoss Ally gateway, as listed in the <c>/ally/devices</c> response.</param>
/// <param name="Latitude">Latitude used for the Open-Meteo outdoor query.</param>
/// <param name="Longitude">Longitude used for the Open-Meteo outdoor query.</param>
/// <param name="HumidityThresholds">
/// Optional per-room humidity alert limits (percent relative humidity) keyed by room name,
/// overriding the global default. <c>null</c> when the location has no overrides.
/// </param>
public sealed record Location(
    string Slug,
    string DanfossGatewayId,
    double Latitude,
    double Longitude,
    IReadOnlyDictionary<string, double>? HumidityThresholds = null)
{
    /// <summary>
    /// Returns the humidity limit for a room: its entry in <see cref="HumidityThresholds"/> if there
    /// is one, otherwise <paramref name="defaultThreshold"/>. Room names match case-insensitively and
    /// ignoring surrounding spaces, since names set in the Danfoss app can carry stray spaces.
    /// </summary>
    /// <param name="roomName">The room's name in the Danfoss app.</param>
    /// <param name="defaultThreshold">The limit for rooms without an override.</param>
    public double GetHumidityThreshold(string roomName, double defaultThreshold)
    {
        foreach (var (name, limit) in HumidityThresholds ?? new Dictionary<string, double>())
        {
            if (IsSameRoom(name, roomName))
            {
                return limit;
            }
        }

        return defaultThreshold;
    }

    /// <summary>Whether two room names refer to the same room (case-insensitive, ignoring surrounding spaces).</summary>
    /// <param name="first">A room name.</param>
    /// <param name="second">Another room name.</param>
    public static bool IsSameRoom(string first, string second) =>
        string.Equals(first.Trim(), second.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Checks the location against the configuration rules: a slug and gateway id are required, and
    /// every humidity limit must be valid (<see cref="HumidityThreshold.IsValid"/>) and set at most once per room.
    /// </summary>
    /// <returns>A description of each broken rule; empty when the location is valid.</returns>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        // Configuration binding can leave required strings null despite the non-nullable types.
        if (string.IsNullOrWhiteSpace(Slug))
        {
            errors.Add("'slug' is required.");
        }

        if (string.IsNullOrWhiteSpace(DanfossGatewayId))
        {
            errors.Add("'danfossGatewayId' is required.");
        }

        var roomNames = new List<string>();
        foreach (var (roomName, limit) in HumidityThresholds ?? new Dictionary<string, double>())
        {
            if (!HumidityThreshold.IsValid(limit))
            {
                errors.Add($"Humidity limit {limit} for room '{roomName}' must be greater than 0 and at most 100.");
            }

            if (roomNames.Any(seen => IsSameRoom(seen, roomName)))
            {
                errors.Add($"Room '{roomName.Trim()}' has more than one humidity limit.");
            }

            roomNames.Add(roomName);
        }

        return errors;
    }
}
