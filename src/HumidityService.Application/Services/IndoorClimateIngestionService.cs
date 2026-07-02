using HumidityService.Application.Interfaces;
using HumidityService.Domain.Models;
using Microsoft.Extensions.Logging;

namespace HumidityService.Application.Services;

/// <summary>
/// Default <see cref="IIndoorClimateIngestionService"/> implementation. Fetches an OAuth token
/// and the full Danfoss device list, then persists the raw response as a single account-wide snapshot.
/// </summary>
public sealed class IndoorClimateIngestionService : IIndoorClimateIngestionService
{
    private readonly IDanfossAuthClient _authClient;
    private readonly IDanfossClimateApiClient _climateApiClient;
    private readonly IRawClimateBlobStorage _blobStorage;
    private readonly ILogger<IndoorClimateIngestionService> _logger;

    /// <summary>Initializes a new instance of <see cref="IndoorClimateIngestionService"/>.</summary>
    public IndoorClimateIngestionService(
        IDanfossAuthClient authClient,
        IDanfossClimateApiClient climateApiClient,
        IRawClimateBlobStorage blobStorage,
        ILogger<IndoorClimateIngestionService> logger)
    {
        _authClient = authClient;
        _climateApiClient = climateApiClient;
        _blobStorage = blobStorage;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task IngestAsync(CancellationToken cancellationToken)
    {
        string accessToken;
        try
        {
            accessToken = await _authClient.GetAccessTokenAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to obtain a Danfoss OAuth access token; aborting indoor climate ingestion.");
            throw;
        }

        try
        {
            var rawJson = await _climateApiClient.GetAllDevicesAsync(accessToken, cancellationToken);
            await _blobStorage.SaveRawReadingAsync(locationSlug: null, ClimateReadingType.Indoor, DateTimeOffset.UtcNow, rawJson, cancellationToken);

            _logger.LogInformation("Indoor climate device snapshot ingested successfully.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to ingest the indoor climate device snapshot.");
            throw;
        }
    }
}
