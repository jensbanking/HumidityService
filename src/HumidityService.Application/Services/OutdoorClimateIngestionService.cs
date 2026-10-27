using HumidityService.Application.Interfaces;
using HumidityService.Application.Models;
using HumidityService.Domain.Models;
using Microsoft.Extensions.Logging;

namespace HumidityService.Application.Services;

/// <summary>
/// Default <see cref="IOutdoorClimateIngestionService"/> implementation. Fetches a location's outdoor
/// reading from Open-Meteo, saves the raw response unmodified and then reads the current values from it.
/// Failures propagate to the caller, which decides how they affect the rest of the run.
/// </summary>
public sealed class OutdoorClimateIngestionService : IOutdoorClimateIngestionService
{
    private readonly IOpenMeteoApiClient _openMeteoApiClient;
    private readonly IRawClimateBlobStorage _blobStorage;
    private readonly ILogger<OutdoorClimateIngestionService> _logger;

    /// <summary>Initializes a new instance of <see cref="OutdoorClimateIngestionService"/>.</summary>
    public OutdoorClimateIngestionService(
        IOpenMeteoApiClient openMeteoApiClient,
        IRawClimateBlobStorage blobStorage,
        ILogger<OutdoorClimateIngestionService> logger)
    {
        _openMeteoApiClient = openMeteoApiClient;
        _blobStorage = blobStorage;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<OutdoorClimateReading?> IngestAsync(Location location, DateTimeOffset timestampUtc, CancellationToken cancellationToken)
    {
        var rawJson = await _openMeteoApiClient.GetOutdoorClimateAsync(location.Latitude, location.Longitude, cancellationToken);
        // ELT: the raw response is stored before anything is read from it.
        await _blobStorage.SaveLocationBlobAsync(location.Slug, LocationBlobType.Outdoor, timestampUtc, rawJson, cancellationToken);

        var reading = OpenMeteoResponseParser.GetCurrentReading(rawJson);
        if (reading is null)
        {
            _logger.LogWarning(
                "Open-Meteo response for location {LocationSlug} has no current temperature or humidity; the outdoor reading is unavailable.",
                location.Slug);
        }

        _logger.LogInformation("Outdoor climate reading ingested successfully for location {LocationSlug}.", location.Slug);
        return reading;
    }
}
