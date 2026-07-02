using System.Net;
using System.Text;
using HumidityService.Infrastructure.ExternalApis.Danfoss;
using HumidityService.Infrastructure.Tests.TestSupport;
using Xunit;

namespace HumidityService.Infrastructure.Tests.ExternalApis.Danfoss;

public class DanfossClimateApiClientTests
{
    [Fact]
    public async Task GetAllDevicesAsync_SendsAuthorizedGetRequestAndReturnsRawBody()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"devices\":[]}", Encoding.UTF8, "application/json"),
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.danfoss.com") };
        var sut = new DanfossClimateApiClient(httpClient);

        var rawJson = await sut.GetAllDevicesAsync("token-abc", CancellationToken.None);

        Assert.Equal("{\"devices\":[]}", rawJson);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal("https://api.danfoss.com/ally/devices", handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization!.Scheme);
        Assert.Equal("token-abc", handler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task GetAllDevicesAsync_NonSuccessStatusCode_Throws()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.danfoss.com") };
        var sut = new DanfossClimateApiClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetAllDevicesAsync("token-abc", CancellationToken.None));
    }
}
