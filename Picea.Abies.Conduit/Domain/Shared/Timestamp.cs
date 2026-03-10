// =============================================================================
// Timestamp — Domain-Level Time Representation
// =============================================================================
// Wraps DateTimeOffset to express "when something happened" in the domain.
// All domain events carry timestamps for ordering and audit purposes.
// =============================================================================

namespace Picea.Abies.Conduit.Domain.Shared;

/// <summary>
/// A point in time, used for domain event timestamps, creation/update times,
/// and temporal ordering.
/// </summary>
/// <param name="Value">The underlying <see cref="DateTimeOffset"/>.</param>
public readonly record struct Timestamp(DateTimeOffset Value)
{
    /// <summary>
    /// Creates a <see cref="Timestamp"/> representing the current UTC time.
    /// </summary>
    /// <remarks>
    /// In domain logic, prefer injecting time as a capability
    /// (<c>Func&lt;Timestamp&gt;</c>) rather than calling this directly,
    /// to keep domain functions pure and testable.
    /// </remarks>
    public static Timestamp Now() => new(DateTimeOffset.UtcNow);

    /// <inheritdoc />
    public override string ToString() => Value.ToString("O");
}
