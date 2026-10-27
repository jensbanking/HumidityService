using HumidityService.Domain.Models;
using Xunit;

namespace HumidityService.Domain.Tests.Models;

public class HumidityThresholdTests
{
    [Theory]
    [InlineData(0.1, true)]
    [InlineData(70, true)]
    [InlineData(100, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(100.1, false)]
    public void IsValid_AcceptsOnlyAboveZeroUpToHundred(double percent, bool expected)
    {
        Assert.Equal(expected, HumidityThreshold.IsValid(percent));
    }

    [Fact]
    public void Default_IsValid()
    {
        Assert.True(HumidityThreshold.IsValid(HumidityThreshold.Default));
    }
}
