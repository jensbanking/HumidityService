using HumidityService.Domain.Models;

namespace HumidityService.Application.Interfaces;

/// <summary>
/// Persists raw, unmodified climate readings to blob storage, one blob per location per run.
/// </summary>
public interface IRawClimateBlobStorage
{
    /// <summary>Saves a raw JSON climate reading using the standard location/date blob naming convention.</summary>
    /// <param name="locationSlug">The location's URL/path-safe identifier.</param>
    /// <param name="readingType">Whether the reading is indoor or outdoor.</param>
    /// <param name="timestampUtc">The UTC timestamp the reading was captured at; determines the blob's date/hour path segments.</param>
    /// <param name="rawJson">The unmodified raw JSON payload to persist.</param>
    /// <param name="cancellationToken">Token used to cancel the upload.</param>
    Task SaveRawReadingAsync(string locationSlug, ClimateReadingType readingType, DateTimeOffset timestampUtc, string rawJson, CancellationToken cancellationToken);
}
