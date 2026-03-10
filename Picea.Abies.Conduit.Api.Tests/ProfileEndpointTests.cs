// =============================================================================
// Profile Endpoint Tests — Profile Viewing & Follow/Unfollow
// =============================================================================
// Tests the /api/profiles endpoints including:
//   - GET profile returns public profile
//   - GET nonexistent profile returns 404
//   - Follow/unfollow require authentication
// =============================================================================

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Picea.Abies.Conduit.Api.Dto;

namespace Picea.Abies.Conduit.Api.Tests;

public sealed class ProfileEndpointTests : IClassFixture<ConduitApiFactory>
{
    private readonly ConduitApiFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ProfileEndpointTests(ConduitApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProfile_ExistingUser_ReturnsProfile()
    {
        var user = _factory.SeedUser(
            username: "profileuser",
            email: "profile@example.com",
            bio: "A bio");

        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/profiles/profileuser");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ProfileResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("profileuser", body.Profile.Username);
        Assert.Equal("A bio", body.Profile.Bio);
        Assert.False(body.Profile.Following);
    }

    [Fact]
    public async Task GetProfile_NonexistentUser_Returns404()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/profiles/nobody_here");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task FollowUser_Unauthenticated_Returns401()
    {
        _factory.SeedUser(username: "followtarget", email: "followtarget@example.com");

        using var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/profiles/followtarget/follow", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UnfollowUser_Unauthenticated_Returns401()
    {
        _factory.SeedUser(username: "unfollowtarget", email: "unfollowtarget@example.com");

        using var client = _factory.CreateClient();
        var response = await client.DeleteAsync("/api/profiles/unfollowtarget/follow");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
