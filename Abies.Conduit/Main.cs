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
    public sealed record Home(Conduit.Home.Model Model) : Page;
    public sealed record Settings(Conduit.Settings.Model Model) : Page;
    public sealed record Login(Conduit.Login.Model Model) : Page;
    public sealed record Register(Conduit.Register.Model Model) : Page;
    public sealed record Profile(UserName UserName, Conduit.Profile.Model Model) : Page;
    public sealed record Article(Conduit.Article.Model Model) : Page;
}

public record Arguments;

public class Application : Application<Model, Arguments>
{
    private static Model GetNextModel(Url url)
    {
        Route currentRoute = Route.FromUrl(Route.Match, url);
        switch (currentRoute)
        {
            case Route.Home:
                return new(new Page.Home(new Home.Model()), currentRoute);
            case Route.Settings:
                return new(new Page.Settings(new Settings.Model()), currentRoute);
            case Route.Login:
                return new(new Page.Login(new Login.Model()), currentRoute);
            case Route.Register:
                return new(new Page.Register(new Register.Model()), currentRoute);
            case Route.Profile profile:
                return new (new Page.Profile(profile.UserName, new Profile.Model()), currentRoute);
            case Route.Article article:
                return new(new Page.Article(new Article.Model()), currentRoute);
            case Route.NewArticle:
                return new(new Page.Article(new Article.Model()), currentRoute);
            case Route.EditArticle article:
                return new(new Page.Article(new Article.Model()), currentRoute);
            case Route.Redirect:
                return new(new Page.Redirect(), currentRoute);
            default:
                return new(new Page.NotFound(), currentRoute);
        }
    }

         

    private static (Model model, IEnumerable<Command>) HandleUrlChanged(Url url, Model model)
    {
        return ( GetNextModel(url), [Commands.None]);
    }

    private static (Model model, IEnumerable<Command>) HandleLinkClicked(UrlRequest urlRequest, Model model)
        => urlRequest switch
        {
            Internal @internal => (model with { CurrentRoute = Route.FromUrl(Route.Match, @internal.Url) }, [new PushState(@internal.Url)]),
            External externalUrl => (model, [new Load(Create(new Decoded.String(externalUrl.Url)))]),
            _ => (model, [])
        };

    public static Subscription Subscriptions(Model model) =>
        new();


    public static Model Initialize(Url url, Arguments argument)
        => GetNextModel(url);

    public static (Model model, IEnumerable<Command> commands) Update(Abies.Message message, Model model)
    {
        switch(message, model.Page)
        {
            case (UrlChanged urlChanged, _):
                return HandleUrlChanged(urlChanged.Url, model);
            case (LinkClicked linkClicked, _):
                return HandleLinkClicked(linkClicked.UrlRequest, model);
            case (Login.Message msg, Page.Login login):
                var (nextLoginModel, nextLoginCommand) = Login.Page.Update(msg, login.Model);
                return (model with { Page = new Page.Login(nextLoginModel) }, nextLoginCommand);  
            case (Register.Message msg, Page.Register register):
                var (nextRegisterModel, nextRegisterCommand) = Register.Page.Update(msg, register.Model);
                return (model with { Page = new Page.Register(nextRegisterModel) }, nextRegisterCommand);
            case (Home.Message msg, Page.Home home):
                var (nextHomeModel, nextHomeCommand) = Home.Page.Update(msg, home.Model);
                return (model with { Page = new Page.Home(nextHomeModel) }, nextHomeCommand);
            case (Settings.Message msg, Page.Settings settings):
                var (nextSettingsModel, nextSettingsCommand) = Settings.Page.Update(msg, settings.Model);
                return (model with { Page = new Page.Settings(nextSettingsModel) }, nextSettingsCommand);
            case (Profile.Message msg, Page.Profile profile):
                var (nextProfileModel, nextProfileCommand) = Profile.Page.Update(msg, profile.Model);
                return (model with { Page = new Page.Profile(profile.UserName, nextProfileModel) }, nextProfileCommand);
            case (Article.Message msg, Page.Article article):
                var (nextArticleModel, nextArticleCommand) = Article.Page.Update(msg, article.Model);
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
            Page.Home home => new Document(string.Format(Title, nameof(Home)), Home.Page.View(home.Model)),
            Page.Settings settings => new Document(string.Format(Title, nameof(Settings)), Settings.Page.View(settings.Model)),
            Page.Login login => new Document(string.Format(Title, nameof(Login)), Login.Page.View(login.Model)),
            Page.Register register => new Document(string.Format(Title, nameof(Register)), Register.Page.View(register.Model)),
            Page.Profile profile => new Document(string.Format(Title, nameof(Profile)), Profile.Page.View(profile.Model)),
            Page.Article article => new Document(string.Format(Title, nameof(Article)), Article.Page.View(article.Model)),

            _ => new Document(string.Format(Title, "Not Found"), h1([], [text("Not Found")]))
        };

    public static Abies.Message OnUrlChanged(Url url)
        => new UrlChanged(url);

    public static Abies.Message OnLinkClicked(UrlRequest urlRequest)
        => new LinkClicked(urlRequest);


}
