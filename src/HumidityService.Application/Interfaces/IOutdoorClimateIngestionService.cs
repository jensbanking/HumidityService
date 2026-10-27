using HumidityService.Domain.Models;

namespace HumidityService.Application.Interfaces;

/// <summary>
/// Ingests the outdoor climate reading for a single location: one step of the hourly climate ingestion run.
/// </summary>
public interface IOutdoorClimateIngestionService
{
    /// <summary>
    /// Fetches the location's outdoor reading from Open-Meteo, persists the raw response and returns
    /// the current values read from it.
    /// </summary>
    /// <param name="location">The location to fetch the reading for.</param>
    /// <param name="timestampUtc">The run's timestamp, which decides the blob's hourly folder.</param>
    /// <param name="cancellationToken">Token used to cancel the step.</param>
    /// <returns>
    /// The current outdoor reading, or <see langword="null"/> when the response lacks the temperature or humidity.
    /// </returns>
    /// <exception cref="Exception">The fetch, the save or parsing the response failed.</exception>
    Task<OutdoorClimateReading?> IngestAsync(Location location, DateTimeOffset timestampUtc, CancellationToken cancellationToken);
}
