using System.Text.RegularExpressions;
using HumidityService.Application.Models;
using HumidityService.Application.Services;
using HumidityService.Domain.Models;
using Xunit;

namespace HumidityService.Application.Tests.Services;

public class HumidityAlertEmailBuilderTests
{
    private static readonly DateTime ReadingTime = new(2026, 10, 26, 0, 0, 0);

    private static readonly RoomHumidityStatus[] Rooms =
    [
        new(new RoomClimateReading("Kælder", 16.1, 81.5), 75),
        new(new RoomClimateReading("Stue", 21.4, 58.2), 75),
    ];

    // Row cells as (style, text) pairs, ignoring the exact inline styling.
    private static List<(string Style, string Text)> Cells(string html, string firstCellText)
    {
        var row = Regex.Matches(html, "<tr>(.*?)</tr>").Select(m => m.Groups[1].Value)
            .Single(r => r.Contains($">{firstCellText}</td>"));
        return Regex.Matches(row, "<td style=\"([^\"]*)\">(.*?)</td>").Select(m => (m.Groups[1].Value, m.Groups[2].Value)).ToList();
    }

    [Fact]
    public void Build_SetsSubjectAndRecipients()
    {
        var message = HumidityAlertEmailBuilder.Build("lyngby", ReadingTime, Rooms, ["a@example.com"]);

        Assert.Equal("Fugtigheds alarm fra lyngby", message.Subject);
        Assert.Equal(new[] { "a@example.com" }, message.Recipients);
    }

    [Fact]
    public void Build_HeadingHasLocationAndDanishDateTime()
    {
        var message = HumidityAlertEmailBuilder.Build("lyngby", ReadingTime, Rooms, ["a@example.com"]);

        Assert.Contains("<p>Fugtighedsmålinger for lyngby 26. Okt 2026 kl. 00:00</p>", message.HtmlBody);
        Assert.StartsWith("Fugtighedsmålinger for lyngby 26. Okt 2026 kl. 00:00" + Environment.NewLine, message.PlainTextBody);
    }

    [Theory]
    [InlineData(2026, 1, 5, 7, 3, "5. Jan 2026 kl. 07:03")]
    [InlineData(2026, 5, 17, 14, 0, "17. Maj 2026 kl. 14:00")]
    [InlineData(2026, 11, 26, 23, 59, "26. Nov 2026 kl. 23:59")]
    public void Build_HeadingUsesDanishMonthNamesWithoutLeadingZeroDay(int year, int month, int day, int hour, int minute, string expected)
    {
        var message = HumidityAlertEmailBuilder.Build("lyngby", new DateTime(year, month, day, hour, minute, 0), Rooms, ["a@example.com"]);

        Assert.StartsWith($"Fugtighedsmålinger for lyngby {expected}{Environment.NewLine}", message.PlainTextBody);
    }

    [Fact]
    public void Build_HtmlIsTableWithRumTempFugtHeaders()
    {
        var message = HumidityAlertEmailBuilder.Build("lyngby", ReadingTime, Rooms, ["a@example.com"]);

        var headers = Regex.Matches(message.HtmlBody, "<th[^>]*>(.*?)</th>").Select(m => m.Groups[1].Value);
        Assert.Equal(new[] { "Rum", "Temp", "Fugt %" }, headers);
        Assert.DoesNotContain("<li>", message.HtmlBody);
    }

    [Fact]
    public void Build_HtmlHasOneRowPerRoomWithFormattedValues()
    {
        var message = HumidityAlertEmailBuilder.Build("lyngby", ReadingTime, Rooms, ["a@example.com"]);

        Assert.Equal(new[] { "Kælder", "16,1 °C", "81,5 %" }, Cells(message.HtmlBody, "Kælder").Select(c => c.Text));
        Assert.Equal(new[] { "Stue", "21,4 °C", "58,2 %" }, Cells(message.HtmlBody, "Stue").Select(c => c.Text));
    }

    [Fact]
    public void Build_HtmlBoldsAndColoursRedEveryCellOfRoomsAboveLimitOnly()
    {
        var message = HumidityAlertEmailBuilder.Build("lyngby", ReadingTime, Rooms, ["a@example.com"]);

        Assert.All(Cells(message.HtmlBody, "Kælder"), c =>
        {
            Assert.Contains("font-weight:bold", c.Style);
            Assert.Contains("color:#c00000", c.Style);
        });
        Assert.All(Cells(message.HtmlBody, "Stue"), c =>
        {
            Assert.DoesNotContain("font-weight:bold", c.Style);
            Assert.DoesNotContain("color:", c.Style);
        });
    }

    [Fact]
    public void Build_PlainTextIsAlignedTableMarkingRoomsAboveLimit()
    {
        var message = HumidityAlertEmailBuilder.Build("lyngby", ReadingTime, Rooms, ["a@example.com"]);
        var lines = message.PlainTextBody.Split(Environment.NewLine);

        Assert.Contains("Rum        Temp  Fugt %", lines);
        Assert.Contains("Kælder  16,1 °C  81,5 %  (over grænsen)", lines);
        Assert.Contains("Stue    21,4 °C  58,2 %", lines);
    }

    [Fact]
    public void Build_HtmlEncodesRoomNames()
    {
        RoomHumidityStatus[] rooms = [new(new RoomClimateReading("<Bad & Toilet>", 22, 60), 75)];

        var message = HumidityAlertEmailBuilder.Build("lyngby", ReadingTime, rooms, ["a@example.com"]);

        Assert.Contains("&lt;Bad &amp; Toilet&gt;", message.HtmlBody);
        Assert.DoesNotContain("<Bad", message.HtmlBody);
    }
}
