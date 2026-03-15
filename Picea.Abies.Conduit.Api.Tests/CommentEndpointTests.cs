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

public sealed class CommentEndpointTests
{
    private readonly ConduitApiFactory _factory = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Test]
    public async Task GetComments_NoAuth_ReturnsOk()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/articles/any-slug/comments");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<MultipleCommentsResponse>(JsonOptions);
        await Assert.That(body).IsNotNull();
        await Assert.That(body.Comments).IsNotNull();
    }

    [Test]
    public async Task AddComment_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateClient();

        var request = new AddCommentRequest(new AddCommentBody("Nice article!"));
        var response = await client.PostAsJsonAsync(
            "/api/articles/any-slug/comments", request, JsonOptions);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task DeleteComment_Unauthenticated_Returns401()
    {
        using var client = _factory.CreateClient();
        var commentId = Guid.NewGuid();
        var response = await client.DeleteAsync(
            $"/api/articles/any-slug/comments/{commentId}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
