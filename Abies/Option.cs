// =============================================================================
// Option Type
// =============================================================================
// Represents optional values explicitly, avoiding null reference issues.
//
// Architecture Decision Records:
// - ADR-010: Option Type for Optional Values (docs/adr/ADR-010-option-type.md)
// - ADR-002: Pure Functional Programming Style (docs/adr/ADR-002-pure-functional-programming.md)
// =============================================================================

using System;

namespace Abies;

/// <summary>
/// Represents an optional value that may or may not be present.
/// </summary>
/// <remarks>
/// Use Option instead of null within domain and application code.
/// Pattern match on Some/None to handle both cases explicitly.
/// 
/// See ADR-010: Option Type for Optional Values
/// </remarks>
public interface Option<T>;

/// <summary>
/// Represents a present value.
/// </summary>
public readonly record struct Some<T>(T Value) : Option<T>;

/// <summary>
/// Represents an absent value (no allocation due to struct).
/// </summary>
public readonly struct None<T> : Option<T>;
