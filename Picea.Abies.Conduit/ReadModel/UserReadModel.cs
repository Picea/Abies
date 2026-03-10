// =============================================================================
// User Read Model — Denormalized View for Query APIs
// =============================================================================
// Flat records optimized for the Conduit API response shapes.
// These are the CQRS read side — projected from UserEvents.
//
// Read models are annotation-free domain types. Serialization/mapping
// to API DTOs happens at the boundary (API layer).
// =============================================================================

namespace Picea.Abies.Conduit.ReadModel;

/// <summary>
/// Denormalized user read model projected from the User event stream.
/// </summary>
/// <param name="Id">The user's unique identifier.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Username">The user's display name.</param>
/// <param name="PasswordHash">The hashed password (for login verification).</param>
/// <param name="Bio">The user's biography.</param>
/// <param name="Image">The user's avatar URL.</param>
/// <param name="CreatedAt">When the user registered.</param>
/// <param name="UpdatedAt">When the user profile was last updated.</param>
public sealed record UserReadModel(
    Guid Id,
    string Email,
    string Username,
    string PasswordHash,
    string Bio,
    string Image,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// A follow relationship between two users.
/// </summary>
/// <param name="FollowerId">The user who is following.</param>
/// <param name="FolloweeId">The user being followed.</param>
public sealed record FollowReadModel(
    Guid FollowerId,
    Guid FolloweeId);

/// <summary>
/// Profile view — a user as seen by another user (includes following status).
/// </summary>
/// <param name="Username">The profile owner's username.</param>
/// <param name="Bio">The profile owner's biography.</param>
/// <param name="Image">The profile owner's avatar URL.</param>
/// <param name="Following">Whether the requesting user follows this profile.</param>
public sealed record ProfileReadModel(
    string Username,
    string Bio,
    string Image,
    bool Following);
