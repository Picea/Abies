using Abies.Conduit.Page.Home;

namespace Abies.Conduit.Services;

public static class ProfileService
{
    public static async Task<Profile> GetProfileAsync(string username)
    {
        var response = await ApiClient.GetProfileAsync(username);
        return new Profile(
            response.Profile.Username,
            response.Profile.Bio ?? "",
            response.Profile.Image ?? "",
            response.Profile.Following
        );
    }

    public static async Task<Profile> FollowUserAsync(string username)
    {
        var response = await ApiClient.FollowUserAsync(username);
        return new Profile(
            response.Profile.Username,
            response.Profile.Bio ?? "",
            response.Profile.Image ?? "",
            response.Profile.Following
        );
    }

    public static async Task<Profile> UnfollowUserAsync(string username)
    {
        var response = await ApiClient.UnfollowUserAsync(username);
        return new Profile(
            response.Profile.Username,
            response.Profile.Bio ?? "",
            response.Profile.Image ?? "",
            response.Profile.Following
        );
    }
}
