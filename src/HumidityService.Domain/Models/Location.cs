using System.Text.RegularExpressions;

namespace HumidityService.Domain.Models;

/// <summary>
/// A single statically-configured location (e.g. a house) that indoor and outdoor
/// climate readings are collected for. The same <see cref="Slug"/> is used for both
/// indoor and outdoor blob naming so readings for a location can be correlated.
/// </summary>
/// <param name="Slug">
/// URL/path-safe identifier: lowercase letters a-z, digits and single hyphens between them, e.g. "aarhus-house1".
/// </param>
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
    // The slug is a blob path segment and part of file names, so it is kept to a-z, 0-9 and inner hyphens.
    private static readonly Regex SlugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.CultureInvariant);

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
    /// Checks the location against the configuration rules: a slug in the <see cref="Slug"/> format and a
    /// gateway id are required, latitude and longitude must be valid coordinates, and every humidity limit must
    /// be valid (<see cref="HumidityThreshold.IsValid"/>) and set at most once per room.
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
        else if (!SlugPattern.IsMatch(Slug))
        {
            errors.Add($"'slug' '{Slug}' may only contain lowercase letters a-z, digits and single hyphens between them (e.g. 'aarhus-house1').");
        }

        if (string.IsNullOrWhiteSpace(DanfossGatewayId))
        {
            errors.Add("'danfossGatewayId' is required.");
        }

        if (!double.IsFinite(Latitude) || Latitude is < -90 or > 90)
        {
            errors.Add($"'latitude' {Latitude} must be between -90 and 90.");
        }

        if (!double.IsFinite(Longitude) || Longitude is < -180 or > 180)
        {
            errors.Add($"'longitude' {Longitude} must be between -180 and 180.");
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

    /// <summary>
    /// Checks the rules that span all configured locations: no two locations may share a slug (it names
    /// their blobs) or a Danfoss gateway (it would ingest the same house twice).
    /// </summary>
    /// <param name="locations">Every configured location.</param>
    /// <returns>A description of each broken rule; empty when the locations are valid together.</returns>
    public static IReadOnlyList<string> ValidateAll(IReadOnlyList<Location> locations)
    {
        var errors = new List<string>();

        foreach (var slug in Duplicates(locations.Select(l => l.Slug)))
        {
            errors.Add($"Slug '{slug}' is used by more than one location.");
        }

        foreach (var gatewayId in Duplicates(locations.Select(l => l.DanfossGatewayId)))
        {
            errors.Add($"Danfoss gateway '{gatewayId}' is used by more than one location.");
        }

        return errors;
    }

    private static IEnumerable<string> Duplicates(IEnumerable<string> values) =>
        values.GroupBy(v => v, StringComparer.Ordinal).Where(g => g.Count() > 1).Select(g => g.Key);
}
