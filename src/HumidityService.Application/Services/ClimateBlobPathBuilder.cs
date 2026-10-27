using System.Globalization;
using HumidityService.Application.Models;

namespace HumidityService.Application.Services;

/// <summary>
/// Builds the blob paths for an hour's data, grouped by UTC date and hour:
/// <list type="bullet">
/// <item><c>yyyy/MM/dd/HH/apiraw_yyyy_MM_dd_HH.json</c> - the raw, account-wide Danfoss device list.</item>
/// <item><c>yyyy/MM/dd/HH/{location}/{indoor|outdoor|subdevices}_{location}_yyyy_MM_dd_HH.json</c> - per location.</item>
/// </list>
/// </summary>
public static class ClimateBlobPathBuilder
{
    /// <summary>Builds the path of the raw, account-wide Danfoss device list for an hour.</summary>
    /// <param name="timestampUtc">When the data was captured; converted to UTC for the path.</param>
    public static string BuildApiRaw(DateTimeOffset timestampUtc)
    {
        var utc = timestampUtc.ToUniversalTime();

        return $"{HourPath(utc)}/apiraw_{FileTimestamp(utc)}.json";
    }

    /// <summary>Builds the path of a location's blob for an hour.</summary>
    /// <param name="locationSlug">The location's URL/path-safe identifier.</param>
    /// <param name="blobType">Which of the location's blobs.</param>
    /// <param name="timestampUtc">When the data was captured; converted to UTC for the path.</param>
    /// <exception cref="ArgumentException"><paramref name="locationSlug"/> is null, empty or whitespace.</exception>
    public static string Build(string locationSlug, LocationBlobType blobType, DateTimeOffset timestampUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(locationSlug);

        var typeSegment = blobType switch
        {
            LocationBlobType.Indoor => "indoor",
            LocationBlobType.Outdoor => "outdoor",
            LocationBlobType.SubDevices => "subdevices",
            _ => throw new ArgumentOutOfRangeException(nameof(blobType), blobType, "Unsupported location blob type."),
        };
        var utc = timestampUtc.ToUniversalTime();

        return $"{HourPath(utc)}/{locationSlug}/{typeSegment}_{locationSlug}_{FileTimestamp(utc)}.json";
    }

    private static string HourPath(DateTimeOffset utc) => utc.ToString("yyyy/MM/dd/HH", CultureInfo.InvariantCulture);

    private static string FileTimestamp(DateTimeOffset utc) => utc.ToString("yyyy_MM_dd_HH", CultureInfo.InvariantCulture);
}
