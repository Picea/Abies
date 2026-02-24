using System.Diagnostics;
using Abies.DOM;
using static Abies.Conduit.Main.Message.Event;
using static Abies.Navigation.Command;
using static Abies.UrlRequest;

namespace Abies.Conduit.Main;

/// <summary>
/// Simple logging utility for WASM debugging.
/// </summary>
internal static class Log
{
    [Conditional("DEBUG")]
    internal static void Error(string context, Exception ex) =>
        Console.Error.WriteLine($"[Conduit] {context}: {ex.GetType().Name} - {ex.Message}");

    [Conditional("DEBUG")]
    internal static void Warn(string message) =>
        Console.WriteLine($"[Conduit] WARN: {message}");
}

public record struct UserName(string Value);

public record struct Slug(string Value);

public record struct Email(string Value);

public record struct Token(string Value);

public record User(UserName Username, Email Email, Token Token, string Image, string Bio);

/// <summary>
/// Main application model.
/// IsInitializing is true until the initial user check from localStorage completes.
/// This prevents the UI from briefly showing "Sign in" before auth state loads.
/// </summary>
public record Model(Page Page, Routing.Route CurrentRoute, User? CurrentUser = null, bool IsInitializing = false);

public interface Message : Abies.Message
{
    interface Command : Message
    {
        sealed record ChangeRoute(Routing.Route? Route) : Abies.Command;
        sealed record LoadCurrentUser : Abies.Command;
    }

    interface Event : Message
    {
        sealed record UrlChanged(Url Url) : Event;
        sealed record LinkClicked(UrlRequest UrlRequest) : Event;
        sealed record UserLoggedIn(User User) : Event;
        sealed record UserLoggedOut : Event;
        /// <summary>
        /// Dispatched when the initial user check from localStorage completes with no user found.
        /// </summary>
        sealed record InitializationComplete : Event;
    }
}

public interface Page
{
    sealed record Redirect : Page;
    sealed record NotFound : Page;
    sealed record Home(Conduit.Page.Home.Model Model) : Page;
    sealed record Settings(Conduit.Page.Settings.Model Model) : Page;
    sealed record Login(Conduit.Page.Login.Model Model) : Page;
    sealed record Register(Conduit.Page.Register.Model Model) : Page; sealed record Profile(Conduit.Page.Profile.Model Model) : Page;
    sealed record ProfileFavorites(Conduit.Page.Profile.Model Model) : Page;
    sealed record Article(Conduit.Page.Article.Model Model) : Page;
    sealed record NewArticle(Conduit.Page.Editor.Model Model) : Page;
}

/// <summary>
/// Represents the arguments passed to the application.
/// </summary>
public record Arguments;

/// <summary>
/// The main application class.
/// </summary>
public class Program : Program<Model, Arguments>
{
    private static readonly string Title = "Conduit - {0}";

    /// <summary>
    /// Determines the next model based on the given URL.
    /// </summary>
    /// <param name="url">The URL to process.</param>
    /// <returns>The next model.</returns>
    private static bool RequiresAuth(Routing.Route route)
        => route is Routing.Route.Settings
            or Routing.Route.NewArticle
            or Routing.Route.EditArticle;

