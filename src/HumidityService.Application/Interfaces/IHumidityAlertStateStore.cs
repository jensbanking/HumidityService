namespace HumidityService.Application.Interfaces;

/// <summary>
/// Persists when a humidity alert email was last sent per location, so the one-email-per-day
/// limit survives restarts and scale-out.
/// </summary>
public interface IHumidityAlertStateStore
{
    /// <summary>Returns the local date an alert was last sent for the location, or <c>null</c> if never.</summary>
    /// <param name="locationSlug">The location's slug.</param>
    /// <param name="cancellationToken">Token used to cancel the read.</param>
    Task<DateOnly?> GetLastSentDateAsync(string locationSlug, CancellationToken cancellationToken);

    /// <summary>Records the local date an alert was sent for the location.</summary>
    /// <param name="locationSlug">The location's slug.</param>
    /// <param name="date">The local date the alert was sent.</param>
    /// <param name="cancellationToken">Token used to cancel the write.</param>
    Task SetLastSentDateAsync(string locationSlug, DateOnly date, CancellationToken cancellationToken);
}
