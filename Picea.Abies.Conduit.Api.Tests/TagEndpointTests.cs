// =============================================================================
// Tag Endpoint Tests — Tag Listing
// =============================================================================
// Tests the /api/tags endpoint:
//   - GET returns tag list
//   - Endpoint is publicly accessible
// =============================================================================

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Picea.Abies.Conduit.Api.Dto;

namespace Picea.Abies.Conduit.Api.Tests;

public sealed class TagEndpointTests : IAsyncDisposable
{
    private readonly ConduitApiFactory _factory = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Test]
    public async Task GetTags_ReturnsTagList()
    {
        _factory.Tags.Add("elm");
        _factory.Tags.Add("fp");
        _factory.Tags.Add("blazor");

        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/tags");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<TagsResponse>(JsonOptions);
        await Assert.That(body).IsNotNull();
        await Assert.That(body.Tags).IsNotNull();
    }

    [Test]
    public async Task GetTags_NoAuthRequired()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/tags");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();
}