    private static (Model model, Command command) GetNextModel(Url url, User? currentUser = null)
    {
        var currentRoute = Routing.Route.FromUrl(Routing.Route.Match, url);
        return currentRoute switch
        {
            Routing.Route.Home => (new(new Page.Home(new Conduit.Page.Home.Model([], 0, [], Conduit.Page.Home.FeedTab.Global, "", true, 0, currentUser)), currentRoute, currentUser), Commands.None),
            Routing.Route.NotFound => (new(new Page.NotFound(), currentRoute, currentUser), Commands.None),
            Routing.Route.Settings => (new(new Page.Settings(new Conduit.Page.Settings.Model(
                ImageUrl: currentUser?.Image ?? string.Empty,
                Username: currentUser?.Username.Value ?? string.Empty,
                Bio: currentUser?.Bio ?? string.Empty,
                Email: currentUser?.Email.Value ?? string.Empty,
                Password: string.Empty,
                IsSubmitting: false,
                Errors: null,
                CurrentUser: currentUser
            )), currentRoute, currentUser), Commands.None),
            Routing.Route.Login => (new(new Page.Login(new Conduit.Page.Login.Model() with { CurrentUser = currentUser }), currentRoute, currentUser), Commands.None),
            Routing.Route.Register => (new(new Page.Register(new Conduit.Page.Register.Model() with { CurrentUser = currentUser }), currentRoute, currentUser), Commands.None),
            Routing.Route.Profile profile => (new(new Page.Profile(new Conduit.Page.Profile.Model(profile.UserName, CurrentUser: currentUser)), currentRoute, currentUser), Commands.None),
            Routing.Route.ProfileFavorites profileFavorites => (new(new Page.ProfileFavorites(new Conduit.Page.Profile.Model(profileFavorites.UserName, ActiveTab: Conduit.Page.Profile.ProfileTab.FavoritedArticles, CurrentUser: currentUser)), currentRoute, currentUser), Commands.None),
            Routing.Route.Article article => (new(new Page.Article(new Conduit.Page.Article.Model(article.Slug, CurrentUser: currentUser)), currentRoute, currentUser), Commands.None),
            Routing.Route.Redirect => (new(new Page.Redirect(), currentRoute, currentUser), Commands.None),
            Routing.Route.NewArticle => (new(new Page.NewArticle(new Conduit.Page.Editor.Model(TagList: [], CurrentUser: currentUser)), currentRoute, currentUser), Commands.None),
            Routing.Route.EditArticle edit => (new(new Page.NewArticle(new Conduit.Page.Editor.Model(IsLoading: true, Slug: edit.Slug.Value, CurrentUser: currentUser)), currentRoute, currentUser), Commands.None),
            _ => (new(new Page.NotFound(), currentRoute, currentUser), Commands.None)
        };
    }

    /// <summary>
    /// Handles the URL changed event.
    /// </summary>
    /// <param name="url">The new URL.</param>
    /// <param name="model">The current model.</param>
    /// <returns>The updated model and commands.</returns>
    private static (Model model, Command) HandleUrlChanged(Url url, Model model)
    {
        var (nextModel, _) = GetNextModel(url, model.CurrentUser);
        if (RequiresAuth(nextModel.CurrentRoute) && nextModel.CurrentUser is null)
        {
            var loginUrl = Url.Create("/login");
            var (loginModel, _) = GetNextModel(loginUrl, null);
            return (loginModel, new PushState(loginUrl));
        }
        return (nextModel, Commands.None);
    }

    /// <summary>
    /// Handles the link clicked event.
    /// </summary>
    /// <param name="urlRequest">The URL request.</param>
    /// <param name="model">The current model.</param>
    /// <returns>The updated model and commands.</returns>
    private static (Model model, Command) HandleLinkClicked(UrlRequest urlRequest, Model model)
    {
        if (urlRequest is Internal @internal)
        {
            var (nextModel, _) = GetNextModel(@internal.Url, model.CurrentUser);
            if (RequiresAuth(nextModel.CurrentRoute) && nextModel.CurrentUser is null)
            {
                var loginUrl = Url.Create("/login");
                var (loginModel, _) = GetNextModel(loginUrl, null);
                return (loginModel, new PushState(loginUrl));
            }
            // Also kick off the route's init command (e.g., prefill editor on edit routes)
            var initForNext = GetInitCommandForRoute(nextModel);
            // Ensure navigation happens first so subsequent init runs against the new page
            return (nextModel, initForNext is Command.None
                ? new PushState(@internal.Url)
                : Commands.Batch(new Command[] { new PushState(@internal.Url), initForNext }));
        }
        return (model, Commands.None);
    }

    /// <summary>
    /// Subscribes to model changes.
    /// </summary>
    /// <param name="model">The current model.</param>
    /// <returns>The subscription.</returns>
    public static Subscription Subscriptions(Model model) => SubscriptionModule.None;

