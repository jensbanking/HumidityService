using HumidityService.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace HumidityService.Infrastructure.Tests.Configuration;

public class ConfigurationHumidityAlertSettingsTests
{
    private static IConfiguration Build(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    [Fact]
    public void Create_NothingConfigured_UsesDefaultThresholdAndNoRecipients()
    {
        var settings = ConfigurationHumidityAlertSettings.Create(Build());

        Assert.Equal(75, settings.DefaultThresholdPercent);
        Assert.Empty(settings.Recipients);
    }

    [Fact]
    public void Create_ValidValues_ParsesThresholdAndRecipients()
    {
        var settings = ConfigurationHumidityAlertSettings.Create(Build(
            ("HumidityAlert:DefaultThresholdPercent", "70.5"),
            ("HumidityAlert:Recipients", "[\"a@example.com\",\"b@example.com\"]")));

        Assert.Equal(70.5, settings.DefaultThresholdPercent);
        Assert.Equal(new[] { "a@example.com", "b@example.com" }, settings.Recipients);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("101")]
    public void Create_InvalidThreshold_Throws(string value)
    {
        Assert.Throws<InvalidOperationException>(() =>
            ConfigurationHumidityAlertSettings.Create(Build(("HumidityAlert:DefaultThresholdPercent", value))));
    }

    [Theory]
    [InlineData("a@example.com")]
    [InlineData("[\"a@example.com\",\"\"]")]
    public void Create_InvalidRecipients_Throws(string value)
    {
        Assert.Throws<InvalidOperationException>(() =>
            ConfigurationHumidityAlertSettings.Create(Build(("HumidityAlert:Recipients", value))));
    }
}
