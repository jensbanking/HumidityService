using System.Globalization;
using HumidityService.Domain.Models;

namespace HumidityService.Application.Services;

/// <summary>
/// Builds the standard blob path for a raw climate reading:
/// <c>{location}/yyyy/MM/dd/{location}_{indoor|outdoor}_yyyy_MM_dd_HH.json</c> for a location-scoped
/// reading, or <c>{indoor|outdoor}/yyyy/MM/dd/{indoor|outdoor}_yyyy_MM_dd_HH.json</c> for an
/// account-wide reading that is not tied to a single location.
/// </summary>
public static class ClimateBlobPathBuilder
{
    /// <summary>Builds the blob name (including virtual directory segments) for a single reading.</summary>
    /// <param name="locationSlug">The location's URL/path-safe identifier, or <c>null</c> for an account-wide reading.</param>
    /// <param name="readingType">Whether the reading is indoor or outdoor.</param>
    /// <param name="timestampUtc">The UTC timestamp the reading was captured at.</param>
    public static string Build(string? locationSlug, ClimateReadingType readingType, DateTimeOffset timestampUtc)
    {
        var typeSegment = readingType switch
        {
            ClimateReadingType.Indoor => "indoor",
            ClimateReadingType.Outdoor => "outdoor",
            _ => throw new ArgumentOutOfRangeException(nameof(readingType), readingType, "Unsupported climate reading type."),
        };

        var datePath = timestampUtc.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
        var fileTimestamp = timestampUtc.ToString("yyyy_MM_dd_HH", CultureInfo.InvariantCulture);

        if (string.IsNullOrWhiteSpace(locationSlug))
        {
            return $"{typeSegment}/{datePath}/{typeSegment}_{fileTimestamp}.json";
        }

        return $"{locationSlug}/{datePath}/{locationSlug}_{typeSegment}_{fileTimestamp}.json";
    }
}
