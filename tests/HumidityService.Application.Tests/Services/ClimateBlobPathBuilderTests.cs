using HumidityService.Application.Models;
using HumidityService.Application.Services;
using Xunit;

namespace HumidityService.Application.Tests.Services;

public class ClimateBlobPathBuilderTests
{
    private static readonly DateTimeOffset Timestamp = new(2026, 7, 2, 14, 0, 3, TimeSpan.Zero);

    [Fact]
    public void BuildApiRaw_ProducesHourFolderPath()
    {
        Assert.Equal("2026/07/02/14/apiraw_2026_07_02_14.json", ClimateBlobPathBuilder.BuildApiRaw(Timestamp));
    }

    [Theory]
    [InlineData(LocationBlobType.Indoor, "2026/07/02/14/lyngby/indoor_lyngby_2026_07_02_14.json")]
    [InlineData(LocationBlobType.Outdoor, "2026/07/02/14/lyngby/outdoor_lyngby_2026_07_02_14.json")]
    [InlineData(LocationBlobType.SubDevices, "2026/07/02/14/lyngby/subdevices_lyngby_2026_07_02_14.json")]
    public void Build_ProducesLocationFolderPathUnderTheHour(LocationBlobType blobType, string expected)
    {
        Assert.Equal(expected, ClimateBlobPathBuilder.Build("lyngby", blobType, Timestamp));
    }

    [Fact]
    public void Build_NonUtcTimestamp_UsesUtcDateAndHour()
    {
        // 00:30 Danish summer time (UTC+2) is 22:30 UTC the previous day.
        var local = new DateTimeOffset(2026, 7, 3, 0, 30, 0, TimeSpan.FromHours(2));

        Assert.Equal("2026/07/02/22/lyngby/indoor_lyngby_2026_07_02_22.json", ClimateBlobPathBuilder.Build("lyngby", LocationBlobType.Indoor, local));
        Assert.Equal("2026/07/02/22/apiraw_2026_07_02_22.json", ClimateBlobPathBuilder.BuildApiRaw(local));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Build_MissingSlug_Throws(string? slug)
    {
        Assert.ThrowsAny<ArgumentException>(() => ClimateBlobPathBuilder.Build(slug!, LocationBlobType.Indoor, Timestamp));
    }
}
