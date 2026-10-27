namespace HumidityService.Application.Interfaces;

/// <summary>
/// Orchestrates a single hourly climate ingestion run: outdoor and indoor readings and the humidity alert.
/// </summary>
public interface IClimateIngestionService
{
    /// <summary>
    /// Fetches the account-wide Danfoss device list once and saves it unmodified (<c>apiraw</c>). Then, for
    /// each configured location: fetches and saves its outdoor reading, saves its gateway's raw
    /// <c>sub-devices</c> response and its own devices extracted from the device list, and runs the
    /// humidity alert check on them together with the outdoor reading. Every blob in the run uses the
    /// same timestamp.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the run.</param>
    /// <exception cref="AggregateException">
    /// One or more locations failed (outdoor fetch or save, gateway missing, sub-devices fetch or save, or
    /// humidity alert). Thrown only after every location has been processed, so a failure never stops the
    /// others, and an outdoor failure never stops the location's indoor ingestion or alert.
    /// </exception>
    Task IngestAsync(CancellationToken cancellationToken);
}
