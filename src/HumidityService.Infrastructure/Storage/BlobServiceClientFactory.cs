using Azure.Core;
using Azure.Storage.Blobs;
using HumidityService.Infrastructure.Configuration;
using HumidityService.Infrastructure.Resilience;

namespace HumidityService.Infrastructure.Storage;

/// <summary>
/// Creates the <see cref="BlobServiceClient"/> for the configured storage: Entra ID auth against
/// <see cref="ClimateStorageOptions.ServiceUri"/> when set, otherwise the connection string
/// (the local Azurite emulator by default).
/// </summary>
public static class BlobServiceClientFactory
{
    /// <summary>Creates the client. SDK retries are off; callers retry with <see cref="AzureRetryPipeline"/>.</summary>
    /// <param name="options">The storage settings.</param>
    /// <param name="credential">The credential used with <see cref="ClimateStorageOptions.ServiceUri"/>.</param>
    public static BlobServiceClient Create(ClimateStorageOptions options, TokenCredential credential)
    {
        var clientOptions = AzureRetryPipeline.WithoutSdkRetries(new BlobClientOptions());

        return string.IsNullOrWhiteSpace(options.ServiceUri)
            ? new BlobServiceClient(options.ConnectionString, clientOptions)
            : new BlobServiceClient(new Uri(options.ServiceUri), credential, clientOptions);
    }
}
