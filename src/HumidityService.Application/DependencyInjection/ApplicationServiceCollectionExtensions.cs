using HumidityService.Application.Interfaces;
using HumidityService.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HumidityService.Application.DependencyInjection;

/// <summary>
/// Registers Application-layer services with the dependency injection container.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>Adds Application-layer orchestration services.</summary>
    public static IServiceCollection AddHumidityApplication(this IServiceCollection services)
    {
        services.AddScoped<IClimateIngestionService, ClimateIngestionService>();
        services.AddScoped<IOutdoorClimateIngestionService, OutdoorClimateIngestionService>();
        services.AddScoped<IHumidityAlertService, HumidityAlertService>();
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