    /// <summary>
    /// Initializes the application with the given URL and arguments.
    /// </summary>
    /// <param name="url">The initial URL.</param>
    /// <param name="argument">The arguments.</param>
    /// <returns>The initial model.</returns>
    private static Command GetInitCommandForRoute(Model model)
        => model.Page switch
        {
            Page.Home => Commands.Batch(new Command[] { new LoadArticlesCommand(), new LoadTagsCommand() }),
            Page.Article article => Commands.Batch(new Command[] { new LoadArticleCommand(article.Model.Slug.Value), new LoadCommentsCommand(article.Model.Slug.Value) }),
            Page.Profile profile => Commands.Batch(new Command[] { new LoadProfileCommand(profile.Model.UserName.Value), new LoadArticlesCommand(null, profile.Model.UserName.Value) }),
            Page.ProfileFavorites profileFav => Commands.Batch(new Command[] { new LoadProfileCommand(profileFav.Model.UserName.Value), new LoadArticlesCommand(null, null, profileFav.Model.UserName.Value) }),
            Page.NewArticle newArticle when model.CurrentRoute is Routing.Route.EditArticle edit => new LoadArticleForEditorCommand(edit.Slug.Value),
            _ => Commands.None
        };

    public static (Model, Command) Initialize(Url url, Arguments argument)
    {
        var (model, _) = GetNextModel(url, null);
        // Set IsInitializing=true until LoadCurrentUser completes
        model = model with { IsInitializing = true };
        Command redirectCommand = Commands.None;
        if (RequiresAuth(model.CurrentRoute))
        {
            var loginUrl = Url.Create("/login");
            (model, _) = GetNextModel(loginUrl, null);
            model = model with { IsInitializing = true };
            redirectCommand = new PushState(loginUrl);
        }
        var init = GetInitCommandForRoute(model);
        if (redirectCommand is Command.None)
        {
            return (model, Commands.Batch(new Command[] { new Message.Command.LoadCurrentUser(), init }));
        }
        else
        {
            return (model, Commands.Batch(new Command[] { new Message.Command.LoadCurrentUser(), init, redirectCommand }));
        }
    }



    /// <summary>
    /// Updates the model based on the given message.
    /// </summary>
    /// <param name="message">The message to process.</param>
    /// <param name="model">The current model.</param>
    /// <returns>The updated model and commands.</returns>
    public static (Model model, Command command) Update(Abies.Message message, Model model)
    {
        switch (message)
        {
            case UrlChanged urlChanged:
                var (nextModel, _) = HandleUrlChanged(urlChanged.Url, model);
                var initCommand = GetInitCommandForRoute(nextModel);
                return (nextModel, initCommand);

            case LinkClicked linkClicked:
                return HandleLinkClicked(linkClicked.UrlRequest, model);

            case UserLoggedIn loggedIn:
                return (
                    model with
                    {
                        CurrentUser = loggedIn.User,
                        IsInitializing = false,
                        Page = SetUser(model.Page, loggedIn.User)
                    },
                    Commands.None);

            case InitializationComplete:
                return (model with { IsInitializing = false }, Commands.None);

            case UserLoggedOut:
                return (
                    model with
                    {
                        CurrentUser = null,
                        Page = SetUser(model.Page, null)
                    },
                    Commands.None);

            default:
                return (message, model.Page) switch
                {
                    (Conduit.Page.Login.Message.LoginSuccess success, Page.Login _) =>
                        RedirectAfterAuth(success.User),

                    (Conduit.Page.Login.Message loginMsg, Page.Login login) =>
                        UpdatePage(model, m => new Page.Login(m), login.Model, loginMsg, Conduit.Page.Login.Page.Update),

                    (Conduit.Page.Register.Message.RegisterSuccess success, Page.Register _) =>
                        RedirectAfterAuth(success.User),

                    (Conduit.Page.Register.Message registerMsg, Page.Register register) =>
                        UpdatePage(model, m => new Page.Register(m), register.Model, registerMsg, Conduit.Page.Register.Page.Update),

                    (Conduit.Page.Home.Message homeMsg, Page.Home home) =>
                        UpdatePage(model, m => new Page.Home(m), home.Model, homeMsg, Conduit.Page.Home.Page.Update),

                    (Conduit.Page.Settings.Message.LogoutRequested, Page.Settings _) =>
                        LogoutAndPushHome(),

                    (Conduit.Page.Settings.Message.SettingsSuccess success, Page.Settings _) =>
                        RedirectHome(success.User),

                    (Conduit.Page.Settings.Message settingsMsg, Page.Settings settings) =>
                        UpdatePage(model, m => new Page.Settings(m), settings.Model, settingsMsg, Conduit.Page.Settings.Page.Update),

                    (Conduit.Page.Profile.Message profileMsg, Page.Profile profile) =>
                        UpdatePage(model, m => new Page.Profile(m), profile.Model, profileMsg, Conduit.Page.Profile.Page.Update),

                    (Conduit.Page.Profile.Message profileFavMsg, Page.ProfileFavorites fav) =>
                        UpdatePage(model, m => new Page.ProfileFavorites(m), fav.Model, profileFavMsg, Conduit.Page.Profile.Page.Update),

                    (Conduit.Page.Article.Message.ArticleDeleted, Page.Article _) =>
                        RedirectHome(model.CurrentUser),

                    (Conduit.Page.Article.Message articleMsg, Page.Article article) =>
                        UpdatePage(model, m => new Page.Article(m), article.Model, articleMsg, Conduit.Page.Article.Page.Update),

                    (Conduit.Page.Editor.Message.ArticleSubmitSuccess success, Page.NewArticle _) =>
                        RedirectToArticle(success.Slug, model.CurrentUser),

                    (Conduit.Page.Editor.Message editorMsg, Page.NewArticle editor) =>
                        UpdatePage(model, m => new Page.NewArticle(m), editor.Model, editorMsg, Conduit.Page.Editor.Page.Update),

                    _ => (model, Commands.None)
                };
        }

    }

