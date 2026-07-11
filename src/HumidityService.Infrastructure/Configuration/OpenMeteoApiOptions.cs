namespace HumidityService.Infrastructure.Configuration;

/// <summary>
/// Binds the "OpenMeteoApi" configuration section. Open-Meteo's forecast endpoint requires
/// no API key, so only the base URL is configurable.
/// </summary>
public sealed class OpenMeteoApiOptions
{
    /// <summary>The configuration section name this options type binds to.</summary>
    public const string SectionName = "OpenMeteoApi";

    /// <summary>Base URL of the Open-Meteo API, e.g. "https://api.open-meteo.com".</summary>
    public string BaseUrl { get; init; } = string.Empty;
}
