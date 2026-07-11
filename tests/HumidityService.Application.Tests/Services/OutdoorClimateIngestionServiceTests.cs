using HumidityService.Application.Interfaces;
using HumidityService.Application.Services;
using HumidityService.Domain.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace HumidityService.Application.Tests.Services;

public class OutdoorClimateIngestionServiceTests
{
    private readonly ILocationProvider _locationProvider = Substitute.For<ILocationProvider>();
    private readonly IOpenMeteoApiClient _openMeteoApiClient = Substitute.For<IOpenMeteoApiClient>();
    private readonly IRawClimateBlobStorage _blobStorage = Substitute.For<IRawClimateBlobStorage>();
    private readonly ILogger<OutdoorClimateIngestionService> _logger = Substitute.For<ILogger<OutdoorClimateIngestionService>>();

    private OutdoorClimateIngestionService CreateSut() =>
        new(_locationProvider, _openMeteoApiClient, _blobStorage, _logger);

    [Fact]
    public async Task IngestAsync_MultipleLocations_FetchesAndSavesEachIndependently()
    {
        var locations = new[]
        {
            new Location("aarhus-house1", 56.1629, 10.2039),
            new Location("odense-house2", 55.4038, 10.4024),
        };
        _locationProvider.GetLocations().Returns(locations);
        _openMeteoApiClient.GetOutdoorClimateAsync(56.1629, 10.2039, Arg.Any<CancellationToken>()).Returns("{\"aarhus\":true}");
        _openMeteoApiClient.GetOutdoorClimateAsync(55.4038, 10.4024, Arg.Any<CancellationToken>()).Returns("{\"odense\":true}");

        await CreateSut().IngestAsync(CancellationToken.None);

        await _blobStorage.Received(1).SaveRawReadingAsync("aarhus-house1", ClimateReadingType.Outdoor, Arg.Any<DateTimeOffset>(), "{\"aarhus\":true}", Arg.Any<CancellationToken>());
        await _blobStorage.Received(1).SaveRawReadingAsync("odense-house2", ClimateReadingType.Outdoor, Arg.Any<DateTimeOffset>(), "{\"odense\":true}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_OneLocationFetchFails_StillProcessesRemainingLocations()
    {
        var locations = new[]
        {
            new Location("aarhus-house1", 56.1629, 10.2039),
            new Location("odense-house2", 55.4038, 10.4024),
        };
        _locationProvider.GetLocations().Returns(locations);
        _openMeteoApiClient.GetOutdoorClimateAsync(56.1629, 10.2039, Arg.Any<CancellationToken>())
            .Returns<string>(_ => throw new HttpRequestException("boom"));
        _openMeteoApiClient.GetOutdoorClimateAsync(55.4038, 10.4024, Arg.Any<CancellationToken>()).Returns("{\"odense\":true}");

        await CreateSut().IngestAsync(CancellationToken.None);

        await _blobStorage.DidNotReceive().SaveRawReadingAsync("aarhus-house1", Arg.Any<ClimateReadingType>(), Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _blobStorage.Received(1).SaveRawReadingAsync("odense-house2", ClimateReadingType.Outdoor, Arg.Any<DateTimeOffset>(), "{\"odense\":true}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_OneLocationSaveFails_StillProcessesRemainingLocations()
    {
        var locations = new[]
        {
            new Location("aarhus-house1", 56.1629, 10.2039),
            new Location("odense-house2", 55.4038, 10.4024),
        };
        _locationProvider.GetLocations().Returns(locations);
        _openMeteoApiClient.GetOutdoorClimateAsync(56.1629, 10.2039, Arg.Any<CancellationToken>()).Returns("{\"aarhus\":true}");
        _openMeteoApiClient.GetOutdoorClimateAsync(55.4038, 10.4024, Arg.Any<CancellationToken>()).Returns("{\"odense\":true}");
        _blobStorage.SaveRawReadingAsync("aarhus-house1", ClimateReadingType.Outdoor, Arg.Any<DateTimeOffset>(), "{\"aarhus\":true}", Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("storage down"));

        await CreateSut().IngestAsync(CancellationToken.None);

        await _blobStorage.Received(1).SaveRawReadingAsync("odense-house2", ClimateReadingType.Outdoor, Arg.Any<DateTimeOffset>(), "{\"odense\":true}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_NoLocationsConfigured_DoesNotCallApiOrStorage()
    {
        _locationProvider.GetLocations().Returns(Array.Empty<Location>());

        await CreateSut().IngestAsync(CancellationToken.None);

        await _openMeteoApiClient.DidNotReceive().GetOutdoorClimateAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<CancellationToken>());
        await _blobStorage.DidNotReceive().SaveRawReadingAsync(Arg.Any<string?>(), Arg.Any<ClimateReadingType>(), Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