    private static Page SetUser(Page page, User? user) => page switch
    {
        Page.Home p => new Page.Home(p.Model with { CurrentUser = user }),
        Page.Settings s => new Page.Settings(s.Model with { CurrentUser = user }),
        Page.Login l => new Page.Login(l.Model with { CurrentUser = user }),
        Page.Register r => new Page.Register(r.Model with { CurrentUser = user }),
        Page.Profile pr => new Page.Profile(pr.Model with { CurrentUser = user }),
        Page.ProfileFavorites pf => new Page.ProfileFavorites(pf.Model with { CurrentUser = user }),
        Page.Article a => new Page.Article(a.Model with { CurrentUser = user }),
        Page.NewArticle e => new Page.NewArticle(e.Model with { CurrentUser = user }),
        _ => page
    };

    private static (Model, Command) UpdatePage<TModel, TMsg>(Model model, Func<TModel, Page> ctor, TModel pageModel, TMsg msg, Func<TMsg, TModel, (TModel, Command)> update)
        where TMsg : Abies.Message
    {
        var (nextPageModel, cmd) = update(msg, pageModel);
        return (model with { Page = ctor(nextPageModel) }, cmd);
    }

    private static (Model, Command) RedirectAfterAuth(User user)
    {
        var url = Url.Create("/");
        var (homeModel, _) = GetNextModel(url, user);
        var init = GetInitCommandForRoute(homeModel);
        var batch = Commands.Batch(new Command[] { new PushState(url), init });
        return (homeModel with { CurrentUser = user }, batch);
    }

    private static (Model, Command) RedirectHome(User? user)
    {
        var current = new Uri(Interop.GetCurrentUrl());
        var root = new Uri(current.GetLeftPart(UriPartial.Authority) + "/");
        var url = Url.Create(root.ToString());
        var (next, _) = GetNextModel(url, user);
        return (next, new PushState(url));
    }

    private static (Model, Command) RedirectToArticle(string slug, User? user)
    {
        var current = new Uri(Interop.GetCurrentUrl());
        var root = new Uri(current.GetLeftPart(UriPartial.Authority) + "/");
        var url = Url.Create($"{root}article/{slug}");
        var (next, _) = GetNextModel(url, user);
        var init = GetInitCommandForRoute(next);
        var batch = Commands.Batch(new Command[] { new PushState(url), init });
        return (next, batch);
    }

    private static (Model, Command) LogoutAndPushHome()
    {
        var (next, push) = RedirectHome(null);
        return (next, Commands.Batch(new Command[] { new LogoutCommand(), push }));
    }

