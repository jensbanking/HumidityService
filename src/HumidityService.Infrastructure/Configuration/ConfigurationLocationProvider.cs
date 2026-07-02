using System.Text.Json;
using HumidityService.Application.Interfaces;
using HumidityService.Domain.Models;
using Microsoft.Extensions.Configuration;

namespace HumidityService.Infrastructure.Configuration;

/// <summary>
/// Resolves the statically-configured locations from the "Locations" app setting, which
/// holds a JSON array (app settings are flat key/value pairs, so the list is encoded as a string).
/// </summary>
public sealed class ConfigurationLocationProvider : ILocationProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IReadOnlyList<Location> _locations;

    /// <summary>Parses and validates the "Locations" configuration value.</summary>
    /// <exception cref="InvalidOperationException">The value is missing, malformed, or contains an incomplete location.</exception>
    public ConfigurationLocationProvider(IConfiguration configuration)
    {
        var rawValue = configuration["Locations"];
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            throw new InvalidOperationException("Configuration value 'Locations' is missing or empty. At least one location must be configured.");
        }

        List<Location>? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<List<Location>>(rawValue, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Configuration value 'Locations' is not valid JSON.", ex);
        }

        if (parsed is null || parsed.Count == 0)
        {
            throw new InvalidOperationException("Configuration value 'Locations' did not contain any locations.");
        }

        foreach (var location in parsed)
        {
            if (string.IsNullOrWhiteSpace(location.Slug))
            {
                throw new InvalidOperationException("A configured location is missing a required 'slug' value.");
            }
        }

        _locations = parsed;
    }

    /// <inheritdoc />
    public IReadOnlyList<Location> GetLocations() => _locations;
}
