using System.Globalization;
using Azure;
using Azure.Storage.Blobs;
using HumidityService.Application.Interfaces;
using HumidityService.Infrastructure.Configuration;
using HumidityService.Infrastructure.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace HumidityService.Infrastructure.Storage;

/// <summary>
/// Stores the date a humidity alert was last sent per location as a small blob,
/// <c>humidity-alert/{location}.txt</c> containing <c>yyyy-MM-dd</c>, in its own container so it
/// never mixes with the raw climate readings. Storage calls are retried on transient failures
/// (see <see cref="AzureRetryPipeline"/>).
/// </summary>
public sealed class BlobHumidityAlertStateStore : IHumidityAlertStateStore
{
    private const string DateFormat = "yyyy-MM-dd";

    private readonly BlobServiceClient _blobServiceClient;
    private readonly ClimateStorageOptions _options;
    private readonly ResiliencePipeline _retryPipeline;

    /// <summary>Initializes a new instance of <see cref="BlobHumidityAlertStateStore"/>.</summary>
    public BlobHumidityAlertStateStore(BlobServiceClient blobServiceClient, IOptions<ClimateStorageOptions> options)
    {
        _blobServiceClient = blobServiceClient;
        _options = options.Value;
        _retryPipeline = AzureRetryPipeline.Create();
    }

    /// <inheritdoc />
    public async Task<DateOnly?> GetLastSentDateAsync(string locationSlug, CancellationToken cancellationToken)
    {
        var blobClient = GetBlobClient(locationSlug);

        return await _retryPipeline.ExecuteAsync<DateOnly?>(
            async ct =>
            {
                try
                {
                    var result = await blobClient.DownloadContentAsync(ct);
                    var text = result.Value.Content.ToString().Trim();
                    return DateOnly.ParseExact(text, DateFormat, CultureInfo.InvariantCulture);
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                    // No alert has been sent for this location yet (blob or container missing).
                    return null;
                }
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetLastSentDateAsync(string locationSlug, DateOnly date, CancellationToken cancellationToken)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_options.AlertStateContainerName);
        var blobClient = GetBlobClient(locationSlug);

        await _retryPipeline.ExecuteAsync(
            async ct =>
            {
                await containerClient.CreateIfNotExistsAsync(cancellationToken: ct);

                var content = BinaryData.FromString(date.ToString(DateFormat, CultureInfo.InvariantCulture));
                await blobClient.UploadAsync(content, overwrite: true, cancellationToken: ct);
            },
            cancellationToken);
    }

    private BlobClient GetBlobClient(string locationSlug) =>
        _blobServiceClient
            .GetBlobContainerClient(_options.AlertStateContainerName)
            .GetBlobClient($"humidity-alert/{locationSlug}.txt");
}
