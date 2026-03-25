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
using Microsoft.Extensions.DependencyInjection;
using Picea.Abies.Conduit.Api.Dto;
using Picea.Abies.Conduit.Api.Infrastructure;
using Picea.Abies.Conduit.Domain.Article;
using Picea.Abies.Conduit.Domain.Shared;
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
    public async Task ListArticles_InvalidLimit_Returns422WithValidationError()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/articles?limit=0&offset=0");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        await Assert.That(body).IsNotNull();
        await Assert.That(body.Errors.Body.Length).IsEqualTo(1);
        await Assert.That(body.Errors.Body[0]).IsEqualTo("limit must be between 1 and 100.");
    }

    [Test]
    public async Task ListArticles_InvalidOffset_Returns422WithValidationError()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/articles?limit=20&offset=-1");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);

        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        await Assert.That(body).IsNotNull();
        await Assert.That(body.Errors.Body.Length).IsEqualTo(1);
        await Assert.That(body.Errors.Body[0]).IsEqualTo("offset must be greater than or equal to 0.");
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
    public async Task CreateArticle_Authenticated_ProjectionMiss_UsesShadowFallback()
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

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<SingleArticleResponse>(JsonOptions);
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.Article.Title).IsEqualTo("My New Article");
        await Assert.That(body.Article.Slug).IsEqualTo("my-new-article");
    }

    [Test]
    public async Task CreateArticle_WithSameIdempotencyKey_ReplaysInitialCreatedResponse()
    {
        var user = _factory.SeedUser(
            username: "articleidempotent",
            email: "articleidempotent@example.com");

        using var client = _factory.CreateAuthenticatedClient(user);

        var request = new CreateArticleRequest(new CreateArticleBody(
            Title: "Idempotent Article",
            Description: "Replay check",
            Body: "Create exactly once, replay many times.",
            TagList: ["replay", "idempotency"]));

        using var firstMessage = new HttpRequestMessage(HttpMethod.Post, "/api/articles")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
        firstMessage.Headers.Add(RequestIdempotencyStore.HeaderName, "create-article-idempotency-001");

        using var firstResponse = await client.SendAsync(firstMessage);
        var firstBody = await firstResponse.Content.ReadAsStringAsync();

        await Assert.That(firstResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);

        using var secondMessage = new HttpRequestMessage(HttpMethod.Post, "/api/articles")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
        secondMessage.Headers.Add(RequestIdempotencyStore.HeaderName, "create-article-idempotency-001");

        using var secondResponse = await client.SendAsync(secondMessage);
        var secondBody = await secondResponse.Content.ReadAsStringAsync();

        await Assert.That(secondResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(secondResponse.Headers.Contains(RequestIdempotencyStore.ReplayHeaderName)).IsTrue();
        await Assert.That(secondBody).IsEqualTo(firstBody);
    }

    [Test]
    public async Task CreateArticle_WithSameIdempotencyKeyAndDifferentPayload_ReturnsValidationError()
    {
        var user = _factory.SeedUser(
            username: "articleidempotent-diff",
            email: "articleidempotent-diff@example.com");

        using var client = _factory.CreateAuthenticatedClient(user);

        var firstRequest = new CreateArticleRequest(new CreateArticleBody(
            Title: "Idempotent Article A",
            Description: "Replay check A",
            Body: "Body A",
            TagList: ["replay"]));

        var secondRequest = new CreateArticleRequest(new CreateArticleBody(
            Title: "Idempotent Article B",
            Description: "Replay check B",
            Body: "Body B",
            TagList: ["replay", "different"]));

        using var firstMessage = new HttpRequestMessage(HttpMethod.Post, "/api/articles")
        {
            Content = JsonContent.Create(firstRequest, options: JsonOptions)
        };
        firstMessage.Headers.Add(RequestIdempotencyStore.HeaderName, "create-article-idempotency-002");

        using var firstResponse = await client.SendAsync(firstMessage);
        await Assert.That(firstResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);

        using var secondMessage = new HttpRequestMessage(HttpMethod.Post, "/api/articles")
        {
            Content = JsonContent.Create(secondRequest, options: JsonOptions)
        };
        secondMessage.Headers.Add(RequestIdempotencyStore.HeaderName, "create-article-idempotency-002");

        using var secondResponse = await client.SendAsync(secondMessage);
        await Assert.That(secondResponse.StatusCode).IsEqualTo(HttpStatusCode.UnprocessableEntity);
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
    public async Task UpdateArticle_Authenticated_ProjectionMiss_UsesShadowFallback()
    {
        var user = _factory.SeedUser(
            username: "projectionupdateuser",
            email: "projectionupdate@example.com");

        using var setupScope = _factory.Services.CreateScope();
        var aggregateStore = setupScope.ServiceProvider.GetRequiredService<AggregateStore>();
        var articleId = ArticleId.New();
        var createResult = await aggregateStore.HandleArticleCommand(
            articleId.Value,
            new ArticleCommand.CreateArticle(
                articleId,
                Title.Create("Projection Miss Update").Value,
                Description.Create("Projection miss update path").Value,
                Body.Create("Seed write model only").Value,
                new HashSet<Tag>(),
                new UserId(user.Id),
                Timestamp.Now()));

        await Assert.That(createResult.IsOk).IsTrue();

        var slug = createResult.Value.Slug.Value;
        _factory.ArticleIdsBySlug[slug] = articleId.Value;

        using var client = _factory.CreateAuthenticatedClient(user);
        var request = new UpdateArticleRequest(new UpdateArticleBody(
            Title: "Updated title",
            Description: null,
            Body: null));

        var response = await client.PutAsJsonAsync($"/api/articles/{slug}", request, JsonOptions);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<SingleArticleResponse>(JsonOptions);
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.Article.Title).IsEqualTo("Updated title");
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
