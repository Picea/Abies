using Abies.Conduit.Routing;
using Abies.Conduit.Services;
using Abies.DOM;
using Abies;
using System;
using System.Threading.Tasks;
using static Abies.Conduit.Main.Message.Event;
using static Abies.UrlRequest;
using static Abies.Navigation.Command;

namespace Abies.Conduit.Main;

public record struct UserName(string Value);

public record struct Slug(string Value);

public record struct Email(string Value);

public record struct Token(string Value);

public record User(UserName Username, Email Email, Token Token, string Image);

public record Model(Page Page, Routing.Route CurrentRoute, User? CurrentUser = null);

public interface Message : Abies.Message
{
    public interface Command : Message
    {
        public sealed record ChangeRoute(Routing.Route? Route) : Abies.Command;
        public sealed record LoadCurrentUser : Abies.Command;
    }

    public interface Event : Message
    {
        public sealed record UrlChanged(Url Url) : Event;
        public sealed record LinkClicked(UrlRequest UrlRequest) : Event;
        public sealed record UserLoggedIn(User User) : Event;
        public sealed record UserLoggedOut : Event;
    }
}

public interface Page
{
    public sealed record Redirect : Page;
    public sealed record NotFound : Page;
    public sealed record Home(Conduit.Page.Home.Model Model) : Page;
    public sealed record Settings(Conduit.Page.Settings.Model Model) : Page;
    public sealed record Login(Conduit.Page.Login.Model Model) : Page;
    public sealed record Register(Conduit.Page.Register.Model Model) : Page;    public sealed record Profile(Conduit.Page.Profile.Model Model) : Page;
    public sealed record ProfileFavorites(Conduit.Page.Profile.Model Model) : Page;
    public sealed record Article(Conduit.Page.Article.Model Model) : Page;
    public sealed record NewArticle(Conduit.Page.Editor.Model Model) : Page;
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
    private static string Title = "Conduit - {0}";

    /// <summary>
    /// Determines the next model based on the given URL.
    /// </summary>
    /// <param name="url">The URL to process.</param>
    /// <returns>The next model.</returns>
    private static bool RequiresAuth(Routing.Route route)
        => route is Routing.Route.Settings
            || route is Routing.Route.NewArticle
            || route is Routing.Route.EditArticle;

    private static (Model model, Command command) GetNextModel(Url url, User? currentUser = null)
    {
        Routing.Route currentRoute = Routing.Route.FromUrl(Routing.Route.Match, url);
        return currentRoute switch
        {
            Routing.Route.Home => (new(new Page.Home(new Conduit.Page.Home.Model(new List<Conduit.Page.Home.Article>(), 0, new List<string>(), Conduit.Page.Home.FeedTab.Global, "", true, 0, currentUser)), currentRoute, currentUser), Commands.None),
            Routing.Route.NotFound => (new(new Page.NotFound(), currentRoute, currentUser), Commands.None),
            Routing.Route.Settings => (new(new Page.Settings(new Conduit.Page.Settings.Model(CurrentUser: currentUser)), currentRoute, currentUser), Commands.None),
            Routing.Route.Login => (new(new Page.Login(new Conduit.Page.Login.Model() with { CurrentUser = currentUser }), currentRoute, currentUser), Commands.None),
            Routing.Route.Register => (new(new Page.Register(new Conduit.Page.Register.Model() with { CurrentUser = currentUser }), currentRoute, currentUser), Commands.None),
            Routing.Route.Profile profile => (new(new Page.Profile(new Conduit.Page.Profile.Model(profile.UserName, CurrentUser: currentUser)), currentRoute, currentUser), Commands.None),
            Routing.Route.ProfileFavorites profileFavorites => (new(new Page.ProfileFavorites(new Conduit.Page.Profile.Model(profileFavorites.UserName, ActiveTab: Conduit.Page.Profile.ProfileTab.FavoritedArticles, CurrentUser: currentUser)), currentRoute, currentUser), Commands.None),
            Routing.Route.Article article => (new(new Page.Article(new Conduit.Page.Article.Model(article.Slug, CurrentUser: currentUser)), currentRoute, currentUser), Commands.None),
            Routing.Route.Redirect => (new(new Page.Redirect(), currentRoute, currentUser), Commands.None),
            Routing.Route.NewArticle => (new(new Page.NewArticle(new Conduit.Page.Editor.Model(CurrentUser: currentUser)), currentRoute, currentUser), Commands.None),
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
        if (RequiresAuth(nextModel.CurrentRoute) && nextModel.CurrentUser == null)
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
            if (RequiresAuth(nextModel.CurrentRoute) && nextModel.CurrentUser == null)
            {
                var loginUrl = Url.Create("/login");
                var (loginModel, _) = GetNextModel(loginUrl, null);
                return (loginModel, new PushState(loginUrl));
            }
            return (nextModel, new PushState(@internal.Url));
        }
        return (model, Commands.None);
    }

