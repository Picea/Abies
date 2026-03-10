// =============================================================================
// Route — URL ↔ Page Routing
// =============================================================================

using Picea;

namespace Picea.Abies.Conduit.App;

public static class Route
{
    public static (Page Page, Command Command) FromUrl(Url url, Session? session, string apiUrl) =>
        url.Path switch
        {
        [] or [""] => HomeRoute(session, apiUrl),
            ["login"] => LoginRoute(),
            ["register"] => RegisterRoute(),
            ["settings"] => SettingsRoute(session),
            ["editor"] => EditorRoute(null, session?.Token, apiUrl),
            ["editor", var slug] => EditorRoute(slug, session?.Token, apiUrl),
            ["article", var slug] => ArticleRoute(slug, session?.Token, apiUrl),
            ["profile", var username] => ProfileRoute(username, showFavorites: false, session?.Token, apiUrl),
            ["profile", var username, "favorites"] => ProfileRoute(username, showFavorites: true, session?.Token, apiUrl),
            _ => (new Page.NotFound(), Commands.None)
        };

    public static Url ToUrl(Page page) => page switch
    {
        Page.Home => MakeUrl([]),
        Page.Login => MakeUrl(["login"]),
        Page.Register => MakeUrl(["register"]),
        Page.Settings => MakeUrl(["settings"]),
        Page.Editor { Data.Slug: null } => MakeUrl(["editor"]),
        Page.Editor { Data.Slug: var slug } => MakeUrl(["editor", slug]),
        Page.Article { Data.Slug: var slug } => MakeUrl(["article", slug]),
        Page.Profile { Data: { ShowFavorites: true, Username: var u } } => MakeUrl(["profile", u, "favorites"]),
        Page.Profile { Data.Username: var u } => MakeUrl(["profile", u]),
        _ => MakeUrl([])
    };

    private static (Page, Command) HomeRoute(Session? session, string apiUrl)
    {
        var tab = session is not null ? FeedTab.Your : FeedTab.Global;
        var model = new HomeModel(tab, null, [], 0, 1, [], true);
        var fetchArticles = tab switch
        {
            FeedTab.Your => (Command)new FetchFeed(apiUrl, session!.Token, Constants.ArticlesPerPage, 0),
            _ => new FetchArticles(apiUrl, session?.Token, Constants.ArticlesPerPage, 0)
        };
        return (new Page.Home(model), Commands.Batch(fetchArticles, new FetchTags(apiUrl)));
    }

    private static (Page, Command) LoginRoute() =>
        (new Page.Login(new LoginModel("", "", [], false)), Commands.None);

    private static (Page, Command) RegisterRoute() =>
        (new Page.Register(new RegisterModel("", "", "", [], false)), Commands.None);

    private static (Page, Command) SettingsRoute(Session? session)
    {
        if (session is null)
            return (new Page.Login(new LoginModel("", "", [], false)), Commands.None);
        return (new Page.Settings(new SettingsModel(
            session.Image ?? "", session.Username, session.Bio,
            session.Email, "", [], false)), Commands.None);
    }

    private static (Page, Command) EditorRoute(string? slug, string? token, string apiUrl)
    {
        var model = new EditorModel(slug, "", "", "", "", [], [], false);
        var command = slug is not null ? new FetchArticle(apiUrl, token, slug) : Commands.None;
        return (new Page.Editor(model), command);
    }

    private static (Page, Command) ArticleRoute(string slug, string? token, string apiUrl)
    {
        var model = new ArticleModel(slug, null, [], "", true);
        return (new Page.Article(model), Commands.Batch(
            new FetchArticle(apiUrl, token, slug),
            new FetchComments(apiUrl, token, slug)));
    }

    private static (Page, Command) ProfileRoute(string username, bool showFavorites, string? token, string apiUrl)
    {
        var model = new ProfileModel(username, null, [], 0, 1, showFavorites, true);
        Command articleCmd = showFavorites
            ? new FetchArticles(apiUrl, token, Constants.ArticlesPerPage, 0, Favorited: username)
            : new FetchArticles(apiUrl, token, Constants.ArticlesPerPage, 0, Author: username);
        return (new Page.Profile(model), Commands.Batch(
            new FetchProfile(apiUrl, token, username), articleCmd));
    }

    private static Url MakeUrl(string[] path) =>
        new(path, new Dictionary<string, string>(), Option<string>.None);
}
