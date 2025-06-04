using Abies.Conduit.Main;
using static Abies.Route.Parse;

namespace Abies.Conduit.Routing;

public abstract record Route
{
    public sealed record Root : Route;
    public sealed record Redirect : Route;
    public sealed record NotFound : Route;
    public sealed record Home : Route;
    public sealed record Settings : Route;
    public sealed record Login : Route;
    public sealed record Register : Route;    public sealed record Profile(UserName UserName) : Route;
    public sealed record ProfileFavorites(UserName UserName) : Route;
    public sealed record Article(Slug Slug) : Route;
    public sealed record NewArticle : Route;
    public sealed record EditArticle(Slug Slug) : Route;

    /// <summary>
    /// Parse a route from a URL. 
    /// When the URL is not recognized, a NotFound route is returned.
    /// 
    /// todo: move this to Abies.
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public static Route FromUrl(Parser<Route> parser, Url url)
    {
        var result = parser.Parse(url.Path.Value);
        return result.Success 
            ? result.Value 
            : new NotFound();
    }    /// <summary>    /// The parser for the route table. Be carefull when parsing routes using the | (OR)
    /// parser, because it will select the FIRST match and NOT the BEST match
    /// </summary>
    public static Parser<Route> Match =>
        // We need to place more specific routes before less specific ones
        // match the path /profile/{username}/favorites where {username} is a string
        ((Segment.String("profile") / Abies.Route.Parse.String / Segment.String("favorites")).Map(Handlers.ProfileFavorites))
        // match the path /profile/{username} where {username} is a string
        | ((Segment.String("profile") / Abies.Route.Parse.String).Map(Handlers.Profile))
        // match the path /article/{slug} where {slug} is a string
        | ((Segment.String("article") / Abies.Route.Parse.String).Map(Handlers.Article))
        // match the path /editor/{slug} where {slug} is a string
        | ((Segment.String("editor") / Abies.Route.Parse.String).Map(Handlers.EditArticle))
        // match the path /editor
        | (Segment.String("editor").Map(Handlers.NewArticle))
        // match the path /home
        | (Segment.String("home").Map(Handlers.Home))
        // match the path /login
        | (Segment.String("login").Map(Handlers.Login))
        // match the path /settings
        | (Segment.String("settings").Map(Handlers.Settings))
        // match the path /register
        | (Segment.String("register").Map(Handlers.Register))
        // match the root path
        | (Segment.Empty.Select(Handlers.Home));


}

public static class Handlers
{
    public static Route Home(string _) => new Route.Home();
    public static Route Login(string _) => new Route.Login();
    public static Route Settings(string _) => new Route.Settings();
    public static Route Profile(string userName) => new Route.Profile(new UserName(userName));
    public static Route ProfileFavorites(string userName) => new Route.ProfileFavorites(new UserName(userName));
    public static Route Register(string _) => new Route.Register();
    public static Route Article(string slug) => new Route.Article(new Slug(slug));
    public static Route NewArticle(string _) => new Route.NewArticle();
    public static Route EditArticle(string slug) => new Route.EditArticle(new Slug(slug));
}



    