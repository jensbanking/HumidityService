namespace HumidityService.Domain.Models;

/// <summary>
/// The current temperature and humidity of a single room, as reported by a Danfoss Ally
/// device that has a humidity sensor.
/// </summary>
/// <param name="RoomName">The room name, i.e. the Danfoss device name.</param>
/// <param name="TemperatureCelsius">Current temperature in degrees Celsius.</param>
/// <param name="HumidityPercent">Current relative humidity in percent.</param>
public sealed record RoomClimateReading(string RoomName, double TemperatureCelsius, double HumidityPercent);
