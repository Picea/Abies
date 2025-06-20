using Abies.Conduit.Main;
using Abies.Conduit.Routing;
using Abies.DOM;
using System.Collections.Generic;
using static Abies.Html.Attributes;
using static Abies.Html.Events;

namespace Abies.Conduit.Page.Article;

public interface Message : Abies.Message
{
    public record ArticleLoaded(Conduit.Page.Home.Article? Article) : Message;
    public record CommentsLoaded(List<Comment> Comments) : Message;
    public record CommentInputChanged(string Value) : Message;
    public record SubmitComment : Message;
    public record CommentSubmitted(Comment Comment) : Message;
    public record DeleteComment(string Id) : Message;
    public record CommentDeleted(string Id) : Message;
    public record ToggleFavorite : Message;
    public record ToggleFollow : Message;
    public record DeleteArticle : Message;
}

public record Comment(
    string Id,
    string CreatedAt,
    string UpdatedAt,
    string Body,
    Conduit.Page.Home.Profile Author
);

public record Model(
    Slug Slug,
    bool IsLoading = true,
    Conduit.Page.Home.Article? Article = null,
    List<Comment>? Comments = null,
    string CommentInput = "",
    bool SubmittingComment = false
);

public class Page : Element<Model, Message>
{
    public static Model Initialize(Message argument)
    {
        return new Model(new Slug(""));
    }    public static Subscription Subscriptions(Model model)
    {
        return new Subscription();
    }
    
    public static (Model model, IEnumerable<Command> commands) Init(Slug slug)
    {
        return (
            new Model(slug, true),
            new List<Command> { new LoadArticleCommand(slug.Value), new LoadCommentsCommand(slug.Value) }
        );
    }

    public static (Model model, IEnumerable<Command> commands) Update(Abies.Message message, Model model)
        => message switch
        {
            Message.ArticleLoaded articleLoaded => (
                model with 
                {
                    Article = articleLoaded.Article,
                    IsLoading = false
                },
                []
            ),
            Message.CommentsLoaded commentsLoaded => (
                model with { Comments = commentsLoaded.Comments },
                []
            ),
            Message.CommentInputChanged inputChanged => (
                model with { CommentInput = inputChanged.Value },
                []
            ),
            Message.SubmitComment => (
                model with { SubmittingComment = true },
                []
            ),
            Message.CommentSubmitted commentSubmitted => (
                model with 
                { 
                    Comments = model.Comments != null 
                        ? new List<Comment>(model.Comments) { commentSubmitted.Comment }
                        : new List<Comment> { commentSubmitted.Comment },
                    CommentInput = "",
                    SubmittingComment = false
                },
                []
            ),
            Message.DeleteComment => (model, []),
            Message.CommentDeleted commentDeleted => (
                model with 
                { 
                    Comments = model.Comments?.FindAll(c => c.Id != commentDeleted.Id)
                },
                []
            ),
            _ => (model, [])
        };

    private static Node ArticleMeta(Conduit.Page.Home.Article article, bool showEditDelete = false) =>
        div([@class("article-meta")], [            a([href($"/profile/{article.Author.Username}")], [
                img([src(article.Author.Image)])
            ]),            div([@class("info")], [
                a([href($"/profile/{article.Author.Username}")], [
                    text(article.Author.Username)
                ]),
                span([@class("date")], [text(article.CreatedAt)])
            ]),
            showEditDelete
                ? div([], [                    a([@class("btn btn-outline-secondary btn-sm"), 
                      href($"/editor/{article.Slug}")], 
                      [
                          i([@class("ion-edit")], []),
                          text(" Edit Article")
                      ]),                    button([@class("btn btn-outline-danger btn-sm"),
                        onclick(new Message.DeleteArticle())],
                        [
                            i([@class("ion-trash-a")], []),
                            text(" Delete Article")
                        ])
                  ])
                : div([], [                    button([@class(article.Author.Following 
                        ? "btn btn-sm btn-secondary" 
                        : "btn btn-sm btn-outline-secondary"),
                        onclick(new Message.ToggleFollow())],
                        [
                            i([@class("ion-plus-round")], []),
                            text(article.Author.Following 
                                ? $" Unfollow {article.Author.Username}" 
                                : $" Follow {article.Author.Username}")
                        ]),
                    text(" "),                    button([@class(article.Favorited 
                        ? "btn btn-sm btn-primary" 
                        : "btn btn-sm btn-outline-primary"),
                        onclick(new Message.ToggleFavorite())],
                        [
                            i([@class("ion-heart")], []),
                            text(article.Favorited 
                                ? $" Unfavorite Article ({article.FavoritesCount})" 
                                : $" Favorite Article ({article.FavoritesCount})")
                        ])
                  ])
        ]);

