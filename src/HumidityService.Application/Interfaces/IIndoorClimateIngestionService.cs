namespace HumidityService.Application.Interfaces;

/// <summary>
/// Orchestrates a single indoor climate ingestion run.
/// </summary>
public interface IIndoorClimateIngestionService
{
    /// <summary>
    /// Fetches the account-wide Danfoss device list once and saves it unmodified (<c>apiraw</c>), then,
    /// for each configured location, saves its gateway's raw <c>sub-devices</c> response and its own
    /// devices extracted from the device list, and runs the humidity alert check on them.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the run.</param>
    /// <exception cref="AggregateException">
    /// One or more locations failed (gateway missing, sub-devices fetch or save failed, or humidity alert
    /// failed). Thrown only after every location has been processed, so a failure never stops the others.
    /// </exception>
    Task IngestAsync(CancellationToken cancellationToken);
}
