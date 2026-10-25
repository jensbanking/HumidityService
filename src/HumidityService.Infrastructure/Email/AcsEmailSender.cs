using Azure;
using Azure.Communication.Email;
using HumidityService.Application.Interfaces;
using EmailMessage = HumidityService.Application.Models.EmailMessage;
using HumidityService.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace HumidityService.Infrastructure.Email;

/// <summary>
/// Sends email through Azure Communication Services Email. The <see cref="EmailClient"/> is created
/// on first use, so a missing endpoint only fails the send (and is logged by the caller) rather than
/// the whole ingestion pipeline at startup. Sends are retried with a Polly resilience pipeline on
/// transient ACS failures.
/// </summary>
public sealed class AcsEmailSender : IEmailSender
{
    private readonly AcsEmailOptions _options;
    private readonly Lazy<EmailClient> _client;
    private readonly ResiliencePipeline _retryPipeline;

    /// <summary>Initializes a new instance of <see cref="AcsEmailSender"/>.</summary>
    /// <param name="options">The ACS endpoint and sender address.</param>
    /// <param name="clientFactory">Creates the <see cref="EmailClient"/> for the configured endpoint.</param>
    public AcsEmailSender(IOptions<AcsEmailOptions> options, Func<Uri, EmailClient> clientFactory)
    {
        _options = options.Value;
        _client = new Lazy<EmailClient>(() =>
        {
            if (string.IsNullOrWhiteSpace(_options.Endpoint))
            {
                throw new InvalidOperationException("Configuration value 'CommunicationServices:Endpoint' is missing; cannot send email.");
            }

            return clientFactory(new Uri(_options.Endpoint));
        });
        _retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<RequestFailedException>(ex => ex.Status is 0 or 408 or 429 or >= 500),
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(1),
            })
            .Build();
    }

    /// <inheritdoc />
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.SenderAddress))
        {
            throw new InvalidOperationException("Configuration value 'HumidityAlert:SenderAddress' is missing; cannot send email.");
        }

        var content = new EmailContent(message.Subject)
        {
            Html = message.HtmlBody,
            PlainText = message.PlainTextBody,
        };
        var recipients = new EmailRecipients(message.Recipients.Select(address => new EmailAddress(address)));
        var acsMessage = new Azure.Communication.Email.EmailMessage(_options.SenderAddress, recipients, content);

        // WaitUntil.Started: return once ACS has accepted the message rather than polling until
        // delivery, so a retry never re-sends an email ACS already accepted.
        await _retryPipeline.ExecuteAsync(
            async ct => await _client.Value.SendAsync(WaitUntil.Started, acsMessage, ct),
            cancellationToken);
    }
}
