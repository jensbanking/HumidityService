using HumidityService.Application.Interfaces;
using HumidityService.Application.Services;
using HumidityService.Domain.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace HumidityService.Application.Tests.Services;

public class IndoorClimateIngestionServiceTests
{
    private const string DevicesJson = "{\"result\":[{\"id\":\"gw-1\",\"device_type\":\"Danfoss Ally™ Gateway\"},{\"id\":\"room-1\"},{\"id\":\"gw-2\"}]}";

    private readonly ILocationProvider _locationProvider = Substitute.For<ILocationProvider>();
    private readonly IDanfossAuthClient _authClient = Substitute.For<IDanfossAuthClient>();
    private readonly IDanfossClimateApiClient _climateApiClient = Substitute.For<IDanfossClimateApiClient>();
    private readonly IRawClimateBlobStorage _blobStorage = Substitute.For<IRawClimateBlobStorage>();
    private readonly IHumidityAlertService _humidityAlertService = Substitute.For<IHumidityAlertService>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly ILogger<IndoorClimateIngestionService> _logger = Substitute.For<ILogger<IndoorClimateIngestionService>>();

    private IndoorClimateIngestionService CreateSut() =>
        new(_locationProvider, _authClient, _climateApiClient, _blobStorage, _humidityAlertService, _timeProvider, _logger);

    private void GivenDeviceList(string json)
    {
        _authClient.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Returns("token-123");
        _climateApiClient.GetAllDevicesAsync("token-123", Arg.Any<CancellationToken>()).Returns(json);
    }

