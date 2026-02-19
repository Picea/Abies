using System.Globalization;
using Abies.Conduit.Main;
using Abies.DOM;
using static Abies.Html.Attributes;
using static Abies.Html.Events;

namespace Abies.Conduit.Page.Article;

public interface Message : Abies.Message
{
    record ArticleLoaded(Home.Article? Article) : Message;
    record CommentsLoaded(List<Comment> Comments) : Message;
    record CommentInputChanged(string Value) : Message;
    record SubmitComment : Message;
    record CommentSubmitted(Comment Comment) : Message;
    record DeleteComment(string Id) : Message;
    record CommentDeleted(string Id) : Message;
    record ToggleFavorite : Message;
    record ToggleFollow : Message;
    record DeleteArticle : Message;
    record ArticleDeleted : Message;
}

public record Comment(
    string Id,
    string CreatedAt,
    string UpdatedAt,
    string Body,
    Home.Profile Author
);

public record Model(
    Slug Slug,
    bool IsLoading = true,
    Home.Article? Article = null,
    List<Comment>? Comments = null,
    string CommentInput = "",
    bool SubmittingComment = false,
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
        return new Model(new Slug(""));
    }

    public static Subscription Subscriptions(Model model)
    {
        return SubscriptionModule.None;
    }

    public static (Model model, IEnumerable<Command> commands) Init(Slug slug)
    {
        return (
            new Model(slug, true),
            [new LoadArticleCommand(slug.Value), new LoadCommentsCommand(slug.Value)]
        );
    }

    public static (Model model, Command command) Update(Abies.Message message, Model model)
        => message switch
        {
            Message.ArticleLoaded articleLoaded => (
                model with
                {
                    Article = articleLoaded.Article,
                    IsLoading = false
                },
                Commands.None
            ),
            Message.CommentsLoaded commentsLoaded => (
                model with { Comments = commentsLoaded.Comments },
                Commands.None
            ),
            Message.CommentInputChanged inputChanged => (
                model with { CommentInput = inputChanged.Value },
                Commands.None
            ),
            Message.SubmitComment => (
                model with { SubmittingComment = true },
                new SubmitCommentCommand(model.Slug.Value, model.CommentInput)
            ),
            Message.CommentSubmitted commentSubmitted => (
                model with
                {
                    Comments = model.Comments is not null
                        ? [.. model.Comments, commentSubmitted.Comment]
                        : [commentSubmitted.Comment],
                    CommentInput = "",
                    SubmittingComment = false
                },
                new LoadCommentsCommand(model.Slug.Value)
            ),
            Message.DeleteComment delete => (
                model,
                new DeleteCommentCommand(model.Slug.Value, delete.Id)
            ),
            Message.CommentDeleted commentDeleted => (
                model with
                {
                    Comments = model.Comments?.FindAll(c => c.Id != commentDeleted.Id)
                },
                Commands.None
            ),
            Message.ToggleFavorite => (
                model.Article is not null
                    ? model with
                    {
                        Article = model.Article with
                        {
                            Favorited = !model.Article.Favorited,
                            FavoritesCount = model.Article.Favorited
                                ? Math.Max(0, model.Article.FavoritesCount - 1)
                                : model.Article.FavoritesCount + 1
                        }
                    }
                    : model,
                model.Article is not null ? new ToggleFavoriteCommand(model.Article.Slug, model.Article.Favorited) : Commands.None
            ),
            Message.ToggleFollow => (
                model.Article is not null
                    ? model with { Article = model.Article with { Author = model.Article.Author with { Following = !model.Article.Author.Following } } }
                    : model,
                model.Article is not null ? new ToggleFollowCommand(model.Article.Author.Username, model.Article.Author.Following) : Commands.None
            ),
            Message.DeleteArticle => (
                model,
                model.Article is not null ? new DeleteArticleCommand(model.Article.Slug) : Commands.None
            ),
            Message.ArticleDeleted => (
                model,
                Commands.None
            ),
            _ => (model, Commands.None)
        };

    private static Node ArticleMeta(Home.Article article, bool showEditDelete = false) =>
        div([class_("article-meta")], [            a([href($"/profile/{article.Author.Username}")], [
                img([src(article.Author.Image), alt($"{article.Author.Username} profile image")])
            ]),            div([class_("info")], [
                a([href($"/profile/{article.Author.Username}")], [
                    text(article.Author.Username)
                ]),
                span([class_("date")], [text(FormatDate(article.CreatedAt))])
            ]),
            showEditDelete
                ? div([], [                    a([class_("btn btn-outline-secondary btn-sm"),
                      href($"/editor/{article.Slug}")],
                      [
                          i([class_("ion-edit")], []),
                          text(" Edit Article")
                      ]),                    button([class_("btn btn-outline-danger btn-sm"),
                        onclick(new Message.DeleteArticle())],
                        [
                            i([class_("ion-trash-a")], []),
                            text(" Delete Article")
                        ])
                  ])
                : div([], [                    button([class_(article.Author.Following
                        ? "btn btn-sm btn-secondary action-btn"
                        : "btn btn-sm btn-outline-secondary action-btn"),
                        onclick(new Message.ToggleFollow())],
                        [
                            i([class_("ion-plus-round")], []),
                            text(article.Author.Following
                                ? $" Unfollow {article.Author.Username}"
                                : $" Follow {article.Author.Username}")
                        ]),
                    text(" "),                    button([class_(article.Favorited
                        ? "btn btn-sm btn-primary action-btn"
                        : "btn btn-sm btn-outline-primary action-btn"),
                        onclick(new Message.ToggleFavorite())],
                        [
                            i([class_("ion-heart")], []),
                            text(article.Favorited
                                ? $" Unfavorite Article ({article.FavoritesCount})"
                                : $" Favorite Article ({article.FavoritesCount})")
                        ])
                  ])
        ]);

    private static Node ArticleBanner(Home.Article article, bool isAuthor) =>
        div([class_("banner")], [
            div([class_("container")], [
                h1([], [text(article.Title)]),
                ArticleMeta(article, isAuthor)
            ])
        ]);

    private static Node ArticleContent(Home.Article article)
    {
        var html = Markdig.Markdown.ToHtml(article.Body);
        return div([class_("row article-content"), data("testid", "article-content")], [
                div([class_("col-md-12")], [
                raw(html),
                ul([class_("tag-list")],
                    article.TagList.ConvertAll(tag =>
                        li([class_("tag-default tag-pill tag-outline")], [text(tag)])
                    ).ToArray()
                )
            ])
            ]);
    }

    private static Node CommentForm(Model model) =>
        model.CurrentUser is null
            ? div([class_("row")], [
                div([class_("col-xs-12 col-md-8 offset-md-2")], [
                    p([], [                        a([href("/login")], [text("Sign in")]),
                        text(" or "),
                        a([href("/register")], [text("sign up")]),
                        text(" to add comments on this article.")
                    ])
                ])
              ])
            : form([class_("card comment-form"), onsubmit(new Message.SubmitComment())], [
                div([class_("card-block")], [
                    textarea([
                        class_("form-control"),
                        placeholder("Write a comment..."),
                        rows("3"),
                        oninput(d => new Message.CommentInputChanged(d?.Value ?? "")),
                        onchange(d => new Message.CommentInputChanged(d?.Value ?? ""))
                    ], [text(model.CommentInput)])
                ]),
                div([class_("card-footer")], [
                    img([class_("comment-author-img"), src(model.CurrentUser?.Image ?? ""), alt($"{model.CurrentUser?.Username.Value ?? "User"} profile image")]),
                    button([
                        class_("btn btn-sm btn-primary"),
                        type("submit"),
                        ..((string.IsNullOrWhiteSpace(model.CommentInput) || model.SubmittingComment) ? (DOM.Attribute[])[disabled()] : []),
                        onclick(new Message.SubmitComment())
                    ], [text("Post Comment")])
                ])
              ]);

    private static Node CommentCard(Model model, Comment comment) =>
            div([class_("card")], [
                div([class_("card-block")], [
                p([class_("card-text")], [text(comment.Body)])
            ]),
            div([class_("card-footer")], [                a([class_("comment-author"), href($"/profile/{comment.Author.Username}")], [
                    img([class_("comment-author-img"), src(comment.Author.Image), alt($"{comment.Author.Username} profile image")])
                ]),
                text(" "),
                a([class_("comment-author"), href($"/profile/{comment.Author.Username}")], [
                    text(comment.Author.Username)
                ]),
                span([class_("date-posted")], [text(FormatDate(comment.CreatedAt))]),
                comment.Author.Username == (model.CurrentUser?.Username.Value ?? "")
                    ? span([class_("mod-options")], [                        i([class_("ion-trash-a"),
                          onclick(new Message.DeleteComment(comment.Id))], [])
                      ])
                    : text("")
            ])
            ], id: $"comment-{comment.Id}");

    private static Node CommentSection(Model model) =>
        div([class_("row")], [
            div([class_("col-xs-12 col-md-8 offset-md-2")], [
                CommentForm(model),
                ..(model.Comments?.ConvertAll(c => CommentCard(model, c)) ??
                    [text("Loading comments...")])
            ])
        ]);

    public static Node View(Model model) =>
        model.IsLoading || model.Article is null
            ? div([class_("article-page"), data("testid", "article-page")], [
                div([class_("container")], [
                    div([class_("row")], [
                        div([class_("col-md-10 offset-md-1")], [
                            text("Loading article...")
                        ])
                    ])
                ])
              ])
            : div([class_("article-page"), data("testid", "article-page")], [
                ArticleBanner(model.Article, model.CurrentUser is User u && u.Username.Value == model.Article.Author.Username),                div([class_("container page")], [                    div([], [ArticleContent(model.Article)]),
                    hr([class_("hr")]),
                    div([class_("article-actions")], [
                        ArticleMeta(model.Article, model.CurrentUser is User u2 && u2.Username.Value == model.Article.Author.Username)
                    ]),
                    CommentSection(model)
                ])
              ]);
}
