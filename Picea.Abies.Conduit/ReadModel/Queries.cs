// =============================================================================
// Query Capabilities — Delegate Types for Read Store Access
// =============================================================================
// These are the "ports" (in Hexagonal Architecture / Ports & Adapters) for
// the read side of CQRS. They are expressed as delegate types rather than
// interfaces, following the DDD instruction: "capabilities as function
// delegates, not domain-level service objects."
//
// The API layer depends on these delegates. The PostgreSQL project provides
// the concrete implementations. Wiring happens in the composition root
// (Aspire AppHost / DI container).
//
// Each delegate returns ValueTask for async I/O with minimal allocation.
// Optional results use Picea.Option<T> — no null.
// =============================================================================

namespace Picea.Abies.Conduit.ReadModel;

// ─── User Queries ─────────────────────────────────────────────────────────────

/// <summary>
/// Finds a user by email address (used for login).
/// </summary>
public delegate ValueTask<Option<UserReadModel>> FindUserByEmail(
    string email,
    CancellationToken cancellationToken = default);

/// <summary>
/// Finds a user by their unique identifier.
/// </summary>
public delegate ValueTask<Option<UserReadModel>> FindUserById(
    Guid userId,
    CancellationToken cancellationToken = default);

/// <summary>
/// Finds a user by their username.
/// </summary>
public delegate ValueTask<Option<UserReadModel>> FindUserByUsername(
    string username,
    CancellationToken cancellationToken = default);

/// <summary>
/// Gets a user's profile as seen by another user (includes following status).
/// </summary>
/// <param name="username">The username of the profile to retrieve.</param>
/// <param name="currentUserId">The requesting user's ID (for following status). None if unauthenticated.</param>
/// <param name="cancellationToken">Cancellation token.</param>
public delegate ValueTask<Option<ProfileReadModel>> GetProfile(
    string username,
    Option<Guid> currentUserId,
    CancellationToken cancellationToken = default);

// ─── Article Queries ──────────────────────────────────────────────────────────

/// <summary>
/// Lists articles with optional filters, pagination, and author profile data.
/// </summary>
/// <param name="filter">The filter criteria.</param>
/// <param name="cancellationToken">Cancellation token.</param>
public delegate ValueTask<ArticleListResult> ListArticles(
    ArticleFilter filter,
    CancellationToken cancellationToken = default);

/// <summary>
/// Gets the feed for a user (articles by authors they follow).
/// </summary>
/// <param name="userId">The authenticated user's ID.</param>
/// <param name="limit">Maximum number of articles to return.</param>
/// <param name="offset">Number of articles to skip.</param>
/// <param name="cancellationToken">Cancellation token.</param>
public delegate ValueTask<ArticleListResult> GetFeed(
    Guid userId,
    int limit,
    int offset,
    CancellationToken cancellationToken = default);

/// <summary>
/// Finds a single article by its slug.
/// </summary>
/// <param name="slug">The article slug.</param>
/// <param name="currentUserId">The requesting user's ID (for favorited status). None if unauthenticated.</param>
/// <param name="cancellationToken">Cancellation token.</param>
public delegate ValueTask<Option<ArticleQueryResult>> FindArticleBySlug(
    string slug,
    Option<Guid> currentUserId,
    CancellationToken cancellationToken = default);

/// <summary>
/// Finds an article's aggregate ID by its slug.
/// Needed for command routing — maps the URL slug to the aggregate GUID.
/// </summary>
/// <param name="slug">The article slug.</param>
/// <param name="cancellationToken">Cancellation token.</param>
public delegate ValueTask<Option<Guid>> FindArticleIdBySlug(
    string slug,
    CancellationToken cancellationToken = default);

// ─── Comment Queries ──────────────────────────────────────────────────────────

/// <summary>
/// Gets all comments for an article.
/// </summary>
/// <param name="slug">The article slug.</param>
/// <param name="currentUserId">The requesting user's ID (for author following status). None if unauthenticated.</param>
/// <param name="cancellationToken">Cancellation token.</param>
public delegate ValueTask<IReadOnlyList<CommentQueryResult>> GetComments(
    string slug,
    Option<Guid> currentUserId,
    CancellationToken cancellationToken = default);

// ─── Tag Queries ──────────────────────────────────────────────────────────────

/// <summary>
/// Gets all tags that have been used across all articles.
/// </summary>
public delegate ValueTask<IReadOnlyList<string>> GetTags(
    CancellationToken cancellationToken = default);

// ─── Query Parameters & Results ───────────────────────────────────────────────

/// <summary>
/// Filter criteria for listing articles.
/// </summary>
/// <param name="Tag">Filter by tag name.</param>
/// <param name="Author">Filter by author username.</param>
/// <param name="FavoritedBy">Filter by username of user who favorited.</param>
/// <param name="Limit">Maximum number of results (default 20).</param>
/// <param name="Offset">Number of results to skip (default 0).</param>
/// <param name="CurrentUserId">The requesting user's ID (for favorited/following status).</param>
public sealed record ArticleFilter(
    Option<string> Tag,
    Option<string> Author,
    Option<string> FavoritedBy,
    int Limit = 20,
    int Offset = 0,
    Option<Guid> CurrentUserId = default);

/// <summary>
/// Result of listing articles — includes total count for pagination.
/// </summary>
/// <param name="Articles">The articles matching the filter.</param>
/// <param name="ArticlesCount">Total number of matching articles (before pagination).</param>
public sealed record ArticleListResult(
    IReadOnlyList<ArticleQueryResult> Articles,
    int ArticlesCount);

/// <summary>
/// A fully hydrated article for API responses — includes author profile,
/// tag list, favorited status, and favorites count.
/// </summary>
/// <param name="Slug">The URL-friendly slug.</param>
/// <param name="Title">The article title.</param>
/// <param name="Description">The article description.</param>
/// <param name="Body">The article body (Markdown).</param>
/// <param name="TagList">The list of tags.</param>
/// <param name="CreatedAt">When the article was created.</param>
/// <param name="UpdatedAt">When the article was last updated.</param>
/// <param name="Favorited">Whether the requesting user has favorited this article.</param>
/// <param name="FavoritesCount">Total number of favorites.</param>
/// <param name="Author">The author's profile (includes following status relative to requester).</param>
public sealed record ArticleQueryResult(
    string Slug,
    string Title,
    string Description,
    string Body,
    IReadOnlyList<string> TagList,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool Favorited,
    int FavoritesCount,
    ProfileReadModel Author);

/// <summary>
/// A fully hydrated comment for API responses — includes author profile.
/// </summary>
/// <param name="Id">The comment's unique identifier (integer for Conduit spec compatibility).</param>
/// <param name="CreatedAt">When the comment was posted.</param>
/// <param name="UpdatedAt">When the comment was last updated.</param>
/// <param name="Body">The comment text.</param>
/// <param name="Author">The author's profile (includes following status relative to requester).</param>
public sealed record CommentQueryResult(
    Guid Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string Body,
    ProfileReadModel Author);
