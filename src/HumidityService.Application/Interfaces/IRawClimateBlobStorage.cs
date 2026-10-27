using HumidityService.Application.Models;

namespace HumidityService.Application.Interfaces;

/// <summary>
/// Persists an hour's climate data to blob storage using the <see cref="Services.ClimateBlobPathBuilder"/>
/// paths: the raw, account-wide Danfoss device list once per hour, and each location's blobs.
/// </summary>
public interface IRawClimateBlobStorage
{
    /// <summary>Saves the unmodified, account-wide Danfoss <c>/ally/devices</c> response (<c>apiraw</c>).</summary>
    /// <param name="timestampUtc">When the data was captured; determines the blob's date/hour path.</param>
    /// <param name="rawJson">The unmodified raw JSON response.</param>
    /// <param name="cancellationToken">Token used to cancel the upload.</param>
    Task SaveApiRawAsync(DateTimeOffset timestampUtc, string rawJson, CancellationToken cancellationToken);

    /// <summary>Saves one of a location's blobs for the hour.</summary>
    /// <param name="locationSlug">The location's URL/path-safe identifier.</param>
    /// <param name="blobType">Which of the location's blobs.</param>
    /// <param name="timestampUtc">When the data was captured; determines the blob's date/hour path.</param>
    /// <param name="json">The JSON to persist.</param>
    /// <param name="cancellationToken">Token used to cancel the upload.</param>
    Task SaveLocationBlobAsync(string locationSlug, LocationBlobType blobType, DateTimeOffset timestampUtc, string json, CancellationToken cancellationToken);
}
