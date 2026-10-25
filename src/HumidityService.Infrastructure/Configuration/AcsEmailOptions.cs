namespace HumidityService.Infrastructure.Configuration;

/// <summary>
/// Settings for sending email through Azure Communication Services, bound from
/// <c>CommunicationServices__Endpoint</c> and <c>HumidityAlert__SenderAddress</c>.
/// </summary>
public sealed class AcsEmailOptions
{
    /// <summary>The ACS resource endpoint, e.g. <c>https://acs-humidity-development.europe.communication.azure.com</c>.</summary>
    public string? Endpoint { get; set; }

    /// <summary>The sender address on the ACS email domain, e.g. <c>DoNotReply@&lt;domain&gt;.azurecomm.net</c>.</summary>
    public string? SenderAddress { get; set; }
}
