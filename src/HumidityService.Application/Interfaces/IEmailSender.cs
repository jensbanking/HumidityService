using HumidityService.Application.Models;

namespace HumidityService.Application.Interfaces;

/// <summary>
/// Sends email notifications.
/// </summary>
public interface IEmailSender
{
    /// <summary>Submits an email for delivery.</summary>
    /// <param name="message">The email to send.</param>
    /// <param name="cancellationToken">Token used to cancel the send.</param>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
