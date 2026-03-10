// =============================================================================
// UserId — Unique User Identity
// =============================================================================
// A constrained type wrapping Guid to prevent accidental misuse of raw Guids
// as user identifiers. Used by both User and Article aggregates.
// =============================================================================

namespace Picea.Abies.Conduit.Domain.Shared;

/// <summary>
/// A unique identifier for a user. Wraps <see cref="Guid"/> to prevent
/// primitive obsession and accidental parameter swapping.
/// </summary>
/// <param name="Value">The underlying unique identifier.</param>
public readonly record struct UserId(Guid Value)
{
    /// <summary>
    /// Creates a new unique <see cref="UserId"/>.
    /// </summary>
    public static UserId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public override string ToString() => Value.ToString();
}
