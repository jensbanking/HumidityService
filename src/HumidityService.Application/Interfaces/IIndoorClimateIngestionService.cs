namespace HumidityService.Application.Interfaces;

/// <summary>
/// Orchestrates a single indoor climate ingestion run.
/// </summary>
public interface IIndoorClimateIngestionService
{
    /// <summary>
    /// Fetches the full device list for the authenticated Danfoss account and persists it as a
    /// single raw JSON snapshot. The Danfoss Ally API scopes devices to the account, so this is
    /// not iterated per configured location.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the run.</param>
    Task IngestAsync(CancellationToken cancellationToken);
}
