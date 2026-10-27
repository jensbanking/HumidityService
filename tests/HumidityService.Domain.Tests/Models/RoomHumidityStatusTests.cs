using HumidityService.Domain.Models;
using Xunit;

namespace HumidityService.Domain.Tests.Models;

public class RoomHumidityStatusTests
{
    [Theory]
    [InlineData(69.9, false)]
    [InlineData(70, true)]
    [InlineData(70.1, true)]
    public void IsAboveThreshold_TrueAtOrAboveLimit(double humidity, bool expected)
    {
        var status = new RoomHumidityStatus(new RoomClimateReading("Kælder", 18, humidity), 70);

        Assert.Equal(expected, status.IsAboveThreshold);
    }
}
