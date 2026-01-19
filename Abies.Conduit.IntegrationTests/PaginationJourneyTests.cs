using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Abies.Conduit.IntegrationTests.Testing;
using Abies.Conduit.Services;
using Xunit;

namespace Abies.Conduit.IntegrationTests;

public class PaginationJourneyTests
{
    [Fact]
    public async Task Home_GlobalFeed_Pagination_RequestsCorrectOffsets_AndMapsArticles()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();

        // Home also loads tags on init.
        handler.When(HttpMethod.Get, "/api/tags", HttpStatusCode.OK, new
        {
            tags = (string[])["pages"]
        });

        // Page 1 (offset 0)
        handler.When(
            HttpMethod.Get,
            "/api/articles/feed?limit=10&offset=0",
            HttpStatusCode.OK,
            ConduitApiFixtures.ArticlesResponse(
                totalCount: 11,
                ConduitApiFixtures.Article(
                    slug: "page-1-article",
                    title: "Page 1",
                    description: "Desc",
                    body: "Body",
                    tagList: ["pages"])
            )
        );

        // Page 2 (offset 10)
        handler.When(
            HttpMethod.Get,
            "/api/articles/feed?limit=10&offset=10",
            HttpStatusCode.OK,
            ConduitApiFixtures.ArticlesResponse(
                totalCount: 11,
                ConduitApiFixtures.Article(
                    slug: "page-2-article",
                    title: "Page 2",
                    description: "Desc",
                    body: "Body",
                    tagList: ["pages"])
            )
        );

        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5179")
        };

        ApiClient.ConfigureHttpClient(http);
        ApiClient.ConfigureBaseUrl("http://localhost:5179/api");

        // Act
    var (page1Articles, total1) = await ArticleService.GetFeedArticlesAsync(limit: 10, offset: 0);
    var (page2Articles, total2) = await ArticleService.GetFeedArticlesAsync(limit: 10, offset: 10);

        // Assert (requests)
    Assert.Contains(handler.Requests, r => r.Method == HttpMethod.Get && r.Uri.PathAndQuery == "/api/articles/feed?limit=10&offset=0");
    Assert.Contains(handler.Requests, r => r.Method == HttpMethod.Get && r.Uri.PathAndQuery == "/api/articles/feed?limit=10&offset=10");

        // Assert (mapping)
        Assert.Equal(11, total1);
        Assert.Equal(11, total2);

        Assert.Single(page1Articles);
        Assert.Single(page2Articles);

        Assert.Equal("page-1-article", page1Articles.Single().Slug);
        Assert.Equal("Page 1", page1Articles.Single().Title);

        Assert.Equal("page-2-article", page2Articles.Single().Slug);
        Assert.Equal("Page 2", page2Articles.Single().Title);
    }
}
