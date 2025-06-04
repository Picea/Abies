using Abies.Conduit.Routing;
using Abies.Conduit.Services;
using System.Threading.Tasks;
using static Abies.Conduit.Main.Message.Event;
using static Abies.UrlRequest;
using static Abies.Navigation.Command;

namespace Abies.Conduit.Main;

public record struct UserName(string Value);

public record struct Slug(string Value);

public record struct Email(string Value);

public record struct Token(string Value);

public record User(UserName Username, Email Email, Token Token);

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
public class Application : Application<Model, Arguments>
{
    private static string Title = "Conduit - {0}";

    /// <summary>
    /// Determines the next model based on the given URL.
    /// </summary>
    /// <param name="url">The URL to process.</param>
    /// <returns>The next model.</returns>
    private static Model GetNextModel(Url url, User? currentUser = null)
    {        Routing.Route currentRoute = Routing.Route.FromUrl(Routing.Route.Match, url);
        return currentRoute switch
        {
            Routing.Route.Home => new(new Page.Home(new Conduit.Page.Home.Model(new List<Conduit.Page.Home.Article>(), 0, new List<string>(), Conduit.Page.Home.FeedTab.Global, "", true)), currentRoute, currentUser),
            Routing.Route.NotFound => new(new Page.NotFound(), currentRoute, currentUser),
            Routing.Route.Settings => new(new Page.Settings(new Conduit.Page.Settings.Model()), currentRoute, currentUser),
            Routing.Route.Login => new(new Page.Login(new Conduit.Page.Login.Model()), currentRoute, currentUser),
            Routing.Route.Register => new(new Page.Register(new Conduit.Page.Register.Model()), currentRoute, currentUser),
            Routing.Route.Profile profile => new(new Page.Profile(new Conduit.Page.Profile.Model(profile.UserName)), currentRoute, currentUser),
            Routing.Route.ProfileFavorites profileFavorites => new(new Page.ProfileFavorites(new Conduit.Page.Profile.Model(profileFavorites.UserName, ActiveTab: Conduit.Page.Profile.ProfileTab.FavoritedArticles)), currentRoute, currentUser),
            Routing.Route.Article article => new(new Page.Article(new Conduit.Page.Article.Model(article.Slug)), currentRoute, currentUser),
            Routing.Route.Redirect => new(new Page.Redirect(), currentRoute, currentUser),
            Routing.Route.NewArticle => new(new Page.NewArticle(new Conduit.Page.Editor.Model()), currentRoute, currentUser),
            _ => new(new Page.NotFound(), currentRoute, currentUser),
        };
    }

    /// <summary>
    /// Handles the URL changed event.
    /// </summary>
    /// <param name="url">The new URL.</param>
    /// <param name="model">The current model.</param>
    /// <returns>The updated model and commands.</returns>
    private static (Model model, IEnumerable<Command>) HandleUrlChanged(Url url, Model model)
    {
        return (GetNextModel(url, model.CurrentUser), new List<Command> { new Command.None() });
    }

    /// <summary>
    /// Handles the link clicked event.
    /// </summary>
    /// <param name="urlRequest">The URL request.</param>
    /// <param name="model">The current model.</param>
    /// <returns>The updated model and commands.</returns>
    private static (Model model, IEnumerable<Command>) HandleLinkClicked(UrlRequest urlRequest, Model model)
        => urlRequest switch
        {
            Internal @internal => (model with { CurrentRoute = Abies.Conduit.Routing.Route.FromUrl(Abies.Conduit.Routing.Route.Match, @internal.Url), CurrentUser = model.CurrentUser }, new List<Command> { new PushState(@internal.Url) }),
            _ => (model, new List<Command> { new Command.None() })
        };

    /// <summary>
    /// Subscribes to model changes.
    /// </summary>
    /// <param name="model">The current model.</param>
    /// <returns>The subscription.</returns>
    public static Subscription Subscriptions(Model model) => new();

