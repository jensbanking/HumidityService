namespace HumidityService.Application.Interfaces;

/// <summary>
/// Orchestrates a single indoor climate ingestion run.
/// </summary>
public interface IIndoorClimateIngestionService
{
    /// <summary>
    /// Fetches the full device list for the authenticated Danfoss account once, then persists the
    /// raw response for each configured location whose gateway id appears in it. Devices carry no
    /// reference to their gateway, so the whole account response is stored per location.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the run.</param>
    Task IngestAsync(CancellationToken cancellationToken);
}
