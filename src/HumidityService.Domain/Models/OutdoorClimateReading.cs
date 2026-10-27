namespace HumidityService.Domain.Models;

/// <summary>
/// The current outdoor temperature and humidity at a location, as reported by Open-Meteo.
/// </summary>
/// <param name="TemperatureCelsius">Air temperature 2 m above ground, in degrees Celsius.</param>
/// <param name="HumidityPercent">Relative humidity 2 m above ground, in percent.</param>
public sealed record OutdoorClimateReading(double TemperatureCelsius, double HumidityPercent);
