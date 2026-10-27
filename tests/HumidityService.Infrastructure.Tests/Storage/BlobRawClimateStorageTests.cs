using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using HumidityService.Domain.Models;
using HumidityService.Infrastructure.Configuration;
using HumidityService.Infrastructure.Storage;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace HumidityService.Infrastructure.Tests.Storage;

public class BlobRawClimateStorageTests
{
    [Fact]
    public async Task SaveRawReadingAsync_UploadsToExpectedContainerAndBlobName()
    {
        var blobClient = Substitute.For<BlobClient>();
        var containerClient = Substitute.For<BlobContainerClient>();
        containerClient.GetBlobClient(Arg.Any<string>()).Returns(blobClient);

        var blobServiceClient = Substitute.For<BlobServiceClient>();
        blobServiceClient.GetBlobContainerClient("climate-readings").Returns(containerClient);

        var options = Options.Create(new ClimateStorageOptions { ContainerName = "climate-readings" });
        var sut = new BlobRawClimateStorage(blobServiceClient, options);
        var timestamp = new DateTimeOffset(2026, 7, 2, 14, 0, 0, TimeSpan.Zero);

        await sut.SaveRawReadingAsync("aarhus-house1", ClimateReadingType.Indoor, timestamp, "{\"humidity\":42}", CancellationToken.None);

        blobServiceClient.Received(1).GetBlobContainerClient("climate-readings");
        containerClient.Received(1).GetBlobClient("aarhus-house1/2026/07/02/aarhus-house1_indoor_2026_07_02_14.json");
        await containerClient.Received(1).CreateIfNotExistsAsync(cancellationToken: Arg.Any<CancellationToken>());
        await blobClient.Received(1).UploadAsync(Arg.Any<Stream>(), overwrite: true, cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveRawReadingAsync_NonTransientFailure_ThrowsWithoutRetry()
    {
        var (sut, blobClient) = CreateSutWithUpload(() => throw new RequestFailedException(403, "forbidden"));

        await Assert.ThrowsAsync<RequestFailedException>(() =>
            sut.SaveRawReadingAsync("aarhus-house1", ClimateReadingType.Indoor, DateTimeOffset.UtcNow, "{}", CancellationToken.None));

        await blobClient.Received(1).UploadAsync(Arg.Any<Stream>(), overwrite: true, cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveRawReadingAsync_TransientFailure_Retries()
    {
        var calls = 0;
        var (sut, blobClient) = CreateSutWithUpload(() =>
            ++calls == 1 ? throw new RequestFailedException(503, "unavailable") : Substitute.For<Response<BlobContentInfo>>());

        await sut.SaveRawReadingAsync("aarhus-house1", ClimateReadingType.Indoor, DateTimeOffset.UtcNow, "{}", CancellationToken.None);

        await blobClient.Received(2).UploadAsync(Arg.Any<Stream>(), overwrite: true, cancellationToken: Arg.Any<CancellationToken>());
    }

    private static (BlobRawClimateStorage Sut, BlobClient BlobClient) CreateSutWithUpload(Func<Response<BlobContentInfo>> upload)
    {
        var blobClient = Substitute.For<BlobClient>();
        blobClient.UploadAsync(Arg.Any<Stream>(), overwrite: true, cancellationToken: Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(upload()));
        var containerClient = Substitute.For<BlobContainerClient>();
        containerClient.GetBlobClient(Arg.Any<string>()).Returns(blobClient);
        var blobServiceClient = Substitute.For<BlobServiceClient>();
        blobServiceClient.GetBlobContainerClient(Arg.Any<string>()).Returns(containerClient);

        return (new BlobRawClimateStorage(blobServiceClient, Options.Create(new ClimateStorageOptions())), blobClient);
    }
}
