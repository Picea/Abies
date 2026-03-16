using Picea.Abies.DOM;
using static Picea.Abies.Html.Attributes;
using static Picea.Abies.Html.Elements;
using static Picea.Abies.Html.Events;

namespace Picea.Abies.Conduit.App.Pages;

public static class Article
{
    public static Node View(ArticleModel model, Session? session)
    {
        if (model.IsLoading || model.Article is null)
            return div([class_("article-page")],
                [div([class_("container page")],
                    [div([class_("row article-content")],
                        [div([class_("col-md-12")], [text("Loading article...")])])])]);

        var article = model.Article;
        return div([class_("article-page")],
        [
            Banner(article, session),
            div([class_("container page")],
            [
                div([class_("row article-content")],
                    [div([class_("col-md-12")], [p([], [text(article.Body)]), TagList(article.TagList)])]),
                hr([]),
                div([class_("article-actions")], [ArticleMeta(article, session)]),
                div([class_("row")],
                    [div([class_("col-xs-12 col-md-8 offset-md-2")],
                        [CommentForm(model.CommentBody, session),
                         ..model.Comments.Select(c => CommentCard(c, model.Slug, session)).ToArray()])])
            ])
        ]);
    }

    private static Node Banner(ArticleData article, Session? session) =>
        div([class_("banner")],
            [div([class_("container")], [h1([], [text(article.Title)]), ArticleMeta(article, session)])]);

    private static Node ArticleMeta(ArticleData article, Session? session) =>
        div([class_("article-meta")],
        [
            a([href($"/profile/{article.Author.Username}")],
                [img([src(Views.Avatar.Url(article.Author.Image))])]),
            div([class_("info")],
            [
                a([href($"/profile/{article.Author.Username}"), class_("author")], [text(article.Author.Username)]),
                span([class_("date")], [text(article.CreatedAt.ToString("MMMM d, yyyy"))])
            ]),
            ..ArticleActions(article, session)
        ]);

    private static Node[] ArticleActions(ArticleData article, Session? session)
    {
        if (session is not null && session.Username == article.Author.Username)
            return
            [
                a([class_("btn btn-outline-secondary btn-sm"), href($"/editor/{article.Slug}")],
                    [i([class_("ion-edit")], []), text("\u2003Edit Article")]),
                text(" "),
                button([class_("btn btn-outline-danger btn-sm"), onclick(new DeleteArticle(article.Slug))],
                    [i([class_("ion-trash-a")], []), text("\u2003Delete Article")])
            ];

        var followClass = article.Author.Following ? "btn btn-sm btn-secondary" : "btn btn-sm btn-outline-secondary";
        var followLabel = article.Author.Following ? $"\u2003Unfollow {article.Author.Username}" : $"\u2003Follow {article.Author.Username}";
        var favClass = article.Favorited ? "btn btn-sm btn-primary" : "btn btn-sm btn-outline-primary";
        var favLabel = article.Favorited ? $"\u2003Unfavorite Article ({article.FavoritesCount})" : $"\u2003Favorite Article ({article.FavoritesCount})";

        return
        [
            button([class_(followClass), onclick(new ToggleFollow(article.Author.Username, article.Author.Following))],
                [i([class_("ion-plus-round")], []), text(followLabel)]),
            text("\u00A0\u00A0"),
            button([class_(favClass), onclick(new ToggleFavorite(article.Slug, article.Favorited))],
                [i([class_("ion-heart")], []), text(favLabel)])
        ];
    }

    private static Node CommentForm(string commentBody, Session? session)
    {
        if (session is null)
            return p([], [a([href("/login")], [text("Sign in")]), text(" or "),
                          a([href("/register")], [text("sign up")]), text(" to add comments on this article.")]);

        return form([class_("card comment-form"), onsubmit(new CommentSubmitted())],
        [
            div([class_("card-block")],
                [textarea([class_("form-control"), placeholder("Write a comment..."), rows("3"),
                           value(commentBody), oninput(e => new CommentBodyChanged(e?.Value ?? ""))], [])]),
            div([class_("card-footer")],
            [
                img([src(Views.Avatar.Url(session.Image)), class_("comment-author-img")]),
                button([class_("btn btn-sm btn-primary"), type("submit")], [text("Post Comment")])
            ])
        ]);
    }

    private static Node CommentCard(CommentData comment, string slug, Session? session) =>
        div([class_("card")],
        [
            div([class_("card-block")], [p([class_("card-text")], [text(comment.Body)])]),
            div([class_("card-footer")],
            [
                a([href($"/profile/{comment.Author.Username}"), class_("comment-author")],
                    [img([src(Views.Avatar.Url(comment.Author.Image)), class_("comment-author-img")])]),
                text("\u00A0"),
                a([href($"/profile/{comment.Author.Username}"), class_("comment-author")], [text(comment.Author.Username)]),
                span([class_("date-posted")], [text(comment.CreatedAt.ToString("MMMM d, yyyy"))]),
                ..DeleteButton(comment, slug, session)
            ])
        ]);

    private static Node[] DeleteButton(CommentData comment, string slug, Session? session)
    {
        if (session is null || session.Username != comment.Author.Username) return [];
        return [span([class_("mod-options")], [i([class_("ion-trash-a"), onclick(new DeleteComment(slug, comment.Id))], [])])];
    }

    private static Node TagList(IReadOnlyList<string> tags) =>
        tags.Count == 0 ? text("") :
        ul([class_("tag-list")], tags.Select(tag => li([class_("tag-default tag-pill tag-outline")], [text(tag)])).ToArray());
}
