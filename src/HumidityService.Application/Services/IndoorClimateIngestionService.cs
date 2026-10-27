using HumidityService.Application.Interfaces;
using HumidityService.Domain.Models;
using Microsoft.Extensions.Logging;

namespace HumidityService.Application.Services;

/// <summary>
/// Default <see cref="IIndoorClimateIngestionService"/> implementation. Fetches an OAuth token
/// and the Danfoss device list once per run, then persists the raw response for every configured
/// location whose gateway appears in it, then runs the humidity alert check for that location.
/// A failure for one location (or its alert) is logged and does not stop the others; once every
/// location has been processed, the run fails if any of them did.
/// </summary>
public sealed class IndoorClimateIngestionService : IIndoorClimateIngestionService
{
    private readonly ILocationProvider _locationProvider;
    private readonly IDanfossAuthClient _authClient;
    private readonly IDanfossClimateApiClient _climateApiClient;
    private readonly IRawClimateBlobStorage _blobStorage;
    private readonly IHumidityAlertService _humidityAlertService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<IndoorClimateIngestionService> _logger;

    /// <summary>Initializes a new instance of <see cref="IndoorClimateIngestionService"/>.</summary>
    public IndoorClimateIngestionService(
        ILocationProvider locationProvider,
        IDanfossAuthClient authClient,
        IDanfossClimateApiClient climateApiClient,
        IRawClimateBlobStorage blobStorage,
        IHumidityAlertService humidityAlertService,
        TimeProvider timeProvider,
        ILogger<IndoorClimateIngestionService> logger)
    {
        _locationProvider = locationProvider;
        _authClient = authClient;
        _climateApiClient = climateApiClient;
        _blobStorage = blobStorage;
        _humidityAlertService = humidityAlertService;
        _timeProvider = timeProvider;
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

        var timestampUtc = _timeProvider.GetUtcNow();
        var failures = new List<Exception>();
        foreach (var location in _locationProvider.GetLocations())
        {
            if (!deviceIds.Contains(location.DanfossGatewayId))
            {
                _logger.LogError(
                    "Danfoss gateway {GatewayId} for location {LocationSlug} was not found in the device list; skipping indoor reading.",
                    location.DanfossGatewayId,
                    location.Slug);
                failures.Add(new InvalidOperationException(
                    $"Danfoss gateway '{location.DanfossGatewayId}' for location '{location.Slug}' was not found in the device list."));
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
                failures.Add(ex);
                continue;
            }

            try
            {
                await _humidityAlertService.CheckAndNotifyAsync(location, rawJson, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Humidity alert check failed for location {LocationSlug}.", location.Slug);
                failures.Add(ex);
            }
        }

        if (failures.Count > 0)
        {
            // Each failure is already logged above; failing the run makes it visible as a failed
            // function invocation (e.g. for alerting) instead of a silent success.
            throw new AggregateException($"Indoor climate ingestion had {failures.Count} failure(s); see the logged errors per location.", failures);
        }
    }
}
