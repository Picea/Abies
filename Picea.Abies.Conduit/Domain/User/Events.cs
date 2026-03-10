// =============================================================================
// User Events — Facts That Happened
// =============================================================================
// Events are immutable records representing validated facts in the past tense.
// They are the source of truth — state is derived by folding events.
// =============================================================================

using Picea.Abies.Conduit.Domain.Shared;
using Picea;

namespace Picea.Abies.Conduit.Domain.User;

/// <summary>
/// Events that have occurred in the User aggregate.
/// </summary>
public interface UserEvent
{
    /// <summary>A new user registered.</summary>
    record Registered(
        UserId Id,
        EmailAddress Email,
        Username Username,
        PasswordHash PasswordHash,
        Timestamp CreatedAt) : UserEvent;

    /// <summary>The user's profile was updated.</summary>
    record ProfileUpdated(
        Option<EmailAddress> Email,
        Option<Username> Username,
        Option<PasswordHash> PasswordHash,
        Option<Bio> Bio,
        Option<ImageUrl> Image,
        Timestamp UpdatedAt) : UserEvent;

    /// <summary>The user started following another user.</summary>
    record Followed(UserId FolloweeId) : UserEvent;

    /// <summary>The user stopped following another user.</summary>
    record Unfollowed(UserId FolloweeId) : UserEvent;
}
