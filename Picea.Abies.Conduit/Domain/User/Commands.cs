// =============================================================================
// User Commands — Intent Representations
// =============================================================================
// Commands are what users want to do. The Decider validates them against
// the current state and produces events (or rejects them with errors).
//
// Commands carry pre-validated constrained types where possible (e.g.,
// EmailAddress instead of raw string). Validation of the raw input into
// constrained types happens at the API boundary (anti-corruption layer).
// =============================================================================

using Picea.Abies.Conduit.Domain.Shared;

namespace Picea.Abies.Conduit.Domain.User;

/// <summary>
/// Commands representing user intent for the User aggregate.
/// </summary>
public interface UserCommand
{
    /// <summary>
    /// Register a new user account.
    /// </summary>
    record Register(
        UserId Id,
        EmailAddress Email,
        Username Username,
        PasswordHash PasswordHash,
        Timestamp CreatedAt) : UserCommand;

    /// <summary>
    /// Update the user's profile information.
    /// </summary>
    record UpdateProfile(
        Option<EmailAddress> Email,
        Option<Username> Username,
        Option<PasswordHash> PasswordHash,
        Option<Bio> Bio,
        Option<ImageUrl> Image,
        Timestamp UpdatedAt) : UserCommand;

    /// <summary>
    /// Follow another user.
    /// </summary>
    record Follow(UserId FolloweeId) : UserCommand;

    /// <summary>
    /// Unfollow a previously followed user.
    /// </summary>
    record Unfollow(UserId FolloweeId) : UserCommand;
}
