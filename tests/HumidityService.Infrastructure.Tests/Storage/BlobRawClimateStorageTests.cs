using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using HumidityService.Application.Models;
using HumidityService.Infrastructure.Configuration;
using HumidityService.Infrastructure.Storage;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace HumidityService.Infrastructure.Tests.Storage;

public class BlobRawClimateStorageTests
{
    [Fact]
    public async Task SaveLocationBlobAsync_UploadsToExpectedContainerAndBlobName()
    {
        var (sut, blobServiceClient, containerClient, blobClient) = CreateSut();

        await sut.SaveLocationBlobAsync("lyngby", LocationBlobType.Indoor, Timestamp, "{\"result\":[]}", CancellationToken.None);

        blobServiceClient.Received(1).GetBlobContainerClient("climate-readings");
        containerClient.Received(1).GetBlobClient("2026/07/02/14/lyngby/indoor_lyngby_2026_07_02_14.json");
        await containerClient.Received(1).CreateIfNotExistsAsync(cancellationToken: Arg.Any<CancellationToken>());
        await blobClient.Received(1).UploadAsync(Arg.Any<Stream>(), overwrite: true, cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveApiRawAsync_UploadsToHourFolder()
    {
        var (sut, _, containerClient, blobClient) = CreateSut();

        await sut.SaveApiRawAsync(Timestamp, "{\"result\":[]}", CancellationToken.None);

        containerClient.Received(1).GetBlobClient("2026/07/02/14/apiraw_2026_07_02_14.json");
        await blobClient.Received(1).UploadAsync(Arg.Any<Stream>(), overwrite: true, cancellationToken: Arg.Any<CancellationToken>());
    }

    private static readonly DateTimeOffset Timestamp = new(2026, 7, 2, 14, 0, 0, TimeSpan.Zero);

    private static (BlobRawClimateStorage Sut, BlobServiceClient BlobServiceClient, BlobContainerClient ContainerClient, BlobClient BlobClient) CreateSut()
    {
        var blobClient = Substitute.For<BlobClient>();
        var containerClient = Substitute.For<BlobContainerClient>();
        containerClient.GetBlobClient(Arg.Any<string>()).Returns(blobClient);
        var blobServiceClient = Substitute.For<BlobServiceClient>();
        blobServiceClient.GetBlobContainerClient("climate-readings").Returns(containerClient);
        var options = Options.Create(new ClimateStorageOptions { ContainerName = "climate-readings" });

        return (new BlobRawClimateStorage(blobServiceClient, options), blobServiceClient, containerClient, blobClient);
    }

    [Fact]
    public async Task SaveLocationBlobAsync_NonTransientFailure_ThrowsWithoutRetry()
    {
        var (sut, blobClient) = CreateSutWithUpload(() => throw new RequestFailedException(403, "forbidden"));

        await Assert.ThrowsAsync<RequestFailedException>(() =>
            sut.SaveLocationBlobAsync("aarhus-house1", LocationBlobType.Indoor, DateTimeOffset.UtcNow, "{}", CancellationToken.None));

        await blobClient.Received(1).UploadAsync(Arg.Any<Stream>(), overwrite: true, cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveLocationBlobAsync_TransientFailure_Retries()
    {
        var calls = 0;
        var (sut, blobClient) = CreateSutWithUpload(() =>
            ++calls == 1 ? throw new RequestFailedException(503, "unavailable") : Substitute.For<Response<BlobContentInfo>>());

        await sut.SaveLocationBlobAsync("aarhus-house1", LocationBlobType.Indoor, DateTimeOffset.UtcNow, "{}", CancellationToken.None);

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
