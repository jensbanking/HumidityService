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
}
