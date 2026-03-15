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

public sealed class ProfileEndpointTests
{
    private readonly ConduitApiFactory _factory = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Test]
    public async Task GetProfile_ExistingUser_ReturnsProfile()
    {
        var user = _factory.SeedUser(
            username: "profileuser",
            email: "profile@example.com",
            bio: "A bio");

        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/profiles/profileuser");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ProfileResponse>(JsonOptions);
        await Assert.That(body).IsNotNull();
        await Assert.That(body.Profile.Username).IsEqualTo("profileuser");
        await Assert.That(body.Profile.Bio).IsEqualTo("A bio");
        await Assert.That(body.Profile.Following).IsFalse();
    }

    [Test]
    public async Task GetProfile_NonexistentUser_Returns404()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/profiles/nobody_here");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task FollowUser_Unauthenticated_Returns401()
    {
        _factory.SeedUser(username: "followtarget", email: "followtarget@example.com");

        using var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/profiles/followtarget/follow", null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task UnfollowUser_Unauthenticated_Returns401()
    {
        _factory.SeedUser(username: "unfollowtarget", email: "unfollowtarget@example.com");

        using var client = _factory.CreateClient();
        var response = await client.DeleteAsync("/api/profiles/unfollowtarget/follow");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
