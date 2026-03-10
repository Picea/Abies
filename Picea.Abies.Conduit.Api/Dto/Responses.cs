// =============================================================================
// API DTOs — Response Bodies
// =============================================================================
// These records model the JSON response payloads per the Conduit spec.
// All responses are wrapped in a named root object:
//   { "user": { ... } }
//   { "profile": { ... } }
//   { "article": { ... } }
//   { "articles": [...], "articlesCount": N }   ← no body field per spec
//   { "comment": { ... } }
//   { "comments": [...] }
//   { "tags": [...] }
//
// Spec note (2024-08-16): Multiple Articles response EXCLUDES the body field.
// =============================================================================

using Picea.Abies.Conduit.ReadModel;

namespace Picea.Abies.Conduit.Api.Dto;

// ─── User / Auth ──────────────────────────────────────────────────────────────

/// <summary>Wrapper: <c>{ "user": { ... } }</c></summary>
public sealed record UserResponse(UserDto User);

/// <summary>User DTO — includes the JWT token.</summary>
public sealed record UserDto(
    string Email,
    string Token,
    string Username,
    string Bio,
    string? Image);

// ─── Profile ──────────────────────────────────────────────────────────────────

/// <summary>Wrapper: <c>{ "profile": { ... } }</c></summary>
public sealed record ProfileResponse(ProfileDto Profile);

/// <summary>Profile DTO.</summary>
public sealed record ProfileDto(
    string Username,
    string Bio,
    string? Image,
    bool Following);

// ─── Article ──────────────────────────────────────────────────────────────────

/// <summary>Wrapper: <c>{ "article": { ... } }</c></summary>
public sealed record SingleArticleResponse(ArticleDto Article);

/// <summary>Wrapper: <c>{ "articles": [...], "articlesCount": N }</c></summary>
public sealed record MultipleArticlesResponse(
    IReadOnlyList<ArticleListDto> Articles,
    int ArticlesCount);

/// <summary>
/// Full article DTO — includes body field.
/// Used for single-article responses.
/// </summary>
public sealed record ArticleDto(
    string Slug,
    string Title,
    string Description,
    string Body,
    IReadOnlyList<string> TagList,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool Favorited,
    int FavoritesCount,
    ProfileDto Author);

/// <summary>
/// Article DTO for list responses — EXCLUDES body field per spec (since 2024-08-16).
/// </summary>
public sealed record ArticleListDto(
    string Slug,
    string Title,
    string Description,
    IReadOnlyList<string> TagList,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool Favorited,
    int FavoritesCount,
    ProfileDto Author);

// ─── Comment ──────────────────────────────────────────────────────────────────

/// <summary>Wrapper: <c>{ "comment": { ... } }</c></summary>
public sealed record SingleCommentResponse(CommentDto Comment);

/// <summary>Wrapper: <c>{ "comments": [...] }</c></summary>
public sealed record MultipleCommentsResponse(IReadOnlyList<CommentDto> Comments);

/// <summary>Comment DTO.</summary>
public sealed record CommentDto(
    Guid Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string Body,
    ProfileDto Author);

// ─── Tags ─────────────────────────────────────────────────────────────────────

/// <summary>Wrapper: <c>{ "tags": [...] }</c></summary>
public sealed record TagsResponse(IReadOnlyList<string> Tags);

// ─── Mapping Extensions ──────────────────────────────────────────────────────

/// <summary>
/// Pure mapping functions from read models to API DTOs.
/// </summary>
public static class DtoMapping
{
    /// <summary>Maps a <see cref="ProfileReadModel"/> to a <see cref="ProfileDto"/>.</summary>
    public static ProfileDto ToProfileDto(this ProfileReadModel profile) =>
        new(
            Username: profile.Username,
            Bio: profile.Bio,
            Image: string.IsNullOrEmpty(profile.Image) ? null : profile.Image,
            Following: profile.Following);

    /// <summary>Maps an <see cref="ArticleQueryResult"/> to a full <see cref="ArticleDto"/>.</summary>
    public static ArticleDto ToArticleDto(this ArticleQueryResult article) =>
        new(
            Slug: article.Slug,
            Title: article.Title,
            Description: article.Description,
            Body: article.Body,
            TagList: article.TagList,
            CreatedAt: article.CreatedAt,
            UpdatedAt: article.UpdatedAt,
            Favorited: article.Favorited,
            FavoritesCount: article.FavoritesCount,
            Author: article.Author.ToProfileDto());

    /// <summary>Maps an <see cref="ArticleQueryResult"/> to an <see cref="ArticleListDto"/> (no body).</summary>
    public static ArticleListDto ToArticleListDto(this ArticleQueryResult article) =>
        new(
            Slug: article.Slug,
            Title: article.Title,
            Description: article.Description,
            TagList: article.TagList,
            CreatedAt: article.CreatedAt,
            UpdatedAt: article.UpdatedAt,
            Favorited: article.Favorited,
            FavoritesCount: article.FavoritesCount,
            Author: article.Author.ToProfileDto());

    /// <summary>Maps a <see cref="CommentQueryResult"/> to a <see cref="CommentDto"/>.</summary>
    public static CommentDto ToCommentDto(this CommentQueryResult comment) =>
        new(
            Id: comment.Id,
            CreatedAt: comment.CreatedAt,
            UpdatedAt: comment.UpdatedAt,
            Body: comment.Body,
            Author: comment.Author.ToProfileDto());
}
