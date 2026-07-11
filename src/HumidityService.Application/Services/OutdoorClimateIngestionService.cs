using HumidityService.Application.Interfaces;
using HumidityService.Domain.Models;
using Microsoft.Extensions.Logging;

namespace HumidityService.Application.Services;

/// <summary>
/// Default <see cref="IOutdoorClimateIngestionService"/> implementation. Iterates over every
/// configured location, fetching outdoor climate data from Open-Meteo and persisting the raw
/// response per location. A failure for one location is logged and does not stop the others.
/// </summary>
public sealed class OutdoorClimateIngestionService : IOutdoorClimateIngestionService
{
    private readonly ILocationProvider _locationProvider;
    private readonly IOpenMeteoApiClient _openMeteoApiClient;
    private readonly IRawClimateBlobStorage _blobStorage;
    private readonly ILogger<OutdoorClimateIngestionService> _logger;

    /// <summary>Initializes a new instance of <see cref="OutdoorClimateIngestionService"/>.</summary>
    public OutdoorClimateIngestionService(
        ILocationProvider locationProvider,
        IOpenMeteoApiClient openMeteoApiClient,
        IRawClimateBlobStorage blobStorage,
        ILogger<OutdoorClimateIngestionService> logger)
    {
        _locationProvider = locationProvider;
        _openMeteoApiClient = openMeteoApiClient;
        _blobStorage = blobStorage;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task IngestAsync(CancellationToken cancellationToken)
    {
        foreach (var location in _locationProvider.GetLocations())
        {
            try
            {
                var rawJson = await _openMeteoApiClient.GetOutdoorClimateAsync(location.Latitude, location.Longitude, cancellationToken);
                await _blobStorage.SaveRawReadingAsync(location.Slug, ClimateReadingType.Outdoor, DateTimeOffset.UtcNow, rawJson, cancellationToken);

                _logger.LogInformation("Outdoor climate reading ingested successfully for location {LocationSlug}.", location.Slug);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to ingest the outdoor climate reading for location {LocationSlug}.", location.Slug);
            }
        }
    }
}
