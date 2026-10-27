using HumidityService.Application.Interfaces;
using HumidityService.Application.Models;
using HumidityService.Domain.Models;
using Microsoft.Extensions.Logging;

namespace HumidityService.Application.Services;

/// <summary>
/// Default <see cref="IClimateIngestionService"/> implementation. Fetches an OAuth token and the
/// account-wide Danfoss device list once per run and saves it unmodified (<c>apiraw</c>). Then, for
/// every configured location, it ingests the outdoor reading, saves the gateway's raw sub-devices
/// response and the location's own devices extracted from the device list, and runs the humidity
/// alert check on them with the outdoor reading. Each step is isolated: a failed outdoor step leaves
/// the alert without an outdoor reading, and a failure for one location does not stop the others;
/// once every location has been processed, the run fails if any step did.
/// </summary>
public sealed class ClimateIngestionService : IClimateIngestionService
{
    private readonly ILocationProvider _locationProvider;
    private readonly IDanfossAuthClient _authClient;
    private readonly IDanfossClimateApiClient _climateApiClient;
    private readonly IOutdoorClimateIngestionService _outdoorIngestionService;
    private readonly IRawClimateBlobStorage _blobStorage;
    private readonly IHumidityAlertService _humidityAlertService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ClimateIngestionService> _logger;

    /// <summary>Initializes a new instance of <see cref="ClimateIngestionService"/>.</summary>
    public ClimateIngestionService(
        ILocationProvider locationProvider,
        IDanfossAuthClient authClient,
        IDanfossClimateApiClient climateApiClient,
        IOutdoorClimateIngestionService outdoorIngestionService,
        IRawClimateBlobStorage blobStorage,
        IHumidityAlertService humidityAlertService,
        TimeProvider timeProvider,
        ILogger<ClimateIngestionService> logger)
    {
        _locationProvider = locationProvider;
        _authClient = authClient;
        _climateApiClient = climateApiClient;
        _outdoorIngestionService = outdoorIngestionService;
        _blobStorage = blobStorage;
        _humidityAlertService = humidityAlertService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task IngestAsync(CancellationToken cancellationToken)
    {
        // One timestamp per run, so a location's indoor and outdoor blobs always share an hourly folder.
        var timestampUtc = _timeProvider.GetUtcNow();

        string accessToken;
        try
        {
            accessToken = await _authClient.GetAccessTokenAsync(cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Failed to obtain a Danfoss OAuth access token; aborting climate ingestion.");
            throw;
        }

        string rawJson;
        IReadOnlySet<string> deviceIds;
        try
        {
            rawJson = await _climateApiClient.GetAllDevicesAsync(accessToken, cancellationToken);
            deviceIds = DanfossDeviceListParser.GetDeviceIds(rawJson);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Failed to fetch or parse the Danfoss device list; aborting climate ingestion.");
            throw;
        }

        var locations = _locationProvider.GetLocations();

        // ELT: the unmodified account-wide response is stored first; everything per location is
        // extracted from it afterwards.
        try
        {
            await _blobStorage.SaveApiRawAsync(timestampUtc, rawJson, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Failed to save the raw Danfoss device list; aborting climate ingestion.");
            throw;
        }

        WarnAboutUnconfiguredGateways(rawJson, locations);

        // Failures are caught unless the run itself was cancelled: an HttpClient timeout surfaces as a
        // TaskCanceledException too, and must not stop the other steps and locations.
        var failures = new List<Exception>();
        foreach (var location in locations)
        {
            // Outdoor first, so the hour's outdoor reading is there for the alert check.
            OutdoorClimateReading? outdoorReading = null;
            try
            {
                outdoorReading = await _outdoorIngestionService.IngestAsync(location, timestampUtc, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Failed to ingest the outdoor climate reading for location {LocationSlug}; continuing without it.", location.Slug);
                failures.Add(ex);
            }

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
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Failed to ingest the indoor climate reading for location {LocationSlug}.", location.Slug);
                failures.Add(ex);
                continue;
            }

            try
            {
                await _humidityAlertService.CheckAndNotifyAsync(location, locationJson, outdoorReading, timestampUtc, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Humidity alert check failed for location {LocationSlug}.", location.Slug);
                failures.Add(ex);
            }
        }

        if (failures.Count > 0)
        {
            // Each failure is already logged above; failing the run makes it visible as a failed
            // function invocation (e.g. for alerting) instead of a silent success.
            throw new AggregateException($"Climate ingestion had {failures.Count} failure(s); see the logged errors per location.", failures);
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
