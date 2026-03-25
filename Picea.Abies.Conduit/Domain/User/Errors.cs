// =============================================================================
// User Errors — Domain-Specific Failure Cases
// =============================================================================
// Errors are values, not exceptions. Each case represents a specific
// business rule violation that the Decider can produce.
// =============================================================================

using Picea.Abies.Conduit.Domain.Shared;

namespace Picea.Abies.Conduit.Domain.User;

/// <summary>
/// Errors produced when User command validation fails.
/// </summary>
public interface UserError
{
    /// <summary>A constrained type validation failed (email format, password length, etc.).</summary>
    record Validation(string Message) : UserError;

    /// <summary>A user with this email already exists (prevents duplicate registration).</summary>
    record DuplicateEmail : UserError;

    /// <summary>A user with this username already exists (prevents duplicate registration).</summary>
    record DuplicateUsername : UserError;

    /// <summary>The user has already been registered (duplicate Register command).</summary>
    record AlreadyRegistered : UserError;

    /// <summary>The user has not been registered yet (commands before Register).</summary>
    record NotRegistered : UserError;

    /// <summary>The user is already following the specified user.</summary>
    record AlreadyFollowing(UserId FolloweeId) : UserError;

    /// <summary>The user is not following the specified user.</summary>
    record NotFollowing(UserId FolloweeId) : UserError;

    /// <summary>A user cannot follow themselves.</summary>
    record CannotFollowSelf : UserError;
}
