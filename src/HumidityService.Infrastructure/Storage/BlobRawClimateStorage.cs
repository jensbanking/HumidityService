using System.Text;
using Azure.Storage.Blobs;
using HumidityService.Application.Interfaces;
using HumidityService.Application.Models;
using HumidityService.Application.Services;
using HumidityService.Infrastructure.Configuration;
using HumidityService.Infrastructure.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace HumidityService.Infrastructure.Storage;

/// <summary>
/// Persists an hour's climate data to Azure Blob Storage (or the local Azurite emulator),
/// using the <see cref="ClimateBlobPathBuilder"/> paths. Uploads are
/// retried on transient storage failures (see <see cref="AzureRetryPipeline"/>).
/// </summary>
public sealed class BlobRawClimateStorage : IRawClimateBlobStorage
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ClimateStorageOptions _options;
    private readonly ResiliencePipeline _retryPipeline;

    /// <summary>Initializes a new instance of <see cref="BlobRawClimateStorage"/>.</summary>
    public BlobRawClimateStorage(BlobServiceClient blobServiceClient, IOptions<ClimateStorageOptions> options)
    {
        _blobServiceClient = blobServiceClient;
        _options = options.Value;
        _retryPipeline = AzureRetryPipeline.Create();
    }

    /// <inheritdoc />
    public Task SaveApiRawAsync(DateTimeOffset timestampUtc, string rawJson, CancellationToken cancellationToken) =>
        UploadAsync(ClimateBlobPathBuilder.BuildApiRaw(timestampUtc), rawJson, cancellationToken);

    /// <inheritdoc />
    public Task SaveLocationBlobAsync(string locationSlug, LocationBlobType blobType, DateTimeOffset timestampUtc, string json, CancellationToken cancellationToken) =>
        UploadAsync(ClimateBlobPathBuilder.Build(locationSlug, blobType, timestampUtc), json, cancellationToken);

    private async Task UploadAsync(string blobName, string json, CancellationToken cancellationToken)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        await _retryPipeline.ExecuteAsync(
            async ct =>
            {
                await containerClient.CreateIfNotExistsAsync(cancellationToken: ct);

                using var content = new MemoryStream(Encoding.UTF8.GetBytes(json));
                await blobClient.UploadAsync(content, overwrite: true, cancellationToken: ct);
            },
            cancellationToken);
    }
}
