using HumidityService.Application.Interfaces;
using HumidityService.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HumidityService.Application.DependencyInjection;

/// <summary>
/// Registers Application-layer services with the dependency injection container.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Adds Application-layer orchestration services.</summary>
    public static IServiceCollection AddHumidityApplication(this IServiceCollection services)
    {
        services.AddScoped<IIndoorClimateIngestionService, IndoorClimateIngestionService>();
        services.AddScoped<IOutdoorClimateIngestionService, OutdoorClimateIngestionService>();

        return services;
    }
}
