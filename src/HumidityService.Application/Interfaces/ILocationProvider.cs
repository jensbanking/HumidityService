using HumidityService.Domain.Models;

namespace HumidityService.Application.Interfaces;

/// <summary>
/// Supplies the statically-configured set of locations that indoor and outdoor
/// ingestion iterate over. Implementations resolve locations from configuration,
/// never from runtime discovery against an external API.
/// </summary>
public interface ILocationProvider
{
    /// <summary>Gets the full set of configured locations for the current environment.</summary>
    IReadOnlyList<Location> GetLocations();
}
