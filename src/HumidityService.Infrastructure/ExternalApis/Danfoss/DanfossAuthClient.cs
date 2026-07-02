using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using HumidityService.Application.Interfaces;
using HumidityService.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace HumidityService.Infrastructure.ExternalApis.Danfoss;

/// <summary>
/// Requests OAuth access tokens from the Danfoss API using the client-credentials grant.
/// Transient HTTP failures are retried by a Polly policy configured on the underlying <see cref="HttpClient"/>.
/// </summary>
public sealed class DanfossAuthClient : IDanfossAuthClient
{
    private readonly HttpClient _httpClient;
    private readonly DanfossApiOptions _options;

    /// <summary>Initializes a new instance of <see cref="DanfossAuthClient"/>.</summary>
    /// <exception cref="InvalidOperationException">The Danfoss client id/secret are not configured.</exception>
    public DanfossAuthClient(HttpClient httpClient, IOptions<DanfossApiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.ClientId) || string.IsNullOrWhiteSpace(_options.ClientSecret))
        {
            throw new InvalidOperationException("Danfoss API client credentials are not configured. Set 'DanfossApi:ClientId' and 'DanfossApi:ClientSecret'.");
        }
    }

    /// <inheritdoc />
    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));

        using var request = new HttpRequestMessage(HttpMethod.Post, "oauth2/token")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Basic", credentials) },
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
            }),
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<DanfossTokenResponse>(cancellationToken);
        if (payload is null || string.IsNullOrWhiteSpace(payload.AccessToken))
        {
            throw new InvalidOperationException("Danfoss OAuth token response did not contain an access token.");
        }

        return payload.AccessToken;
    }
}
