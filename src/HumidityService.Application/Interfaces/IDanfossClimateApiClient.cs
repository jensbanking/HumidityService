namespace HumidityService.Application.Interfaces;

/// <summary>
/// Fetches raw indoor climate data from the Danfoss Ally devices API.
/// </summary>
public interface IDanfossClimateApiClient
{
    /// <summary>
    /// Fetches the raw JSON list of all devices visible to the authenticated Danfoss account.
    /// The Danfoss Ally API scopes devices to the account behind the OAuth token, so no
    /// per-device or per-location identifier is required.
    /// </summary>
    /// <param name="accessToken">A valid OAuth bearer access token.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The unmodified raw JSON response body.</returns>
    Task<string> GetAllDevicesAsync(string accessToken, CancellationToken cancellationToken);

    /// <summary>
    /// Fetches the raw JSON list of the devices paired with a gateway, shaped like the device list
    /// (<c>{"result":[{"id":"...","name":"..."}, ...]}</c>) but without device status.
    /// </summary>
    /// <param name="accessToken">A valid OAuth bearer access token.</param>
    /// <param name="gatewayId">The Danfoss Ally gateway's device id.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The unmodified raw JSON response body.</returns>
    Task<string> GetSubDevicesAsync(string accessToken, string gatewayId, CancellationToken cancellationToken);
}
