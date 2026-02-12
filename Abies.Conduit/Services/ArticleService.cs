using Abies.Conduit.Page.Article;
using Abies.Conduit.Page.Home;

namespace Abies.Conduit.Services;

public static class ArticleService
{
    public static async Task<(List<Article> articles, int count)> GetArticlesAsync(string? tag = null, string? author = null, string? favoritedBy = null, int limit = 10, int offset = 0)
    {
        var response = await ApiClient.GetArticlesAsync(tag, author, favoritedBy, limit, offset);

        return (
            response.Articles.Select(a => MapArticleFromApi(a)).ToList(),
            response.ArticlesCount
        );
    }

    public static async Task<(List<Article> articles, int count)> GetFeedArticlesAsync(int limit = 10, int offset = 0)
    {
        var response = await ApiClient.GetFeedArticlesAsync(limit, offset);

        return (
            response.Articles.Select(a => MapArticleFromApi(a)).ToList(),
            response.ArticlesCount
        );
    }

    public static async Task<Article> GetArticleAsync(string slug)
    {
        var response = await ApiClient.GetArticleAsync(slug);
        return MapArticleFromApi(response.Article);
    }

    public static async Task<List<Comment>> GetCommentsAsync(string slug)
    {
        var response = await ApiClient.GetCommentsAsync(slug);
        return response.Comments.Select(c => new Comment(
            c.Id.ToString(),
            c.CreatedAt,
            c.UpdatedAt,
            c.Body,
            new Profile(
                c.Author.Username,
                c.Author.Bio ?? "",
                c.Author.Image ?? "",
                c.Author.Following
            )
        )).ToList();
    }

    public static async Task<Comment> AddCommentAsync(string slug, string body)
    {
        var response = await ApiClient.AddCommentAsync(slug, body);
        return new Comment(
            response.Comment.Id.ToString(),
            response.Comment.CreatedAt,
            response.Comment.UpdatedAt,
            response.Comment.Body,
            new Profile(
                response.Comment.Author.Username,
                response.Comment.Author.Bio ?? "",
                response.Comment.Author.Image ?? "",
                response.Comment.Author.Following
            )
        );
    }

    public static async Task DeleteCommentAsync(string slug, string id)
    {
        await ApiClient.DeleteCommentAsync(slug, id);
    }

    public static async Task<Article> CreateArticleAsync(string title, string description, string body, List<string> tagList)
    {
        var response = await ApiClient.CreateArticleAsync(title, description, body, tagList);
        return MapArticleFromApi(response.Article);
    }

    public static async Task<Article> UpdateArticleAsync(string slug, string title, string description, string body)
    {
        var response = await ApiClient.UpdateArticleAsync(slug, title, description, body);
        return MapArticleFromApi(response.Article);
    }

    public static async Task DeleteArticleAsync(string slug)
    {
        await ApiClient.DeleteArticleAsync(slug);
    }

    public static async Task<Article> FavoriteArticleAsync(string slug)
    {
        var response = await ApiClient.FavoriteArticleAsync(slug);
        return MapArticleFromApi(response.Article);
    }

    public static async Task<Article> UnfavoriteArticleAsync(string slug)
    {
        var response = await ApiClient.UnfavoriteArticleAsync(slug);
        return MapArticleFromApi(response.Article);
    }

    private static Article MapArticleFromApi(ArticleData articleData)
    {
        return new Article(
            articleData.Slug,
            articleData.Title,
            articleData.Description,
            articleData.Body,
            articleData.TagList,
            articleData.CreatedAt,
            articleData.UpdatedAt,
            articleData.Favorited,
            articleData.FavoritesCount,
            new Profile(
                articleData.Author.Username,
                articleData.Author.Bio ?? "",
                articleData.Author.Image ?? "",
                articleData.Author.Following
            )
        );
    }
}
