using HumidityService.Domain.Models;

namespace HumidityService.Application.Interfaces;

/// <summary>
/// Checks a location's room humidity against its alert thresholds and sends an email alert
/// when a room is at or above its limit (at most one email per location per day).
/// </summary>
public interface IHumidityAlertService
{
    /// <summary>
    /// Evaluates the rooms in a freshly persisted Danfoss device list for one location and sends
    /// an alert email if needed.
    /// </summary>
    /// <param name="location">The location the device list belongs to.</param>
    /// <param name="rawDevicesJson">The raw Danfoss Ally <c>/ally/devices</c> response.</param>
    /// <param name="outdoorReading">
    /// The location's outdoor reading for the same hour, shown in the email; <see langword="null"/> when it
    /// is unavailable, which never stops the alert.
    /// </param>
    /// <param name="cancellationToken">Token used to cancel the check.</param>
    Task CheckAndNotifyAsync(Location location, string rawDevicesJson, OutdoorClimateReading? outdoorReading, CancellationToken cancellationToken);
}
