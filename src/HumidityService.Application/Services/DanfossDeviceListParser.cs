using System.Text.Json;
using HumidityService.Domain.Models;

namespace HumidityService.Application.Services;

/// <summary>
/// Reads devices from a raw Danfoss Ally <c>/ally/devices</c> response, shaped as
/// <c>{"result":[{"id":"...","name":"...","device_type":"...","status":[{"code":"...","value":...}]}, ...]}</c>.
/// </summary>
public static class DanfossDeviceListParser
{
    private const string TemperatureCode = "temp_current";
    private const string HumidityCode = "humidity_value";

    // Danfoss reports temperature and humidity as integers scaled by 10 (e.g. 597 = 59.7 %).
    private const double ValueScale = 10.0;

    /// <summary>Returns the ids of every device in the response.</summary>
    /// <param name="rawJson">The unmodified raw JSON response body.</param>
    /// <exception cref="JsonException">The body is not valid JSON or has no <c>result</c> array.</exception>
    public static IReadOnlySet<string> GetDeviceIds(string rawJson)
    {
        using var document = JsonDocument.Parse(rawJson);

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var device in GetResultArray(document).EnumerateArray())
        {
            if (device.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
            {
                ids.Add(id.GetString()!);
            }
        }

        return ids;
    }

    /// <summary>
    /// Returns the current temperature and humidity of every device that reports both
    /// <c>temp_current</c> and <c>humidity_value</c>, i.e. the rooms that have a humidity sensor.
    /// Room names are trimmed, since names set in the Danfoss app can carry stray spaces.
    /// </summary>
    /// <param name="rawJson">The unmodified raw JSON response body.</param>
    /// <exception cref="JsonException">The body is not valid JSON or has no <c>result</c> array.</exception>
    public static IReadOnlyList<RoomClimateReading> GetRoomReadings(string rawJson)
    {
        using var document = JsonDocument.Parse(rawJson);

        var readings = new List<RoomClimateReading>();
        foreach (var device in GetResultArray(document).EnumerateArray())
        {
            if (!device.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String ||
                !device.TryGetProperty("status", out var status) || status.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            double? temperature = null;
            double? humidity = null;
            foreach (var entry in status.EnumerateArray())
            {
                if (!entry.TryGetProperty("code", out var code) || code.ValueKind != JsonValueKind.String ||
                    !entry.TryGetProperty("value", out var value) || value.ValueKind != JsonValueKind.Number)
                {
                    continue;
                }

                switch (code.GetString())
                {
                    case TemperatureCode:
                        temperature = value.GetDouble() / ValueScale;
                        break;
                    case HumidityCode:
                        humidity = value.GetDouble() / ValueScale;
                        break;
                }
            }

            if (temperature is not null && humidity is not null)
            {
                readings.Add(new RoomClimateReading(name.GetString()!.Trim(), temperature.Value, humidity.Value));
            }
        }

        return readings;
    }

    private static JsonElement GetResultArray(JsonDocument document)
    {
        if (!document.RootElement.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Danfoss device list response does not contain a 'result' array.");
        }

        return result;
    }
}
