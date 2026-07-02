namespace HumidityService.Application.Interfaces;

/// <summary>
/// Obtains OAuth access tokens from the Danfoss API for use against the climate data endpoint.
/// </summary>
public interface IDanfossAuthClient
{
    /// <summary>Requests a fresh OAuth access token using client-credentials.</summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The bearer access token to use for subsequent Danfoss API calls.</returns>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken);
}
