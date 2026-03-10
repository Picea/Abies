// =============================================================================
// Comment Read Model — Denormalized View for Query APIs
// =============================================================================
// Flat record optimized for the Conduit API response shape.
// Includes denormalized author data for single-query responses.
// =============================================================================

namespace Picea.Abies.Conduit.ReadModel;

/// <summary>
/// Denormalized comment read model projected from ArticleEvent.CommentAdded.
/// Includes author profile data for single-query API responses.
/// </summary>
/// <param name="Id">The comment's unique identifier.</param>
/// <param name="ArticleId">The article this comment belongs to.</param>
/// <param name="ArticleSlug">Denormalized article slug (for API lookups by slug).</param>
/// <param name="AuthorId">The comment author's user ID.</param>
/// <param name="AuthorUsername">Denormalized author username.</param>
/// <param name="AuthorBio">Denormalized author biography.</param>
/// <param name="AuthorImage">Denormalized author avatar URL.</param>
/// <param name="Body">The comment text.</param>
/// <param name="CreatedAt">When the comment was posted.</param>
/// <param name="UpdatedAt">When the comment was last updated (same as CreatedAt — comments are immutable).</param>
/// <param name="Deleted">Whether the comment has been soft-deleted.</param>
public sealed record CommentReadModel(
    Guid Id,
    Guid ArticleId,
    string ArticleSlug,
    Guid AuthorId,
    string AuthorUsername,
    string AuthorBio,
    string AuthorImage,
    string Body,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool Deleted);
