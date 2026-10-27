using System.Text.Json;
using HumidityService.Domain.Models;

namespace HumidityService.Application.Services;

/// <summary>
/// Reads the current outdoor reading from a raw Open-Meteo forecast response, shaped as
/// <c>{"current":{"time":"...","temperature_2m":8.3,"relative_humidity_2m":92}, ...}</c>.
/// </summary>
public static class OpenMeteoResponseParser
{
    /// <summary>
    /// Returns the current temperature and relative humidity, or <see langword="null"/> when the
    /// response has no <c>current</c> object or either value is missing or <c>null</c>.
    /// </summary>
    /// <param name="rawJson">The unmodified raw JSON response body.</param>
    /// <exception cref="JsonException">The body is not valid JSON.</exception>
    public static OutdoorClimateReading? GetCurrentReading(string rawJson)
    {
        using var document = JsonDocument.Parse(rawJson);

        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            !document.RootElement.TryGetProperty("current", out var current) || current.ValueKind != JsonValueKind.Object ||
            !current.TryGetProperty("temperature_2m", out var temperature) || temperature.ValueKind != JsonValueKind.Number ||
            !current.TryGetProperty("relative_humidity_2m", out var humidity) || humidity.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return new OutdoorClimateReading(temperature.GetDouble(), humidity.GetDouble());
    }
}