    /// <summary>
    /// Subscribes to model changes.
    /// </summary>
    /// <param name="model">The current model.</param>
    /// <returns>The subscription.</returns>
    public static Subscription Subscriptions(Model model) => new();

    public static async Task HandleCommand(Command command, Func<Abies.Message, System.ValueTuple> dispatch)
    {
        switch (command)
        {
            case Command.None:
                break;
            case Command.Batch batch:
                foreach (var cmd in batch.Commands)
                {
                    await HandleCommand(cmd, dispatch);
                }
                break;
            case Message.Command.LoadCurrentUser:
                try
                {
                    var user = await AuthService.LoadUserFromLocalStorageAsync();
                    if (user != null)
                    {
                        dispatch(new UserLoggedIn(user));
                    }
                }
                catch (Exception)
                {
                    // ignore invalid token
                }
                break;
            case LoginCommand login:
                try
                {
                    var user = await AuthService.LoginAsync(login.Email, login.Password);
                    dispatch(new Conduit.Page.Login.Message.LoginSuccess(user));
                }
                catch (ApiException)
                {
                    dispatch(new Conduit.Page.Login.Message.LoginError(new[] { "Invalid email or password" }));
                }
                catch (Exception)
                {
                    dispatch(new Conduit.Page.Login.Message.LoginError(new[] { "An unexpected error occurred" }));
                }
                break;
            case RegisterCommand register:
                try
                {
                    var user = await AuthService.RegisterAsync(register.Username, register.Email, register.Password);
                    dispatch(new Conduit.Page.Register.Message.RegisterSuccess(user));
                }
                catch (ApiException ex)
                {
                    dispatch(new Conduit.Page.Register.Message.RegisterError(ex.Errors));
                }
                catch (Exception)
                {
                    var errors = new System.Collections.Generic.Dictionary<string, string[]>
                    {
                        { "error", new[] { "An unexpected error occurred" } }
                    };
                    dispatch(new Conduit.Page.Register.Message.RegisterError(errors));
                }
                break;
            case UpdateUserCommand updateUser:
                try
                {
                    var user = await AuthService.UpdateUserAsync(updateUser.Username, updateUser.Email, updateUser.Bio, updateUser.Image, updateUser.Password);
                    dispatch(new Conduit.Page.Settings.Message.SettingsSuccess(user));
                }
                catch (ApiException ex)
                {
                    dispatch(new Conduit.Page.Settings.Message.SettingsError(ex.Errors));
                }
                catch (Exception)
                {
                    var errors = new System.Collections.Generic.Dictionary<string, string[]>
                    {
                        { "error", new[] { "An unexpected error occurred" } }
                    };
                    dispatch(new Conduit.Page.Settings.Message.SettingsError(errors));
                }
                break;
            case LoadArticlesCommand loadArticles:
                try
                {
                    var (articles, count) = await ArticleService.GetArticlesAsync(loadArticles.Tag, loadArticles.Author, loadArticles.FavoritedBy, loadArticles.Limit, loadArticles.Offset);
                    dispatch(new Conduit.Page.Home.Message.ArticlesLoaded(articles, count));
                }
                catch (Exception)
                {
                    dispatch(new Conduit.Page.Home.Message.ArticlesLoaded(new System.Collections.Generic.List<Conduit.Page.Home.Article>(), 0));
                }
                break;
            case LoadFeedCommand loadFeed:
                try
                {
                    var (articles, count) = await ArticleService.GetFeedArticlesAsync(loadFeed.Limit, loadFeed.Offset);
                    dispatch(new Conduit.Page.Home.Message.ArticlesLoaded(articles, count));
                }
                catch (Exception)
                {
                    dispatch(new Conduit.Page.Home.Message.ArticlesLoaded(new System.Collections.Generic.List<Conduit.Page.Home.Article>(), 0));
                }
                break;
            case LoadTagsCommand:
                try
                {
                    var tags = await TagService.GetTagsAsync();
                    dispatch(new Conduit.Page.Home.Message.TagsLoaded(tags));
                }
                catch (Exception)
                {
                    dispatch(new Conduit.Page.Home.Message.TagsLoaded(new System.Collections.Generic.List<string>()));
                }
                break;
            case LoadArticleCommand loadArticle:
                try
                {
                    var article = await ArticleService.GetArticleAsync(loadArticle.Slug);
                    dispatch(new Conduit.Page.Article.Message.ArticleLoaded(article));
                }
                catch (Exception)
                {
                    dispatch(new Conduit.Page.Article.Message.ArticleLoaded(null));
                }
                break;
            case LoadCommentsCommand loadComments:
                try
                {
                    var comments = await ArticleService.GetCommentsAsync(loadComments.Slug);
                    dispatch(new Conduit.Page.Article.Message.CommentsLoaded(comments));
                }
                catch (Exception)
                {
                    dispatch(new Conduit.Page.Article.Message.CommentsLoaded(new System.Collections.Generic.List<Conduit.Page.Article.Comment>()));
                }
                break;
            case SubmitCommentCommand submitComment:
                try
                {
                    var comment = await ArticleService.AddCommentAsync(submitComment.Slug, submitComment.Body);
                    dispatch(new Conduit.Page.Article.Message.CommentSubmitted(comment));
                }
                catch (Exception)
                {
                    dispatch(new Conduit.Page.Article.Message.SubmitComment());
                }
                break;
            case DeleteCommentCommand deleteComment:
                try
                {
                    await ArticleService.DeleteCommentAsync(deleteComment.Slug, deleteComment.CommentId);
                    dispatch(new Conduit.Page.Article.Message.CommentDeleted(deleteComment.CommentId));
                }
                catch (Exception)
                {
                    dispatch(new Conduit.Page.Article.Message.CommentDeleted(""));
                }
                break;
            case CreateArticleCommand createArticle:
                try
                {
                    var article = await ArticleService.CreateArticleAsync(createArticle.Title, createArticle.Description, createArticle.Body, createArticle.TagList);
                    dispatch(new Conduit.Page.Editor.Message.ArticleSubmitSuccess(article.Slug));
                }
                catch (ApiException ex)
                {
                    dispatch(new Conduit.Page.Editor.Message.ArticleSubmitError(ex.Errors));
                }
                catch (Exception)
                {
                    var errors = new System.Collections.Generic.Dictionary<string, string[]>
                    {
                        { "error", new[] { "An unexpected error occurred" } }
                    };
                    dispatch(new Conduit.Page.Editor.Message.ArticleSubmitError(errors));
                }
                break;
            case UpdateArticleCommand updateArticle:
                try
                {
                    var article = await ArticleService.UpdateArticleAsync(updateArticle.Slug, updateArticle.Title, updateArticle.Description, updateArticle.Body);
                    dispatch(new Conduit.Page.Editor.Message.ArticleSubmitSuccess(article.Slug));
                }
                catch (ApiException ex)
                {
                    dispatch(new Conduit.Page.Editor.Message.ArticleSubmitError(ex.Errors));
                }
                catch (Exception)
                {
                    var errors = new System.Collections.Generic.Dictionary<string, string[]>
                    {
                        { "error", new[] { "An unexpected error occurred" } }
                    };
                    dispatch(new Conduit.Page.Editor.Message.ArticleSubmitError(errors));
                }
                break;
            case ToggleFavoriteCommand toggleFavorite:
                try
                {
                    var article = toggleFavorite.CurrentState ? await ArticleService.UnfavoriteArticleAsync(toggleFavorite.Slug) : await ArticleService.FavoriteArticleAsync(toggleFavorite.Slug);
                    dispatch(new Conduit.Page.Article.Message.ArticleLoaded(article));
                }
                catch (Exception)
                {
                    dispatch(new Conduit.Page.Article.Message.ToggleFavorite());
                }
                break;
            case LoadProfileCommand loadProfile:
                try
                {
                    var profile = await ProfileService.GetProfileAsync(loadProfile.Username);
                    dispatch(new Conduit.Page.Profile.Message.ProfileLoaded(profile));
                }
                catch (Exception)
                {
                    dispatch(new Conduit.Page.Profile.Message.ProfileLoaded(new Conduit.Page.Home.Profile(loadProfile.Username, "", "", false)));
                }
                break;
            case ToggleFollowCommand toggleFollow:
                try
                {
                    var profile = toggleFollow.CurrentState ? await ProfileService.UnfollowUserAsync(toggleFollow.Username) : await ProfileService.FollowUserAsync(toggleFollow.Username);
                    dispatch(new Conduit.Page.Profile.Message.ProfileLoaded(profile));
                }
                catch (Exception)
                {
                    dispatch(new Conduit.Page.Profile.Message.ToggleFollow());
                }
                break;
            case DeleteArticleCommand deleteArticle:
                try
                {
                    await ArticleService.DeleteArticleAsync(deleteArticle.Slug);
                    dispatch(new Conduit.Page.Article.Message.ArticleDeleted());
                }
                catch (Exception)
                {
                    dispatch(new Conduit.Page.Article.Message.ArticleDeleted());
                }
                break;
            default:
                break;
        }
    }

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
            Page.NewArticle newArticle when model.CurrentRoute is Routing.Route.EditArticle edit => new LoadArticleCommand(edit.Slug.Value),
            _ => Commands.None
        };

    public static (Model, Command) Initialize(Url url, Arguments argument)
    {
        var (model, _) = GetNextModel(url, null);
        Command redirectCommand = Commands.None;
        if (RequiresAuth(model.CurrentRoute))
        {
            var loginUrl = Url.Create("/login");
            (model, _) = GetNextModel(loginUrl, null);
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
    switch   (message)
        {
            case UrlChanged urlChanged:
                var (nextModel, _) = HandleUrlChanged(urlChanged.Url, model);

                // Prepare initialization commands based on route type
                Command initCommand = Commands.None;
                if (nextModel.Page is Page.Home)
                {
                    initCommand = Commands.Batch(new Command[]
                    {
                        new LoadArticlesCommand(),
                        new LoadTagsCommand()
                    });
                }
                else if (nextModel.Page is Page.Article article)
                {
                    initCommand = Commands.Batch(new Command[]
                    {
                        new LoadArticleCommand(article.Model.Slug.Value),
                        new LoadCommentsCommand(article.Model.Slug.Value)
                    });
                }
                else if (nextModel.Page is Page.Profile profile)
                {
                    initCommand = Commands.Batch(new Command[]
                    {
                        new LoadProfileCommand(profile.Model.UserName.Value),
                        new LoadArticlesCommand(null, profile.Model.UserName.Value)
                    });
                }
                else if (nextModel.Page is Page.ProfileFavorites profileFavorites)
                {
                    initCommand = Commands.Batch(new Command[]
                    {
                        new LoadProfileCommand(profileFavorites.Model.UserName.Value),
                        new LoadArticlesCommand(null, null, profileFavorites.Model.UserName.Value)
                    });
                }
                else if (nextModel.Page is Page.NewArticle editPage && nextModel.CurrentRoute is Routing.Route.EditArticle edit)
                {
                    initCommand = new LoadArticleCommand(edit.Slug.Value);
                }
                return (nextModel, initCommand);
            case LinkClicked linkClicked:
                return HandleLinkClicked(linkClicked.UrlRequest, model);
            case UserLoggedIn userLoggedIn:
                return (
                    model with
                    {
                        CurrentUser = userLoggedIn.User,
                        Page = model.Page switch
                        {
                            Page.Home home => new Page.Home(home.Model with { CurrentUser = userLoggedIn.User }),
                            Page.Settings settings => new Page.Settings(settings.Model with { CurrentUser = userLoggedIn.User }),
                            Page.Login loginAfterLogin => new Page.Login(loginAfterLogin.Model with { CurrentUser = userLoggedIn.User }),
                            Page.Register register => new Page.Register(register.Model with { CurrentUser = userLoggedIn.User }),
                            Page.Profile profile => new Page.Profile(profile.Model with { CurrentUser = userLoggedIn.User }),
                            Page.ProfileFavorites profileFav => new Page.ProfileFavorites(profileFav.Model with { CurrentUser = userLoggedIn.User }),
                            Page.Article article => new Page.Article(article.Model with { CurrentUser = userLoggedIn.User }),
                            Page.NewArticle editor => new Page.NewArticle(editor.Model with { CurrentUser = userLoggedIn.User }),
                            _ => model.Page
                        }
                    },
                    Commands.None);
            case UserLoggedOut:
                AuthService.Logout().GetAwaiter().GetResult();
                return (
                    model with
                    {
                        CurrentUser = null,
                        Page = model.Page switch
                        {
                            Page.Home home => new Page.Home(home.Model with { CurrentUser = null }),
                            Page.Settings settings => new Page.Settings(settings.Model with { CurrentUser = null }),
                            Page.Login loginAfterLogout => new Page.Login(loginAfterLogout.Model with { CurrentUser = null }),
                            Page.Register register => new Page.Register(register.Model with { CurrentUser = null }),
                            Page.Profile profile => new Page.Profile(profile.Model with { CurrentUser = null }),
                            Page.ProfileFavorites profileFav => new Page.ProfileFavorites(profileFav.Model with { CurrentUser = null }),
                            Page.Article article => new Page.Article(article.Model with { CurrentUser = null }),
                            Page.NewArticle editor => new Page.NewArticle(editor.Model with { CurrentUser = null }),
                            _ => model.Page
                        }
                    },
                    Commands.None);
            default:
                // Check page-specific messages based on current page type
                if (message is Conduit.Page.Login.Message loginMsg && model.Page is Page.Login loginCurrent)
                {
                    if (loginMsg is Conduit.Page.Login.Message.LoginSuccess loginSuccess)
                    {
                        var url = Url.Create("/");
                        var homeModel = new Conduit.Page.Home.Model(
                            new System.Collections.Generic.List<Conduit.Page.Home.Article>(),
                            0,
                            new System.Collections.Generic.List<string>(),
                            Conduit.Page.Home.FeedTab.YourFeed,
                            "",
                            true,
                            0,
                            loginSuccess.User);
                        var redirectModel = new Model(new Page.Home(homeModel), new Routing.Route.Home(), loginSuccess.User);
                        var redirectCommand = Commands.Batch(new Command[] { new LoadFeedCommand(), new LoadTagsCommand() });
                        return (redirectModel, Commands.Batch(new Command[] { new PushState(url), redirectCommand }));
                    }

                    var (nextLoginModel, nextLoginCommand) = Conduit.Page.Login.Page.Update(loginMsg, loginCurrent.Model);
                    return (model with { Page = new Page.Login(nextLoginModel) }, nextLoginCommand);
                }
                else if (message is Conduit.Page.Register.Message registerMsg && model.Page is Page.Register register)
                {
                    if (registerMsg is Conduit.Page.Register.Message.RegisterSuccess registerSuccess)
                    {
                        var url = Url.Create("/");
                        var homeModel = new Conduit.Page.Home.Model(
                            new System.Collections.Generic.List<Conduit.Page.Home.Article>(),
                            0,
                            new System.Collections.Generic.List<string>(),
                            Conduit.Page.Home.FeedTab.YourFeed,
                            "",
                            true,
                            0,
                            registerSuccess.User);
                        var redirectModel = new Model(new Page.Home(homeModel), new Routing.Route.Home(), registerSuccess.User);
                        var redirectCommand = Commands.Batch(new Command[] { new LoadFeedCommand(), new LoadTagsCommand() });
                        return (redirectModel, Commands.Batch(new Command[] { new PushState(url), redirectCommand }));
                    }

                    var (nextRegisterModel, nextRegisterCommand) = Conduit.Page.Register.Page.Update(registerMsg, register.Model);
                    return (model with { Page = new Page.Register(nextRegisterModel) }, nextRegisterCommand);
                }
                else if (message is Conduit.Page.Home.Message homeMsg && model.Page is Page.Home home)
                {
                    var (nextHomeModel, nextHomeCommand) = Conduit.Page.Home.Page.Update(homeMsg, home.Model);
                    return (model with { Page = new Page.Home(nextHomeModel) }, nextHomeCommand);
                }
                else if (message is Conduit.Page.Settings.Message settingsMsg && model.Page is Page.Settings settings)
                {
                    if (settingsMsg is Conduit.Page.Settings.Message.LogoutRequested)
                    {
                        AuthService.Logout().GetAwaiter().GetResult();
                        var current = new Uri(Interop.GetCurrentUrl());
                        var root = new Uri(current.GetLeftPart(UriPartial.Authority) + "/");
                        var url = Url.Create(root.ToString());
                        var (loggedOutModel, _) = GetNextModel(url, null);
                        return (loggedOutModel, new PushState(url));
                    }

                    var (nextSettingsModel, nextSettingsCommand) = Conduit.Page.Settings.Page.Update(settingsMsg, settings.Model);
                    return (model with { Page = new Page.Settings(nextSettingsModel) }, nextSettingsCommand);
                }
                else if (message is Conduit.Page.Profile.Message profileMsg && model.Page is Page.Profile profile)
                {
                    var (nextProfileModel, nextProfileCommand) = Conduit.Page.Profile.Page.Update(profileMsg, profile.Model);
                    return (model with { Page = new Page.Profile(nextProfileModel) }, nextProfileCommand);
                }
                else if (message is Conduit.Page.Profile.Message profileFavoritesMsg && model.Page is Page.ProfileFavorites profileFavorites)
                {
                    var (nextProfileFavoritesModel, nextProfileFavoritesCommand) = Conduit.Page.Profile.Page.Update(profileFavoritesMsg, profileFavorites.Model);
                    return (model with { Page = new Page.ProfileFavorites(nextProfileFavoritesModel) }, nextProfileFavoritesCommand);
                }
                else if (message is Conduit.Page.Article.Message articleMsg && model.Page is Page.Article article)
                {
                    if (articleMsg is Conduit.Page.Article.Message.ArticleDeleted)
                    {
                        var current = new Uri(Interop.GetCurrentUrl());
                        var root = new Uri(current.GetLeftPart(UriPartial.Authority) + "/");
                        var url = Url.Create(root.ToString());
                        var (afterDeleteModel, _) = GetNextModel(url, model.CurrentUser);
                        return (afterDeleteModel, new PushState(url));
                    }
                    else
                    {
                        var (nextArticleModel, nextArticleCommand) = Conduit.Page.Article.Page.Update(articleMsg, article.Model);
                        return (model with { Page = new Page.Article(nextArticleModel) }, nextArticleCommand);
                    }
                }
                else if (message is Conduit.Page.Editor.Message editorMsg && model.Page is Page.NewArticle newArticle)
                {
                    if (editorMsg is Conduit.Page.Editor.Message.ArticleSubmitSuccess success)
                    {
                        var current = new Uri(Interop.GetCurrentUrl());
                        var root = new Uri(current.GetLeftPart(UriPartial.Authority) + "/");
                        var url = Url.Create($"{root}article/{success.Slug}");
                        var (redirectModel, _) = GetNextModel(url, model.CurrentUser);
                        return (redirectModel, new PushState(url));
                    }

                    var (nextEditorModel, nextEditorCommand) = Conduit.Page.Editor.Page.Update(editorMsg, newArticle.Model);
                    return (model with { Page = new Page.NewArticle(nextEditorModel) }, nextEditorCommand);
                }
                
                return (model, Commands.None);
        }
        
    }
    private static Node WithLayout(Node page, Model model) =>
        div([], [
            Navigation.View(model),
            page
        ]);

    public static Document View(Model model)
        => model.Page switch
        {
            Page.Redirect => new Document(string.Format(Title, "Redirect"), WithLayout(h1([], [text("Redirecting...")]), model)),
            Page.NotFound => new Document(string.Format(Title, "Not Found"), WithLayout(h1([], [text("Not Found")]), model)),
            Page.Home home => new Document(string.Format(Title, nameof(Conduit.Page.Home)), WithLayout(Conduit.Page.Home.Page.View(home.Model), model)),
            Page.Settings settings => new Document(string.Format(Title, nameof(Conduit.Page.Settings)), WithLayout(Conduit.Page.Settings.Page.View(settings.Model), model)),
            Page.Login loginDoc => new Document(string.Format(Title, nameof(Conduit.Page.Login)), WithLayout(Conduit.Page.Login.Page.View(loginDoc.Model), model)),
            Page.Register register => new Document(string.Format(Title, nameof(Conduit.Page.Register)), WithLayout(Conduit.Page.Register.Page.View(register.Model), model)),
            Page.Profile profile => new Document(string.Format(Title, nameof(Conduit.Page.Profile)), WithLayout(Conduit.Page.Profile.Page.View(profile.Model), model)),
            Page.ProfileFavorites profileFavorites => new Document(string.Format(Title, "Profile Favorites"), WithLayout(Conduit.Page.Profile.Page.View(profileFavorites.Model), model)),
            Page.Article article => new Document(string.Format(Title, nameof(Conduit.Page.Article)), WithLayout(Conduit.Page.Article.Page.View(article.Model), model)),
            Page.NewArticle newArticle => new Document(string.Format(Title, model.CurrentRoute is Routing.Route.EditArticle ? "Edit Article" : "New Article"), WithLayout(Conduit.Page.Editor.Page.View(newArticle.Model), model)),
            _ => new Document(string.Format(Title, "Not Found"), WithLayout(h1([], [text("Not Found")]), model))
        };



    

    public static Abies.Message OnUrlChanged(Url url)
        => new UrlChanged(url);

    public static Abies.Message OnLinkClicked(UrlRequest urlRequest)
        => new LinkClicked(urlRequest);
}
