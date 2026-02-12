using System.Net;
using System.Text.Json;
using Abies.Conduit.IntegrationTests.Testing;
using Abies.Conduit.Services;
using Xunit;

namespace Abies.Conduit.IntegrationTests;

public class ArticleCrudJourneyTests
{
    [Fact]
    public async Task CreateUpdateDeleteArticle_UsesExpectedEndpoints_AndSendsExpectedBodies()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();

        // Create
        handler.When(
            HttpMethod.Post,
            "/api/articles",
            HttpStatusCode.OK,
            new
            {
                article = ConduitApiFixtures.Article(
                    slug: "crud-slug",
                    title: "Created",
                    description: "Desc",
                    body: "Body",
                    tagList: ["one", "two"])
            });

        // Update
        handler.When(
            HttpMethod.Put,
            "/api/articles/crud-slug",
            HttpStatusCode.OK,
            new
            {
                article = ConduitApiFixtures.Article(
                    slug: "crud-slug",
                    title: "Updated",
                    description: "Desc2",
                    body: "Body2",
                    tagList: ["one"])
            });

        // Delete (NoContent)
        handler.When(HttpMethod.Delete, "/api/articles/crud-slug", HttpStatusCode.NoContent, jsonBody: null);

        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5179")
        };

        ApiClient.ConfigureHttpClient(http);
        ApiClient.ConfigureBaseUrl("http://localhost:5179/api");

        // Act
        var created = await ArticleService.CreateArticleAsync(
            title: "Created",
            description: "Desc",
            body: "Body",
            tagList: new() { "one", "two" });

        var updated = await ArticleService.UpdateArticleAsync(
            slug: "crud-slug",
            title: "Updated",
            description: "Desc2",
            body: "Body2");

        await ArticleService.DeleteArticleAsync("crud-slug");

        // Assert (requests)
        var createReq = handler.Requests.Single(r => r.Method == HttpMethod.Post && r.Uri.PathAndQuery == "/api/articles");
        var updateReq = handler.Requests.Single(r => r.Method == HttpMethod.Put && r.Uri.PathAndQuery == "/api/articles/crud-slug");
        Assert.Contains(handler.Requests, r => r.Method == HttpMethod.Delete && r.Uri.PathAndQuery == "/api/articles/crud-slug");

        // Assert (mapping)
        Assert.Equal("crud-slug", created.Slug);
        Assert.Equal("Created", created.Title);
        Assert.Equal("Updated", updated.Title);

        // Assert (bodies)
        AssertJsonContains(createReq.Body, "article", "title", "Created");
        AssertJsonContainsArray(createReq.Body, "article", "tagList");

        AssertJsonContains(updateReq.Body, "article", "title", "Updated");
        AssertJsonContains(updateReq.Body, "article", "body", "Body2");
    }

    private static void AssertJsonContains(string? json, params string[] path)
    {
        Assert.False(string.IsNullOrWhiteSpace(json), "Expected request body JSON to be present.");

        using var doc = JsonDocument.Parse(json!);
        JsonElement el = doc.RootElement;

        foreach (var segment in path)
        {
            Assert.Equal(
                JsonValueKind.Object,
                el.ValueKind);
            Assert.True(el.TryGetProperty(segment, out var next), $"Expected JSON to contain property '{segment}'. JSON: {json}");
            el = next;
        }
    }

    private static void AssertJsonContainsArray(string? json, string p1, string p2)
    {
        Assert.False(string.IsNullOrWhiteSpace(json), "Expected request body JSON to be present.");

        using var doc = JsonDocument.Parse(json!);
        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
        Assert.True(doc.RootElement.TryGetProperty(p1, out var e1), $"Expected JSON to contain property '{p1}'. JSON: {json}");
        Assert.Equal(JsonValueKind.Object, e1.ValueKind);
        Assert.True(e1.TryGetProperty(p2, out var e2), $"Expected JSON to contain property '{p1}.{p2}'. JSON: {json}");
        Assert.Equal(JsonValueKind.Array, e2.ValueKind);
    }

    private static void AssertJsonContains(string? json, string p1, string p2, string expectedValue)
    {
        Assert.False(string.IsNullOrWhiteSpace(json), "Expected request body JSON to be present.");

        using var doc = JsonDocument.Parse(json!);
        Assert.Equal(JsonValueKind.Object, doc.RootElement.ValueKind);
        Assert.True(doc.RootElement.TryGetProperty(p1, out var e1), $"Expected JSON to contain property '{p1}'. JSON: {json}");
        Assert.Equal(JsonValueKind.Object, e1.ValueKind);
        Assert.True(e1.TryGetProperty(p2, out var e2), $"Expected JSON to contain property '{p1}.{p2}'. JSON: {json}");
        Assert.Equal(expectedValue, e2.GetString());
    }
}
