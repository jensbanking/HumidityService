using Azure.Core;
using HumidityService.Infrastructure.Configuration;
using HumidityService.Infrastructure.Storage;
using NSubstitute;
using Xunit;

namespace HumidityService.Infrastructure.Tests.Storage;

public class BlobServiceClientFactoryTests
{
    private readonly TokenCredential _credential = Substitute.For<TokenCredential>();

    [Fact]
    public void Create_ServiceUriSet_UsesServiceUriEvenWithDefaultConnectionString()
    {
        var options = new ClimateStorageOptions { ServiceUri = "https://sthumiditydev.blob.core.windows.net/" };

        var client = BlobServiceClientFactory.Create(options, _credential);

        Assert.Equal(new Uri("https://sthumiditydev.blob.core.windows.net/"), client.Uri);
        Assert.Equal("sthumiditydev", client.AccountName);
        Assert.False(client.CanGenerateAccountSasUri);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Create_NoServiceUri_UsesConnectionString(string? serviceUri)
    {
        var options = new ClimateStorageOptions { ServiceUri = serviceUri };

        var client = BlobServiceClientFactory.Create(options, _credential);

        Assert.Equal("devstoreaccount1", client.AccountName);
        Assert.True(client.CanGenerateAccountSasUri);
    }
}
