using HumidityService.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;

namespace HumidityService.Infrastructure.Tests.Configuration;

public class ConfigurationLocationProviderTests
{
    [Fact]
    public void GetLocations_ValidJson_ReturnsParsedLocations()
    {
        var configuration = Substitute.For<IConfiguration>();
        configuration["Locations"].Returns(
            "[{\"slug\":\"aarhus-house1\",\"danfossGatewayId\":\"gw-1\",\"latitude\":56.1629,\"longitude\":10.2039}]");

        var sut = new ConfigurationLocationProvider(configuration);
        var locations = sut.GetLocations();

        var location = Assert.Single(locations);
        Assert.Equal("aarhus-house1", location.Slug);
        Assert.Equal("gw-1", location.DanfossGatewayId);
        Assert.Equal(56.1629, location.Latitude);
        Assert.Equal(10.2039, location.Longitude);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_MissingConfiguration_Throws(string? rawValue)
    {
        var configuration = Substitute.For<IConfiguration>();
        configuration["Locations"].Returns(rawValue);

        Assert.Throws<InvalidOperationException>(() => new ConfigurationLocationProvider(configuration));
    }

    [Fact]
    public void Constructor_InvalidJson_Throws()
    {
        var configuration = Substitute.For<IConfiguration>();
        configuration["Locations"].Returns("not-json");

        Assert.Throws<InvalidOperationException>(() => new ConfigurationLocationProvider(configuration));
    }

    [Fact]
    public void Constructor_EmptyArray_Throws()
    {
        var configuration = Substitute.For<IConfiguration>();
        configuration["Locations"].Returns("[]");

        Assert.Throws<InvalidOperationException>(() => new ConfigurationLocationProvider(configuration));
    }

    [Fact]
    public void Constructor_LocationMissingSlug_Throws()
    {
        var configuration = Substitute.For<IConfiguration>();
        configuration["Locations"].Returns(
            "[{\"slug\":\"\",\"danfossGatewayId\":\"gw-1\",\"latitude\":56.1629,\"longitude\":10.2039}]");

        Assert.Throws<InvalidOperationException>(() => new ConfigurationLocationProvider(configuration));
    }

    [Fact]
    public void GetLocations_WithHumidityThresholds_ParsesThem()
    {
        var configuration = Substitute.For<IConfiguration>();
        configuration["Locations"].Returns(
            "[{\"slug\":\"lyngby\",\"danfossGatewayId\":\"gw-1\",\"latitude\":56.1629,\"longitude\":10.2039,\"humidityThresholds\":{\"Kælder\":85}}]");

        var location = Assert.Single(new ConfigurationLocationProvider(configuration).GetLocations());

        Assert.NotNull(location.HumidityThresholds);
        Assert.Equal(85, location.HumidityThresholds["Kælder"]);
    }

    [Fact]
    public void GetLocations_WithoutHumidityThresholds_LeavesThemNull()
    {
        var configuration = Substitute.For<IConfiguration>();
        configuration["Locations"].Returns(
            "[{\"slug\":\"lyngby\",\"danfossGatewayId\":\"gw-1\",\"latitude\":56.1629,\"longitude\":10.2039,\"humidityThresholds\":null}]");

        Assert.Null(Assert.Single(new ConfigurationLocationProvider(configuration).GetLocations()).HumidityThresholds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Constructor_HumidityThresholdOutOfRange_Throws(int limit)
    {
        var configuration = Substitute.For<IConfiguration>();
        configuration["Locations"].Returns(
            $"[{{\"slug\":\"lyngby\",\"danfossGatewayId\":\"gw-1\",\"latitude\":56.1629,\"longitude\":10.2039,\"humidityThresholds\":{{\"Kælder\":{limit}}}}}]");

        Assert.Throws<InvalidOperationException>(() => new ConfigurationLocationProvider(configuration));
    }

    [Theory]
    [InlineData("[{\"slug\":\"aarhus-house1\",\"latitude\":56.1629,\"longitude\":10.2039}]")]
    [InlineData("[{\"slug\":\"aarhus-house1\",\"danfossGatewayId\":\" \",\"latitude\":56.1629,\"longitude\":10.2039}]")]
    public void Constructor_LocationMissingGatewayId_Throws(string rawValue)
    {
        var configuration = Substitute.For<IConfiguration>();
        configuration["Locations"].Returns(rawValue);

        Assert.Throws<InvalidOperationException>(() => new ConfigurationLocationProvider(configuration));
    }

    [Fact]
    public void Constructor_SameRoomTwice_ThrowsNamingTheLocation()
    {
        var configuration = Substitute.For<IConfiguration>();
        configuration["Locations"].Returns(
            "[{\"slug\":\"lyngby\",\"danfossGatewayId\":\"gw-1\",\"latitude\":56.1629,\"longitude\":10.2039,\"humidityThresholds\":{\"Kælder\":75,\"kælder\":80}}]");

        var exception = Assert.Throws<InvalidOperationException>(() => new ConfigurationLocationProvider(configuration));

        Assert.Contains("'lyngby'", exception.Message);
        Assert.Contains("more than one humidity limit", exception.Message);
    }

    [Fact]
    public void Constructor_LocationWithoutSlug_ThrowsNamingItsPosition()
    {
        var configuration = Substitute.For<IConfiguration>();
        configuration["Locations"].Returns(
            "[{\"slug\":\"lyngby\",\"danfossGatewayId\":\"gw-1\",\"latitude\":56.1629,\"longitude\":10.2039},{\"danfossGatewayId\":\"gw-2\",\"latitude\":56.1629,\"longitude\":10.2039}]");

        var exception = Assert.Throws<InvalidOperationException>(() => new ConfigurationLocationProvider(configuration));

        Assert.Contains("#2", exception.Message);
    }

    [Fact]
    public void Constructor_SlugNotInFormat_ThrowsNamingTheLocation()
    {
        var configuration = Substitute.For<IConfiguration>();
        configuration["Locations"].Returns(
            "[{\"slug\":\"Aarhus House1\",\"danfossGatewayId\":\"gw-1\",\"latitude\":56.1629,\"longitude\":10.2039}]");

        var exception = Assert.Throws<InvalidOperationException>(() => new ConfigurationLocationProvider(configuration));

        Assert.Contains("'Aarhus House1'", exception.Message);
    }

    [Fact]
    public void Constructor_LatitudeOutOfRange_Throws()
    {
        var configuration = Substitute.For<IConfiguration>();
        configuration["Locations"].Returns(
            "[{\"slug\":\"lyngby\",\"danfossGatewayId\":\"gw-1\",\"latitude\":156.1629,\"longitude\":10.2039}]");

        var exception = Assert.Throws<InvalidOperationException>(() => new ConfigurationLocationProvider(configuration));

        Assert.Contains("'latitude'", exception.Message);
    }

    [Fact]
    public void Constructor_DuplicateSlug_Throws()
    {
        var configuration = Substitute.For<IConfiguration>();
        configuration["Locations"].Returns(
            "[{\"slug\":\"lyngby\",\"danfossGatewayId\":\"gw-1\",\"latitude\":56.1629,\"longitude\":10.2039}," +
            "{\"slug\":\"lyngby\",\"danfossGatewayId\":\"gw-2\",\"latitude\":56.1629,\"longitude\":10.2039}]");

        var exception = Assert.Throws<InvalidOperationException>(() => new ConfigurationLocationProvider(configuration));

        Assert.Contains("Slug 'lyngby' is used by more than one location", exception.Message);
    }
}
