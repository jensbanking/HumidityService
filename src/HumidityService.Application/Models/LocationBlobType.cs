namespace HumidityService.Application.Models;

/// <summary>
/// The kinds of blob stored per location and hour, next to the account-wide <c>apiraw</c> blob.
/// </summary>
public enum LocationBlobType
{
    /// <summary>The location's devices, extracted from the account-wide Danfoss device list.</summary>
    Indoor,

    /// <summary>The raw Open-Meteo response for the location's coordinates.</summary>
    Outdoor,

    /// <summary>The raw Danfoss <c>sub-devices</c> response for the location's gateway.</summary>
    SubDevices,
}
