using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using HumidityService.Infrastructure.Configuration;
using HumidityService.Infrastructure.Storage;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace HumidityService.Infrastructure.Tests.Storage;

public class BlobHumidityAlertStateStoreTests
{
    private readonly BlobClient _blobClient = Substitute.For<BlobClient>();
    private readonly BlobContainerClient _containerClient = Substitute.For<BlobContainerClient>();
    private readonly BlobServiceClient _blobServiceClient = Substitute.For<BlobServiceClient>();

    public BlobHumidityAlertStateStoreTests()
    {
        _containerClient.GetBlobClient(Arg.Any<string>()).Returns(_blobClient);
        _blobServiceClient.GetBlobContainerClient("alert-state").Returns(_containerClient);
    }

    private BlobHumidityAlertStateStore CreateSut() =>
        new(_blobServiceClient, Options.Create(new ClimateStorageOptions()));

    [Fact]
    public async Task GetLastSentDateAsync_BlobExists_ReturnsStoredDate()
    {
        var result = BlobsModelFactory.BlobDownloadResult(content: BinaryData.FromString("2026-10-26\n"));
        _blobClient.DownloadContentAsync(Arg.Any<CancellationToken>()).Returns(Response.FromValue(result, Substitute.For<Response>()));

        var date = await CreateSut().GetLastSentDateAsync("lyngby", CancellationToken.None);

        Assert.Equal(new DateOnly(2026, 10, 26), date);
        _containerClient.Received().GetBlobClient("humidity-alert/lyngby.txt");
    }

    [Fact]
    public async Task GetLastSentDateAsync_BlobMissing_ReturnsNull()
    {
        _blobClient.DownloadContentAsync(Arg.Any<CancellationToken>())
            .Returns<Task<Response<BlobDownloadResult>>>(_ => throw new RequestFailedException(404, "not found"));

        var date = await CreateSut().GetLastSentDateAsync("lyngby", CancellationToken.None);

        Assert.Null(date);
        await _blobClient.Received(1).DownloadContentAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetLastSentDateAsync_CreatesContainerAndUploadsDate()
    {
        await CreateSut().SetLastSentDateAsync("lyngby", new DateOnly(2026, 10, 26), CancellationToken.None);

        await _containerClient.Received(1).CreateIfNotExistsAsync(cancellationToken: Arg.Any<CancellationToken>());
        await _blobClient.Received(1).UploadAsync(
            Arg.Is<BinaryData>(d => d.ToString() == "2026-10-26"),
            overwrite: true,
            cancellationToken: Arg.Any<CancellationToken>());
    }
}
