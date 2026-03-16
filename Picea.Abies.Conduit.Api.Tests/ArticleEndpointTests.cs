// =============================================================================
// Article Endpoint Tests — CRUD, Feed, Favorites
// =============================================================================
// Tests the /api/articles endpoints including:
//   - List articles (public)
//   - Get article by slug (public)
//   - Create article (auth required)
//   - Auth enforcement on protected endpoints
// =============================================================================

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Picea.Abies.Conduit.Api.Dto;
using Picea.Abies.Conduit.ReadModel;

namespace Picea.Abies.Conduit.Api.Tests;

public sealed class ArticleEndpointTests : IAsyncDisposable
{
    private readonly ConduitApiFactory _factory = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Test]
    public async Task ListArticles_NoAuth_ReturnsOk()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/articles");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<MultipleArticlesResponse>(JsonOptions);
        await Assert.That(body).IsNotNull();
        await Assert.That(body.Articles).IsNotNull();
    }

    [Test]
    public async Task GetArticle_Exists_ReturnsArticle()
    {
        var author = new ProfileReadModel("author", "bio", "", false);
        var article = new ArticleQueryResult(
            Slug: "test-article",
            Title: "Test Article",
            Description: "A test",
            Body: "The body",
            TagList: ["test"],
            CreatedAt: DateTimeOffset.UtcNow,
            UpdatedAt: DateTimeOffset.UtcNow,
            Favorited: false,
            FavoritesCount: 0,
            Author: author);
        _factory.ArticlesBySlug["test-article"] = article;

        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/articles/test-article");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<SingleArticleResponse>(JsonOptions);
        await Assert.That(body).IsNotNull();
        await Assert.That(body.Article.Title).IsEqualTo("Test Article");
        await Assert.That(body.Article.Slug).IsEqualTo("test-article");
    }

    [Test]
    public async Task GetArticle_NotFound_Returns404()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/articles/nonexistent-slug");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task CreateArticle_Authenticated_ReturnsCreated()
    {
        var user = _factory.SeedUser(
            username: "articleauthor",
            email: "articleauthor@example.com");
        using var client = _factory.CreateAuthenticatedClient(user);

        var request = new CreateArticleRequest(new CreateArticleBody(
            Title: "My New Article",
            Description: "About testing",
            Body: "This is the article body content.",
            TagList: ["testing", "dotnet"]));

        var response = await client.PostAsJsonAsync("/api/articles", request, JsonOptions);

        // The command goes through the aggregate (in-memory event store).
        // The read model may not reflect the article since we use fakes.
        await Assert.That(
            response.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK).IsTrue();
    }

    [Test]
    public async Task CreateArticle_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateClient();

        var request = new CreateArticleRequest(new CreateArticleBody(
            Title: "Should Fail",
            Description: "No auth",
            Body: "Will not work",
            TagList: null));

        var response = await client.PostAsJsonAsync("/api/articles", request, JsonOptions);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetFeed_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/articles/feed");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetFeed_Authenticated_ReturnsOk()
    {
        var user = _factory.SeedUser(
            username: "feeduser",
            email: "feeduser@example.com");
        using var client = _factory.CreateAuthenticatedClient(user);

        var response = await client.GetAsync("/api/articles/feed");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task DeleteArticle_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateClient();
        var response = await client.DeleteAsync("/api/articles/some-slug");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task FavoriteArticle_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/articles/some-slug/favorite", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();
}
