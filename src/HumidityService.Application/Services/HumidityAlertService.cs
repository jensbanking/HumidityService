using HumidityService.Application.Interfaces;
using HumidityService.Application.Models;
using HumidityService.Domain.Models;
using Microsoft.Extensions.Logging;

namespace HumidityService.Application.Services;

/// <summary>
/// Default <see cref="IHumidityAlertService"/> implementation. Compares every room with a humidity
/// sensor against its effective limit and, when at least one room is at or above it, sends one
/// email per location per calendar day (Europe/Copenhagen).
/// </summary>
public sealed class HumidityAlertService : IHumidityAlertService
{
    private static readonly TimeZoneInfo AlertTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

    private readonly HumidityAlertSettings _settings;
    private readonly IHumidityAlertStateStore _stateStore;
    private readonly IEmailSender _emailSender;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<HumidityAlertService> _logger;

    /// <summary>Initializes a new instance of <see cref="HumidityAlertService"/>.</summary>
    public HumidityAlertService(
        HumidityAlertSettings settings,
        IHumidityAlertStateStore stateStore,
        IEmailSender emailSender,
        TimeProvider timeProvider,
        ILogger<HumidityAlertService> logger)
    {
        _settings = settings;
        _stateStore = stateStore;
        _emailSender = emailSender;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task CheckAndNotifyAsync(Location location, string rawDevicesJson, OutdoorClimateReading? outdoorReading, CancellationToken cancellationToken)
    {
        var readings = DanfossDeviceListParser.GetRoomReadings(rawDevicesJson);
        if (readings.Count == 0)
        {
            _logger.LogInformation("No rooms with a humidity sensor found for location {LocationSlug}; skipping humidity alert check.", location.Slug);
            return;
        }

        WarnAboutUnmatchedOverrides(location, readings);
        var rooms = readings
            .OrderBy(r => r.RoomName, StringComparer.OrdinalIgnoreCase)
            .Select(r => new RoomHumidityStatus(r, location.GetHumidityThreshold(r.RoomName, _settings.DefaultThresholdPercent)))
            .ToList();

        var roomsAbove = rooms.Where(r => r.IsAboveThreshold).Select(r => r.Reading.RoomName).ToList();
        if (roomsAbove.Count == 0)
        {
            return;
        }

        if (_settings.Recipients.Count == 0)
        {
            _logger.LogWarning(
                "Rooms {RoomNames} in location {LocationSlug} are at or above their humidity limit, but no alert recipients are configured.",
                roomsAbove,
                location.Slug);
            return;
        }

        var nowLocal = TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), AlertTimeZone).DateTime;
        var today = DateOnly.FromDateTime(nowLocal);
        if (await _stateStore.GetLastSentDateAsync(location.Slug, cancellationToken) == today)
        {
            _logger.LogInformation(
                "Rooms {RoomNames} in location {LocationSlug} are at or above their humidity limit; today's alert has already been sent.",
                roomsAbove,
                location.Slug);
            return;
        }

        await _emailSender.SendAsync(HumidityAlertEmailBuilder.Build(location.Slug, nowLocal, outdoorReading, rooms, _settings.Recipients), cancellationToken);
        await _stateStore.SetLastSentDateAsync(location.Slug, today, cancellationToken);

        _logger.LogInformation(
            "Humidity alert sent for location {LocationSlug}; rooms at or above their limit: {RoomNames}.",
            location.Slug,
            roomsAbove);
    }

    private void WarnAboutUnmatchedOverrides(Location location, IReadOnlyList<RoomClimateReading> readings)
    {
        foreach (var roomName in location.HumidityThresholds?.Keys ?? [])
        {
            if (!readings.Any(r => Location.IsSameRoom(r.RoomName, roomName)))
            {
                _logger.LogWarning(
                    "Humidity limit override for room {RoomName} in location {LocationSlug} matches no room with a humidity sensor; was the room renamed?",
                    roomName.Trim(),
                    location.Slug);
            }
        }
    }
}
