using System.Text;
using Azure.Storage.Blobs;
using HumidityService.Application.Interfaces;
using HumidityService.Application.Services;
using HumidityService.Domain.Models;
using HumidityService.Infrastructure.Configuration;
using HumidityService.Infrastructure.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace HumidityService.Infrastructure.Storage;

/// <summary>
/// Persists raw climate readings to Azure Blob Storage (or the local Azurite emulator),
/// using the standard <see cref="ClimateBlobPathBuilder"/> naming convention. Uploads are
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
    public async Task SaveRawReadingAsync(string locationSlug, ClimateReadingType readingType, DateTimeOffset timestampUtc, string rawJson, CancellationToken cancellationToken)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_options.ContainerName);
        var blobName = ClimateBlobPathBuilder.Build(locationSlug, readingType, timestampUtc);
        var blobClient = containerClient.GetBlobClient(blobName);

        await _retryPipeline.ExecuteAsync(
            async ct =>
            {
                await containerClient.CreateIfNotExistsAsync(cancellationToken: ct);

                using var content = new MemoryStream(Encoding.UTF8.GetBytes(rawJson));
                await blobClient.UploadAsync(content, overwrite: true, cancellationToken: ct);
            },
            cancellationToken);
    }
}
