using HumidityService.Application.Models;
using HumidityService.Application.Services;
using HumidityService.Domain.Models;
using Xunit;

namespace HumidityService.Application.Tests.Services;

public class HumidityAlertEmailBuilderTests
{
    private static readonly RoomHumidityStatus[] Rooms =
    [
        new(new RoomClimateReading("Kælder", 16.1, 81.5), 75),
        new(new RoomClimateReading("Stue", 21.4, 58.2), 75),
    ];

    [Fact]
    public void Build_SetsSubjectAndRecipients()
    {
        var message = HumidityAlertEmailBuilder.Build("lyngby", Rooms, ["a@example.com"]);

        Assert.Equal("Fugtigheds alarm fra lyngby", message.Subject);
        Assert.Equal(new[] { "a@example.com" }, message.Recipients);
    }

    [Fact]
    public void Build_HtmlListsEveryRoomAndBoldsRoomsAboveLimit()
    {
        var message = HumidityAlertEmailBuilder.Build("lyngby", Rooms, ["a@example.com"]);

        Assert.Contains("<li><strong>Kælder: 16,1 °C, 81,5 %</strong></li>", message.HtmlBody);
        Assert.Contains("<li>Stue: 21,4 °C, 58,2 %</li>", message.HtmlBody);
    }

    [Fact]
    public void Build_PlainTextMarksRoomsAboveLimit()
    {
        var message = HumidityAlertEmailBuilder.Build("lyngby", Rooms, ["a@example.com"]);

        Assert.Contains("- Kælder: 16,1 °C, 81,5 % (over grænsen)", message.PlainTextBody);
        Assert.Contains("- Stue: 21,4 °C, 58,2 %" + Environment.NewLine, message.PlainTextBody);
    }

    [Fact]
    public void Build_HtmlEncodesRoomNames()
    {
        RoomHumidityStatus[] rooms = [new(new RoomClimateReading("<Bad & Toilet>", 22, 60), 75)];

        var message = HumidityAlertEmailBuilder.Build("lyngby", rooms, ["a@example.com"]);

        Assert.Contains("&lt;Bad &amp; Toilet&gt;", message.HtmlBody);
        Assert.DoesNotContain("<Bad", message.HtmlBody);
    }
}
