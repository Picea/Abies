using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Abies.Conduit.IntegrationTests.Testing;
using Abies.Conduit.Services;
using Xunit;

namespace Abies.Conduit.IntegrationTests;

public class TagsJourneyTests
{
    [Fact]
    public async Task LoadTags_HitsTagsEndpoint_AndReturnsList()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        handler.When(
            HttpMethod.Get,
            "/api/tags",
            HttpStatusCode.OK,
            new
            {
                tags = new[] { "a", "b" }
            });

        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5179")
        };

        ApiClient.ConfigureHttpClient(http);
        ApiClient.ConfigureBaseUrl("http://localhost:5179/api");

        // Act
        var tags = await TagService.GetTagsAsync();

        // Assert
        Assert.Contains(handler.Requests, r => r.Method == HttpMethod.Get && r.Uri.PathAndQuery == "/api/tags");
        Assert.Equal(new[] { "a", "b" }, tags.ToArray());
    }
}
