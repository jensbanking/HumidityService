using HumidityService.Application.Interfaces;
using HumidityService.Domain.Models;
using Microsoft.Extensions.Logging;

namespace HumidityService.Application.Services;

/// <summary>
/// Default <see cref="IIndoorClimateIngestionService"/> implementation. Fetches an OAuth token
/// and the Danfoss device list once per run, then persists the raw response for every configured
/// location whose gateway appears in it. A failure for one location is logged and does not stop the others.
/// </summary>
public sealed class IndoorClimateIngestionService : IIndoorClimateIngestionService
{
    private readonly ILocationProvider _locationProvider;
    private readonly IDanfossAuthClient _authClient;
    private readonly IDanfossClimateApiClient _climateApiClient;
    private readonly IRawClimateBlobStorage _blobStorage;
    private readonly ILogger<IndoorClimateIngestionService> _logger;

    /// <summary>Initializes a new instance of <see cref="IndoorClimateIngestionService"/>.</summary>
    public IndoorClimateIngestionService(
        ILocationProvider locationProvider,
        IDanfossAuthClient authClient,
        IDanfossClimateApiClient climateApiClient,
        IRawClimateBlobStorage blobStorage,
        ILogger<IndoorClimateIngestionService> logger)
    {
        _locationProvider = locationProvider;
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

        string rawJson;
        IReadOnlySet<string> deviceIds;
        try
        {
            rawJson = await _climateApiClient.GetAllDevicesAsync(accessToken, cancellationToken);
            deviceIds = DanfossDeviceListParser.GetDeviceIds(rawJson);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to fetch or parse the Danfoss device list; aborting indoor climate ingestion.");
            throw;
        }

        var timestampUtc = DateTimeOffset.UtcNow;
        foreach (var location in _locationProvider.GetLocations())
        {
            if (!deviceIds.Contains(location.DanfossGatewayId))
            {
                _logger.LogError(
                    "Danfoss gateway {GatewayId} for location {LocationSlug} was not found in the device list; skipping indoor reading.",
                    location.DanfossGatewayId,
                    location.Slug);
                continue;
            }

            try
            {
                await _blobStorage.SaveRawReadingAsync(location.Slug, ClimateReadingType.Indoor, timestampUtc, rawJson, cancellationToken);

                _logger.LogInformation("Indoor climate reading ingested successfully for location {LocationSlug}.", location.Slug);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Failed to ingest the indoor climate reading for location {LocationSlug}.", location.Slug);
            }
        }
    }
}
