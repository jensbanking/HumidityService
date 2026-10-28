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
    [InlineData("lyngby")]
    [InlineData("aarhus-house1")]
    [InlineData("2nd-home")]
    public void Validate_SlugInFormat_ReturnsNoErrors(string slug)
    {
        Assert.Empty(new Location(slug, "gw-1", 56.1629, 10.2039).Validate());
    }

    [Theory]
    [InlineData("Lyngby")]
    [InlineData("aarhus house1")]
    [InlineData("aarhus/house1")]
    [InlineData("kø-havn")]
    [InlineData("-lyngby")]
    [InlineData("lyngby-")]
    [InlineData("aarhus--house1")]
    [InlineData("aarhus_house1")]
    public void Validate_SlugNotInFormat_ReportsIt(string slug)
    {
        var error = Assert.Single(new Location(slug, "gw-1", 56.1629, 10.2039).Validate());

        Assert.Contains($"'slug' '{slug}'", error);
    }

    [Theory]
    [InlineData(-90, -180)]
    [InlineData(90, 180)]
    [InlineData(0, 0)]
    public void Validate_CoordinatesAtTheirLimits_ReturnNoErrors(double latitude, double longitude)
    {
        Assert.Empty(new Location("lyngby", "gw-1", latitude, longitude).Validate());
    }

    [Theory]
    [InlineData(90.1, 10, "'latitude'")]
    [InlineData(-90.1, 10, "'latitude'")]
    [InlineData(double.NaN, 10, "'latitude'")]
    [InlineData(56, 180.1, "'longitude'")]
    [InlineData(56, -180.1, "'longitude'")]
    [InlineData(56, double.PositiveInfinity, "'longitude'")]
    public void Validate_CoordinateOutOfRange_ReportsIt(double latitude, double longitude, string expectedField)
    {
        var error = Assert.Single(new Location("lyngby", "gw-1", latitude, longitude).Validate());

        Assert.Contains(expectedField, error);
    }

    [Fact]
    public void ValidateAll_DistinctSlugsAndGateways_ReturnsNoErrors()
    {
        Location[] locations = [new("lyngby", "gw-1", 56.1629, 10.2039), new("aarhus-house1", "gw-2", 56.1629, 10.2039)];

        Assert.Empty(Location.ValidateAll(locations));
    }

    [Fact]
    public void ValidateAll_DuplicateSlug_ReportsItOnce()
    {
        Location[] locations =
        [
            new("lyngby", "gw-1", 56.1629, 10.2039),
            new("lyngby", "gw-2", 56.1629, 10.2039),
            new("lyngby", "gw-3", 56.1629, 10.2039),
        ];

        var error = Assert.Single(Location.ValidateAll(locations));

        Assert.Contains("Slug 'lyngby'", error);
    }

    [Fact]
    public void ValidateAll_DuplicateGateway_ReportsIt()
    {
        Location[] locations = [new("lyngby", "gw-1", 56.1629, 10.2039), new("aarhus-house1", "gw-1", 56.1629, 10.2039)];

        var error = Assert.Single(Location.ValidateAll(locations));

        Assert.Contains("gateway 'gw-1'", error);
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
