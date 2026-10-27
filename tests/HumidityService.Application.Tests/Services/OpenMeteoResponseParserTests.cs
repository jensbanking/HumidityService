using System.Text.Json;
using HumidityService.Application.Services;
using HumidityService.Domain.Models;
using Xunit;

namespace HumidityService.Application.Tests.Services;

public class OpenMeteoResponseParserTests
{
    [Fact]
    public void GetCurrentReading_ReturnsCurrentTemperatureAndHumidity()
    {
        const string json =
            "{\"latitude\":56.16,\"longitude\":10.2,\"current_units\":{\"temperature_2m\":\"°C\",\"relative_humidity_2m\":\"%\"}," +
            "\"current\":{\"time\":\"2026-10-28T14:00\",\"interval\":900,\"temperature_2m\":8.3,\"relative_humidity_2m\":92}}";

        Assert.Equal(new OutdoorClimateReading(8.3, 92), OpenMeteoResponseParser.GetCurrentReading(json));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("{\"current\":null}")]
    [InlineData("{\"current\":{\"relative_humidity_2m\":92}}")]
    [InlineData("{\"current\":{\"temperature_2m\":8.3}}")]
    [InlineData("{\"current\":{\"temperature_2m\":null,\"relative_humidity_2m\":92}}")]
    [InlineData("{\"current\":{\"temperature_2m\":8.3,\"relative_humidity_2m\":null}}")]
    public void GetCurrentReading_ValueMissingOrNull_ReturnsNull(string json)
    {
        Assert.Null(OpenMeteoResponseParser.GetCurrentReading(json));
    }

    [Fact]
    public void GetCurrentReading_InvalidJson_Throws()
    {
        Assert.ThrowsAny<JsonException>(() => OpenMeteoResponseParser.GetCurrentReading("not json"));
    }
}
