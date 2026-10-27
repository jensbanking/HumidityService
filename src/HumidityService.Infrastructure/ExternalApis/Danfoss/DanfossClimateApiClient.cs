using System.Net.Http.Headers;
using HumidityService.Application.Interfaces;

namespace HumidityService.Infrastructure.ExternalApis.Danfoss;

/// <summary>
/// Fetches the raw device list from the Danfoss Ally devices API. The API scopes devices to the
/// account behind the OAuth token, so a single call returns every device for that account.
/// Transient HTTP failures are retried by a Polly policy configured on the underlying <see cref="HttpClient"/>.
/// </summary>
public sealed class DanfossClimateApiClient : IDanfossClimateApiClient
{
    private readonly HttpClient _httpClient;

    /// <summary>Initializes a new instance of <see cref="DanfossClimateApiClient"/>.</summary>
    public DanfossClimateApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public Task<string> GetAllDevicesAsync(string accessToken, CancellationToken cancellationToken) =>
        GetAsync("ally/devices", accessToken, cancellationToken);

    /// <inheritdoc />
    public Task<string> GetSubDevicesAsync(string accessToken, string gatewayId, CancellationToken cancellationToken) =>
        GetAsync($"ally/devices/{Uri.EscapeDataString(gatewayId)}/sub-devices", accessToken, cancellationToken);

    private async Task<string> GetAsync(string requestUri, string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
