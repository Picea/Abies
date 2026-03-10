// =============================================================================
// Article State — The Write Model for the Article Aggregate
// =============================================================================

using System.Collections.Immutable;
using Picea.Abies.Conduit.Domain.Shared;

namespace Picea.Abies.Conduit.Domain.Article;

/// <summary>
/// The current state of an article aggregate.
/// </summary>
public record ArticleState(
    ArticleId Id,
    Slug Slug,
    Title Title,
    Description Description,
    Body Body,
    IReadOnlySet<Tag> Tags,
    UserId AuthorId,
    IReadOnlySet<UserId> FavoritedBy,
    IReadOnlyDictionary<CommentId, Comment> Comments,
    Timestamp CreatedAt,
    Timestamp UpdatedAt,
    bool Published,
    bool Deleted)
{
    /// <summary>
    /// The initial (unpublished) article state.
    /// </summary>
    public static readonly ArticleState Initial = new(
        Id: new ArticleId(Guid.Empty),
        Slug: new Slug(string.Empty),
        Title: new Title(string.Empty),
        Description: new Description(string.Empty),
        Body: new Body(string.Empty),
        Tags: ImmutableHashSet<Tag>.Empty,
        AuthorId: new UserId(Guid.Empty),
        FavoritedBy: ImmutableHashSet<UserId>.Empty,
        Comments: ImmutableDictionary<CommentId, Comment>.Empty,
        CreatedAt: new Timestamp(DateTimeOffset.MinValue),
        UpdatedAt: new Timestamp(DateTimeOffset.MinValue),
        Published: false,
        Deleted: false);
}
