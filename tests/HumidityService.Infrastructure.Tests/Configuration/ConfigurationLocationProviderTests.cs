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
            "[{\"slug\":\"aarhus-house1\",\"latitude\":56.1629,\"longitude\":10.2039}]");

        var sut = new ConfigurationLocationProvider(configuration);
        var locations = sut.GetLocations();

        var location = Assert.Single(locations);
        Assert.Equal("aarhus-house1", location.Slug);
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
            "[{\"slug\":\"\",\"latitude\":56.1629,\"longitude\":10.2039}]");

        Assert.Throws<InvalidOperationException>(() => new ConfigurationLocationProvider(configuration));
    }
}
