using System;
using ConduitRoute = Abies.Conduit.Routing.Route;
using Xunit;

namespace Abies.Tests;

public class RouteParserTests
{
    [Theory]
    [InlineData("/profile/johndoe/favorites", typeof(ConduitRoute.ProfileFavorites), "userName", "johndoe")]
    [InlineData("/profile/johndoe", typeof(ConduitRoute.Profile), "userName", "johndoe")]
    [InlineData("/article/how-to-build", typeof(ConduitRoute.Article), "slug", "how-to-build")]
    [InlineData("/editor/how-to-build", typeof(ConduitRoute.EditArticle), "slug", "how-to-build")]
    [InlineData("/editor", typeof(ConduitRoute.NewArticle), null, null)]
    [InlineData("/home", typeof(ConduitRoute.Home), null, null)]
    [InlineData("/login", typeof(ConduitRoute.Login), null, null)]
    [InlineData("/settings", typeof(ConduitRoute.Settings), null, null)]
    [InlineData("/register", typeof(ConduitRoute.Register), null, null)]
    [InlineData("/", typeof(ConduitRoute.Home), null, null)]
    public void Functional_router_matches_expected_paths(string path, Type expectedType, string? expectedKey, string? expectedValue)
    {
        var result = ConduitRoute.Match.Parse(path.AsSpan());

        Assert.True(result.Success, $"Expected path '{path}' to match the functional router.");
        Assert.IsType(expectedType, result.Value);

        if (expectedKey is not null && expectedValue is not null)
        {
            AssertCapturedValue(result.Value, expectedKey, expectedValue);
        }
    }

    [Theory]
    [InlineData("/profile/johndoe/favorites", typeof(ConduitRoute.ProfileFavorites), "userName", "johndoe")]
    [InlineData("/profile/johndoe", typeof(ConduitRoute.Profile), "userName", "johndoe")]
    [InlineData("/article/how-to-build", typeof(ConduitRoute.Article), "slug", "how-to-build")]
    [InlineData("/editor/how-to-build", typeof(ConduitRoute.EditArticle), "slug", "how-to-build")]
    [InlineData("/editor", typeof(ConduitRoute.NewArticle), null, null)]
    [InlineData("/home", typeof(ConduitRoute.Home), null, null)]
    [InlineData("/login", typeof(ConduitRoute.Login), null, null)]
    [InlineData("/settings", typeof(ConduitRoute.Settings), null, null)]
    [InlineData("/register", typeof(ConduitRoute.Register), null, null)]
    [InlineData("/", typeof(ConduitRoute.Home), null, null)]
    public void Template_router_matches_expected_paths(string path, Type expectedType, string? expectedKey, string? expectedValue)
    {
        var matched = ConduitRoute.Templates.TryMatch(path, out var route, out var match);

        Assert.True(matched, $"Expected path '{path}' to match the template router.");
        Assert.IsType(expectedType, route);

        if (expectedKey is not null && expectedValue is not null)
        {
            var value = match.GetRequired<string>(expectedKey);
            Assert.Equal(expectedValue, value);
        }
    }

    [Fact]
    public void Template_router_returns_false_for_unknown_route()
    {
        var matched = ConduitRoute.Templates.TryMatch("/unknown/path", out var route, out var match);

        Assert.False(matched);
        Assert.Equal(Route.RouteMatch.Empty, match);
        Assert.Null(route);
    }

    [Fact]
    public void Functional_router_returns_failure_for_unknown_route()
    {
        var result = ConduitRoute.Match.Parse("/unknown/path".AsSpan());

        Assert.False(result.Success);
    }

    private static void AssertCapturedValue(ConduitRoute route, string key, string expectedValue)
    {
        switch (route)
        {
            case ConduitRoute.Profile profile when key == "userName":
                Assert.Equal(expectedValue, profile.UserName.Value);
                break;
            case ConduitRoute.ProfileFavorites favorites when key == "userName":
                Assert.Equal(expectedValue, favorites.UserName.Value);
                break;
            case ConduitRoute.Article article when key == "slug":
                Assert.Equal(expectedValue, article.Slug.Value);
                break;
            case ConduitRoute.EditArticle editArticle when key == "slug":
                Assert.Equal(expectedValue, editArticle.Slug.Value);
                break;
            default:
                throw new InvalidOperationException($"Unexpected route type '{route.GetType().Name}' for captured key '{key}'.");
        }
    }
}
