using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Abies.Conduit.IntegrationTests.Testing;
using Abies.Conduit.Services;
using Xunit;

namespace Abies.Conduit.IntegrationTests;

public class CommentsJourneyTests
{
    [Fact]
    public async Task AddThenDeleteComment_UsesExpectedEndpoints_AndMapsComment()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();

        handler.When(
            HttpMethod.Post,
            "/api/articles/crud-slug/comments",
            HttpStatusCode.OK,
            new
            {
                comment = new
                {
                    id = 123,
                    createdAt = "2025-01-01T00:00:00.000Z",
                    updatedAt = "2025-01-01T00:00:00.000Z",
                    body = "Hello",
                    author = new
                    {
                        username = "tester",
                        bio = "",
                        image = "",
                        following = false
                    }
                }
            });

        handler.When(
            HttpMethod.Delete,
            "/api/articles/crud-slug/comments/123",
            HttpStatusCode.NoContent,
            jsonBody: null);

        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5179")
        };

        ApiClient.ConfigureHttpClient(http);
        ApiClient.ConfigureBaseUrl("http://localhost:5179/api");

        // Act
        var created = await ArticleService.AddCommentAsync("crud-slug", "Hello");
        await ArticleService.DeleteCommentAsync("crud-slug", created.Id);

        // Assert (requests)
        var post = handler.Requests.Single(r => r.Method == HttpMethod.Post && r.Uri.PathAndQuery == "/api/articles/crud-slug/comments");
        Assert.Contains(handler.Requests, r => r.Method == HttpMethod.Delete && r.Uri.PathAndQuery == "/api/articles/crud-slug/comments/123");

        // Assert (mapping)
        Assert.Equal("123", created.Id);
        Assert.Equal("Hello", created.Body);
        Assert.Equal("tester", created.Author.Username);

        // Assert (body shape)
        AssertJsonContains(post.Body, "comment", "body", "Hello");
    }

    private static void AssertJsonContains(string? json, string p1, string p2, string expectedValue)
    {
        Assert.False(string.IsNullOrWhiteSpace(json), "Expected request body JSON to be present.");

        using var doc = JsonDocument.Parse(json!);
        Assert.True(doc.RootElement.TryGetProperty(p1, out var e1), $"Expected JSON to contain property '{p1}'. JSON: {json}");
        Assert.True(e1.TryGetProperty(p2, out var e2), $"Expected JSON to contain property '{p1}.{p2}'. JSON: {json}");
        Assert.Equal(expectedValue, e2.GetString());
    }
}