    [Fact]
    public async Task IngestAsync_MultipleLocations_FetchesDevicesOnceAndSavesPerLocation()
    {
        _locationProvider.GetLocations().Returns(new[]
        {
            new Location("aarhus-house1", "gw-1", 56.1629, 10.2039),
            new Location("odense-house2", "gw-2", 55.4038, 10.4024),
        });
        GivenDeviceList(DevicesJson);

        await CreateSut().IngestAsync(CancellationToken.None);

        await _authClient.Received(1).GetAccessTokenAsync(Arg.Any<CancellationToken>());
        await _climateApiClient.Received(1).GetAllDevicesAsync("token-123", Arg.Any<CancellationToken>());
        await _blobStorage.Received(1).SaveRawReadingAsync("aarhus-house1", ClimateReadingType.Indoor, Arg.Any<DateTimeOffset>(), DevicesJson, Arg.Any<CancellationToken>());
        await _blobStorage.Received(1).SaveRawReadingAsync("odense-house2", ClimateReadingType.Indoor, Arg.Any<DateTimeOffset>(), DevicesJson, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_SavesEveryLocationWithTheRunTimestampFromTimeProvider()
    {
        var now = new DateTimeOffset(2026, 10, 27, 14, 0, 3, TimeSpan.Zero);
        _timeProvider.GetUtcNow().Returns(now);
        _locationProvider.GetLocations().Returns(new[]
        {
            new Location("aarhus-house1", "gw-1", 56.1629, 10.2039),
            new Location("odense-house2", "gw-2", 55.4038, 10.4024),
        });
        GivenDeviceList(DevicesJson);

        await CreateSut().IngestAsync(CancellationToken.None);

        await _blobStorage.Received(1).SaveRawReadingAsync("aarhus-house1", ClimateReadingType.Indoor, now, DevicesJson, Arg.Any<CancellationToken>());
        await _blobStorage.Received(1).SaveRawReadingAsync("odense-house2", ClimateReadingType.Indoor, now, DevicesJson, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_GatewayNotInDeviceList_SkipsThatLocationOnly()
    {
        _locationProvider.GetLocations().Returns(new[]
        {
            new Location("unknown-house", "gw-missing", 56.1629, 10.2039),
            new Location("aarhus-house1", "gw-1", 56.1629, 10.2039),
        });
        GivenDeviceList(DevicesJson);

        await Assert.ThrowsAsync<AggregateException>(() => CreateSut().IngestAsync(CancellationToken.None));

        await _blobStorage.DidNotReceive().SaveRawReadingAsync("unknown-house", Arg.Any<ClimateReadingType>(), Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _blobStorage.Received(1).SaveRawReadingAsync("aarhus-house1", ClimateReadingType.Indoor, Arg.Any<DateTimeOffset>(), DevicesJson, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_OneLocationSaveFails_StillSavesRemainingLocations()
    {
        _locationProvider.GetLocations().Returns(new[]
        {
            new Location("aarhus-house1", "gw-1", 56.1629, 10.2039),
            new Location("odense-house2", "gw-2", 55.4038, 10.4024),
        });
        GivenDeviceList(DevicesJson);
        _blobStorage.SaveRawReadingAsync("aarhus-house1", Arg.Any<ClimateReadingType>(), Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("storage down"));

        await Assert.ThrowsAsync<AggregateException>(() => CreateSut().IngestAsync(CancellationToken.None));

        await _blobStorage.Received(1).SaveRawReadingAsync("odense-house2", ClimateReadingType.Indoor, Arg.Any<DateTimeOffset>(), DevicesJson, Arg.Any<CancellationToken>());
        await _humidityAlertService.DidNotReceive().CheckAndNotifyAsync(Arg.Is<Location>(l => l.Slug == "aarhus-house1"), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _humidityAlertService.Received(1).CheckAndNotifyAsync(Arg.Is<Location>(l => l.Slug == "odense-house2"), DevicesJson, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_Success_RunsHumidityAlertCheckAfterSavingEachLocation()
    {
        var location = new Location("aarhus-house1", "gw-1", 56.1629, 10.2039);
        _locationProvider.GetLocations().Returns(new[] { location });
        GivenDeviceList(DevicesJson);

        await CreateSut().IngestAsync(CancellationToken.None);

        Received.InOrder(() =>
        {
            _blobStorage.SaveRawReadingAsync("aarhus-house1", ClimateReadingType.Indoor, Arg.Any<DateTimeOffset>(), DevicesJson, Arg.Any<CancellationToken>());
            _humidityAlertService.CheckAndNotifyAsync(location, DevicesJson, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task IngestAsync_HumidityAlertFails_StillSavesAndChecksRemainingLocations()
    {
        _locationProvider.GetLocations().Returns(new[]
        {
            new Location("aarhus-house1", "gw-1", 56.1629, 10.2039),
            new Location("odense-house2", "gw-2", 55.4038, 10.4024),
        });
        GivenDeviceList(DevicesJson);
        _humidityAlertService.CheckAndNotifyAsync(Arg.Is<Location>(l => l.Slug == "aarhus-house1"), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("email down"));

        await Assert.ThrowsAsync<AggregateException>(() => CreateSut().IngestAsync(CancellationToken.None));

        await _blobStorage.Received(1).SaveRawReadingAsync("odense-house2", ClimateReadingType.Indoor, Arg.Any<DateTimeOffset>(), DevicesJson, Arg.Any<CancellationToken>());
        await _humidityAlertService.Received(1).CheckAndNotifyAsync(Arg.Is<Location>(l => l.Slug == "odense-house2"), DevicesJson, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_SeveralLocationsFail_ThrowsOnceWithEveryFailureAfterProcessingAll()
    {
        _locationProvider.GetLocations().Returns(new[]
        {
            new Location("unknown-house", "gw-missing", 56.1629, 10.2039),
            new Location("aarhus-house1", "gw-1", 56.1629, 10.2039),
            new Location("odense-house2", "gw-2", 55.4038, 10.4024),
        });
        GivenDeviceList(DevicesJson);
        var saveFailure = new InvalidOperationException("storage down");
        _blobStorage.SaveRawReadingAsync("aarhus-house1", Arg.Any<ClimateReadingType>(), Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw saveFailure);

        var exception = await Assert.ThrowsAsync<AggregateException>(() => CreateSut().IngestAsync(CancellationToken.None));

        Assert.Equal(2, exception.InnerExceptions.Count);
        Assert.Contains(exception.InnerExceptions, e => e.Message.Contains("unknown-house"));
        Assert.Contains(saveFailure, exception.InnerExceptions);
        await _blobStorage.Received(1).SaveRawReadingAsync("odense-house2", ClimateReadingType.Indoor, Arg.Any<DateTimeOffset>(), DevicesJson, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_TokenFetchFails_ThrowsAndNeverCallsDevicesApi()
    {
        _authClient.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Returns<string>(_ => throw new InvalidOperationException("no token"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSut().IngestAsync(CancellationToken.None));

        await _climateApiClient.DidNotReceive().GetAllDevicesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _blobStorage.DidNotReceive().SaveRawReadingAsync(Arg.Any<string?>(), Arg.Any<ClimateReadingType>(), Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_DevicesFetchFails_ThrowsAndDoesNotSave()
    {
        _authClient.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Returns("token-123");
        _climateApiClient.GetAllDevicesAsync("token-123", Arg.Any<CancellationToken>()).Returns<string>(_ => throw new HttpRequestException("boom"));

        await Assert.ThrowsAsync<HttpRequestException>(() => CreateSut().IngestAsync(CancellationToken.None));

        await _blobStorage.DidNotReceive().SaveRawReadingAsync(Arg.Any<string?>(), Arg.Any<ClimateReadingType>(), Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_MalformedDeviceList_ThrowsAndDoesNotSave()
    {
        _locationProvider.GetLocations().Returns(new[] { new Location("aarhus-house1", "gw-1", 56.1629, 10.2039) });
        GivenDeviceList("{\"devices\":[]}");

        await Assert.ThrowsAnyAsync<Exception>(() => CreateSut().IngestAsync(CancellationToken.None));

        await _blobStorage.DidNotReceive().SaveRawReadingAsync(Arg.Any<string?>(), Arg.Any<ClimateReadingType>(), Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
