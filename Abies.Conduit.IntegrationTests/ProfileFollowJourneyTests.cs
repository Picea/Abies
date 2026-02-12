using System.Net;
using Abies.Conduit.IntegrationTests.Testing;
using Abies.Conduit.Services;
using Xunit;

namespace Abies.Conduit.IntegrationTests;

public class ProfileFollowJourneyTests
{
    [Fact]
    public async Task FollowThenUnfollow_UsesCorrectEndpoints_AndMapsFollowing()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();

        handler.When(
            HttpMethod.Post,
            "/api/profiles/jake/follow",
            HttpStatusCode.OK,
            new
            {
                profile = new
                {
                    username = "jake",
                    bio = "",
                    image = "",
                    following = true
                }
            });

        handler.When(
            HttpMethod.Delete,
            "/api/profiles/jake/follow",
            HttpStatusCode.OK,
            new
            {
                profile = new
                {
                    username = "jake",
                    bio = "",
                    image = "",
                    following = false
                }
            });

        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5179")
        };

        ApiClient.ConfigureHttpClient(http);
        ApiClient.ConfigureBaseUrl("http://localhost:5179/api");

        // Act
        var followed = await ProfileService.FollowUserAsync("jake");
        var unfollowed = await ProfileService.UnfollowUserAsync("jake");

        // Assert (requests)
        Assert.Contains(handler.Requests, r => r.Method == HttpMethod.Post && r.Uri.PathAndQuery == "/api/profiles/jake/follow");
        Assert.Contains(handler.Requests, r => r.Method == HttpMethod.Delete && r.Uri.PathAndQuery == "/api/profiles/jake/follow");

        // Assert (mapping)
        Assert.True(followed.Following);
        Assert.False(unfollowed.Following);
        Assert.Equal("jake", followed.Username);
        Assert.Equal("jake", unfollowed.Username);
    }
}
