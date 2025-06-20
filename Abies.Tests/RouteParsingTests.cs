using Abies.Conduit.Routing;
using FsCheck;
using FsCheck.Xunit;
using static Abies.Url;

namespace Abies.Tests;

public static class ValidPathGenerator
{
    public static Arbitrary<string> Arbitrary() =>
        Arb.From(Gen.Elements(
            "/",
            "/home",
            "/login",
            "/settings",
            "/register",
            "/profile/johndoe",
            "/article/interesting-article",
            "/editor",
            "/editor/interesting-article"));
}

public class RouteParsingTests
{

    [Property(Arbitrary = new[] { typeof(ValidPathGenerator) })]
    public void RouteParser_ShouldCorrectlyParseKnownRoutes(string path)
    {
        // Arrange
        var url = Url.Create(new($"http://localhost:80{path}"));

        // Act
        var route = Route.FromUrl(Route.Match, url);

        // Assert
        switch (path)
        {
            case "/":
            case "/home":
                Assert.IsType<Route.Home>(route);
                break;
            case "/login":
                Assert.IsType<Route.Login>(route);
                break;
            case "/settings":
                Assert.IsType<Route.Settings>(route);
                break;
            case "/register":
                Assert.IsType<Route.Register>(route);
                break;
            case "/editor":
                Assert.IsType<Route.NewArticle>(route);
                break;
            case var p when p.StartsWith("/profile/"):
                Assert.IsType<Route.Profile>(route);
                break;
            case var p when p.StartsWith("/article/"):
                Assert.IsType<Route.Article>(route);
                break;
            case var p when p.StartsWith("/editor/"):
                Assert.IsType<Route.EditArticle>(route);
                break;
            default:
                Assert.IsType<Route.NotFound>(route);
                break;
        }
    }

    [Property]
    public void RouteParser_ShouldReturnNotFound_ForUnknownRoutes(NonEmptyString randomPath)
    {
        // Arrange
        var path = "/" + randomPath.Get;
        var url = Url.Create(new($"http://localhost:80{path}"));

        // Act
        var route = Route.FromUrl(Route.Match, url);

        // Assert
        // Assuming known routes are limited to those defined earlier
        var knownRoutes = new[]
        {
            "/",
            "/home",
            "/login",
            "/settings",
            "/register",
            "/editor",
            "/profile/",
            "/article/",
            "/editor/"
        };

        var isKnownRoute = knownRoutes.Any(r => path == r || path.StartsWith(r));
        if (!isKnownRoute)
        {
            Assert.IsType<Route.NotFound>(route);
        }
    }

    //[Property]
    //public Property RouteParser_ShouldHandleAnyValidPath()
    //{
    //    // Generate any string that can represent a path
    //    return Prop.ForAll(Arb.Generate<string>(), path =>
    //    {
    //        // Arrange
    //        var url = Url.Create(new($"http://localhost:80{path}"));

    //        // Act
    //        var route = Route.FromUrl(Route.Match, url);

    //        // Assert
    //        // The parser should not throw exceptions
    //        return true.ToProperty();
    //    });
    //}
}
