using HumidityService.Domain.Models;

namespace HumidityService.Application.Models;

/// <summary>
/// A room's reading evaluated against its effective humidity limit.
/// </summary>
/// <param name="Reading">The room's current temperature and humidity.</param>
/// <param name="ThresholdPercent">The effective limit: the room's override if set, otherwise the global default.</param>
public sealed record RoomHumidityStatus(RoomClimateReading Reading, double ThresholdPercent)
{
    /// <summary>Whether the room's humidity is at or above its limit.</summary>
    public bool IsAboveThreshold => Reading.HumidityPercent >= ThresholdPercent;
}
