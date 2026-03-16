using Picea.Abies.DOM;
using static Picea.Abies.Html.Attributes;
using static Picea.Abies.Html.Elements;
using static Picea.Abies.Html.Events;

namespace Picea.Abies.Conduit.App.Views;

public static class ArticlePreview
{
    public static Node Render(ArticlePreviewData article) =>
        div([class_("article-preview")],
        [
            div([class_("article-meta")],
            [
                a([href($"/profile/{article.Author.Username}")],
                    [img([src(article.Author.Image ?? "https://api.realworld.io/images/smiley-cyrus.jpeg")])]),
                div([class_("info")],
                [
                    a([href($"/profile/{article.Author.Username}"), class_("author")], [text(article.Author.Username)]),
                    span([class_("date")], [text(article.CreatedAt.ToString("MMMM d, yyyy"))])
                ]),
                FavoriteButton(article)
            ]),
            a([href($"/article/{article.Slug}"), class_("preview-link")],
            [
                h1([], [text(article.Title)]),
                p([], [text(article.Description)]),
                span([], [text("Read more...")]),
                TagList(article.TagList)
            ])
        ]);

    private static Node FavoriteButton(ArticlePreviewData article)
    {
        var btnClass = article.Favorited
            ? "btn btn-primary btn-sm pull-xs-right"
            : "btn btn-outline-primary btn-sm pull-xs-right";
        return button([class_(btnClass), onclick(new ToggleFavorite(article.Slug, article.Favorited))],
            [i([class_("ion-heart")], []), text($" {article.FavoritesCount}")]);
    }

    private static Node TagList(IReadOnlyList<string> tags) =>
        ul([class_("tag-list")],
            tags.Select(tag => li([class_("tag-default tag-pill tag-outline")], [text(tag)])).ToArray());

    public static Node List(IReadOnlyList<ArticlePreviewData> articles, bool isLoading)
    {
        if (isLoading) return div([class_("article-preview")], [text("Loading articles...")]);
        if (articles.Count == 0) return div([class_("article-preview")], [text("No articles are here... yet.")]);
        return div([], articles.Select(Render).ToArray());
    }

    public static Node Pagination(int articlesCount, int currentPage, int articlesPerPage, Func<int, string> hrefForPage)
    {
        var pageCount = (int)Math.Ceiling((double)articlesCount / articlesPerPage);
        if (pageCount <= 1) return new Empty();
        return nav([],
            [ul([class_("pagination")],
                Enumerable.Range(1, pageCount).Select(page =>
                {
                    var activeClass = page == currentPage ? "page-item active" : "page-item";
                    return li([class_(activeClass)],
                        [a([class_("page-link"), href(hrefForPage(page))], [text(page.ToString())])]);
                }).ToArray())]);
    }
}
