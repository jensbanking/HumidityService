namespace HumidityService.Application.Models;

/// <summary>
/// Environment-wide humidity alert settings.
/// </summary>
/// <param name="DefaultThresholdPercent">
/// Humidity limit (percent relative humidity) for rooms without a per-location override.
/// </param>
/// <param name="Recipients">Email addresses that receive alerts. Alerts are skipped when empty.</param>
public sealed record HumidityAlertSettings(double DefaultThresholdPercent, IReadOnlyList<string> Recipients);
