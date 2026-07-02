using System.Text.Json.Serialization;

namespace HumidityService.Infrastructure.ExternalApis.Danfoss;

/// <summary>Deserialization target for the Danfoss OAuth2 token endpoint response.</summary>
internal sealed class DanfossTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; init; }
}
