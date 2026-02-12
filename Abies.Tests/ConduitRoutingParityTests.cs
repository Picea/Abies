using System.Runtime.Versioning;
using ConduitRoute = Abies.Conduit.Routing.Route;

namespace Abies.Tests;

[SupportedOSPlatform("browser")]
public class ConduitRoutingParityTests
{
    public static IEnumerable<object[]> KnownPaths()
    {
        // Keep these aligned with `Abies.Conduit/Routing/Route.cs`.
        // We include representative parameter values and a few edge-ish strings.
        yield return Case("/", typeof(ConduitRoute.Home));
        yield return Case("/home", typeof(ConduitRoute.Home));
        yield return Case("/login", typeof(ConduitRoute.Login));
        yield return Case("/settings", typeof(ConduitRoute.Settings));
        yield return Case("/register", typeof(ConduitRoute.Register));
        yield return Case("/editor", typeof(ConduitRoute.NewArticle));

        yield return Case("/profile/johndoe", typeof(ConduitRoute.Profile), ("userName", "johndoe"));
        yield return Case("/profile/jane.doe", typeof(ConduitRoute.Profile), ("userName", "jane.doe"));
        yield return Case("/profile/johndoe/favorites", typeof(ConduitRoute.ProfileFavorites), ("userName", "johndoe"));

        yield return Case("/article/how-to-build", typeof(ConduitRoute.Article), ("slug", "how-to-build"));
        yield return Case("/article/2025-12-31", typeof(ConduitRoute.Article), ("slug", "2025-12-31"));
        yield return Case("/editor/how-to-build", typeof(ConduitRoute.EditArticle), ("slug", "how-to-build"));
    }

    public static IEnumerable<object[]> UnknownPaths()
    {
        yield return new object[] { "/unknown" };
        yield return new object[] { "/unknown/path" };
        yield return new object[] { "/profile" }; // missing required userName
        yield return new object[] { "/profile/" }; // empty segment
        yield return new object[] { "/article" }; // missing required slug
    }

    [Theory]
    [MemberData(nameof(KnownPaths))]
    public void Functional_and_template_router_should_return_same_route_for_known_paths(
        string path,
        Type expectedType,
        Dictionary<string, string> expectedCaptures)
    {
        // Functional
        var functional = ConduitRoute.Match.Parse(path.AsSpan());
        Assert.True(functional.Success, $"Expected functional router to match '{path}'.");

        // Template
        var templatedMatched = ConduitRoute.Templates.TryMatch(path, out var templatedRoute, out var templateMatch);
        Assert.True(templatedMatched, $"Expected template router to match '{path}'.");

        // Parity: route type
        Assert.IsType(expectedType, functional.Value);
        Assert.IsType(expectedType, templatedRoute);

        // Parity: captures
        foreach (var (key, value) in expectedCaptures)
        {
            Assert.Equal(value, GetCaptureFromRoute(functional.Value, key));
            Assert.Equal(value, templateMatch.GetRequired<string>(key));
        }
    }

    [Theory]
    [MemberData(nameof(UnknownPaths))]
    public void Both_routers_should_not_match_unknown_paths(string path)
    {
        var functional = ConduitRoute.Match.Parse(path.AsSpan());
        Assert.False(functional.Success);

        var templatedMatched = ConduitRoute.Templates.TryMatch(path, out var templatedRoute, out var templateMatch);
        Assert.False(templatedMatched);
        Assert.Null(templatedRoute);
        Assert.Equal(Route.RouteMatch.Empty, templateMatch);
    }

    private static object[] Case(string path, Type expectedType, params (string key, string value)[] captures)
    {
        Dictionary<string, string> dict = new(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in captures)
        {
            dict[key] = value;
        }

        return [path, expectedType, dict];
    }

    private static string GetCaptureFromRoute(ConduitRoute route, string key)
    {
        return route switch
        {
            ConduitRoute.Profile r when key.Equals("userName", StringComparison.OrdinalIgnoreCase) => r.UserName.Value,
            ConduitRoute.ProfileFavorites r when key.Equals("userName", StringComparison.OrdinalIgnoreCase) => r.UserName.Value,
            ConduitRoute.Article r when key.Equals("slug", StringComparison.OrdinalIgnoreCase) => r.Slug.Value,
            ConduitRoute.EditArticle r when key.Equals("slug", StringComparison.OrdinalIgnoreCase) => r.Slug.Value,
            _ => throw new InvalidOperationException($"No capture '{key}' available on route type '{route.GetType().Name}'.")
        };
    }
}
