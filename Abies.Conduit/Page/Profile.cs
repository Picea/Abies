using Abies.Conduit.Main;
using Abies.Conduit.Routing;
using Abies.DOM;
using System.Collections.Generic;
using static Abies.Html.Attributes;
using static Abies.Html.Events;
using Abies.Conduit;

namespace Abies.Conduit.Page.Profile;

public interface Message : Abies.Message
{
    public record ProfileLoaded(Home.Profile Profile) : Message;
    public record ArticlesLoaded(List<Home.Article> Articles, int ArticlesCount) : Message;
    public record FavoritedArticlesLoaded(List<Home.Article> Articles, int ArticlesCount) : Message;
    public record ToggleTab(ProfileTab Tab) : Message;
    public record ToggleFollow : Message;
}

public enum ProfileTab
{
    MyArticles,
    FavoritedArticles
}

public record Model(
    UserName UserName,
    bool IsLoading = true,
    Home.Profile? Profile = null,
    List<Home.Article>? Articles = null,
    int ArticlesCount = 0,
    ProfileTab ActiveTab = ProfileTab.MyArticles
);

public class Page : Element<Model, Message>
{
    public static Model Initialize(Message argument)
    {
        return new Model(new UserName(""));
    }

    public static Subscription Subscriptions(Model model)
    {
        return new Subscription();
    }

    public static (Model model, IEnumerable<Command> commands) Update(Abies.Message message, Model model)
        => message switch
        {
            Message.ProfileLoaded profileLoaded => (
                model with 
                { 
                    Profile = profileLoaded.Profile,
                    IsLoading = false
                },
                []
            ),
            Message.ArticlesLoaded articlesLoaded => (
                model with 
                { 
                    Articles = articlesLoaded.Articles,
                    ArticlesCount = articlesLoaded.ArticlesCount
                },
                []
            ),
            Message.FavoritedArticlesLoaded favoritedArticlesLoaded => (
                model with 
                { 
                    Articles = favoritedArticlesLoaded.Articles,
                    ArticlesCount = favoritedArticlesLoaded.ArticlesCount
                },
                []
            ),            Message.ToggleTab toggleTab => (
                model with { ActiveTab = toggleTab.Tab },
                toggleTab.Tab == ProfileTab.FavoritedArticles 
                    ? [new LoadArticlesCommand(favoritedBy: model.UserName.Value)]
                    : [new LoadArticlesCommand(author: model.UserName.Value)]
            ),
            Message.ToggleFollow => (model, []),
            _ => (model, [])
        };

    private static Node UserInfo(Model model, bool isCurrentUser) =>
        div([class_("user-info")], [
            div([class_("container")], [
                div([class_("row")], [
                    div([class_("col-xs-12 col-md-10 offset-md-1")], [
                        img([class_("user-img"), src(model.Profile?.Image ?? "")]),
                        h4([], [text(model.UserName.Value)]),
                        p([], [text(model.Profile?.Bio ?? "")]),
                        isCurrentUser
                            ? a([class_("btn btn-sm btn-outline-secondary action-btn"),
                                href("/settings")],[
                                
                                    i([class_("ion-gear-a")], []),
                                    text(" Edit Profile Settings")
                                ])
                            : button([class_(model.Profile?.Following ?? false 
                                ? "btn btn-sm btn-secondary action-btn" 
                                : "btn btn-sm btn-outline-secondary action-btn"),
                                onclick(new Message.ToggleFollow())],[
                                
                                    i([class_("ion-plus-round")], []),
                                    text(model.Profile?.Following ?? false 
                                        ? $" Unfollow {model.UserName.Value}" 
                                        : $" Follow {model.UserName.Value}")
                                ])
                    ])
                ])
            ])
        ]);    private static Node ArticleToggle(Model model) =>
        div([class_("articles-toggle")], [
            ul([class_("nav nav-pills outline-active")], [
                li([class_("nav-item")], [
                    a([class_(model.ActiveTab == ProfileTab.MyArticles 
                        ? "nav-link active" 
                        : "nav-link"),
                      href($"/profile/{model.UserName.Value}")],
                      [text("My Articles")])
                ]),
                li([class_("nav-item")], [
                    a([class_(model.ActiveTab == ProfileTab.FavoritedArticles 
                        ? "nav-link active" 
                        : "nav-link"),
                      href($"/profile/{model.UserName.Value}/favorites")],
                      [text("Favorited Articles")])
                ])
            ])
        ]);

    private static Node ArticlePreview(Home.Article article) =>
        div([class_("article-preview")], [
            div([class_("article-meta")], [
                a([href($"/profile/{article.Author.Username}")], [
                    img([src(article.Author.Image)])
                ]),
                div([class_("info")], [
                    a([class_("author"), href($"/profile/{article.Author.Username}")], [
                        text(article.Author.Username)
                    ]),
                    span([class_("date")], [text(article.CreatedAt)])
                ]),
                div([class_("pull-xs-right")], [
                    button([class_(article.Favorited 
                            ? "btn btn-primary btn-sm pull-xs-right" 
                            : "btn btn-outline-primary btn-sm pull-xs-right")],
                    [
                        i([class_("ion-heart")], []),
                        text($" {article.FavoritesCount}")
                    ])
                ])
            ]),
            a([class_("preview-link"), href($"/article/{article.Slug}")], [
                h1([], [text(article.Title)]),
                p([], [text(article.Description)]),
                span([], [text("Read more...")])
            ])
        ]);

    private static Node ArticleList(Model model) =>
        model.Articles == null || model.Articles.Count == 0
            ? div([class_("article-preview")], [text("No articles are here... yet.")])
            : div([], [ ..model.Articles.ConvertAll(article => ArticlePreview(article)) ]);

    public static Node View(Model model) =>
        model.IsLoading
            ? div([class_("profile-page")], [
                div([class_("container")], [
                    div([class_("row")], [
                        div([class_("col-xs-12 col-md-10 offset-md-1")], [
                            text("Loading profile...")
                        ])
                    ])
                ])
              ])
            : div([class_("profile-page")], [
                UserInfo(model, false),
                div([class_("container")], [
                    div([class_("row")], [
                        div([class_("col-xs-12 col-md-10 offset-md-1")], [
                            ArticleToggle(model),
                            ArticleList(model)
                        ])
                    ])
                ])
              ]);
}