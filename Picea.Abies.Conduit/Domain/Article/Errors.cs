// =============================================================================
// Article Errors — Domain-Specific Failure Cases
// =============================================================================

using Picea.Abies.Conduit.Domain.Shared;

namespace Picea.Abies.Conduit.Domain.Article;

/// <summary>
/// Errors produced when Article command validation fails.
/// </summary>
public interface ArticleError
{
    record Validation(string Message) : ArticleError;
    record DuplicateSlug : ArticleError;
    record AlreadyPublished : ArticleError;
    record NotPublished : ArticleError;
    record AlreadyDeleted : ArticleError;
    record NotAuthor(UserId RequesterId) : ArticleError;
    record AlreadyFavorited(UserId UserId) : ArticleError;
    record NotFavorited(UserId UserId) : ArticleError;
    record CommentNotFound(CommentId CommentId) : ArticleError;
    record NotCommentAuthor(CommentId CommentId, UserId RequesterId) : ArticleError;
}
