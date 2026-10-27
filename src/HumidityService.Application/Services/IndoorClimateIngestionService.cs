using HumidityService.Application.Interfaces;
using HumidityService.Application.Models;
using HumidityService.Domain.Models;
using Microsoft.Extensions.Logging;

namespace HumidityService.Application.Services;

/// <summary>
/// Default <see cref="IIndoorClimateIngestionService"/> implementation. Fetches an OAuth token and
/// the account-wide Danfoss device list once per run and saves it unmodified (<c>apiraw</c>). Then,
/// for every configured location, it saves the gateway's raw sub-devices response and the
/// location's own devices extracted from the device list, and runs the humidity alert check on them.
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
        var locations = _locationProvider.GetLocations();

        // ELT: the unmodified account-wide response is stored first; everything per location is
        // extracted from it afterwards.
        try
        {
            await _blobStorage.SaveApiRawAsync(timestampUtc, rawJson, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Failed to save the raw Danfoss device list; aborting indoor climate ingestion.");
            throw;
        }

        WarnAboutUnconfiguredGateways(rawJson, locations);

        var failures = new List<Exception>();
        foreach (var location in locations)
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

            string locationJson;
            try
            {
                locationJson = await ExtractLocationAsync(location, accessToken, rawJson, timestampUtc, cancellationToken);

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
                await _humidityAlertService.CheckAndNotifyAsync(location, locationJson, cancellationToken);
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

    // Saves the gateway's raw sub-devices response and the location's devices (the gateway and its
    // sub-devices) extracted from the account-wide device list; returns the extracted list.
    private async Task<string> ExtractLocationAsync(Location location, string accessToken, string rawJson, DateTimeOffset timestampUtc, CancellationToken cancellationToken)
    {
        var subDevicesJson = await _climateApiClient.GetSubDevicesAsync(accessToken, location.DanfossGatewayId, cancellationToken);
        await _blobStorage.SaveLocationBlobAsync(location.Slug, LocationBlobType.SubDevices, timestampUtc, subDevicesJson, cancellationToken);

        var locationDeviceIds = new HashSet<string>(DanfossDeviceListParser.GetDeviceIds(subDevicesJson), StringComparer.Ordinal)
        {
            location.DanfossGatewayId,
        };
        var locationJson = DanfossDeviceListParser.FilterDevices(rawJson, locationDeviceIds);
        await _blobStorage.SaveLocationBlobAsync(location.Slug, LocationBlobType.Indoor, timestampUtc, locationJson, cancellationToken);

        return locationJson;
    }

    // Locations are configured statically; a gateway on the account without a location is only reported.
    private void WarnAboutUnconfiguredGateways(string rawJson, IReadOnlyList<Location> locations)
    {
        foreach (var gatewayId in DanfossDeviceListParser.GetGatewayIds(rawJson))
        {
            if (!locations.Any(l => l.DanfossGatewayId == gatewayId))
            {
                _logger.LogWarning("Danfoss gateway {GatewayId} is on the account but not configured as a location; its devices are only in the raw device list.", gatewayId);
            }
        }
    }
}
