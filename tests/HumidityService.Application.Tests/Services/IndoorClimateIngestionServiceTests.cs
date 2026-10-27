using HumidityService.Application.Interfaces;
using HumidityService.Application.Models;
using HumidityService.Application.Services;
using HumidityService.Domain.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace HumidityService.Application.Tests.Services;

public class IndoorClimateIngestionServiceTests
{
    private const string DevicesJson =
        "{\"result\":[{\"id\":\"gw-1\",\"device_type\":\"Danfoss Ally Gateway\"},{\"id\":\"room-1\"},{\"id\":\"gw-2\",\"device_type\":\"Danfoss Ally Gateway\"},{\"id\":\"room-2\"}],\"t\":1}";
    private const string House1SubDevicesJson = "{\"result\":[{\"id\":\"room-1\"}]}";
    private const string House2SubDevicesJson = "{\"result\":[{\"id\":\"room-2\"}]}";
    private const string House1Json = "{\"result\":[{\"id\":\"gw-1\",\"device_type\":\"Danfoss Ally Gateway\"},{\"id\":\"room-1\"}],\"t\":1}";
    private const string House2Json = "{\"result\":[{\"id\":\"gw-2\",\"device_type\":\"Danfoss Ally Gateway\"},{\"id\":\"room-2\"}],\"t\":1}";

