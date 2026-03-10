// =============================================================================
// Article Decider — Pure Domain Logic
// =============================================================================
// Implements the Decider pattern for the Article aggregate:
//
//     Command → Decide(state) → Result<Events, Error>
//     Event   → Transition(state) → (State', Effect)
//
// All logic is pure — no IO, no side effects. Slug generation, uniqueness
// checks, and authorization happen here using pre-validated inputs.
//
// The Article aggregate manages:
//   - Creation (one-time per aggregate)
//   - Content updates (title, description, body — author only)
//   - Deletion (author only, terminal state)
//   - Comments (add by anyone, delete by comment author only)
//   - Favorites (toggle per user)
// =============================================================================

using System.Collections.Immutable;
using System.Diagnostics;
using Picea.Abies.Conduit.Domain.Shared;
using Picea;

namespace Picea.Abies.Conduit.Domain.Article;

/// <summary>
/// Effects produced by Article transitions.
/// </summary>
/// <remarks>
/// The Article aggregate currently produces no effects — all side effects
/// (notifications, search indexing) are handled at the application layer.
/// This type exists for Decider interface compliance and future extensibility.
/// </remarks>
public interface ArticleEffect
{
    /// <summary>No effect.</summary>
    record struct None : ArticleEffect;
}

/// <summary>
/// The Article Decider — validates commands against state and produces events.
/// </summary>
/// <remarks>
/// <para>
/// Follows the Decider pattern (Chassaing 2021):
/// <list type="number">
///   <item><c>Initialize</c> — creates an unpublished article state</item>
///   <item><c>Decide</c> — validates commands, produces events or rejects with errors</item>
///   <item><c>Transition</c> — folds events into state (pure evolution)</item>
///   <item><c>IsTerminal</c> — marks deleted articles as terminal</item>
/// </list>
/// </para>
/// <para>
/// Business rules enforced:
/// <list type="bullet">
///   <item>CreateArticle is only accepted once (guard: <c>!Published</c>)</item>
///   <item>All other commands require the article to be published and not deleted</item>
///   <item>Only the author can update or delete the article</item>
///   <item>Only the comment author can delete a comment</item>
///   <item>Cannot favorite an already-favorited article</item>
///   <item>Cannot unfavorite a not-favorited article</item>
///   <item>Slug is regenerated on title change</item>
/// </list>
/// </para>
/// </remarks>
public class Article
    : Decider<ArticleState, ArticleCommand, ArticleEvent, ArticleEffect, ArticleError, Unit>
{
    /// <summary>
    /// Creates the initial unpublished article state.
    /// </summary>
    public static (ArticleState State, ArticleEffect Effect) Initialize(Unit _) =>
        (ArticleState.Initial, new ArticleEffect.None());

    /// <summary>
    /// Returns true when the article has been deleted (terminal state — no more commands accepted).
    /// </summary>
    public static bool IsTerminal(ArticleState state) => state.Deleted;

    /// <summary>
    /// Validates a command against the current article state.
    /// </summary>
    public static Result<ArticleEvent[], ArticleError> Decide(
        ArticleState state, ArticleCommand command) =>
        command switch
        {
            // ── Creation ─────────────────────────────────────────
            ArticleCommand.CreateArticle when state.Published =>
                Result<ArticleEvent[], ArticleError>
                    .Err(new ArticleError.AlreadyPublished()),

            ArticleCommand.CreateArticle cmd =>
                Result<ArticleEvent[], ArticleError>
                    .Ok([new ArticleEvent.ArticleCreated(
                        cmd.Id, Slug.FromTitle(cmd.Title), cmd.Title,
                        cmd.Description, cmd.Body, cmd.Tags,
                        cmd.AuthorId, cmd.CreatedAt)]),

            // ── Guard: must be published and not deleted ─────────
            _ when !state.Published =>
                Result<ArticleEvent[], ArticleError>
                    .Err(new ArticleError.NotPublished()),

            _ when state.Deleted =>
                Result<ArticleEvent[], ArticleError>
                    .Err(new ArticleError.AlreadyDeleted()),

            // ── Update (author only) ─────────────────────────────
            ArticleCommand.UpdateArticle cmd when cmd.RequesterId != state.AuthorId =>
                Result<ArticleEvent[], ArticleError>
                    .Err(new ArticleError.NotAuthor(cmd.RequesterId)),

            ArticleCommand.UpdateArticle cmd =>
                Result<ArticleEvent[], ArticleError>
                    .Ok([new ArticleEvent.ArticleUpdated(
                        cmd.Title, cmd.Description, cmd.Body,
                        cmd.Title.Match(t => Slug.FromTitle(t), () => state.Slug),
                        cmd.UpdatedAt)]),

            // ── Delete (author only) ─────────────────────────────
            ArticleCommand.DeleteArticle cmd when cmd.RequesterId != state.AuthorId =>
                Result<ArticleEvent[], ArticleError>
                    .Err(new ArticleError.NotAuthor(cmd.RequesterId)),

            ArticleCommand.DeleteArticle =>
                Result<ArticleEvent[], ArticleError>
                    .Ok([new ArticleEvent.ArticleDeleted()]),

            // ── Add Comment ──────────────────────────────────────
            ArticleCommand.AddComment cmd =>
                Result<ArticleEvent[], ArticleError>
                    .Ok([new ArticleEvent.CommentAdded(
                        cmd.CommentId, cmd.AuthorId, cmd.Body, cmd.CreatedAt)]),

            // ── Delete Comment (comment author only) ─────────────
            ArticleCommand.DeleteComment cmd when !state.Comments.ContainsKey(cmd.CommentId) =>
                Result<ArticleEvent[], ArticleError>
                    .Err(new ArticleError.CommentNotFound(cmd.CommentId)),

            ArticleCommand.DeleteComment cmd
                when state.Comments[cmd.CommentId].AuthorId != cmd.RequesterId =>
                Result<ArticleEvent[], ArticleError>
                    .Err(new ArticleError.NotCommentAuthor(cmd.CommentId, cmd.RequesterId)),

            ArticleCommand.DeleteComment cmd =>
                Result<ArticleEvent[], ArticleError>
                    .Ok([new ArticleEvent.CommentDeleted(cmd.CommentId)]),

            // ── Favorite ─────────────────────────────────────────
            ArticleCommand.FavoriteArticle cmd when state.FavoritedBy.Contains(cmd.UserId) =>
                Result<ArticleEvent[], ArticleError>
                    .Err(new ArticleError.AlreadyFavorited(cmd.UserId)),

            ArticleCommand.FavoriteArticle cmd =>
                Result<ArticleEvent[], ArticleError>
                    .Ok([new ArticleEvent.ArticleFavorited(cmd.UserId)]),

            // ── Unfavorite ───────────────────────────────────────
            ArticleCommand.UnfavoriteArticle cmd when !state.FavoritedBy.Contains(cmd.UserId) =>
                Result<ArticleEvent[], ArticleError>
                    .Err(new ArticleError.NotFavorited(cmd.UserId)),

            ArticleCommand.UnfavoriteArticle cmd =>
                Result<ArticleEvent[], ArticleError>
                    .Ok([new ArticleEvent.ArticleUnfavorited(cmd.UserId)]),

            _ => throw new UnreachableException()
        };

    /// <summary>
    /// Folds an event into the article state (pure evolution, no validation).
    /// </summary>
    public static (ArticleState State, ArticleEffect Effect) Transition(
        ArticleState state, ArticleEvent @event) =>
        @event switch
        {
            ArticleEvent.ArticleCreated e =>
                (state with
                {
                    Id = e.Id,
                    Slug = e.Slug,
                    Title = e.Title,
                    Description = e.Description,
                    Body = e.Body,
                    Tags = e.Tags,
                    AuthorId = e.AuthorId,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.CreatedAt,
                    Published = true
                }, new ArticleEffect.None()),

            ArticleEvent.ArticleUpdated e =>
                (state with
                {
                    Title = e.Title.Match(v => v, () => state.Title),
                    Description = e.Description.Match(v => v, () => state.Description),
                    Body = e.Body.Match(v => v, () => state.Body),
                    Slug = e.Slug,
                    UpdatedAt = e.UpdatedAt
                }, new ArticleEffect.None()),

            ArticleEvent.ArticleDeleted =>
                (state with { Deleted = true }, new ArticleEffect.None()),

            ArticleEvent.CommentAdded e =>
                (state with
                {
                    Comments = state.Comments.ToImmutableDictionary()
                        .Add(e.CommentId, new Comment(e.CommentId, e.AuthorId, e.Body, e.CreatedAt))
                }, new ArticleEffect.None()),

            ArticleEvent.CommentDeleted e =>
                (state with
                {
                    Comments = state.Comments.ToImmutableDictionary()
                        .Remove(e.CommentId)
                }, new ArticleEffect.None()),

            ArticleEvent.ArticleFavorited e =>
                (state with
                {
                    FavoritedBy = state.FavoritedBy.ToImmutableHashSet().Add(e.UserId)
                }, new ArticleEffect.None()),

            ArticleEvent.ArticleUnfavorited e =>
                (state with
                {
                    FavoritedBy = state.FavoritedBy.ToImmutableHashSet().Remove(e.UserId)
                }, new ArticleEffect.None()),

            _ => throw new UnreachableException()
        };
}
