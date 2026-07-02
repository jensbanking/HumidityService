namespace HumidityService.Infrastructure.Configuration;

/// <summary>
/// Binds the "DanfossApi" configuration section. Client credentials are supplied via
/// app settings/Key Vault, never committed to source control.
/// </summary>
public sealed class DanfossApiOptions
{
    /// <summary>The configuration section name this options type binds to.</summary>
    public const string SectionName = "DanfossApi";

    /// <summary>Base URL of the Danfoss API, e.g. "https://api.danfoss.com".</summary>
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>OAuth client id used for the client-credentials grant.</summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>OAuth client secret used for the client-credentials grant.</summary>
    public string ClientSecret { get; init; } = string.Empty;
}
