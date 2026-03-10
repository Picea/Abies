// =============================================================================
// Article Commands — Intent Representations
// =============================================================================

using Picea.Abies.Conduit.Domain.Shared;
using Picea;

namespace Picea.Abies.Conduit.Domain.Article;

/// <summary>
/// Commands representing user intent for the Article aggregate.
/// </summary>
public interface ArticleCommand
{
    record CreateArticle(
        ArticleId Id,
        Title Title,
        Description Description,
        Body Body,
        IReadOnlySet<Tag> Tags,
        UserId AuthorId,
        Timestamp CreatedAt) : ArticleCommand;

    record UpdateArticle(
        Option<Title> Title,
        Option<Description> Description,
        Option<Body> Body,
        UserId RequesterId,
        Timestamp UpdatedAt) : ArticleCommand;

    record DeleteArticle(UserId RequesterId) : ArticleCommand;

    record AddComment(
        CommentId CommentId,
        UserId AuthorId,
        CommentBody Body,
        Timestamp CreatedAt) : ArticleCommand;

    record DeleteComment(CommentId CommentId, UserId RequesterId) : ArticleCommand;

    record FavoriteArticle(UserId UserId) : ArticleCommand;

    record UnfavoriteArticle(UserId UserId) : ArticleCommand;
}