    private static Node ArticleBanner(Conduit.Page.Home.Article article, bool isAuthor) =>
        div([@class("banner")], [
            div([@class("container")], [
                h1([], [text(article.Title)]),
                ArticleMeta(article, isAuthor)
            ])
        ]);

    private static Node ArticleContent(Conduit.Page.Home.Article article) =>
        div([@class("row article-content")], [
            div([@class("col-md-12")], [
                p([], [text(article.Body)]),                ul([@class("tag-list")], 
                    article.TagList.ConvertAll(tag => 
                        li([@class("tag-default tag-pill tag-outline")], [text(tag)])
                    ).ToArray()
                )
            ])
        ]);

    private static Node CommentForm(Model model, User? currentUser) =>
        currentUser == null
            ? div([@class("row")], [
                div([@class("col-xs-12 col-md-8 offset-md-2")], [
                    p([], [                        a([href("/login")], [text("Sign in")]),
                        text(" or "),
                        a([href("/register")], [text("sign up")]),
                        text(" to add comments on this article.")
                    ])
                ])
              ])
            : form([@class("card comment-form")], [
                div([@class("card-block")], [                    textarea([@class("form-control"),
                        placeholder("Write a comment..."),
                        rows("3"),
                        value(model.CommentInput),
                        oninput(new Message.CommentInputChanged(model.CommentInput))],
                        []
                    )
                ]),
                div([@class("card-footer")], [                    img([@class("comment-author-img"), src("")]),                    button([@class("btn btn-sm btn-primary"),
                        disabled((string.IsNullOrWhiteSpace(model.CommentInput) || model.SubmittingComment).ToString()),
                        onclick(new Message.SubmitComment())],
                        [text("Post Comment")])
                ])
              ]);

    private static Node CommentCard(Comment comment, User? currentUser) =>
        div([@class("card")], [
            div([@class("card-block")], [
                p([@class("card-text")], [text(comment.Body)])
            ]),
            div([@class("card-footer")], [                a([@class("comment-author"), href($"/profile/{comment.Author.Username}")], [
                    img([@class("comment-author-img"), src(comment.Author.Image)])
                ]),
                text(" "),
                a([@class("comment-author"), href($"/profile/{comment.Author.Username}")], [
                    text(comment.Author.Username)
                ]),
                span([@class("date-posted")], [text(comment.CreatedAt)]),
                comment.Author.Username == (currentUser?.Username.Value ?? "") 
                    ? span([@class("mod-options")], [                        i([@class("ion-trash-a"), 
                          onclick(new Message.DeleteComment(comment.Id))], [])
                      ])
                    : text("")
            ])
        ]);

    private static Node CommentSection(Model model, User? currentUser) =>
        div([@class("row")], [
            div([@class("col-xs-12 col-md-8 offset-md-2")], [
                CommentForm(model, currentUser),
                ..(model.Comments?.ConvertAll(c => CommentCard(c, currentUser)) ?? 
                    new List<Node> { text("Loading comments...") })
            ])
        ]);

    public static Node View(Model model) =>
        model.IsLoading || model.Article == null
            ? div([@class("article-page")], [
                div([@class("container")], [
                    div([@class("row")], [
                        div([@class("col-md-10 offset-md-1")], [
                            text("Loading article...")
                        ])
                    ])
                ])
              ])
            : div([@class("article-page")], [
                ArticleBanner(model.Article, false),                div([@class("container page")], [                    div([], [ArticleContent(model.Article)]),
                    hr([@class("hr")]),
                    div([@class("article-actions")], [
                        ArticleMeta(model.Article, false)
                    ]),
                    CommentSection(model, null)
                ])
              ]);
}
