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
    /// <returns>The current outdoor reading.</returns>
    /// <exception cref="Exception">
    /// The fetch or the save failed, or the response is malformed or lacks the temperature or humidity
    /// (<see cref="InvalidDataException"/>; the raw response is saved first).
    /// </exception>
    Task<OutdoorClimateReading> IngestAsync(Location location, DateTimeOffset timestampUtc, CancellationToken cancellationToken);
}
