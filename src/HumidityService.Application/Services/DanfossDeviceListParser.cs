using System.Text.Json;

namespace HumidityService.Application.Services;

/// <summary>
/// Reads device ids from a raw Danfoss Ally <c>/ally/devices</c> response, shaped as
/// <c>{"result":[{"id":"...","device_type":"..."}, ...]}</c>.
/// </summary>
public static class DanfossDeviceListParser
{
    /// <summary>Returns the ids of every device in the response.</summary>
    /// <param name="rawJson">The unmodified raw JSON response body.</param>
    /// <exception cref="JsonException">The body is not valid JSON or has no <c>result</c> array.</exception>
    public static IReadOnlySet<string> GetDeviceIds(string rawJson)
    {
        using var document = JsonDocument.Parse(rawJson);

        if (!document.RootElement.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Danfoss device list response does not contain a 'result' array.");
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var device in result.EnumerateArray())
        {
            if (device.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
            {
                ids.Add(id.GetString()!);
            }
        }

        return ids;
    }
}
