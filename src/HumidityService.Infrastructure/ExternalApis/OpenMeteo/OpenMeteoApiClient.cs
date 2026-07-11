using HumidityService.Application.Interfaces;

namespace HumidityService.Infrastructure.ExternalApis.OpenMeteo;

/// <summary>
/// Fetches raw outdoor climate data from the Open-Meteo forecast API for a single coordinate pair.
/// Transient HTTP failures are retried by a Polly policy configured on the underlying <see cref="HttpClient"/>.
/// </summary>
public sealed class OpenMeteoApiClient : IOpenMeteoApiClient
{
    private readonly HttpClient _httpClient;

    /// <summary>Initializes a new instance of <see cref="OpenMeteoApiClient"/>.</summary>
    public OpenMeteoApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<string> GetOutdoorClimateAsync(double latitude, double longitude, CancellationToken cancellationToken)
    {
        var requestUri = FormattableString.Invariant(
            $"v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,relative_humidity_2m");

        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
