using System;
using System.Diagnostics;
using Abies.Conduit.Routing;
using Abies;
using static Abies.Option.Extensions;
using static Abies.Html.Elements;
using static Abies.Html.Attributes;
using static Abies.Html.Events;
using Abies.Html;
using static Abies.Conduit.Main.Message.Event;
using static Abies.UrlRequest;
using static Abies.Url;
using static Abies.Navigation.Command;
using System.Collections.Generic;

namespace Abies.Conduit.Main;


public record struct UserName(string Value);

public record struct Slug(string Value);

public record Model(Page Page, Route CurrentRoute);

public interface Message : Abies.Message
{
    public interface Command : Message
    {
        public sealed record ChangeRoute(Route? Route) : Command;
    }

    public interface Event : Message
    {
        public sealed record UrlChanged(Url Url) : Event;
        public sealed record LinkClicked(UrlRequest UrlRequest) : Event;
    }
}

public interface Page
{
    public sealed record Redirect : Page;
    public sealed record NotFound : Page;
    public sealed record Home(Conduit.Page.Home.Model Model) : Page;
    public sealed record Settings(Conduit.Page.Settings.Model Model) : Page;
    public sealed record Login(Conduit.Page.Login.Model Model) : Page;
    public sealed record Register(Conduit.Page.Register.Model Model) : Page;
    public sealed record Profile(Conduit.Page.Profile.Model Model) : Page;
    public sealed record Article(Conduit.Page.Article.Model Model) : Page;
    public sealed record NewArticle(Conduit.Page.Article.Model Model) : Page;
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
    /// <summary>
    /// Determines the next model based on the given URL.
    /// </summary>
    /// <param name="url">The URL to process.</param>
    /// <returns>The next model.</returns>
    private static Model GetNextModel(Url url)
    {
        Route currentRoute = Route.FromUrl(Route.Match, url);
        return currentRoute switch
        {
            Route.Home => new(new Page.Home(new Conduit.Page.Home.Model()), currentRoute),
            Route.Settings => new(new Page.Settings(new Conduit.Page.Settings.Model()), currentRoute),
            Route.Login => new(new Page.Login(new Conduit.Page.Login.Model()), currentRoute),
            Route.Register => new(new Page.Register(new Conduit.Page.Register.Model()), currentRoute),
            Route.Profile profile => new(new Page.Profile(new Conduit.Page.Profile.Model(profile.UserName)), currentRoute),
            Route.Article article => new(new Page.Article(new Conduit.Page.Article.Model(article.Slug)), currentRoute),
            Route.Redirect => new(new Page.Redirect(), currentRoute),
            Route.NewArticle => new(new Page.NewArticle(new Conduit.Page.Article.Model(new Slug(""))), currentRoute),
            _ => new(new Page.NotFound(), currentRoute),
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
        return (GetNextModel(url), new List<Command> { new Command.None() });
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
            Internal @internal => (model with { CurrentRoute = Route.FromUrl(Route.Match, @internal.Url) }, new List<Command> { new PushState(@internal.Url) }),
            External externalUrl => (model, new List<Command> { new Load(Create(new Decoded.String(externalUrl.Url))) }),
            _ => (model, new List<Command>())
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
    public static Model Initialize(Url url, Arguments argument) => GetNextModel(url);

    /// <summary>
    /// Updates the model based on the given message.
    /// </summary>
    /// <param name="message">The message to process.</param>
    /// <param name="model">The current model.</param>
    /// <returns>The updated model and commands.</returns>
    public static (Model model, IEnumerable<Command> commands) Update(Abies.Message message, Model model)
    {
        switch(message, model.Page)
        {
            case (UrlChanged urlChanged, _):
                return HandleUrlChanged(urlChanged.Url, model);
            case (LinkClicked linkClicked, _):
                return HandleLinkClicked(linkClicked.UrlRequest, model);
            case (Conduit.Page.Login.Message msg, Page.Login login):
                var (nextLoginModel, nextLoginCommand) = Conduit.Page.Login.Page.Update(msg, login.Model);
                return (model with { Page = new Page.Login(nextLoginModel) }, nextLoginCommand);  
            case (Conduit.Page.Register.Message msg, Page.Register register):
                var (nextRegisterModel, nextRegisterCommand) = Conduit.Page.Register.Page.Update(msg, register.Model);
                return (model with { Page = new Page.Register(nextRegisterModel) }, nextRegisterCommand);
            case (Conduit.Page.Home.Message msg, Page.Home home):
                var (nextHomeModel, nextHomeCommand) = Conduit.Page.Home.Page.Update(msg, home.Model);
                return (model with { Page = new Page.Home(nextHomeModel) }, nextHomeCommand);
            case (Conduit.Page.Settings.Message msg, Page.Settings settings):
                var (nextSettingsModel, nextSettingsCommand) = Conduit.Page.Settings.Page.Update(msg, settings.Model);
                return (model with { Page = new Page.Settings(nextSettingsModel) }, nextSettingsCommand);
            case (Conduit.Page.Profile.Message msg, Page.Profile profile):
                var (nextProfileModel, nextProfileCommand) = Conduit.Page.Profile.Page.Update(msg, profile.Model);
                return (model with { Page = new Page.Profile(nextProfileModel) }, nextProfileCommand);
            case (Conduit.Page.Article.Message msg, Page.Article article):
                var (nextArticleModel, nextArticleCommand) = Conduit.Page.Article.Page.Update(msg, article.Model);
                return (model with { Page = new Page.Article(nextArticleModel) }, nextArticleCommand);
            default:
                return (model, []);
        }

    }

    private static string Title = "Conduit - {0}";

    public static Document View(Model model)
        => model.Page switch
        {
            Page.Redirect => new Document(string.Format(Title, "Redirect"), h1([], [text("Redirecting...")])),
            Page.NotFound => new Document(string.Format(Title, "Not Found"), h1([], [text("Not Found")])),
            Page.Home home => new Document(string.Format(Title, nameof(Conduit.Page.Home)), Conduit.Page.Home.Page.View(home.Model)),
            Page.Settings settings => new Document(string.Format(Title, nameof(Conduit.Page.Settings)), Conduit.Page.Settings.Page.View(settings.Model)),
            Page.Login login => new Document(string.Format(Title, nameof(Conduit.Page.Login)), Conduit.Page.Login.Page.View(login.Model)),
            Page.Register register => new Document(string.Format(Title, nameof(Conduit.Page.Register)), Conduit.Page.Register.Page.View(register.Model)),
            Page.Profile profile => new Document(string.Format(Title, nameof(Conduit.Page.Profile)), Conduit.Page.Profile.Page.View(profile.Model)),
            Page.Article article => new Document(string.Format(Title, nameof(Conduit.Page.Article)), Conduit.Page.Article.Page.View(article.Model)),
            Page.NewArticle newArticle => new Document(string.Format(Title, nameof(Conduit.Page.Article)), Conduit.Page.Article.Page.View(newArticle.Model)),

            _ => new Document(string.Format(Title, "Not Found"), h1([], [text("Not Found")]))
        };

    public static Abies.Message OnUrlChanged(Url url)
        => new UrlChanged(url);

    public static Abies.Message OnLinkClicked(UrlRequest urlRequest)
        => new LinkClicked(urlRequest);


}
