using System.Net;
using System.Text;
using HumidityService.Infrastructure.ExternalApis.OpenMeteo;
using HumidityService.Infrastructure.Tests.TestSupport;
using Xunit;

namespace HumidityService.Infrastructure.Tests.ExternalApis.OpenMeteo;

public class OpenMeteoApiClientTests
{
    [Fact]
    public async Task GetOutdoorClimateAsync_SendsRequestWithExpectedQueryParametersAndReturnsRawBody()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"current\":{}}", Encoding.UTF8, "application/json"),
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.open-meteo.com") };
        var sut = new OpenMeteoApiClient(httpClient);

        var rawJson = await sut.GetOutdoorClimateAsync(56.1629, 10.2039, CancellationToken.None);

        Assert.Equal("{\"current\":{}}", rawJson);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal(
            "https://api.open-meteo.com/v1/forecast?latitude=56.1629&longitude=10.2039&current=temperature_2m,relative_humidity_2m",
            handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetOutdoorClimateAsync_NonSuccessStatusCode_Throws()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.open-meteo.com") };
        var sut = new OpenMeteoApiClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetOutdoorClimateAsync(56.1629, 10.2039, CancellationToken.None));
    }
}
