using HumidityService.Application.Interfaces;
using HumidityService.Application.Models;
using HumidityService.Application.Services;
using HumidityService.Domain.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace HumidityService.Application.Tests.Services;

public class HumidityAlertServiceTests
{
    // 23:30 UTC on 25 Oct 2026 is 00:30 on 26 Oct in Copenhagen (CET, UTC+1 after DST ends).
    private static readonly DateTimeOffset NowUtc = new(2026, 10, 25, 23, 30, 0, TimeSpan.Zero);
    private static readonly DateOnly TodayCopenhagen = new(2026, 10, 26);

    private readonly IHumidityAlertStateStore _stateStore = Substitute.For<IHumidityAlertStateStore>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();
    private readonly ILogger<HumidityAlertService> _logger = Substitute.For<ILogger<HumidityAlertService>>();

    public HumidityAlertServiceTests()
    {
        _timeProvider.GetUtcNow().Returns(NowUtc);
    }

    private HumidityAlertService CreateSut(params string[] recipients) =>
        new(new HumidityAlertSettings(75, recipients.Length == 0 ? ["alerts@example.com"] : recipients), _stateStore, _emailSender, _timeProvider, _logger);

    private static Location CreateLocation(IReadOnlyDictionary<string, double>? thresholds = null) =>
        new("lyngby", "gw-1", 56.864, 9.951, thresholds);

    // humidity/temperature are scaled by 10 as in the real Danfoss response.
    private static string Devices(params (string Name, int Temp, int Humidity)[] rooms) =>
        "{\"result\":["
        + string.Join(",", rooms
            .Select(r => $"{{\"id\":\"{r.Name}\",\"name\":\"{r.Name}\",\"status\":[{{\"code\":\"temp_current\",\"value\":{r.Temp}}},{{\"code\":\"humidity_value\",\"value\":{r.Humidity}}}]}}")
            .Prepend("{\"id\":\"gw-1\",\"name\":\"Gateway\",\"status\":[]}"))
        + "]}";

    // The plain-text body marks rooms at or above their limit; the HTML layout is covered by HumidityAlertEmailBuilderTests.
    private static bool IsMarkedAbove(EmailMessage message, string roomName) =>
        message.PlainTextBody.Split(Environment.NewLine).Single(line => line.StartsWith(roomName + " ")).EndsWith("(over grænsen)");

    [Fact]
    public async Task CheckAndNotifyAsync_AllRoomsBelowLimit_SendsNothing()
    {
        await CreateSut().CheckAndNotifyAsync(CreateLocation(), Devices(("Stue", 214, 582), ("Kælder", 161, 749)), CancellationToken.None);

        await _emailSender.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        await _stateStore.DidNotReceive().GetLastSentDateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckAndNotifyAsync_RoomAtDefaultLimit_SendsEmailWithAllRoomsAndRecordsDate()
    {
        await CreateSut("a@example.com", "b@example.com").CheckAndNotifyAsync(CreateLocation(), Devices(("Stue", 214, 582), ("Kælder", 161, 750)), CancellationToken.None);

        await _emailSender.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m =>
                m.Subject == "Fugtigheds alarm fra lyngby" &&
                m.Recipients.SequenceEqual(new[] { "a@example.com", "b@example.com" }) &&
                IsMarkedAbove(m, "Kælder") &&
                !IsMarkedAbove(m, "Stue")),
            Arg.Any<CancellationToken>());
        await _stateStore.Received(1).SetLastSentDateAsync("lyngby", TodayCopenhagen, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckAndNotifyAsync_RoomOverrideAboveReading_SendsNothing()
    {
        var location = CreateLocation(new Dictionary<string, double> { ["kÆLDER"] = 85 });

        await CreateSut().CheckAndNotifyAsync(location, Devices(("Stue", 214, 582), ("Kælder", 161, 815)), CancellationToken.None);

        await _emailSender.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckAndNotifyAsync_RoomOverrideBelowDefault_SendsEmail()
    {
        var location = CreateLocation(new Dictionary<string, double> { ["Stue"] = 55 });

        await CreateSut().CheckAndNotifyAsync(location, Devices(("Stue", 214, 582)), CancellationToken.None);

        await _emailSender.Received(1).SendAsync(Arg.Is<EmailMessage>(m => IsMarkedAbove(m, "Stue")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckAndNotifyAsync_RoomNameAndOverrideKeyWithStraySpaces_StillMatch()
    {
        var location = CreateLocation(new Dictionary<string, double> { [" Badeværelset  "] = 80 });

        await CreateSut().CheckAndNotifyAsync(location, Devices(("Badeværelset ", 136, 790)), CancellationToken.None);

        await _emailSender.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        _logger.DidNotReceive().Log(LogLevel.Warning, Arg.Any<EventId>(), Arg.Any<object>(), Arg.Any<Exception?>(), Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task CheckAndNotifyAsync_AlreadySentToday_SendsNothing()
    {
        _stateStore.GetLastSentDateAsync("lyngby", Arg.Any<CancellationToken>()).Returns(TodayCopenhagen);

        await CreateSut().CheckAndNotifyAsync(CreateLocation(), Devices(("Kælder", 161, 815)), CancellationToken.None);

        await _emailSender.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        await _stateStore.DidNotReceive().SetLastSentDateAsync(Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckAndNotifyAsync_LastSentYesterday_SendsEmail()
    {
        _stateStore.GetLastSentDateAsync("lyngby", Arg.Any<CancellationToken>()).Returns(TodayCopenhagen.AddDays(-1));

        await CreateSut().CheckAndNotifyAsync(CreateLocation(), Devices(("Kælder", 161, 815)), CancellationToken.None);

        await _emailSender.Received(1).SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        await _stateStore.Received(1).SetLastSentDateAsync("lyngby", TodayCopenhagen, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckAndNotifyAsync_NoRecipients_SendsNothing()
    {
        var sut = new HumidityAlertService(new HumidityAlertSettings(75, []), _stateStore, _emailSender, _timeProvider, _logger);

        await sut.CheckAndNotifyAsync(CreateLocation(), Devices(("Kælder", 161, 815)), CancellationToken.None);

        await _emailSender.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckAndNotifyAsync_NoRoomsWithHumiditySensor_SendsNothing()
    {
        await CreateSut().CheckAndNotifyAsync(CreateLocation(), Devices(), CancellationToken.None);

        await _emailSender.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckAndNotifyAsync_SendFails_ThrowsAndDoesNotRecordDate()
    {
        _emailSender.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("acs down"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateSut().CheckAndNotifyAsync(CreateLocation(), Devices(("Kælder", 161, 815)), CancellationToken.None));

        await _stateStore.DidNotReceive().SetLastSentDateAsync(Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckAndNotifyAsync_OverrideMatchesNoRoom_LogsWarningAndUsesDefaultForOthers()
    {
        var location = CreateLocation(new Dictionary<string, double> { ["Gammelt navn"] = 90 });

        await CreateSut().CheckAndNotifyAsync(location, Devices(("Kælder", 161, 815)), CancellationToken.None);

        _logger.Received().Log(LogLevel.Warning, Arg.Any<EventId>(), Arg.Any<object>(), Arg.Any<Exception?>(), Arg.Any<Func<object, Exception?, string>>());
        await _emailSender.Received(1).SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }
}
