using Azure;
using HumidityService.Infrastructure.Resilience;
using Xunit;

namespace HumidityService.Infrastructure.Tests.Resilience;

public class AzureRetryPipelineTests
{
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
