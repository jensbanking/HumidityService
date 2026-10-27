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
    /// <exception cref="AggregateException">
    /// One or more locations failed (gateway missing, save failed or humidity alert failed). Thrown only
    /// after every location has been processed, so a failure never stops the others.
    /// </exception>
    Task IngestAsync(CancellationToken cancellationToken);
}
