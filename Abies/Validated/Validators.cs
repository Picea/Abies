// =============================================================================
// Built-in Validators
// =============================================================================
// Common validation functions for form fields. Each validator returns a
// Validated<T> that is either Valid (passes) or Invalid with a structured
// ValidationError containing the field name and a descriptive message.
//
// These are designed to be composed with the Validate extension method:
//
//   email.Validate(
//       e => Validate.NonEmpty(e, "email"),
//       e => Validate.MaxLength(e, 255, "email"),
//       e => Validate.Email(e, "email"));
//
// Or used directly with Apply for multi-field validation:
//
//   Valid((string name, string email) => new User(name, email))
//       .Apply(Validate.NonEmpty(name, "name"))
//       .Apply(Validate.Email(email, "email"))
// =============================================================================

using System.Text.RegularExpressions;

namespace Abies.Validated;

/// <summary>
/// Built-in validation functions for common form-field constraints.
/// </summary>
public static partial class Validate
{
    // -------------------------------------------------------------------------
    // String validators
    // -------------------------------------------------------------------------

    /// <summary>
    /// Validates that a string is not null or empty.
    /// </summary>
    public static Validated<string> Required(string? value, string field) =>
        !string.IsNullOrEmpty(value)
            ? new Valid<string>(value)
            : new Invalid<string>(new ValidationError(field, $"{field} is required"));

    /// <summary>
    /// Validates that a string is not null, empty, or whitespace-only.
    /// </summary>
    public static Validated<string> NonEmpty(string? value, string field) =>
        !string.IsNullOrWhiteSpace(value)
            ? new Valid<string>(value)
            : new Invalid<string>(new ValidationError(field, $"{field} must not be empty"));

    /// <summary>
    /// Validates that a string has at least <paramref name="min"/> characters.
    /// </summary>
    public static Validated<string> MinLength(string value, int min, string field) =>
        value.Length >= min
            ? new Valid<string>(value)
            : new Invalid<string>(new ValidationError(field, $"{field} must be at least {min} characters"));

    /// <summary>
    /// Validates that a string has at most <paramref name="max"/> characters.
    /// </summary>
    public static Validated<string> MaxLength(string value, int max, string field) =>
        value.Length <= max
            ? new Valid<string>(value)
            : new Invalid<string>(new ValidationError(field, $"{field} must be at most {max} characters"));

    /// <summary>
    /// Validates that a string matches a valid email format.
    /// </summary>
    public static Validated<string> Email(string value, string field) =>
        EmailRegex().IsMatch(value)
            ? new Valid<string>(value)
            : new Invalid<string>(new ValidationError(field, $"{field} must be a valid email address"));

    /// <summary>
    /// Validates that a string matches the specified regular expression pattern.
    /// </summary>
    public static Validated<string> Pattern(string value, Regex pattern, string field, string? message = null) =>
        pattern.IsMatch(value)
            ? new Valid<string>(value)
            : new Invalid<string>(new ValidationError(field, message ?? $"{field} has an invalid format"));

    // -------------------------------------------------------------------------
    // Numeric validators
    // -------------------------------------------------------------------------

    /// <summary>
    /// Validates that a value falls within the specified inclusive range.
    /// </summary>
    public static Validated<T> Range<T>(T value, T min, T max, string field)
        where T : IComparable<T> =>
        value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0
            ? new Valid<T>(value)
            : new Invalid<T>(new ValidationError(field, $"{field} must be between {min} and {max}"));

    /// <summary>
    /// Validates that a value is greater than or equal to the specified minimum.
    /// </summary>
    public static Validated<T> Min<T>(T value, T min, string field)
        where T : IComparable<T> =>
        value.CompareTo(min) >= 0
            ? new Valid<T>(value)
            : new Invalid<T>(new ValidationError(field, $"{field} must be at least {min}"));

    /// <summary>
    /// Validates that a value is less than or equal to the specified maximum.
    /// </summary>
    public static Validated<T> Max<T>(T value, T max, string field)
        where T : IComparable<T> =>
        value.CompareTo(max) <= 0
            ? new Valid<T>(value)
            : new Invalid<T>(new ValidationError(field, $"{field} must be at most {max}"));

    // -------------------------------------------------------------------------
    // Generic validators
    // -------------------------------------------------------------------------

    /// <summary>
    /// Validates a value against a custom predicate.
    /// </summary>
    /// <example>
    /// <code>
    /// Validate.That(age, a => a >= 18, "age", "Must be 18 or older")
    /// </code>
    /// </example>
    public static Validated<T> That<T>(T value, Func<T, bool> predicate, string field, string message) =>
        predicate(value)
            ? new Valid<T>(value)
            : new Invalid<T>(new ValidationError(field, message));

    /// <summary>
    /// Validates that a value is not null.
    /// </summary>
    public static Validated<T> NotNull<T>(T? value, string field) where T : class =>
        value is not null
            ? new Valid<T>(value)
            : new Invalid<T>(new ValidationError(field, $"{field} is required"));

    /// <summary>
    /// Validates that a nullable value type has a value.
    /// </summary>
    public static Validated<T> NotNull<T>(T? value, string field) where T : struct =>
        value.HasValue
            ? new Valid<T>(value.Value)
            : new Invalid<T>(new ValidationError(field, $"{field} is required"));

    /// <summary>
    /// Validates that a value is equal to the expected value.
    /// </summary>
    public static Validated<T> EqualTo<T>(T value, T expected, string field, string? message = null)
        where T : IEquatable<T> =>
        value.Equals(expected)
            ? new Valid<T>(value)
            : new Invalid<T>(new ValidationError(field, message ?? $"{field} must equal {expected}"));

    // -------------------------------------------------------------------------
    // Combinators
    // -------------------------------------------------------------------------

    /// <summary>
    /// Runs all validations and accumulates errors.
    /// Returns the value from the first validation if all succeed.
    /// </summary>
    /// <example>
    /// <code>
    /// Validate.All(
    ///     Validate.NonEmpty(name, "name"),
    ///     Validate.MinLength(name, 2, "name"),
    ///     Validate.MaxLength(name, 100, "name"))
    /// </code>
    /// </example>
    public static Validated<T> All<T>(params Validated<T>[] validations) =>
        validations.Aggregate(
            (acc, next) =>
                (acc, next) switch
                {
                    (Valid<T>, Valid<T>) => acc,
                    (Valid<T>, Invalid<T>) => next,
                    (Invalid<T>, Valid<T>) => acc,
                    (Invalid<T>(var existing), Invalid<T>(var errors)) =>
                        new Invalid<T>([.. existing, .. errors]),
                    _ => throw new System.Diagnostics.UnreachableException()
                });

    // -------------------------------------------------------------------------
    // Source-generated email regex
    // -------------------------------------------------------------------------

    [GeneratedRegex(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        matchTimeoutMilliseconds: 250)]
    private static partial Regex EmailRegex();
}
