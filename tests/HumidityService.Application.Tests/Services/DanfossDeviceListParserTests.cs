using System.Text.Json;
using HumidityService.Application.Services;
using Xunit;

namespace HumidityService.Application.Tests.Services;

public class DanfossDeviceListParserTests
{
    [Fact]
    public void GetDeviceIds_ValidResponse_ReturnsAllIds()
    {
        var ids = DanfossDeviceListParser.GetDeviceIds(
            "{\"result\":[{\"id\":\"gw-1\",\"device_type\":\"Danfoss Ally™ Gateway\"},{\"id\":\"room-1\"},{\"name\":\"no id\"}],\"t\":1}");

        Assert.Equal(new[] { "gw-1", "room-1" }, ids.OrderBy(id => id));
    }

    [Fact]
    public void GetDeviceIds_EmptyResult_ReturnsEmptySet()
    {
        Assert.Empty(DanfossDeviceListParser.GetDeviceIds("{\"result\":[]}"));
    }

    [Theory]
    [InlineData("{\"devices\":[]}")]
    [InlineData("{\"result\":{}}")]
    public void GetDeviceIds_MissingResultArray_ThrowsJsonException(string rawJson)
    {
        Assert.Throws<JsonException>(() => DanfossDeviceListParser.GetDeviceIds(rawJson));
    }

    [Fact]
    public void GetDeviceIds_InvalidJson_ThrowsJsonException()
    {
        Assert.ThrowsAny<JsonException>(() => DanfossDeviceListParser.GetDeviceIds("not-json"));
    }

    [Fact]
    public void GetRoomReadings_ReturnsOnlyDevicesWithTemperatureAndHumidity_Unscaled()
    {
        const string json = """
            {"result":[
              {"id":"gw-1","name":"Gateway","device_type":"Danfoss Ally™ Gateway","status":[]},
              {"id":"trv-1","name":"Stue radiator","device_type":"Danfoss Ally™ Radiator Thermostat","status":[{"code":"temp_current","value":215}]},
              {"id":"rs-1","name":"Kælder","device_type":"Danfoss Ally™ Room Sensor","status":[{"code":"temp_current","value":198},{"code":"humidity_value","value":597},{"code":"battery_percentage","value":80}]}
            ]}
            """;

        var readings = DanfossDeviceListParser.GetRoomReadings(json);

        var reading = Assert.Single(readings);
        Assert.Equal("Kælder", reading.RoomName);
        Assert.Equal(19.8, reading.TemperatureCelsius, 3);
        Assert.Equal(59.7, reading.HumidityPercent, 3);
    }

    [Fact]
    public void GetRoomReadings_SkipsDevicesWithoutNameOrWithNonNumericValues()
    {
        const string json = """
            {"result":[
              {"id":"rs-1","status":[{"code":"temp_current","value":198},{"code":"humidity_value","value":597}]},
              {"id":"rs-2","name":"Bad","status":[{"code":"temp_current","value":"198"},{"code":"humidity_value","value":597}]}
            ]}
            """;

        Assert.Empty(DanfossDeviceListParser.GetRoomReadings(json));
    }

    [Fact]
    public void GetRoomReadings_MissingResultArray_ThrowsJsonException()
    {
        Assert.Throws<JsonException>(() => DanfossDeviceListParser.GetRoomReadings("{\"devices\":[]}"));
    }
}
