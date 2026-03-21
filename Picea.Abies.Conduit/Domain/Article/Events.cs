// =============================================================================
// Article Events — Facts That Happened
// =============================================================================

using Picea.Abies.Conduit.Domain.Shared;

namespace Picea.Abies.Conduit.Domain.Article;

/// <summary>
/// Events that have occurred in the Article aggregate.
/// </summary>
public interface ArticleEvent
{
    /// <summary>A new article was created.</summary>
    record ArticleCreated(
        ArticleId Id,
        Slug Slug,
        Title Title,
        Description Description,
        Body Body,
        IReadOnlySet<Tag> Tags,
        UserId AuthorId,
        Timestamp CreatedAt) : ArticleEvent;

    /// <summary>The article's content was updated.</summary>
    record ArticleUpdated(
        Option<Title> Title,
        Option<Description> Description,
        Option<Body> Body,
        Slug Slug,
        Timestamp UpdatedAt) : ArticleEvent;

    /// <summary>The article was deleted.</summary>
    record ArticleDeleted : ArticleEvent;

    /// <summary>A comment was added to the article.</summary>
    record CommentAdded(
        CommentId CommentId,
        UserId AuthorId,
        CommentBody Body,
        Timestamp CreatedAt) : ArticleEvent;

    /// <summary>A comment was removed from the article.</summary>
    record CommentDeleted(CommentId CommentId) : ArticleEvent;

    /// <summary>A user favorited the article.</summary>
    record ArticleFavorited(UserId UserId) : ArticleEvent;

    /// <summary>A user unfavorited the article.</summary>
    record ArticleUnfavorited(UserId UserId) : ArticleEvent;
}
