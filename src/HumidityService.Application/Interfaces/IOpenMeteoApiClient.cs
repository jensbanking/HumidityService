namespace HumidityService.Application.Interfaces;

/// <summary>
/// Fetches raw outdoor climate data from the Open-Meteo API for a single coordinate pair.
/// </summary>
public interface IOpenMeteoApiClient
{
    /// <summary>
    /// Fetches the raw JSON outdoor climate reading for the given coordinates.
    /// </summary>
    /// <param name="latitude">Latitude of the location to query.</param>
    /// <param name="longitude">Longitude of the location to query.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The unmodified raw JSON response body.</returns>
    Task<string> GetOutdoorClimateAsync(double latitude, double longitude, CancellationToken cancellationToken);
}
