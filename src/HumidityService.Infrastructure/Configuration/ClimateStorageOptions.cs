namespace HumidityService.Infrastructure.Configuration;

/// <summary>
/// Binds the "ClimateStorage" configuration section that controls where raw climate
/// readings are persisted. Defaults to the local Azurite emulator.
/// </summary>
public sealed class ClimateStorageOptions
{
    /// <summary>The configuration section name this options type binds to.</summary>
    public const string SectionName = "ClimateStorage";

    /// <summary>Blob storage connection string. Defaults to the local Azurite emulator.</summary>
    public string ConnectionString { get; init; } = "UseDevelopmentStorage=true";

    /// <summary>Container that raw indoor/outdoor climate readings are written to.</summary>
    public string ContainerName { get; init; } = "climate-readings";
}
