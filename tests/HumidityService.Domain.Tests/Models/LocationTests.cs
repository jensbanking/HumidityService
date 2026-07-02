using HumidityService.Domain.Models;
using Xunit;

namespace HumidityService.Domain.Tests.Models;

public class LocationTests
{
    [Fact]
    public void Locations_WithSameValues_AreEqual()
    {
        var first = new Location("aarhus-house1", 56.1629, 10.2039);
        var second = new Location("aarhus-house1", 56.1629, 10.2039);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Locations_WithDifferentSlug_AreNotEqual()
    {
        var first = new Location("aarhus-house1", 56.1629, 10.2039);
        var second = new Location("aarhus-house2", 56.1629, 10.2039);

        Assert.NotEqual(first, second);
    }
}
