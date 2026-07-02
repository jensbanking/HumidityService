using HumidityService.Application.Interfaces;
using HumidityService.Application.Services;
using HumidityService.Domain.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace HumidityService.Application.Tests.Services;

public class IndoorClimateIngestionServiceTests
{
    private readonly IDanfossAuthClient _authClient = Substitute.For<IDanfossAuthClient>();
    private readonly IDanfossClimateApiClient _climateApiClient = Substitute.For<IDanfossClimateApiClient>();
    private readonly IRawClimateBlobStorage _blobStorage = Substitute.For<IRawClimateBlobStorage>();
    private readonly ILogger<IndoorClimateIngestionService> _logger = Substitute.For<ILogger<IndoorClimateIngestionService>>();

    private IndoorClimateIngestionService CreateSut() =>
        new(_authClient, _climateApiClient, _blobStorage, _logger);

    [Fact]
    public async Task IngestAsync_Success_FetchesTokenThenDevicesThenSavesAccountWideSnapshot()
    {
        _authClient.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Returns("token-123");
        _climateApiClient.GetAllDevicesAsync("token-123", Arg.Any<CancellationToken>()).Returns("{\"devices\":[]}");

        await CreateSut().IngestAsync(CancellationToken.None);

        await _authClient.Received(1).GetAccessTokenAsync(Arg.Any<CancellationToken>());
        await _climateApiClient.Received(1).GetAllDevicesAsync("token-123", Arg.Any<CancellationToken>());
        await _blobStorage.Received(1).SaveRawReadingAsync(null, ClimateReadingType.Indoor, Arg.Any<DateTimeOffset>(), "{\"devices\":[]}", Arg.Any<CancellationToken>());
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
}
