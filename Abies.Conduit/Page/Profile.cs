using System.Globalization;
using Abies.Conduit.Main;
using Abies.DOM;
using static Abies.Html.Attributes;
using static Abies.Html.Events;

namespace Abies.Conduit.Page.Profile;

public interface Message : Abies.Message
{
    record ProfileLoaded(Home.Profile Profile) : Message;
    record ArticlesLoaded(List<Home.Article> Articles, int ArticlesCount) : Message;
    record FavoritedArticlesLoaded(List<Home.Article> Articles, int ArticlesCount) : Message;
    record ToggleTab(ProfileTab Tab) : Message;
    record ToggleFollow : Message;
    record ToggleFavorite(string Slug, bool CurrentState) : Message;
    record PageSelected(int Page) : Message;
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
    ProfileTab ActiveTab = ProfileTab.MyArticles,
    int CurrentPage = 0,
    User? CurrentUser = null
);

public class Page : Element<Model, Message>
{
    private static string FormatDate(string value)
    {
        if (DateTime.TryParse(value, out var dt))
        {
            return dt.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);
        }

        return value;
    }
    public static Model Initialize(Message argument)
    {
        return new Model(new UserName(""));
    }

    public static Subscription Subscriptions(Model model)
    {
        return SubscriptionModule.None;
    }

    public static (Model model, Command command) Update(Abies.Message message, Model model)
        => message switch
        {
            Message.ProfileLoaded profileLoaded => (
                model with
                {
                    Profile = profileLoaded.Profile,
                    IsLoading = false
                },
                Commands.None
            ),
            Message.ArticlesLoaded articlesLoaded => (
                model with
                {
                    Articles = articlesLoaded.Articles,
                    ArticlesCount = articlesLoaded.ArticlesCount
                },
                Commands.None
            ),
            Message.FavoritedArticlesLoaded favoritedArticlesLoaded => (
                model with
                {
                    Articles = favoritedArticlesLoaded.Articles,
                    ArticlesCount = favoritedArticlesLoaded.ArticlesCount
                },
                Commands.None
            ),
            Message.ToggleTab toggleTab => (
                model with { ActiveTab = toggleTab.Tab, CurrentPage = 0 },
                toggleTab.Tab == ProfileTab.FavoritedArticles
                    ? new LoadArticlesCommand(null, null, model.UserName.Value, Offset: 0)
                    : new LoadArticlesCommand(null, model.UserName.Value, Offset: 0)
            ),
            Message.ToggleFollow => (
                model.Profile is not null
                    ? model with { Profile = model.Profile with { Following = !model.Profile.Following } }
                    : model,
                model.Profile is not null ? new ToggleFollowCommand(model.UserName.Value, model.Profile.Following) : Commands.None
            ),
            Message.ToggleFavorite fav => (
                model,
                Commands.Batch(new Command[]
                {
                    new ToggleFavoriteCommand(fav.Slug, fav.CurrentState),
                    model.ActiveTab == ProfileTab.FavoritedArticles
                        ? new LoadArticlesCommand(null, null, model.UserName.Value, Offset: model.CurrentPage * 10)
                        : new LoadArticlesCommand(null, model.UserName.Value, Offset: model.CurrentPage * 10)
                })
            ),
            Message.PageSelected pageSel => (
                model with { CurrentPage = pageSel.Page },
                model.ActiveTab == ProfileTab.FavoritedArticles
                    ? new LoadArticlesCommand(null, null, model.UserName.Value, Offset: pageSel.Page * 10)
                    : new LoadArticlesCommand(null, model.UserName.Value, Offset: pageSel.Page * 10)
            ),
            _ => (model, Commands.None)
        };

    private static Node UserInfo(Model model, bool isCurrentUser) =>
        div([class_("user-info")], [
            div([class_("container")], [
                div([class_("row")], [
                    div([class_("col-xs-12 col-md-10 offset-md-1")], [
                        img([class_("user-img"), src(model.Profile?.Image ?? ""), alt($"{model.UserName.Value} profile image")]),
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
        ]); private static Node ArticleToggle(Model model) =>
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
                    img([src(article.Author.Image), alt($"{article.Author.Username} profile image")])
                ]),
                div([class_("info")], [
                    a([class_("author"), href($"/profile/{article.Author.Username}")], [
                        text(article.Author.Username)
                    ]),
                    span([class_("date")], [text(FormatDate(article.CreatedAt))])
                ]),
                div([class_("pull-xs-right")], [
                    button([class_(article.Favorited
                            ? "btn btn-primary btn-sm pull-xs-right"
                            : "btn btn-outline-primary btn-sm pull-xs-right"),
                            onclick(new Message.ToggleFavorite(article.Slug, article.Favorited))],
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
        ], id: $"article-{article.Slug}");

    private static Node ArticleList(Model model) =>
        model.Articles is null || model.Articles.Count == 0
            ? div([class_("article-preview")], [text("No articles are here... yet.")])
            : div([], [.. model.Articles.ConvertAll(article => ArticlePreview(article))]);

    private static Node Pagination(Model model)
    {
        int pageCount = (int)Math.Ceiling(model.ArticlesCount / 10.0);
        if (pageCount <= 1)
        {
            return text("");
        }

        List<Node> items = [];
        for (int i = 0; i < pageCount; i++)
        {
            var isActive = i == model.CurrentPage;
            List<DOM.Attribute> attrs =
            [
                class_(isActive ? "page-link active" : "page-link"),
                type("button"),
                onclick(new Message.PageSelected(i))
            ];
            if (isActive)
            {
                attrs.Add(ariaCurrent("page"));
            }

            items.Add(
                li([class_(isActive ? "page-item active" : "page-item")], [
                    button([..attrs], [text((i + 1).ToString())])
                ]));
        }

        return nav([], [ ul([
        class_("pagination"),
        data("current-page", (model.CurrentPage + 1).ToString())
        ], [..items]) ]);
    }

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
                UserInfo(model, model.CurrentUser is User u && u.Username.Value == model.UserName.Value),
                div([class_("container")], [
                    div([class_("row")], [
                        div([class_("col-xs-12 col-md-10 offset-md-1")], [
                            ArticleToggle(model),
                            ArticleList(model),
                            Pagination(model)
                        ])
                    ])
                ])
              ]);
}
