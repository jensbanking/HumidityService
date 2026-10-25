using Azure;
using Azure.Communication.Email;
using HumidityService.Infrastructure.Configuration;
using HumidityService.Infrastructure.Email;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using AppEmailMessage = HumidityService.Application.Models.EmailMessage;

namespace HumidityService.Infrastructure.Tests.Email;

public class AcsEmailSenderTests
{
    private const string Endpoint = "https://acs-test.europe.communication.azure.com";

    private static readonly AppEmailMessage Message = new(["a@example.com", "b@example.com"], "Fugtigheds alarm fra lyngby", "<p>html</p>", "text");

    private readonly EmailClient _client = Substitute.For<EmailClient>();
    private Uri? _clientEndpoint;

    private AcsEmailSender CreateSut(string? endpoint = Endpoint, string? sender = "DoNotReply@test.azurecomm.net") =>
        new(
            Options.Create(new AcsEmailOptions { Endpoint = endpoint, SenderAddress = sender }),
            uri =>
            {
                _clientEndpoint = uri;
                return _client;
            });

    private void GivenSendReturns(params Func<Task<EmailSendOperation>>[] results)
    {
        var calls = 0;
        _client.SendAsync(Arg.Any<WaitUntil>(), Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(_ => results[Math.Min(calls++, results.Length - 1)]());
    }

    [Fact]
    public async Task SendAsync_MapsMessageToAcsAndWaitsOnlyUntilAccepted()
    {
        GivenSendReturns(() => Task.FromResult(Substitute.For<EmailSendOperation>()));

        await CreateSut().SendAsync(Message, CancellationToken.None);

        Assert.Equal(new Uri(Endpoint), _clientEndpoint);
        await _client.Received(1).SendAsync(
            WaitUntil.Started,
            Arg.Is<EmailMessage>(m =>
                m.SenderAddress == "DoNotReply@test.azurecomm.net" &&
                m.Content.Subject == "Fugtigheds alarm fra lyngby" &&
                m.Content.Html == "<p>html</p>" &&
                m.Content.PlainText == "text" &&
                m.Recipients.To.Select(r => r.Address).SequenceEqual(new[] { "a@example.com", "b@example.com" })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_TransientFailure_Retries()
    {
        GivenSendReturns(
            () => throw new RequestFailedException(503, "unavailable"),
            () => Task.FromResult(Substitute.For<EmailSendOperation>()));

        await CreateSut().SendAsync(Message, CancellationToken.None);

        await _client.Received(2).SendAsync(Arg.Any<WaitUntil>(), Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_NonTransientFailure_ThrowsWithoutRetry()
    {
        GivenSendReturns(() => throw new RequestFailedException(400, "bad request"));

        await Assert.ThrowsAsync<RequestFailedException>(() => CreateSut().SendAsync(Message, CancellationToken.None));

        await _client.Received(1).SendAsync(Arg.Any<WaitUntil>(), Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null, "DoNotReply@test.azurecomm.net")]
    [InlineData(Endpoint, null)]
    public async Task SendAsync_MissingConfiguration_Throws(string? endpoint, string? sender)
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSut(endpoint, sender).SendAsync(Message, CancellationToken.None));

        await _client.DidNotReceive().SendAsync(Arg.Any<WaitUntil>(), Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }
}
