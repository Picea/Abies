using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Abies.Conduit.IntegrationTests.Testing;
using Abies.Conduit.Services;
using Xunit;

namespace Abies.Conduit.IntegrationTests;

public class FavoriteJourneyTests
{
    [Fact]
    public async Task FavoriteThenUnfavorite_UsesCorrectEndpoints_AndMapsState()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();

        handler.When(
            HttpMethod.Post,
            "/api/articles/test-slug/favorite",
            HttpStatusCode.OK,
            new
            {
                article = ConduitApiFixtures.Article(
                    slug: "test-slug",
                    title: "Test",
                    description: "Desc",
                    body: "Body",
                    favorited: true,
                    favoritesCount: 1)
            });

        handler.When(
            HttpMethod.Delete,
            "/api/articles/test-slug/favorite",
            HttpStatusCode.OK,
            new
            {
                article = ConduitApiFixtures.Article(
                    slug: "test-slug",
                    title: "Test",
                    description: "Desc",
                    body: "Body",
                    favorited: false,
                    favoritesCount: 0)
            });

        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5179")
        };

        ApiClient.ConfigureHttpClient(http);
        ApiClient.ConfigureBaseUrl("http://localhost:5179/api");

        // Act
        var favorited = await ArticleService.FavoriteArticleAsync("test-slug");
        var unfavorited = await ArticleService.UnfavoriteArticleAsync("test-slug");

        // Assert (outgoing requests)
        Assert.Contains(handler.Requests, r => r.Method == HttpMethod.Post && r.Uri.PathAndQuery == "/api/articles/test-slug/favorite");
        Assert.Contains(handler.Requests, r => r.Method == HttpMethod.Delete && r.Uri.PathAndQuery == "/api/articles/test-slug/favorite");

        // Assert (mapping)
        Assert.True(favorited.Favorited);
        Assert.Equal(1, favorited.FavoritesCount);

        Assert.False(unfavorited.Favorited);
        Assert.Equal(0, unfavorited.FavoritesCount);

        // Sanity: same article
        Assert.Equal("test-slug", favorited.Slug);
        Assert.Equal("test-slug", unfavorited.Slug);
    }
}