    private static readonly Location House1 = new("aarhus-house1", "gw-1", 56.1629, 10.2039);
    private static readonly Location House2 = new("odense-house2", "gw-2", 55.4038, 10.4024);
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 14, 0, 3, TimeSpan.Zero);

    private readonly ILocationProvider _locationProvider = Substitute.For<ILocationProvider>();
    private readonly IDanfossAuthClient _authClient = Substitute.For<IDanfossAuthClient>();
    private readonly IDanfossClimateApiClient _climateApiClient = Substitute.For<IDanfossClimateApiClient>();
    private readonly IRawClimateBlobStorage _blobStorage = Substitute.For<IRawClimateBlobStorage>();
    private readonly IHumidityAlertService _humidityAlertService = Substitute.For<IHumidityAlertService>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly ILogger<IndoorClimateIngestionService> _logger = Substitute.For<ILogger<IndoorClimateIngestionService>>();

    public IndoorClimateIngestionServiceTests()
    {
        _timeProvider.GetUtcNow().Returns(Now);
    }

    private IndoorClimateIngestionService CreateSut() =>
        new(_locationProvider, _authClient, _climateApiClient, _blobStorage, _humidityAlertService, _timeProvider, _logger);

    private void GivenLocations(params Location[] locations) => _locationProvider.GetLocations().Returns(locations);

    private void GivenDeviceList(string json)
    {
        _authClient.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Returns("token-123");
        _climateApiClient.GetAllDevicesAsync("token-123", Arg.Any<CancellationToken>()).Returns(json);
        _climateApiClient.GetSubDevicesAsync("token-123", "gw-1", Arg.Any<CancellationToken>()).Returns(House1SubDevicesJson);
        _climateApiClient.GetSubDevicesAsync("token-123", "gw-2", Arg.Any<CancellationToken>()).Returns(House2SubDevicesJson);
    }

    [Fact]
    public async Task IngestAsync_SavesApiRawOnceThenEachLocationsSubDevicesAndOwnDevices()
    {
        GivenLocations(House1, House2);
        GivenDeviceList(DevicesJson);

        await CreateSut().IngestAsync(CancellationToken.None);

        await _climateApiClient.Received(1).GetAllDevicesAsync("token-123", Arg.Any<CancellationToken>());
        await _blobStorage.Received(1).SaveApiRawAsync(Now, DevicesJson, Arg.Any<CancellationToken>());
        await _blobStorage.Received(1).SaveLocationBlobAsync("aarhus-house1", LocationBlobType.SubDevices, Now, House1SubDevicesJson, Arg.Any<CancellationToken>());
        await _blobStorage.Received(1).SaveLocationBlobAsync("aarhus-house1", LocationBlobType.Indoor, Now, House1Json, Arg.Any<CancellationToken>());
        await _blobStorage.Received(1).SaveLocationBlobAsync("odense-house2", LocationBlobType.SubDevices, Now, House2SubDevicesJson, Arg.Any<CancellationToken>());
        await _blobStorage.Received(1).SaveLocationBlobAsync("odense-house2", LocationBlobType.Indoor, Now, House2Json, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_SavesApiRawBeforeAnythingPerLocationAndChecksAlertOnLocationsOwnDevices()
    {
        GivenLocations(House1);
        GivenDeviceList(DevicesJson);

        await CreateSut().IngestAsync(CancellationToken.None);

        Received.InOrder(() =>
        {
            _blobStorage.SaveApiRawAsync(Now, DevicesJson, Arg.Any<CancellationToken>());
            _blobStorage.SaveLocationBlobAsync("aarhus-house1", LocationBlobType.SubDevices, Now, House1SubDevicesJson, Arg.Any<CancellationToken>());
            _blobStorage.SaveLocationBlobAsync("aarhus-house1", LocationBlobType.Indoor, Now, House1Json, Arg.Any<CancellationToken>());
            _humidityAlertService.CheckAndNotifyAsync(House1, House1Json, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task IngestAsync_GatewayOnAccountButNotConfigured_LogsWarning()
    {
        GivenLocations(House1);
        GivenDeviceList(DevicesJson);

        await CreateSut().IngestAsync(CancellationToken.None);

        Assert.Contains(_logger.ReceivedCalls(), call =>
            call.GetMethodInfo().Name == nameof(ILogger.Log) &&
            (LogLevel)call.GetArguments()[0]! == LogLevel.Warning &&
            call.GetArguments()[2]!.ToString()!.Contains("gw-2"));
        await _climateApiClient.DidNotReceive().GetSubDevicesAsync(Arg.Any<string>(), "gw-2", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_GatewayNotInDeviceList_SkipsThatLocationOnly()
    {
        GivenLocations(new Location("unknown-house", "gw-missing", 56.1629, 10.2039), House1);
        GivenDeviceList(DevicesJson);

        await Assert.ThrowsAsync<AggregateException>(() => CreateSut().IngestAsync(CancellationToken.None));

        await _blobStorage.DidNotReceive().SaveLocationBlobAsync("unknown-house", Arg.Any<LocationBlobType>(), Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _blobStorage.Received(1).SaveLocationBlobAsync("aarhus-house1", LocationBlobType.Indoor, Now, House1Json, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_SubDevicesFetchFails_SkipsThatLocationOnly()
    {
        GivenLocations(House1, House2);
        GivenDeviceList(DevicesJson);
        _climateApiClient.GetSubDevicesAsync("token-123", "gw-1", Arg.Any<CancellationToken>())
            .Returns<string>(_ => throw new HttpRequestException("boom"));

        await Assert.ThrowsAsync<AggregateException>(() => CreateSut().IngestAsync(CancellationToken.None));

        await _blobStorage.DidNotReceive().SaveLocationBlobAsync("aarhus-house1", Arg.Any<LocationBlobType>(), Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _humidityAlertService.DidNotReceive().CheckAndNotifyAsync(House1, Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _blobStorage.Received(1).SaveLocationBlobAsync("odense-house2", LocationBlobType.Indoor, Now, House2Json, Arg.Any<CancellationToken>());
        await _humidityAlertService.Received(1).CheckAndNotifyAsync(House2, House2Json, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_OneLocationSaveFails_StillSavesRemainingLocations()
    {
        GivenLocations(House1, House2);
        GivenDeviceList(DevicesJson);
        _blobStorage.SaveLocationBlobAsync("aarhus-house1", LocationBlobType.Indoor, Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("storage down"));

        await Assert.ThrowsAsync<AggregateException>(() => CreateSut().IngestAsync(CancellationToken.None));

        await _humidityAlertService.DidNotReceive().CheckAndNotifyAsync(House1, Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _blobStorage.Received(1).SaveLocationBlobAsync("odense-house2", LocationBlobType.Indoor, Now, House2Json, Arg.Any<CancellationToken>());
        await _humidityAlertService.Received(1).CheckAndNotifyAsync(House2, House2Json, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_HumidityAlertFails_StillProcessesRemainingLocations()
    {
        GivenLocations(House1, House2);
        GivenDeviceList(DevicesJson);
        _humidityAlertService.CheckAndNotifyAsync(House1, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("email down"));

        await Assert.ThrowsAsync<AggregateException>(() => CreateSut().IngestAsync(CancellationToken.None));

        await _blobStorage.Received(1).SaveLocationBlobAsync("odense-house2", LocationBlobType.Indoor, Now, House2Json, Arg.Any<CancellationToken>());
        await _humidityAlertService.Received(1).CheckAndNotifyAsync(House2, House2Json, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_SeveralLocationsFail_ThrowsOnceWithEveryFailureAfterProcessingAll()
    {
        GivenLocations(new Location("unknown-house", "gw-missing", 56.1629, 10.2039), House1, House2);
        GivenDeviceList(DevicesJson);
        var saveFailure = new InvalidOperationException("storage down");
        _blobStorage.SaveLocationBlobAsync("aarhus-house1", LocationBlobType.Indoor, Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw saveFailure);

        var exception = await Assert.ThrowsAsync<AggregateException>(() => CreateSut().IngestAsync(CancellationToken.None));

        Assert.Equal(2, exception.InnerExceptions.Count);
        Assert.Contains(exception.InnerExceptions, e => e.Message.Contains("unknown-house"));
        Assert.Contains(saveFailure, exception.InnerExceptions);
        await _blobStorage.Received(1).SaveLocationBlobAsync("odense-house2", LocationBlobType.Indoor, Now, House2Json, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_ApiRawSaveFails_ThrowsAndProcessesNoLocation()
    {
        GivenLocations(House1);
        GivenDeviceList(DevicesJson);
        _blobStorage.SaveApiRawAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("storage down"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSut().IngestAsync(CancellationToken.None));

        await _climateApiClient.DidNotReceive().GetSubDevicesAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _blobStorage.DidNotReceive().SaveLocationBlobAsync(Arg.Any<string>(), Arg.Any<LocationBlobType>(), Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _humidityAlertService.DidNotReceive().CheckAndNotifyAsync(Arg.Any<Location>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_TokenFetchFails_ThrowsAndNeverCallsDevicesApi()
    {
        _authClient.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Returns<string>(_ => throw new InvalidOperationException("no token"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSut().IngestAsync(CancellationToken.None));

        await _climateApiClient.DidNotReceive().GetAllDevicesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _blobStorage.DidNotReceive().SaveApiRawAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_DevicesFetchFails_ThrowsAndDoesNotSave()
    {
        _authClient.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Returns("token-123");
        _climateApiClient.GetAllDevicesAsync("token-123", Arg.Any<CancellationToken>()).Returns<string>(_ => throw new HttpRequestException("boom"));

        await Assert.ThrowsAsync<HttpRequestException>(() => CreateSut().IngestAsync(CancellationToken.None));

        await _blobStorage.DidNotReceive().SaveApiRawAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_MalformedDeviceList_ThrowsAndDoesNotSave()
    {
        GivenLocations(House1);
        GivenDeviceList("{\"devices\":[]}");

        await Assert.ThrowsAnyAsync<Exception>(() => CreateSut().IngestAsync(CancellationToken.None));

        await _blobStorage.DidNotReceive().SaveApiRawAsync(Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
