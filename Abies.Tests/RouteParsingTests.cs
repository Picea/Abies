using Route = Abies.Conduit.Routing.Route;
using FsCheck;
using FsCheck.Xunit;

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
        var route = Abies.Conduit.Routing.Route.FromUrl(Abies.Conduit.Routing.Route.Match, url);

        // Assert
        switch (path)
        {
            case "/":
            case "/home":
                Assert.IsType<Abies.Conduit.Routing.Route.Home>(route);
                break;
            case "/login":
                _ = Assert.IsType<Abies.Conduit.Routing.Route.Login>(route);
                break;
            case "/settings":
                Assert.IsType<Abies.Conduit.Routing.Route.Settings>(route);
                break;
            case "/register":
                Assert.IsType<Abies.Conduit.Routing.Route.Register>(route);
                break;
            case "/editor":
                Assert.IsType<Abies.Conduit.Routing.Route.NewArticle>(route);
                break;
            case var p when p.StartsWith("/profile/"):
                Assert.IsType<Abies.Conduit.Routing.Route.Profile>(route);
                break;
            case var p when p.StartsWith("/article/"):
                Assert.IsType<Abies.Conduit.Routing.Route.Article>(route);
                break;
            case var p when p.StartsWith("/editor/"):
                Assert.IsType<Abies.Conduit.Routing.Route.EditArticle>(route);
                break;
            default:
                Assert.IsType<Abies.Conduit.Routing.Route.NotFound>(route);
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
        var route = Abies.Conduit.Routing.Route.FromUrl(Abies.Conduit.Routing.Route.Match, url);

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
            Assert.IsType<Abies.Conduit.Routing.Route.NotFound>(route);
        }
    }
}
