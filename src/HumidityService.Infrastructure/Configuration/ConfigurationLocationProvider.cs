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
    /// <exception cref="InvalidOperationException">
    /// The value is missing or malformed, a location breaks the rules in <see cref="Location.Validate"/>, or the
    /// locations break the rules in <see cref="Location.ValidateAll"/> (e.g. a duplicate slug).
    /// </exception>
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

        for (var i = 0; i < parsed.Count; i++)
        {
            var errors = parsed[i].Validate();
            if (errors.Count > 0)
            {
                var name = string.IsNullOrWhiteSpace(parsed[i].Slug) ? $"#{i + 1}" : $"'{parsed[i].Slug}'";
                throw new InvalidOperationException($"Configured location {name} is invalid: {string.Join(" ", errors)}");
            }
        }

        var setErrors = Location.ValidateAll(parsed);
        if (setErrors.Count > 0)
        {
            throw new InvalidOperationException($"Configured locations are invalid: {string.Join(" ", setErrors)}");
        }

        _locations = parsed;
    }

    /// <inheritdoc />
    public IReadOnlyList<Location> GetLocations() => _locations;
}
