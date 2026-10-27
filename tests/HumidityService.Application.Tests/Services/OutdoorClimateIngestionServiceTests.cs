using System.Text.Json;
using HumidityService.Application.Interfaces;
using HumidityService.Application.Models;
using HumidityService.Application.Services;
using HumidityService.Domain.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace HumidityService.Application.Tests.Services;

public class OutdoorClimateIngestionServiceTests
{
    private const string OpenMeteoJson = "{\"current\":{\"time\":\"2026-10-27T14:00\",\"temperature_2m\":8.3,\"relative_humidity_2m\":92}}";

    private static readonly Location House1 = new("aarhus-house1", "gw-1", 56.1629, 10.2039);
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 14, 0, 3, TimeSpan.Zero);

    private readonly IOpenMeteoApiClient _openMeteoApiClient = Substitute.For<IOpenMeteoApiClient>();
    private readonly IRawClimateBlobStorage _blobStorage = Substitute.For<IRawClimateBlobStorage>();
    private readonly ILogger<OutdoorClimateIngestionService> _logger = Substitute.For<ILogger<OutdoorClimateIngestionService>>();

    private OutdoorClimateIngestionService CreateSut() => new(_openMeteoApiClient, _blobStorage, _logger);

    [Fact]
    public async Task IngestAsync_SavesRawResponseAndReturnsCurrentReading()
    {
        _openMeteoApiClient.GetOutdoorClimateAsync(56.1629, 10.2039, Arg.Any<CancellationToken>()).Returns(OpenMeteoJson);

        var reading = await CreateSut().IngestAsync(House1, Now, CancellationToken.None);

        Assert.Equal(new OutdoorClimateReading(8.3, 92), reading);
        await _blobStorage.Received(1).SaveLocationBlobAsync("aarhus-house1", LocationBlobType.Outdoor, Now, OpenMeteoJson, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_ValuesMissing_SavesRawResponseThenThrows()
    {
        const string json = "{\"current\":{\"temperature_2m\":null,\"relative_humidity_2m\":92}}";
        _openMeteoApiClient.GetOutdoorClimateAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<CancellationToken>()).Returns(json);

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => CreateSut().IngestAsync(House1, Now, CancellationToken.None));

        Assert.Contains("aarhus-house1", exception.Message);
        await _blobStorage.Received(1).SaveLocationBlobAsync("aarhus-house1", LocationBlobType.Outdoor, Now, json, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_MalformedResponse_SavesRawResponseThenThrows()
    {
        _openMeteoApiClient.GetOutdoorClimateAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<CancellationToken>()).Returns("not json");

        await Assert.ThrowsAnyAsync<JsonException>(() => CreateSut().IngestAsync(House1, Now, CancellationToken.None));

        await _blobStorage.Received(1).SaveLocationBlobAsync("aarhus-house1", LocationBlobType.Outdoor, Now, "not json", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_FetchFails_ThrowsAndSavesNothing()
    {
        _openMeteoApiClient.GetOutdoorClimateAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<CancellationToken>())
            .Returns<string>(_ => throw new HttpRequestException("boom"));

        await Assert.ThrowsAsync<HttpRequestException>(() => CreateSut().IngestAsync(House1, Now, CancellationToken.None));

        await _blobStorage.DidNotReceive().SaveLocationBlobAsync(Arg.Any<string>(), Arg.Any<LocationBlobType>(), Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IngestAsync_SaveFails_Throws()
    {
        _openMeteoApiClient.GetOutdoorClimateAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<CancellationToken>()).Returns(OpenMeteoJson);
        _blobStorage.SaveLocationBlobAsync(Arg.Any<string>(), Arg.Any<LocationBlobType>(), Arg.Any<DateTimeOffset>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("storage down"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSut().IngestAsync(House1, Now, CancellationToken.None));
    }
}
