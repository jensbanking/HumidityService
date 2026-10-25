using System.Globalization;
using System.Text;
using HumidityService.Application.Models;

namespace HumidityService.Application.Services;

/// <summary>
/// Builds the humidity alert email for a location: subject <c>Fugtigheds alarm fra {location}</c>
/// and a list of every room with a humidity sensor, with rooms at or above their limit in bold.
/// </summary>
public static class HumidityAlertEmailBuilder
{
    // Danish decimal comma without depending on the host's culture data being available.
    private static readonly NumberFormatInfo DanishNumberFormat = new() { NumberDecimalSeparator = "," };

    /// <summary>Builds the alert email.</summary>
    /// <param name="locationSlug">The location's slug, used in the subject and body.</param>
    /// <param name="rooms">Every room in the location that has a humidity sensor, in display order.</param>
    /// <param name="recipients">The recipients' email addresses.</param>
    public static EmailMessage Build(string locationSlug, IReadOnlyList<RoomHumidityStatus> rooms, IReadOnlyList<string> recipients)
    {
        var subject = $"Fugtigheds alarm fra {locationSlug}";

        var html = new StringBuilder()
            .Append("<html><body>")
            .Append("<p>Fugtighedsmålinger for ").Append(HtmlEscape(locationSlug)).Append(":</p>")
            .Append("<ul>");
        var text = new StringBuilder()
            .Append("Fugtighedsmålinger for ").Append(locationSlug).AppendLine(":");

        foreach (var room in rooms)
        {
            var line = FormatLine(room);
            var encoded = HtmlEscape(line);
            html.Append("<li>").Append(room.IsAboveThreshold ? $"<strong>{encoded}</strong>" : encoded).Append("</li>");
            text.Append("- ").Append(line).AppendLine(room.IsAboveThreshold ? " (over grænsen)" : string.Empty);
        }

        html.Append("</ul>")
            .Append("<p>Rum med fed har nået deres grænseværdi.</p>")
            .Append("</body></html>");

        return new EmailMessage(recipients, subject, html.ToString(), text.ToString());
    }

    // Escapes only the HTML-significant characters, leaving æ/ø/å and ° readable in the source.
    private static string HtmlEscape(string value) =>
        value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static string FormatLine(RoomHumidityStatus room) =>
        string.Format(
            DanishNumberFormat,
            "{0}: {1:0.0} °C, {2:0.0} %",
            room.Reading.RoomName,
            room.Reading.TemperatureCelsius,
            room.Reading.HumidityPercent);
}
