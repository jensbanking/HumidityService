using System.Globalization;
using System.Text;
using HumidityService.Application.Models;
using HumidityService.Domain.Models;

namespace HumidityService.Application.Services;

/// <summary>
/// Builds the humidity alert email for a location: subject <c>Fugtigheds alarm fra {location}</c>,
/// a heading with the location and Danish date/time, and a table (Rum, Temp, Fugt %) of every room with a humidity sensor, with rooms at or above
/// their limit in bold red.
/// </summary>
public static class HumidityAlertEmailBuilder
{
    // Danish decimal comma without depending on the host's culture data being available.
    private static readonly NumberFormatInfo DanishNumberFormat = new() { NumberDecimalSeparator = "," };

    // Danish short month names, capitalised and without the trailing dot da-DK uses (e.g. "26. Nov 2026").
    private static readonly string[] DanishMonths = ["Jan", "Feb", "Mar", "Apr", "Maj", "Jun", "Jul", "Aug", "Sep", "Okt", "Nov", "Dec"];

    // Inline styles: most email clients ignore <style> blocks.
    private const string TableStyle = "border-collapse:collapse;font-family:Arial,sans-serif;font-size:14px";
    private const string CellStyle = "border:1px solid #cccccc;padding:4px 10px";
    private const string NumberCellStyle = CellStyle + ";text-align:right";
    private const string HeaderStyle = CellStyle + ";background-color:#f0f0f0;text-align:left";
    private const string AboveLimitStyle = ";font-weight:bold;color:#c00000";

    /// <summary>Builds the alert email.</summary>
    /// <param name="locationSlug">The location's slug, used in the subject and body.</param>
    /// <param name="readingTimeLocal">When the readings were checked, in Danish local time (shown in the heading).</param>
    /// <param name="rooms">Every room in the location that has a humidity sensor, in display order.</param>
    /// <param name="recipients">The recipients' email addresses.</param>
    public static EmailMessage Build(string locationSlug, DateTime readingTimeLocal, IReadOnlyList<RoomHumidityStatus> rooms, IReadOnlyList<string> recipients)
    {
        var subject = $"Fugtigheds alarm fra {locationSlug}";
        var heading = $"Fugtighedsmålinger for {locationSlug} {FormatDanishDateTime(readingTimeLocal)}";

        var html = new StringBuilder()
            .Append("<html><body>")
            .Append("<p>").Append(HtmlEscape(heading)).Append("</p>")
            .Append($"<table style=\"{TableStyle}\">")
            .Append($"<tr><th style=\"{HeaderStyle}\">Rum</th><th style=\"{HeaderStyle}\">Temp</th><th style=\"{HeaderStyle}\">Fugt %</th></tr>");

        var nameWidth = Math.Max("Rum".Length, rooms.Select(r => r.Reading.RoomName.Length).DefaultIfEmpty(0).Max());
        var text = new StringBuilder()
            .AppendLine(heading)
            .AppendLine()
            .Append("Rum".PadRight(nameWidth)).Append("  ").Append("Temp".PadLeft(7)).Append("  ").AppendLine("Fugt %".PadLeft(6));

        foreach (var room in rooms)
        {
            var temperature = FormatTemperature(room);
            var humidity = FormatHumidity(room);
            var highlight = room.IsAboveThreshold ? AboveLimitStyle : string.Empty;

            html.Append("<tr>")
                .Append($"<td style=\"{CellStyle}{highlight}\">").Append(HtmlEscape(room.Reading.RoomName)).Append("</td>")
                .Append($"<td style=\"{NumberCellStyle}{highlight}\">").Append(temperature).Append("</td>")
                .Append($"<td style=\"{NumberCellStyle}{highlight}\">").Append(humidity).Append("</td>")
                .Append("</tr>");

            text.Append(room.Reading.RoomName.PadRight(nameWidth)).Append("  ")
                .Append(temperature.PadLeft(7)).Append("  ")
                .Append(humidity.PadLeft(6))
                .AppendLine(room.IsAboveThreshold ? "  (over grænsen)" : string.Empty);
        }

        html.Append("</table>")
            .Append("<p>Rum med rød fed skrift har nået deres grænseværdi.</p>")
            .Append("</body></html>");

        return new EmailMessage(recipients, subject, html.ToString(), text.ToString());
    }

    // Escapes only the HTML-significant characters, leaving æ/ø/å and ° readable in the source.
    private static string HtmlEscape(string value) =>
        value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static string FormatDanishDateTime(DateTime value) =>
        string.Create(CultureInfo.InvariantCulture, $"{value.Day}. {DanishMonths[value.Month - 1]} {value.Year} kl. {value:HH:mm}");

    private static string FormatTemperature(RoomHumidityStatus room) =>
        string.Format(DanishNumberFormat, "{0:0.0} °C", room.Reading.TemperatureCelsius);

    private static string FormatHumidity(RoomHumidityStatus room) =>
        string.Format(DanishNumberFormat, "{0:0.0} %", room.Reading.HumidityPercent);
}
