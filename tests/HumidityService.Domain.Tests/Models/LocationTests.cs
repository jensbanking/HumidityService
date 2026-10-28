using HumidityService.Domain.Models;
using Xunit;

namespace HumidityService.Domain.Tests.Models;

public class LocationTests
{
    [Fact]
    public void Locations_WithSameValues_AreEqual()
    {
        var first = new Location("aarhus-house1", "gw-aarhus-house1", 56.1629, 10.2039);
        var second = new Location("aarhus-house1", "gw-aarhus-house1", 56.1629, 10.2039);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Locations_WithDifferentSlug_AreNotEqual()
    {
        var first = new Location("aarhus-house1", "gw-aarhus-house1", 56.1629, 10.2039);
        var second = new Location("aarhus-house2", "gw-aarhus-house1", 56.1629, 10.2039);

        Assert.NotEqual(first, second);
    }

    [Theory]
    [InlineData("Kælder", 75)]
    [InlineData("kælder", 75)]
    [InlineData("  Kælder ", 75)]
    [InlineData("Stuen", 70)]
    public void GetHumidityThreshold_UsesOverrideForMatchingRoomOtherwiseDefault(string roomName, double expected)
    {
        var location = CreateLocation(new Dictionary<string, double> { [" Kælder"] = 75 });

        Assert.Equal(expected, location.GetHumidityThreshold(roomName, 70));
    }

    [Fact]
    public void GetHumidityThreshold_NoOverrides_ReturnsDefault()
    {
        Assert.Equal(70, CreateLocation(null).GetHumidityThreshold("Kælder", 70));
    }

    [Fact]
    public void Validate_ValidLocation_ReturnsNoErrors()
    {
        Assert.Empty(CreateLocation(new Dictionary<string, double> { ["Kælder"] = 75, ["Bad"] = 100 }).Validate());
    }

    [Theory]
    [InlineData(null, "gw-1", "'slug'")]
    [InlineData(" ", "gw-1", "'slug'")]
    [InlineData("lyngby", null, "'danfossGatewayId'")]
    [InlineData("lyngby", "", "'danfossGatewayId'")]
    public void Validate_MissingRequiredValue_ReportsIt(string? slug, string? gatewayId, string expectedField)
    {
        var errors = new Location(slug!, gatewayId!, 56.1629, 10.2039).Validate();

        Assert.Contains(errors, e => e.Contains(expectedField));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(100.1)]
    public void Validate_HumidityLimitOutOfRange_ReportsIt(double limit)
    {
        var error = Assert.Single(CreateLocation(new Dictionary<string, double> { ["Kælder"] = limit }).Validate());

        Assert.Contains("Kælder", error);
    }

    [Fact]
    public void Validate_SameRoomTwiceIgnoringCaseAndSpaces_ReportsIt()
    {
        var error = Assert.Single(CreateLocation(new Dictionary<string, double> { ["Kælder"] = 75, ["kælder "] = 80 }).Validate());

        Assert.Contains("more than one humidity limit", error);
    }

    private static Location CreateLocation(IReadOnlyDictionary<string, double>? thresholds) =>
        new("lyngby", "gw-1", 56.1629, 10.2039, thresholds);
}
