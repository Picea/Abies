// =============================================================================
// Error Handling — Conduit Spec Error Format
// =============================================================================
// The Conduit spec mandates error responses in this shape:
//   { "errors": { "body": ["error message 1", "error message 2"] } }
//
// HTTP status codes:
//   401 — Unauthorized (missing/invalid token)
//   403 — Forbidden (not the author)
//   404 — Not found
//   422 — Unprocessable Entity (validation / domain errors)
//
// This module provides IResult helpers for consistent error formatting.
// =============================================================================

using Picea.Abies.Conduit.Domain.Article;
using Picea.Abies.Conduit.Domain.User;

namespace Picea.Abies.Conduit.Api;

/// <summary>
/// Conduit spec error response format.
/// </summary>
public sealed record ErrorResponse(ErrorBody Errors);

/// <summary>
/// The error body containing an array of messages.
/// </summary>
public sealed record ErrorBody(string[] Body);

/// <summary>
/// Helper methods for creating consistent error responses.
/// </summary>
public static class ApiErrors
{
    /// <summary>Creates a 422 Unprocessable Entity response with the Conduit error format.</summary>
    public static IResult Validation(params string[] messages) =>
        Results.UnprocessableEntity(new ErrorResponse(new ErrorBody(messages)));

    /// <summary>Creates a 404 Not Found response with the Conduit error format.</summary>
    public static IResult NotFound(string message) =>
        Results.NotFound(new ErrorResponse(new ErrorBody([message])));

    /// <summary>Creates a 403 Forbidden response with the Conduit error format.</summary>
    public static IResult Forbidden(string message) =>
        Results.Json(new ErrorResponse(new ErrorBody([message])), statusCode: 403);

    /// <summary>Creates a 401 Unauthorized response with the Conduit error format.</summary>
    public static IResult Unauthorized(string message = "Unauthorized.") =>
        Results.Json(new ErrorResponse(new ErrorBody([message])), statusCode: 401);

    /// <summary>
    /// Maps a <see cref="UserError"/> to the appropriate HTTP error response.
    /// </summary>
    public static IResult FromUserError(UserError error) => error switch
    {
        UserError.Validation v => Validation(v.Message),
        UserError.AlreadyRegistered => Validation("Email is already registered."),
        UserError.NotRegistered => NotFound("User not found."),
        UserError.AlreadyFollowing => Validation("Already following this user."),
        UserError.NotFollowing => Validation("Not following this user."),
        UserError.CannotFollowSelf => Validation("Cannot follow yourself."),
        _ => Validation("An unexpected error occurred.")
    };

    /// <summary>
    /// Maps an <see cref="ArticleError"/> to the appropriate HTTP error response.
    /// </summary>
    public static IResult FromArticleError(ArticleError error) => error switch
    {
        ArticleError.Validation v => Validation(v.Message),
        ArticleError.AlreadyPublished => Validation("Article with this slug already exists."),
        ArticleError.NotPublished => NotFound("Article not found."),
        ArticleError.AlreadyDeleted => NotFound("Article not found."),
        ArticleError.NotAuthor => Forbidden("You are not the author of this article."),
        ArticleError.AlreadyFavorited => Validation("Already favorited."),
        ArticleError.NotFavorited => Validation("Not favorited."),
        ArticleError.CommentNotFound => NotFound("Comment not found."),
        ArticleError.NotCommentAuthor => Forbidden("You are not the author of this comment."),
        _ => Validation("An unexpected error occurred.")
    };
}
