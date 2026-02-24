// =============================================================================
// ValidationError
// =============================================================================
// A structured validation error for form-field-level display.
// Placed in the Abies.Validated namespace to avoid collision with
// domain-specific ValidationError types (e.g. Abies.Conduit.Capabilities).
// =============================================================================

namespace Abies.Validated;

/// <summary>
/// A structured validation error with a field name and message.
/// Designed for form-field-level error display.
/// </summary>
/// <param name="Field">The name of the field that failed validation.</param>
/// <param name="Message">A human-readable description of the validation failure.</param>
/// <example>
/// <code>
/// var error = new ValidationError("Email", "Must be a valid email address");
/// </code>
/// </example>
public readonly record struct ValidationError(string Field, string Message);
