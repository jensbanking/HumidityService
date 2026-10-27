using System.Globalization;
using System.Text.Json;
using HumidityService.Application.Models;
using HumidityService.Domain.Models;
using Microsoft.Extensions.Configuration;

namespace HumidityService.Infrastructure.Configuration;

/// <summary>
/// Builds <see cref="HumidityAlertSettings"/> from the "HumidityAlert" app settings.
/// <c>HumidityAlert__Recipients</c> holds a JSON array of email addresses (app settings are flat
/// key/value pairs, so the list is encoded as a string, like "Locations").
/// </summary>
public static class ConfigurationHumidityAlertSettings
{
    /// <summary>The configuration section name the settings are read from.</summary>
    public const string SectionName = "HumidityAlert";

    /// <summary>Parses and validates the humidity alert settings.</summary>
    /// <param name="configuration">The application configuration.</param>
    /// <exception cref="InvalidOperationException">A configured value is malformed or out of range.</exception>
    public static HumidityAlertSettings Create(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);

        var threshold = HumidityThreshold.Default;
        var rawThreshold = section["DefaultThresholdPercent"];
        if (!string.IsNullOrWhiteSpace(rawThreshold) &&
            (!double.TryParse(rawThreshold, NumberStyles.Float, CultureInfo.InvariantCulture, out threshold) || !HumidityThreshold.IsValid(threshold)))
        {
            throw new InvalidOperationException($"Configuration value '{SectionName}:DefaultThresholdPercent' must be a number greater than 0 and at most 100, but was '{rawThreshold}'.");
        }

        var recipients = new List<string>();
        var rawRecipients = section["Recipients"];
        if (!string.IsNullOrWhiteSpace(rawRecipients))
        {
            try
            {
                recipients = JsonSerializer.Deserialize<List<string>>(rawRecipients) ?? [];
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Configuration value '{SectionName}:Recipients' must be a JSON array of email addresses.", ex);
            }

            if (recipients.Any(string.IsNullOrWhiteSpace))
            {
                throw new InvalidOperationException($"Configuration value '{SectionName}:Recipients' contains an empty email address.");
            }
        }

        return new HumidityAlertSettings(threshold, recipients);
    }
}
