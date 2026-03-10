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

public sealed class TagEndpointTests : IClassFixture<ConduitApiFactory>
{
    private readonly ConduitApiFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public TagEndpointTests(ConduitApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetTags_ReturnsTagList()
    {
        _factory.Tags.Add("elm");
        _factory.Tags.Add("fp");
        _factory.Tags.Add("blazor");

        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/tags");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TagsResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.NotNull(body.Tags);
    }

    [Fact]
    public async Task GetTags_NoAuthRequired()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/tags");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
