// =============================================================================
// Comment Endpoint Tests — Article Comment Operations
// =============================================================================
// Tests the /api/articles/:slug/comments endpoints including:
//   - GET comments is publicly accessible
//   - POST comment requires authentication
//   - DELETE comment requires authentication
// =============================================================================

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Picea.Abies.Conduit.Api.Dto;

namespace Picea.Abies.Conduit.Api.Tests;

public sealed class CommentEndpointTests : IClassFixture<ConduitApiFactory>
{
    private readonly ConduitApiFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public CommentEndpointTests(ConduitApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetComments_NoAuth_ReturnsOk()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/articles/any-slug/comments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<MultipleCommentsResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.NotNull(body.Comments);
    }

    [Fact]
    public async Task AddComment_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateClient();

        var request = new AddCommentRequest(new AddCommentBody("Nice article!"));
        var response = await client.PostAsJsonAsync(
            "/api/articles/any-slug/comments", request, JsonOptions);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteComment_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateClient();
        var commentId = Guid.NewGuid();
        var response = await client.DeleteAsync(
            $"/api/articles/any-slug/comments/{commentId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
