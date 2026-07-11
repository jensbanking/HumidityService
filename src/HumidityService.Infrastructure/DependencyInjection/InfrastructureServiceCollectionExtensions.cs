using Azure.Storage.Blobs;
using HumidityService.Application.Interfaces;
using HumidityService.Infrastructure.Configuration;
using HumidityService.Infrastructure.ExternalApis.Danfoss;
using HumidityService.Infrastructure.ExternalApis.OpenMeteo;
using HumidityService.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace HumidityService.Infrastructure.DependencyInjection;

/// <summary>
/// Registers Infrastructure-layer services (external API clients, blob storage, configuration) with the DI container.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>Adds Infrastructure-layer services required for climate data ingestion.</summary>
    public static IServiceCollection AddHumidityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DanfossApiOptions>(configuration.GetSection(DanfossApiOptions.SectionName));
        services.Configure<OpenMeteoApiOptions>(configuration.GetSection(OpenMeteoApiOptions.SectionName));
        services.Configure<ClimateStorageOptions>(configuration.GetSection(ClimateStorageOptions.SectionName));

        services.AddSingleton<ILocationProvider>(sp => new ConfigurationLocationProvider(sp.GetRequiredService<IConfiguration>()));

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ClimateStorageOptions>>().Value;
            return new BlobServiceClient(options.ConnectionString);
        });
        services.AddSingleton<IRawClimateBlobStorage, BlobRawClimateStorage>();

        services.AddHttpClient<IDanfossAuthClient, DanfossAuthClient>((sp, client) =>
            {
                client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<DanfossApiOptions>>().Value.BaseUrl);
            })
            .AddPolicyHandler(GetRetryPolicy());

        services.AddHttpClient<IDanfossClimateApiClient, DanfossClimateApiClient>((sp, client) =>
            {
                client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<DanfossApiOptions>>().Value.BaseUrl);
            })
            .AddPolicyHandler(GetRetryPolicy());

        services.AddHttpClient<IOpenMeteoApiClient, OpenMeteoApiClient>((sp, client) =>
            {
                client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<OpenMeteoApiOptions>>().Value.BaseUrl);
            })
            .AddPolicyHandler(GetRetryPolicy());

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}
