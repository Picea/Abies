// =============================================================================
// Result Type
// =============================================================================
// Represents the outcome of an operation that can succeed or fail.
// Errors are values, not exceptions — making failure explicit in the type system.
//
// Design references:
// - Scott Wlaschin, "Domain Modeling Made Functional" (Railway-Oriented Programming)
// - F# Result<'T, 'TError> type
// - Rust std::result::Result<T, E>
//
// Architecture Decision Records:
// - ADR-002: Pure Functional Programming Style (docs/adr/ADR-002-pure-functional-programming.md)
// - ADR-010: Option Type for Optional Values (docs/adr/ADR-010-option-type.md)
// =============================================================================

namespace Abies;

/// <summary>
/// Represents the outcome of an operation that can either succeed with <typeparamref name="TSuccess"/>
/// or fail with <typeparamref name="TError"/>.
/// </summary>
/// <remarks>
/// <para>
/// Use Result instead of exceptions for expected domain failures. Pattern match on
/// <see cref="Ok{TSuccess, TError}"/> and <see cref="Error{TSuccess, TError}"/> to handle both cases.
/// </para>
/// <para>
/// Supports LINQ query syntax for Railway-Oriented Programming:
/// </para>
/// <example>
/// <code>
/// var result =
///     from user in await login(email, password)
///     from profile in await loadProfile(user.Username)
///     select (user, profile);
/// </code>
/// </example>
/// </remarks>
/// <typeparam name="TSuccess">The type of the success value.</typeparam>
/// <typeparam name="TError">The type of the error value.</typeparam>
public interface Result<out TSuccess, out TError>;

/// <summary>
/// Represents a successful outcome containing a value.
/// </summary>
public readonly record struct Ok<TSuccess, TError>(TSuccess Value) : Result<TSuccess, TError>;

/// <summary>
/// Represents a failed outcome containing an error.
/// </summary>
public readonly record struct Error<TSuccess, TError>(TError Value) : Result<TSuccess, TError>;

/// <summary>
/// Factory methods for creating <see cref="Result{TSuccess, TError}"/> values.
/// </summary>
/// <example>
/// <code>
/// Result&lt;User, AuthError&gt; result = Results.Ok&lt;User, AuthError&gt;(user);
/// Result&lt;User, AuthError&gt; error = Results.Error&lt;User, AuthError&gt;(AuthError.InvalidCredentials);
/// </code>
/// </example>
public static class Results
{
    /// <summary>Creates a successful result.</summary>
    public static Result<TSuccess, TError> Ok<TSuccess, TError>(TSuccess value) =>
        new Ok<TSuccess, TError>(value);

    /// <summary>Creates a failed result.</summary>
    public static Result<TSuccess, TError> Error<TSuccess, TError>(TError error) =>
        new Error<TSuccess, TError>(error);
}
