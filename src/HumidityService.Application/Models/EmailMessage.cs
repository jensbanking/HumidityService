namespace HumidityService.Application.Models;

/// <summary>
/// An email to send. The sender address is a transport concern and is configured on the sender.
/// </summary>
/// <param name="Recipients">The recipients' email addresses.</param>
/// <param name="Subject">The subject line.</param>
/// <param name="HtmlBody">The HTML body.</param>
/// <param name="PlainTextBody">A plain-text alternative body for clients that don't render HTML.</param>
public sealed record EmailMessage(IReadOnlyList<string> Recipients, string Subject, string HtmlBody, string PlainTextBody);
