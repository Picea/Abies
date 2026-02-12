using System.Net;
using Abies.Conduit.IntegrationTests.Testing;
using Abies.Conduit.Services;
using Xunit;

namespace Abies.Conduit.IntegrationTests;

public class ApiClientContractTests
{
    [Fact]
    public async Task Home_LoadArticles_HitsExpectedEndpoint()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        // Support whichever list endpoint the UI chooses.
        handler.When(HttpMethod.Get, "/api/articles?limit=10&offset=0", HttpStatusCode.OK, new
        {
            articles = new object[] { },
            articlesCount = 0
        });
        handler.When(HttpMethod.Get, "/api/articles/feed?limit=10&offset=0", HttpStatusCode.OK, new
        {
            articles = new object[] { },
            articlesCount = 0
        });
        handler.When(HttpMethod.Get, "/api/tags", HttpStatusCode.OK, new
        {
            tags = new string[] { }
        });

        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5179")
        };

        ApiClient.ConfigureHttpClient(http);
        ApiClient.ConfigureBaseUrl("http://localhost:5179/api");

        // Act
        _ = await ApiClient.GetFeedArticlesAsync(limit: 10, offset: 0);

        // Assert
        // If this fails again, it means the ApiClient didn't use our injected HttpClient.
        // In that case the test should be rewritten (or ApiClient refactored) before we can
        // rely on this style of contract test.
        Assert.True(handler.Requests.Count > 0, "Expected the fake HttpMessageHandler to record at least one request.");
        Assert.Contains(handler.Requests, r => r.Method == HttpMethod.Get && r.Uri.PathAndQuery == "/api/articles/feed?limit=10&offset=0");
    }
}
