using Azure;
using Azure.Core;
using Polly;
using Polly.Retry;

namespace HumidityService.Infrastructure.Resilience;

/// <summary>
/// The Polly retry pipeline shared by the Azure SDK calls (Blob Storage, Communication Services).
/// Only transient failures are retried; anything else (e.g. 400, 403, 404, 409) fails immediately.
/// </summary>
public static class AzureRetryPipeline
{
    /// <summary>Creates a pipeline that retries transient <see cref="RequestFailedException"/>s 3 times with exponential backoff.</summary>
    public static ResiliencePipeline Create() =>
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<RequestFailedException>(IsTransient),
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(1),
            })
            .Build();

    /// <summary>
    /// Turns off the Azure SDK client's own retries, so this pipeline is the only retry layer and a
    /// failing call isn't retried up to 4 x 4 times. Apply it to every client whose calls run through
    /// <see cref="Create"/>.
    /// </summary>
    /// <param name="options">The client's options.</param>
    /// <returns>The same options instance.</returns>
    public static TOptions WithoutSdkRetries<TOptions>(TOptions options)
        where TOptions : ClientOptions
    {
        options.Retry.MaxRetries = 0;
        return options;
    }

    /// <summary>
    /// Whether a failed Azure request is worth retrying: no response at all (status 0, e.g. a network
    /// error), request timeout (408), throttling (429) or a server error (5xx).
    /// </summary>
    /// <param name="exception">The failed request.</param>
    public static bool IsTransient(RequestFailedException exception) =>
        exception.Status is 0 or 408 or 429 or >= 500;
}