    private static Node WithLayout(Node page, Model model) =>
        div([], [
            Navigation.View(model),
            page
        ]);

    public static Document View(Model model)
        => model.Page switch
        {
            Page.Redirect => new Document(
                string.Format(Title, "Redirect"),
                WithLayout(h1([], [text("Redirecting...")]), model),
                Head.meta("robots", "noindex")),

            Page.NotFound => new Document(
                string.Format(Title, "Not Found"),
                WithLayout(h1([], [text("Not Found")]), model),
                Head.meta("robots", "noindex"),
                Head.meta("description", "Page not found")),

            Page.Home home => new Document(
                string.Format(Title, nameof(Conduit.Page.Home)),
                WithLayout(Conduit.Page.Home.Page.View(home.Model), model),
                Head.meta("description", "Conduit - A place to share your knowledge."),
                Head.og("title", "Conduit"),
                Head.og("type", "website"),
                Head.og("description", "A place to share your knowledge.")),

            Page.Settings settings => new Document(
                string.Format(Title, nameof(Conduit.Page.Settings)),
                WithLayout(Conduit.Page.Settings.Page.View(settings.Model), model),
                Head.meta("robots", "noindex")),

            Page.Login loginDoc => new Document(
                string.Format(Title, nameof(Conduit.Page.Login)),
                WithLayout(Conduit.Page.Login.Page.View(loginDoc.Model), model),
                Head.meta("description", "Sign in to Conduit")),

            Page.Register register => new Document(
                string.Format(Title, nameof(Conduit.Page.Register)),
                WithLayout(Conduit.Page.Register.Page.View(register.Model), model),
                Head.meta("description", "Sign up for Conduit")),

            Page.Profile profile => new Document(
                string.Format(Title, nameof(Conduit.Page.Profile)),
                WithLayout(Conduit.Page.Profile.Page.View(profile.Model), model),
                Head.meta("description", $"Profile of {profile.Model.UserName.Value}"),
                Head.og("title", $"{profile.Model.UserName.Value} - Conduit"),
                Head.og("type", "profile")),

            Page.ProfileFavorites profileFavorites => new Document(
                string.Format(Title, "Profile Favorites"),
                WithLayout(Conduit.Page.Profile.Page.View(profileFavorites.Model), model),
                Head.meta("description", $"Favorite articles of {profileFavorites.Model.UserName.Value}"),
                Head.og("title", $"{profileFavorites.Model.UserName.Value}'s Favorites - Conduit"),
                Head.og("type", "profile")),

            Page.Article article => ViewArticle(article, model),

            Page.NewArticle newArticle => new Document(
                string.Format(Title, model.CurrentRoute is Routing.Route.EditArticle ? "Edit Article" : "New Article"),
                WithLayout(Conduit.Page.Editor.Page.View(newArticle.Model), model),
                Head.meta("robots", "noindex")),

            _ => new Document(
                string.Format(Title, "Not Found"),
                WithLayout(h1([], [text("Not Found")]), model),
                Head.meta("robots", "noindex"))
        };

    /// <summary>
    /// Creates the Document for an article page, including Open Graph and structured data
    /// when article data is available.
    /// </summary>
    private static Document ViewArticle(Page.Article article, Model model)
    {
        var body = WithLayout(Conduit.Page.Article.Page.View(article.Model), model);
        var title = string.Format(Title, nameof(Conduit.Page.Article));

        // When article data hasn't loaded yet, return minimal head
        if (article.Model.Article is not { } art)
            return new Document(title, body,
                Head.meta("description", "Loading article..."));

        return new Document(title, body,
            Head.meta("description", art.Description),
            Head.og("title", art.Title),
            Head.og("type", "article"),
            Head.og("description", art.Description),
            Head.property("article:published_time", art.CreatedAt),
            Head.property("article:author", art.Author.Username));
    }





    public static Abies.Message OnUrlChanged(Url url)
        => new UrlChanged(url);

    public static Abies.Message OnLinkClicked(UrlRequest urlRequest)
        => new LinkClicked(urlRequest);
}
