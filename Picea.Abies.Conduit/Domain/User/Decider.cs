// =============================================================================
// User Decider — Pure Domain Logic
// =============================================================================
// Implements the Decider pattern for the User aggregate:
//
//     Command → Decide(state) → Result<Events, Error>
//     Event   → Transition(state) → (State', Effect)
//
// All logic is pure — no IO, no side effects. Password hashing, token
// generation, and uniqueness checks happen at the boundary (capabilities).
//
// The User aggregate manages:
//   - Registration (one-time)
//   - Profile updates (email, username, password, bio, image)
//   - Follow/unfollow relationships
// =============================================================================

using System.Collections.Immutable;
using System.Diagnostics;

namespace Picea.Abies.Conduit.Domain.User;

/// <summary>
/// Effects produced by User transitions.
/// </summary>
public interface UserEffect
{
    /// <summary>No effect.</summary>
    record struct None : UserEffect;
}

/// <summary>
/// The User Decider — validates commands against state and produces events.
/// </summary>
public class User
    : Decider<UserState, UserCommand, UserEvent, UserEffect, UserError, Unit>
{
    /// <summary>
    /// Creates the initial unregistered user state.
    /// </summary>
    public static (UserState State, UserEffect Effect) Initialize(Unit _) =>
        (UserState.Initial, new UserEffect.None());

    /// <summary>
    /// Validates a command against the current user state.
    /// </summary>
    public static Result<UserEvent[], UserError> Decide(
        UserState state, UserCommand command) =>
        command switch
        {
            // ── Registration ─────────────────────────────────────
            UserCommand.Register when state.Registered =>
                Result<UserEvent[], UserError>
                    .Err(new UserError.AlreadyRegistered()),

            UserCommand.Register cmd =>
                Result<UserEvent[], UserError>
                    .Ok([new UserEvent.Registered(
                        cmd.Id, cmd.Email, cmd.Username,
                        cmd.PasswordHash, cmd.CreatedAt)]),

            // ── Guard: must be registered for all other commands ──
            _ when !state.Registered =>
                Result<UserEvent[], UserError>
                    .Err(new UserError.NotRegistered()),

            // ── Profile Update ───────────────────────────────────
            UserCommand.UpdateProfile cmd =>
                Result<UserEvent[], UserError>
                    .Ok([new UserEvent.ProfileUpdated(
                        cmd.Email, cmd.Username, cmd.PasswordHash,
                        cmd.Bio, cmd.Image, cmd.UpdatedAt)]),

            // ── Follow ───────────────────────────────────────────
            UserCommand.Follow(var followeeId) when followeeId == state.Id =>
                Result<UserEvent[], UserError>
                    .Err(new UserError.CannotFollowSelf()),

            UserCommand.Follow(var followeeId) when state.Following.Contains(followeeId) =>
                Result<UserEvent[], UserError>
                    .Err(new UserError.AlreadyFollowing(followeeId)),

            UserCommand.Follow(var followeeId) =>
                Result<UserEvent[], UserError>
                    .Ok([new UserEvent.Followed(followeeId)]),

            // ── Unfollow ─────────────────────────────────────────
            UserCommand.Unfollow(var followeeId) when !state.Following.Contains(followeeId) =>
                Result<UserEvent[], UserError>
                    .Err(new UserError.NotFollowing(followeeId)),

            UserCommand.Unfollow(var followeeId) =>
                Result<UserEvent[], UserError>
                    .Ok([new UserEvent.Unfollowed(followeeId)]),

            _ => throw new UnreachableException()
        };

    /// <summary>
    /// Folds an event into the user state (pure evolution, no validation).
    /// </summary>
    public static (UserState State, UserEffect Effect) Transition(
        UserState state, UserEvent @event) =>
        @event switch
        {
            UserEvent.Registered e =>
                (state with
                {
                    Id = e.Id,
                    Email = e.Email,
                    Username = e.Username,
                    PasswordHash = e.PasswordHash,
                    CreatedAt = e.CreatedAt,
                    UpdatedAt = e.CreatedAt,
                    Registered = true
                }, new UserEffect.None()),

            UserEvent.ProfileUpdated e =>
                (state with
                {
                    Email = e.Email.Match(v => v, () => state.Email),
                    Username = e.Username.Match(v => v, () => state.Username),
                    PasswordHash = e.PasswordHash.Match(v => v, () => state.PasswordHash),
                    Bio = e.Bio.Match(v => v, () => state.Bio),
                    Image = e.Image.Match(v => v, () => state.Image),
                    UpdatedAt = e.UpdatedAt
                }, new UserEffect.None()),

            UserEvent.Followed(var followeeId) =>
                (state with
                {
                    Following = state.Following.ToImmutableHashSet().Add(followeeId)
                }, new UserEffect.None()),

            UserEvent.Unfollowed(var followeeId) =>
                (state with
                {
                    Following = state.Following.ToImmutableHashSet().Remove(followeeId)
                }, new UserEffect.None()),

            _ => throw new UnreachableException()
        };
}
