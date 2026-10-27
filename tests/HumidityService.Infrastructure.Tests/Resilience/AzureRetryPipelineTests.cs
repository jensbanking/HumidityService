using Azure;
using Azure.Communication.Email;
using Azure.Storage.Blobs;
using HumidityService.Infrastructure.Resilience;
using Xunit;

namespace HumidityService.Infrastructure.Tests.Resilience;

public class AzureRetryPipelineTests
{
    [Fact]
    public void WithoutSdkRetries_BlobClientOptions_DisablesSdkRetries()
    {
        var options = new BlobClientOptions();

        Assert.Same(options, AzureRetryPipeline.WithoutSdkRetries(options));
        Assert.Equal(0, options.Retry.MaxRetries);
    }

    [Fact]
    public void WithoutSdkRetries_EmailClientOptions_DisablesSdkRetries()
    {
        Assert.Equal(0, AzureRetryPipeline.WithoutSdkRetries(new EmailClientOptions()).Retry.MaxRetries);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(408)]
    [InlineData(429)]
    [InlineData(500)]
    [InlineData(503)]
    public void IsTransient_TransientStatus_ReturnsTrue(int status)
    {
        Assert.True(AzureRetryPipeline.IsTransient(new RequestFailedException(status, "failed")));
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(409)]
    public void IsTransient_NonTransientStatus_ReturnsFalse(int status)
    {
        Assert.False(AzureRetryPipeline.IsTransient(new RequestFailedException(status, "failed")));
    }
}
