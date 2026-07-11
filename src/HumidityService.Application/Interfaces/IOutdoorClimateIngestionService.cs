namespace HumidityService.Application.Interfaces;

/// <summary>
/// Orchestrates a single outdoor climate ingestion run across all configured locations.
/// </summary>
public interface IOutdoorClimateIngestionService
{
    /// <summary>
    /// Fetches and persists an outdoor climate reading for every configured location.
    /// Each location is processed independently, so a failure fetching or saving one
    /// location's reading does not prevent the others from being ingested in the same run.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the run.</param>
    Task IngestAsync(CancellationToken cancellationToken);
}