    /// <summary>
    /// Initializes the application with the given URL and arguments.
    /// </summary>
    /// <param name="url">The initial URL.</param>
    /// <param name="argument">The arguments.</param>
    /// <returns>The initial model.</returns>
    public static Model Initialize(Url url, Arguments argument) => GetNextModel(url, null);    /// <summary>
                                                                                               /// Updates the model based on the given message.
                                                                                               /// </summary>
                                                                                               /// <param name="message">The message to process.</param>
                                                                                               /// <param name="model">The current model.</param>
                                                                                               /// <returns>The updated model and commands.</returns>
    public static (Model model, IEnumerable<Command> commands) Update(Abies.Message message, Model model)
    {    
    switch   (message)
        {
            case UrlChanged urlChanged:
                var nextModel = HandleUrlChanged(urlChanged.Url, model);
                
                // Send initialization messages based on route type
                if (nextModel.model.Page is Page.Home)
                {
                    Task.Run(async () => {
                        await new LoadArticlesCommand().ExecuteAsync();
                        await new LoadTagsCommand().ExecuteAsync();
                    });
                }
                else if (nextModel.model.Page is Page.Article article)
                {
                    Task.Run(async () => {
                        await new LoadArticleCommand(article.Model.Slug.Value).ExecuteAsync();
                        await new LoadCommentsCommand(article.Model.Slug.Value).ExecuteAsync();
                    });
                }                else if (nextModel.model.Page is Page.Profile profile)
                {
                    Task.Run(async () => {
                        await new LoadProfileCommand(profile.Model.UserName.Value).ExecuteAsync();
                        await new LoadArticlesCommand(author: profile.Model.UserName.Value).ExecuteAsync();
                    });
                }
                else if (nextModel.model.Page is Page.ProfileFavorites profileFavorites)
                {
                    Task.Run(async () => {
                        await new LoadProfileCommand(profileFavorites.Model.UserName.Value).ExecuteAsync();
                        await new LoadArticlesCommand(favoritedBy: profileFavorites.Model.UserName.Value).ExecuteAsync();
                    });
                }
                return nextModel;
            case LinkClicked linkClicked:
                return HandleLinkClicked(linkClicked.UrlRequest, model);
            case UserLoggedIn userLoggedIn:
                return (model with { CurrentUser = userLoggedIn.User }, new List<Command> { new Command.None() });
            case UserLoggedOut:
                AuthService.Logout();
                return (model with { CurrentUser = null }, new List<Command> { new Command.None() });
            default:
                // Check page-specific messages based on current page type
                if (message is Conduit.Page.Login.Message loginMsg && model.Page is Page.Login login)
                {
                    if (loginMsg is Conduit.Page.Login.Message.LoginSuccess loginSuccess)
                        return (model with { CurrentUser = loginSuccess.User }, new List<Command> { new Command.None() });
                    
                    var (nextLoginModel, nextLoginCommand) = Conduit.Page.Login.Page.Update(loginMsg, login.Model);
                    return (model with { Page = new Page.Login(nextLoginModel) }, nextLoginCommand);
                }
                else if (message is Conduit.Page.Register.Message registerMsg && model.Page is Page.Register register)
                {
                    if (registerMsg is Conduit.Page.Register.Message.RegisterSuccess registerSuccess)
                        return (model with { CurrentUser = registerSuccess.User }, new List<Command> { new Command.None() });
                    
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
                    var (nextSettingsModel, nextSettingsCommand) = Conduit.Page.Settings.Page.Update(settingsMsg, settings.Model);
                    return (model with { Page = new Page.Settings(nextSettingsModel) }, nextSettingsCommand);
                }                else if (message is Conduit.Page.Profile.Message profileMsg && model.Page is Page.Profile profile)
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
                    var (nextArticleModel, nextArticleCommand) = Conduit.Page.Article.Page.Update(articleMsg, article.Model);
                    return (model with { Page = new Page.Article(nextArticleModel) }, nextArticleCommand);
                }
                else if (message is Conduit.Page.Editor.Message editorMsg && model.Page is Page.NewArticle newArticle)
                {
                    var (nextEditorModel, nextEditorCommand) = Conduit.Page.Editor.Page.Update(editorMsg, newArticle.Model);
                    return (model with { Page = new Page.NewArticle(nextEditorModel) }, nextEditorCommand);
                }
                
                return (model, new List<Command>());
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
            Page.Login login => new Document(string.Format(Title, nameof(Conduit.Page.Login)), WithLayout(Conduit.Page.Login.Page.View(login.Model), model)),
            Page.Register register => new Document(string.Format(Title, nameof(Conduit.Page.Register)), WithLayout(Conduit.Page.Register.Page.View(register.Model), model)),
            Page.Profile profile => new Document(string.Format(Title, nameof(Conduit.Page.Profile)), WithLayout(Conduit.Page.Profile.Page.View(profile.Model), model)),
            Page.ProfileFavorites profileFavorites => new Document(string.Format(Title, "Profile Favorites"), WithLayout(Conduit.Page.Profile.Page.View(profileFavorites.Model), model)),
            Page.Article article => new Document(string.Format(Title, nameof(Conduit.Page.Article)), WithLayout(Conduit.Page.Article.Page.View(article.Model), model)),
            Page.NewArticle newArticle => new Document(string.Format(Title, "New Article"), WithLayout(Conduit.Page.Editor.Page.View(newArticle.Model), model)),
            _ => new Document(string.Format(Title, "Not Found"), WithLayout(h1([], [text("Not Found")]), model))
        };



    

    public static Abies.Message OnUrlChanged(Url url)
        => new UrlChanged(url);

    public static Abies.Message OnLinkClicked(UrlRequest urlRequest)
        => new LinkClicked(urlRequest);
}
