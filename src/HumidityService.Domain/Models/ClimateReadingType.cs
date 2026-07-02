namespace HumidityService.Domain.Models;

/// <summary>
/// Distinguishes indoor (Danfoss) from outdoor (Open-Meteo) climate readings,
/// used to select the correct blob naming segment for a location.
/// </summary>
public enum ClimateReadingType
{
    /// <summary>Reading captured from the indoor Danfoss device for a location.</summary>
    Indoor,

    /// <summary>Reading captured from the outdoor Open-Meteo source for a location.</summary>
    Outdoor,
}
