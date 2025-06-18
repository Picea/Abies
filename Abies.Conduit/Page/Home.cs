using Abies.Conduit.Main;
using Abies.Conduit.Routing;
using Abies.Conduit;
using Abies.DOM;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Abies.Html.Attributes;
using static Abies.Html.Events;

namespace Abies.Conduit.Page.Home;

public interface Message : Abies.Message
{
    public record ArticlesLoaded(List<Article> Articles, int ArticlesCount) : Message;
    public record TagsLoaded(List<string> Tags) : Message;
    public record ToggleFeedTab(FeedTab Tab) : Message;
    public record TagSelected(string Tag) : Message;
}

public enum FeedTab
{
    Global,
    YourFeed,
    Tag
}

public record Article(
    string Slug,
    string Title,
    string Description,
    string Body,
    List<string> TagList,
    string CreatedAt,
    string UpdatedAt,
    bool Favorited,
    int FavoritesCount,
    Profile Author
);

public record Profile(
    string Username,
    string Bio,
    string Image,
    bool Following
);

public record Model(
    List<Article> Articles,
    int ArticlesCount,
    List<string> Tags,
    FeedTab ActiveTab,
    string ActiveTag,
    bool IsLoading
);

public class Page : Element<Model, Message>
{
    public static Model Initialize(Message argument)
    {
        return new Model(new List<Article>(), 0, new List<string>(), FeedTab.Global, "", true);
    }
    
    public static (Model model, IEnumerable<Command> commands) Init()
    {
        return (
            new Model(new List<Article>(), 0, new List<string>(), FeedTab.Global, "", true),
            new List<Command> { new LoadArticlesCommand(), new LoadTagsCommand() }
        );
    }
      public static Subscription Subscriptions(Model model)
    {
        return new Subscription();
    }

    public static (Model model, Command command) Update(Abies.Message message, Model model)
    {

            return message switch
            {
                Message.ArticlesLoaded articlesLoaded => (
                    model with
                    {
                        Articles = articlesLoaded.Articles,
                        ArticlesCount = articlesLoaded.ArticlesCount,
                        IsLoading = false
                    },
                    Commands.None
                ),
                Message.TagsLoaded tagsLoaded => (
                    model with { Tags = tagsLoaded.Tags },
                    Commands.None
                ),
                Message.ToggleFeedTab toggleFeed => (
                    model with
                    {
                        ActiveTab = toggleFeed.Tab,
                        ActiveTag = toggleFeed.Tab == FeedTab.Tag ? model.ActiveTag : "",
                        IsLoading = true
                    },
                    toggleFeed.Tab switch
                    {
                        FeedTab.YourFeed => new LoadFeedCommand(),
                        FeedTab.Tag => new LoadArticlesCommand(tag: model.ActiveTag),
                        _ => new LoadArticlesCommand()
                    }
                ),
                Message.TagSelected tagSelected => (
                    model with
                    {
                        ActiveTab = FeedTab.Tag,
                        ActiveTag = tagSelected.Tag,
                        IsLoading = true
                    },
                    new LoadArticlesCommand(tag: tagSelected.Tag)
                ),
                _ => (model, Commands.None)
            };
    }

    private static Node Banner() => 
        div([class_("banner")], [
            div([class_("container")], [
                h1([class_("logo-font")], [text("conduit")]),
                p([], [text("A place to share your knowledge.")])
            ])
        ]);

    private static Node FeedToggle(Model model, User? currentUser) =>
        div([class_("feed-toggle")], [
            ul([class_("nav nav-pills outline-active")], [
                currentUser != null
                    ? li([class_("nav-item")], [
                        a([class_(model.ActiveTab == FeedTab.YourFeed 
                            ? "nav-link active" 
                            : "nav-link"),
                          onclick(new Message.ToggleFeedTab(FeedTab.YourFeed)),
                          href("")],
                          [text("Your Feed")])
                      ])
                    : text(""),
                li([class_("nav-item")], [
                    a([class_(model.ActiveTab == FeedTab.Global 
                        ? "nav-link active" 
                        : "nav-link"),
                      onclick(new Message.ToggleFeedTab(FeedTab.Global)),
                      href("")],
                      [text("Global Feed")])
                ]),
                model.ActiveTab == FeedTab.Tag
                    ? li([class_("nav-item")], [
                        a([class_("nav-link active"), 
                          href("")], 
                          [text($"# {model.ActiveTag}")])
                      ])
                    : text("")
            ])
        ]);

    private static Node ArticlePreview(Article article) =>
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
                span([], [text("Read more...")]),
                ul([class_("tag-list")], [..article.TagList.ConvertAll(tag => 
                    li([class_("tag-default tag-pill tag-outline")], [
                        text(tag)
                    ])
                )])
            ])
        ]);

    private static Node ArticleList(Model model) =>
        model.IsLoading
            ? div([class_("article-preview")], [text("Loading articles...")])
            : model.Articles == null || model.Articles.Count == 0
                ? div([class_("article-preview")], [text("No articles are here... yet.")])
                : div([], [..model.Articles.ConvertAll(article => ArticlePreview(article))]);

    private static Node TagCloud(Model model) =>
        div([class_("sidebar")], [
            p([], [text("Popular Tags")]),
            div([class_("tag-list")], [ .. model.Tags == null
                ? [text("Loading tags...")]
                : model.Tags.ConvertAll(tag =>
                    a([class_("tag-pill tag-default"),
                      href(""),
                      onclick(new Message.TagSelected(tag))],
                      [text(tag)]
                    )
                )])
        ]);

    public static Node View(Model model) =>
        div([class_("home-page")], [
            Banner(),
            div([class_("container page")], [
                div([class_("row")], [
                    div([class_("col-md-9")], [
                        FeedToggle(model, null),
                        ArticleList(model)
                    ]),
                    div([class_("col-md-3")], [
                        TagCloud(model)
                    ])
                ])
            ])
        ]);
}