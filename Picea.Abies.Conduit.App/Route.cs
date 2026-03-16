// =============================================================================
// Route — URL ↔ Page Routing
// =============================================================================

namespace Picea.Abies.Conduit.App;

public static class Route
{
    public static (Page Page, Command Command) FromUrl(Url url, Session? session, string apiUrl) =>
        url.Path switch
        {
        [] or [""] => HomeRoute(url.Query, session, apiUrl),
            ["login"] => LoginRoute(),
            ["register"] => RegisterRoute(),
            ["settings"] => SettingsRoute(session),
            ["editor"] => EditorRoute(null, session?.Token, apiUrl),
            ["editor", var slug] => EditorRoute(slug, session?.Token, apiUrl),
            ["article", var slug] => ArticleRoute(slug, session?.Token, apiUrl),
            ["tag", var tag] => TagRoute(tag, url.Query, session, apiUrl),
            ["profile", var username] => ProfileRoute(username, showFavorites: false, url.Query, session?.Token, apiUrl),
            ["profile", var username, "favorites"] => ProfileRoute(username, showFavorites: true, url.Query, session?.Token, apiUrl),
            _ => (new Page.NotFound(), Commands.None)
        };

    public static Url ToUrl(Page page) => page switch
    {
        Page.Home { Data: var home } => HomeUrl(home),
        Page.Login => MakeUrl(["login"]),
        Page.Register => MakeUrl(["register"]),
        Page.Settings => MakeUrl(["settings"]),
        Page.Editor { Data.Slug: null } => MakeUrl(["editor"]),
        Page.Editor { Data.Slug: var slug } => MakeUrl(["editor", slug]),
        Page.Article { Data.Slug: var slug } => MakeUrl(["article", slug]),
        Page.Profile { Data: { ShowFavorites: true } profile } => ProfileUrl(profile),
        Page.Profile { Data: var profile } => ProfileUrl(profile),
        _ => MakeUrl([])
    };

    private static (Page, Command) HomeRoute(IReadOnlyDictionary<string, string> query, Session? session, string apiUrl)
    {
        if (query.TryGetValue("feed", out var feed) && feed == "following" && session is null)
            return LoginRoute();

        var tab = query.TryGetValue("feed", out feed) && feed == "following" && session is not null
            ? FeedTab.Your
            : session is not null ? FeedTab.Your : FeedTab.Global;
        var currentPage = ParsePage(query);
        var offset = (currentPage - 1) * Constants.ArticlesPerPage;
        var model = new HomeModel(tab, null, [], 0, currentPage, [], true);
        Command fetchArticles = tab == FeedTab.Your && session is not null
            ? new FetchFeed(apiUrl, session.Token, Constants.ArticlesPerPage, offset)
            : new FetchArticles(apiUrl, session?.Token, Constants.ArticlesPerPage, offset);
        return (new Page.Home(model), Commands.Batch(fetchArticles, new FetchTags(apiUrl)));
    }

    private static (Page, Command) TagRoute(string tag, IReadOnlyDictionary<string, string> query, Session? session, string apiUrl)
    {
        var currentPage = ParsePage(query);
        var offset = (currentPage - 1) * Constants.ArticlesPerPage;
        var model = new HomeModel(FeedTab.Tag, tag, [], 0, currentPage, [], true);
        return (new Page.Home(model), Commands.Batch(
            new FetchArticles(apiUrl, session?.Token, Constants.ArticlesPerPage, offset, Tag: tag),
            new FetchTags(apiUrl)));
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
        if (token is null)
            return LoginRoute();

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

    private static (Page, Command) ProfileRoute(string username, bool showFavorites, IReadOnlyDictionary<string, string> query, string? token, string apiUrl)
    {
        var currentPage = ParsePage(query);
        var offset = (currentPage - 1) * Constants.ArticlesPerPage;
        var model = new ProfileModel(username, null, [], 0, currentPage, showFavorites, true);
        Command articleCmd = showFavorites
            ? new FetchArticles(apiUrl, token, Constants.ArticlesPerPage, offset, Favorited: username)
            : new FetchArticles(apiUrl, token, Constants.ArticlesPerPage, offset, Author: username);
        return (new Page.Profile(model), Commands.Batch(
            new FetchProfile(apiUrl, token, username), articleCmd));
    }

    private static Url HomeUrl(HomeModel home)
    {
        var query = new Dictionary<string, string>();
        string[] path = [];

        if (home.ActiveTab == FeedTab.Your)
            query["feed"] = "following";
        else if (home.ActiveTab == FeedTab.Tag && home.SelectedTag is not null)
            path = ["tag", home.SelectedTag];

        if (home.CurrentPage > 1)
            query["page"] = home.CurrentPage.ToString();

        return MakeUrl(path, query);
    }

    private static Url ProfileUrl(ProfileModel profile)
    {
        var path = profile.ShowFavorites
            ? new[] { "profile", profile.Username, "favorites" }
            : new[] { "profile", profile.Username };

        var query = new Dictionary<string, string>();
        if (profile.CurrentPage > 1)
            query["page"] = profile.CurrentPage.ToString();

        return MakeUrl(path, query);
    }

    private static int ParsePage(IReadOnlyDictionary<string, string> query) =>
        query.TryGetValue("page", out var value) && int.TryParse(value, out var page) && page > 0
            ? page
            : 1;

    private static Url MakeUrl(string[] path, IReadOnlyDictionary<string, string>? query = null) =>
        new(path, query ?? new Dictionary<string, string>(), Option<string>.None);
}
