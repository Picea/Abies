using System.Runtime.Versioning;
using ConduitRoute = Abies.Conduit.Routing.Route;

namespace Abies.Tests;

[SupportedOSPlatform("browser")]
public class ProfileFavoritesTests
{
    [Fact]
    public void ProfileFavorites_Route_Type_Should_Exist()
    {
        // Verify ProfileFavorites route type exists in the routing namespace
        var routingAssembly = typeof(ConduitRoute).Assembly;
        var profileFavoritesType = routingAssembly.GetType("Abies.Conduit.Routing.Route+ProfileFavorites");

        Assert.NotNull(profileFavoritesType);
        Assert.True(profileFavoritesType.IsClass);
    }

    [Fact]
    public void ProfileFavorites_Template_Route_Should_Match()
    {
        var matched = ConduitRoute.Templates.TryMatch("/profile/janedoe/favorites", out var route, out var match);

        Assert.True(matched);
        var profileFavorites = Assert.IsType<ConduitRoute.ProfileFavorites>(route);
        Assert.Equal("janedoe", profileFavorites.UserName.Value);
        Assert.Equal("janedoe", match.GetRequired<string>("userName"));
    }

    [Fact]
    public void Profile_And_ProfileFavorites_Routes_Should_Be_Distinct_Types()
    {
        // Verify that Profile and ProfileFavorites are distinct route types
        var routingAssembly = typeof(ConduitRoute).Assembly;
        var profileType = routingAssembly.GetType("Abies.Conduit.Routing.Route+Profile");
        var profileFavoritesType = routingAssembly.GetType("Abies.Conduit.Routing.Route+ProfileFavorites");

        Assert.NotNull(profileType);
        Assert.NotNull(profileFavoritesType);
        Assert.NotEqual(profileType, profileFavoritesType);
    }
}
