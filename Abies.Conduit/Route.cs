using Abies.Conduit.Main;
using FunctionalRouteMatch = Abies.Route.RouteMatch;
using RouteParse = Abies.Route.Parse;
using RouteTemplates = Abies.Route.Templates;

namespace Abies.Conduit.Routing;

public abstract record Route
{
    public sealed record Root : Route;
    public sealed record Redirect : Route;
    public sealed record NotFound : Route;
    public sealed record Home : Route;
    public sealed record Settings : Route;
    public sealed record Login : Route;
    public sealed record Register : Route;
    public sealed record Profile(UserName UserName) : Route;
    public sealed record ProfileFavorites(UserName UserName) : Route;
    public sealed record Article(Slug Slug) : Route;
    public sealed record NewArticle : Route;
    public sealed record EditArticle(Slug Slug) : Route;

    private static readonly Parser<Route> FunctionalRouter = BuildFunctionalRouter();
    private static readonly RouteTemplates.TemplateRouter<Route> TemplateRouter = BuildTemplateRouter();

    /// <summary>
    /// The default functional route parser.
    /// </summary>
    public static Parser<Route> Match => FunctionalRouter;

    /// <summary>
    /// A precompiled ASP.NET-style template router.
    /// </summary>
    public static RouteTemplates.TemplateRouter<Route> Templates => TemplateRouter;

    /// <summary>
    /// Parse a route from a URL. When the URL is not recognized, a <see cref="NotFound"/> route is returned.
    /// </summary>
    public static Route FromUrl(Parser<Route> parser, Url url)
    {
        var result = parser.Parse(url.Path.Value);
        return result.Success ? result.Value : new NotFound();
    }

    /// <summary>
    /// Uses the default functional router to parse a URL.
    /// </summary>
    public static Route FromUrl(Url url) => FromUrl(Match, url);

    /// <summary>
    /// Uses the template router to parse a URL.
    /// </summary>
    public static Route FromUrlUsingTemplates(Url url)
        => TemplateRouter.TryMatch(url.Path.Value, out var route) ? route : new NotFound();

    private static Parser<Route> BuildFunctionalRouter()
    {
        return RouteParse.Path(
                RouteParse.Segment.Literal("profile"),
                RouteParse.Segment.Parameter("userName"),
                RouteParse.Segment.Literal("favorites"))
            .Map(RouteHandlers.ProfileFavorites)
            | RouteParse.Path(
                    RouteParse.Segment.Literal("profile"),
                    RouteParse.Segment.Parameter("userName"))
                .Map(RouteHandlers.Profile)
            | RouteParse.Path(
                    RouteParse.Segment.Literal("article"),
                    RouteParse.Segment.Parameter("slug"))
                .Map(RouteHandlers.Article)
            | RouteParse.Path(
                    RouteParse.Segment.Literal("editor"),
                    RouteParse.Segment.Parameter("slug"))
                .Map(RouteHandlers.EditArticle)
            | RouteParse.Path(RouteParse.Segment.Literal("editor"))
                .Map(RouteHandlers.NewArticle)
            | RouteParse.Path(RouteParse.Segment.Literal("home"))
                .Map(RouteHandlers.Home)
            | RouteParse.Path(RouteParse.Segment.Literal("login"))
                .Map(RouteHandlers.Login)
            | RouteParse.Path(RouteParse.Segment.Literal("settings"))
                .Map(RouteHandlers.Settings)
            | RouteParse.Path(RouteParse.Segment.Literal("register"))
                .Map(RouteHandlers.Register)
            | RouteParse.Root.Map(RouteHandlers.Home);
    }

    private static RouteTemplates.TemplateRouter<Route> BuildTemplateRouter()
        => RouteTemplates.Build<Route>(routes =>
        {
            routes.Map("/profile/{userName}/favorites", RouteHandlers.ProfileFavorites);
            routes.Map("/profile/{userName}", RouteHandlers.Profile);
            routes.Map("/article/{slug}", RouteHandlers.Article);
            routes.Map("/editor/{slug}", RouteHandlers.EditArticle);
            routes.Map("/editor", RouteHandlers.NewArticle);
            routes.Map("/home", RouteHandlers.Home);
            routes.Map("/login", RouteHandlers.Login);
            routes.Map("/settings", RouteHandlers.Settings);
            routes.Map("/register", RouteHandlers.Register);
            routes.Map("/", RouteHandlers.Home);
        });
}

internal static class RouteHandlers
{
    public static Route Home(FunctionalRouteMatch _) => new Route.Home();
    public static Route Login(FunctionalRouteMatch _) => new Route.Login();
    public static Route Settings(FunctionalRouteMatch _) => new Route.Settings();
    public static Route Register(FunctionalRouteMatch _) => new Route.Register();
    public static Route NewArticle(FunctionalRouteMatch _) => new Route.NewArticle();

    public static Route Profile(FunctionalRouteMatch match)
        => new Route.Profile(new UserName(match.GetRequired<string>("userName")));

    public static Route ProfileFavorites(FunctionalRouteMatch match)
        => new Route.ProfileFavorites(new UserName(match.GetRequired<string>("userName")));

    public static Route Article(FunctionalRouteMatch match)
        => new Route.Article(new Slug(match.GetRequired<string>("slug")));

    public static Route EditArticle(FunctionalRouteMatch match)
        => new Route.EditArticle(new Slug(match.GetRequired<string>("slug")));
}
