// =============================================================================
// User State — The Write Model for the User Aggregate
// =============================================================================
// Immutable record representing the current state of a user, built by
// folding UserEvents via the Decider's Transition function.
// =============================================================================

using System.Collections.Immutable;
using Picea.Abies.Conduit.Domain.Shared;

namespace Picea.Abies.Conduit.Domain.User;

/// <summary>
/// The current state of a user aggregate.
/// </summary>
public record UserState(
    UserId Id,
    EmailAddress Email,
    Username Username,
    PasswordHash PasswordHash,
    Bio Bio,
    ImageUrl Image,
    IReadOnlySet<UserId> Following,
    Timestamp CreatedAt,
    Timestamp UpdatedAt,
    bool Registered)
{
    /// <summary>
    /// The initial (unregistered) user state.
    /// </summary>
    public static readonly UserState Initial = new(
        Id: new UserId(Guid.Empty),
        Email: new EmailAddress(string.Empty),
        Username: new Username(string.Empty),
        PasswordHash: new PasswordHash(string.Empty),
        Bio: Bio.Empty,
        Image: ImageUrl.Empty,
        Following: ImmutableHashSet<UserId>.Empty,
        CreatedAt: new Timestamp(DateTimeOffset.MinValue),
        UpdatedAt: new Timestamp(DateTimeOffset.MinValue),
        Registered: false);
}
