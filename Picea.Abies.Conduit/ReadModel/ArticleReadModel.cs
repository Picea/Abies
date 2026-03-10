// =============================================================================
// Article Read Model — Denormalized View for Query APIs
// =============================================================================
// Flat records optimized for the Conduit API response shapes.
// These are the CQRS read side — projected from ArticleEvents.
//
// Note: The Conduit spec (since 2024/08/16) excludes the body field from
// list endpoints. The body is included in the read model and filtered at
// the API boundary when serializing list responses.
// =============================================================================

namespace Picea.Abies.Conduit.ReadModel;

/// <summary>
/// Denormalized article read model projected from the Article event stream.
/// Includes author profile data for single-query API responses.
/// </summary>
/// <param name="Id">The article's unique identifier (internal).</param>
/// <param name="Slug">The URL-friendly slug.</param>
/// <param name="Title">The article title.</param>
/// <param name="Description">The article description/summary.</param>
/// <param name="Body">The article body (Markdown).</param>
/// <param name="AuthorId">The author's user ID (for joins).</param>
/// <param name="AuthorUsername">Denormalized author username.</param>
/// <param name="AuthorBio">Denormalized author biography.</param>
/// <param name="AuthorImage">Denormalized author avatar URL.</param>
/// <param name="CreatedAt">When the article was created.</param>
/// <param name="UpdatedAt">When the article was last updated.</param>
/// <param name="FavoritesCount">Total number of users who favorited this article.</param>
/// <param name="Deleted">Whether the article has been soft-deleted.</param>
public sealed record ArticleReadModel(
    Guid Id,
    string Slug,
    string Title,
    string Description,
    string Body,
    Guid AuthorId,
    string AuthorUsername,
    string AuthorBio,
    string AuthorImage,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int FavoritesCount,
    bool Deleted);

/// <summary>
/// A tag associated with an article.
/// </summary>
/// <param name="ArticleId">The article this tag belongs to.</param>
/// <param name="Tag">The tag name.</param>
public sealed record ArticleTagReadModel(
    Guid ArticleId,
    string Tag);

/// <summary>
/// A user's favorite on an article.
/// </summary>
/// <param name="UserId">The user who favorited.</param>
/// <param name="ArticleId">The article that was favorited.</param>
public sealed record FavoriteReadModel(
    Guid UserId,
    Guid ArticleId);
