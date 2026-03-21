// =============================================================================
// Comment Endpoints — CRUD for Article Comments
// =============================================================================
// POST   /api/articles/:slug/comments      — Add comment (auth required)
// GET    /api/articles/:slug/comments      — List comments (public)
// DELETE /api/articles/:slug/comments/:id  — Delete comment (auth required)
// =============================================================================

using Picea;
using Picea.Abies.Conduit.Api.Dto;
using Picea.Abies.Conduit.Api.Infrastructure;
using Picea.Abies.Conduit.Domain.Article;
using Picea.Abies.Conduit.Domain.Shared;
using Picea.Abies.Conduit.ReadModel;

namespace Picea.Abies.Conduit.Api.Endpoints;

/// <summary>
/// Maps the /api/articles/:slug/comments endpoint group.
/// </summary>
public static class CommentEndpoints
{
    /// <summary>Registers the comment endpoints.</summary>
    public static RouteGroupBuilder MapCommentEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/articles/{slug}/comments");

        group.MapPost("/", AddComment)
            .RequireAuthorization()
            .WithName("AddComment");

        group.MapGet("/", GetComments)
            .AllowAnonymous()
            .WithName("GetComments");

        group.MapDelete("/{id:guid}", DeleteComment)
            .RequireAuthorization()
            .WithName("DeleteComment");

        return group;
    }

    /// <summary>
    /// POST /api/articles/:slug/comments — Add a comment to an article.
    /// </summary>
    private static async Task<IResult> AddComment(
        string slug,
        AddCommentRequest request,
        HttpContext context,
        AggregateStore aggregateStore,
        FindArticleIdBySlug findArticleIdBySlug,
        GetComments getComments,
        CancellationToken cancellationToken)
    {
        var currentUserId = JwtTokenService.GetUserId(context.User);
        if (currentUserId is null)
            return ApiErrors.Unauthorized();

        var articleIdOption = await findArticleIdBySlug(slug, cancellationToken)
            .ConfigureAwait(false);
        if (articleIdOption.IsNone)
            return ApiErrors.NotFound("Article not found.");

        var bodyResult = CommentBody.Create(request.Comment.Body);
        if (bodyResult.IsErr)
            return ApiErrors.FromArticleError(bodyResult.Error);

        var commentId = CommentId.New();
        var command = new ArticleCommand.AddComment(
            commentId, new UserId(currentUserId.Value), bodyResult.Value, Timestamp.Now());

        var result = await aggregateStore.HandleArticleCommand(
            articleIdOption.Value, command, cancellationToken).ConfigureAwait(false);

        return await result.Match(
            ok: async _ =>
            {
                // Re-fetch comments to get the hydrated comment with author profile
                var currentUserIdOption = Option<Guid>.Some(currentUserId.Value);
                var comments = await getComments(slug, currentUserIdOption, cancellationToken)
                    .ConfigureAwait(false);

                var created = comments.FirstOrDefault(c => c.Id == commentId.Value);
                return created is not null
                    ? Results.Ok(new SingleCommentResponse(created.ToCommentDto()))
                    : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            },
            err: error => Task.FromResult(ApiErrors.FromArticleError(error)));
    }

    /// <summary>
    /// GET /api/articles/:slug/comments — List all comments for an article.
    /// </summary>
    private static async Task<IResult> GetComments(
        string slug,
        HttpContext context,
        GetComments getComments,
        CancellationToken cancellationToken)
    {
        var currentUserId = JwtTokenService.GetUserId(context.User);
        var currentUserIdOption = currentUserId is { } uid
            ? Option<Guid>.Some(uid)
            : Option<Guid>.None;

        var comments = await getComments(slug, currentUserIdOption, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(new MultipleCommentsResponse(
            comments.Select(c => c.ToCommentDto()).ToList()));
    }

    /// <summary>
    /// DELETE /api/articles/:slug/comments/:id — Delete a comment.
    /// </summary>
    private static async Task<IResult> DeleteComment(
        string slug,
        Guid id,
        HttpContext context,
        AggregateStore aggregateStore,
        FindArticleIdBySlug findArticleIdBySlug,
        CancellationToken cancellationToken)
    {
        var currentUserId = JwtTokenService.GetUserId(context.User);
        if (currentUserId is null)
            return ApiErrors.Unauthorized();

        var articleIdOption = await findArticleIdBySlug(slug, cancellationToken)
            .ConfigureAwait(false);
        if (articleIdOption.IsNone)
            return ApiErrors.NotFound("Article not found.");

        var command = new ArticleCommand.DeleteComment(
            new CommentId(id), new UserId(currentUserId.Value));

        var result = await aggregateStore.HandleArticleCommand(
            articleIdOption.Value, command, cancellationToken).ConfigureAwait(false);

        return result.Match(
            ok: _ => Results.Ok(),
            err: error => ApiErrors.FromArticleError(error));
    }
}
