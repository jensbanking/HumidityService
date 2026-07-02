using HumidityService.Application.Services;
using HumidityService.Domain.Models;
using Xunit;

namespace HumidityService.Application.Tests.Services;

public class ClimateBlobPathBuilderTests
{
    [Fact]
    public void Build_IndoorReading_ProducesExpectedPath()
    {
        var timestamp = new DateTimeOffset(2026, 7, 2, 14, 0, 0, TimeSpan.Zero);

        var path = ClimateBlobPathBuilder.Build("aarhus-house1", ClimateReadingType.Indoor, timestamp);

        Assert.Equal("aarhus-house1/2026/07/02/aarhus-house1_indoor_2026_07_02_14.json", path);
    }

    [Fact]
    public void Build_OutdoorReading_ProducesExpectedPath()
    {
        var timestamp = new DateTimeOffset(2026, 1, 9, 3, 0, 0, TimeSpan.Zero);

        var path = ClimateBlobPathBuilder.Build("aarhus-house1", ClimateReadingType.Outdoor, timestamp);

        Assert.Equal("aarhus-house1/2026/01/09/aarhus-house1_outdoor_2026_01_09_03.json", path);
    }

    [Fact]
    public void Build_NullSlug_ProducesAccountWidePath()
    {
        var timestamp = new DateTimeOffset(2026, 7, 2, 14, 0, 0, TimeSpan.Zero);

        var path = ClimateBlobPathBuilder.Build(null, ClimateReadingType.Indoor, timestamp);

        Assert.Equal("indoor/2026/07/02/indoor_2026_07_02_14.json", path);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Build_BlankSlug_ProducesAccountWidePath(string slug)
    {
        var timestamp = new DateTimeOffset(2026, 7, 2, 14, 0, 0, TimeSpan.Zero);

        var path = ClimateBlobPathBuilder.Build(slug, ClimateReadingType.Indoor, timestamp);

        Assert.Equal("indoor/2026/07/02/indoor_2026_07_02_14.json", path);
    }
}
