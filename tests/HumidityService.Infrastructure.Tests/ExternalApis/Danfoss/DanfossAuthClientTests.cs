using System.Net;
using System.Text;
using HumidityService.Infrastructure.Configuration;
using HumidityService.Infrastructure.ExternalApis.Danfoss;
using HumidityService.Infrastructure.Tests.TestSupport;
using Microsoft.Extensions.Options;
using Xunit;

namespace HumidityService.Infrastructure.Tests.ExternalApis.Danfoss;

public class DanfossAuthClientTests
{
    private static DanfossApiOptions ValidOptions => new()
    {
        BaseUrl = "https://api.danfoss.com",
        ClientId = "client-id",
        ClientSecret = "client-secret",
    };

    [Fact]
    public void Constructor_MissingClientCredentials_Throws()
    {
        var options = new DanfossApiOptions { BaseUrl = "https://api.danfoss.com", ClientId = "", ClientSecret = "" };
        var httpClient = new HttpClient { BaseAddress = new Uri(options.BaseUrl) };

        Assert.Throws<InvalidOperationException>(() => new DanfossAuthClient(httpClient, Options.Create(options)));
    }

    [Fact]
    public async Task GetAccessTokenAsync_PostsClientCredentialsAndReturnsAccessToken()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"access_token\":\"abc123\"}", Encoding.UTF8, "application/json"),
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(ValidOptions.BaseUrl) };
        var sut = new DanfossAuthClient(httpClient, Options.Create(ValidOptions));

        var token = await sut.GetAccessTokenAsync(CancellationToken.None);

        Assert.Equal("abc123", token);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("https://api.danfoss.com/oauth2/token", handler.LastRequest.RequestUri!.ToString());
        Assert.Contains("grant_type=client_credentials", handler.LastRequestBody);
        Assert.DoesNotContain("client_id", handler.LastRequestBody);
        Assert.DoesNotContain("client_secret", handler.LastRequestBody);

        var expectedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes("client-id:client-secret"));
        Assert.Equal("Basic", handler.LastRequest.Headers.Authorization!.Scheme);
        Assert.Equal(expectedCredentials, handler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task GetAccessTokenAsync_ResponseMissingAccessToken_Throws()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json"),
        });
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(ValidOptions.BaseUrl) };
        var sut = new DanfossAuthClient(httpClient, Options.Create(ValidOptions));

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetAccessTokenAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetAccessTokenAsync_NonSuccessStatusCode_Throws()
    {
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(ValidOptions.BaseUrl) };
        var sut = new DanfossAuthClient(httpClient, Options.Create(ValidOptions));

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetAccessTokenAsync(CancellationToken.None));
    }
}
