// =============================================================================
// Validated Type (Applicative Validation)
// =============================================================================
// Represents the outcome of a validation that can accumulate multiple errors.
// Unlike Result<T, E> which short-circuits on the first error (monadic),
// Validated<T> collects ALL errors via the Apply combinator (applicative).
//
// This is essential for form validation: when a user submits a form with
// 5 invalid fields, all 5 errors should be shown at once.
//
// Design references:
// - Radix library (github.com/MCGPPeters/Radix) — original Validated<T> implementation
// - Haskell Data.Validation (applicative validation)
// - Cats Validated (Scala)
// - Scott Wlaschin, "Domain Modeling Made Functional" (constrained types)
//
// Key distinction from Result<T, E>:
//   Result.Bind:     short-circuits on first error  (monad)
//   Validated.Apply: accumulates ALL errors          (applicative functor)
//
// Architecture Decision Records:
// - ADR-002: Pure Functional Programming Style (docs/adr/ADR-002-pure-functional-programming.md)
// =============================================================================

using Abies.Validated;

namespace Abies;

/// <summary>
/// Represents the outcome of a validation that can either succeed with
/// <typeparamref name="T"/> or fail with one or more <see cref="ValidationError"/>s.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="Result{TSuccess, TError}"/> which short-circuits on the first error,
/// <c>Validated</c> accumulates all errors via the <c>Apply</c> combinator.
/// Use <c>Result</c> for dependent validations (where step 2 depends on step 1),
/// and <c>Validated</c> for independent validations (form fields).
/// </para>
/// <example>
/// <code>
/// // Accumulates ALL errors — doesn't stop at the first one:
/// Valid((string name, string email) => new User(name, email))
///     .Apply(Validate.NonEmpty(name, nameof(name)))
///     .Apply(Validate.Email(email, nameof(email)))
/// </code>
/// </example>
/// </remarks>
/// <typeparam name="T">The type of the validated value.</typeparam>
public interface Validated<out T>;

/// <summary>
/// Represents a successful validation containing a value.
/// </summary>
/// <example>
/// <code>
/// Validated&lt;string&gt; name = new Valid&lt;string&gt;("Alice");
/// </code>
/// </example>
public readonly record struct Valid<T>(T Value) : Validated<T>;

/// <summary>
/// Represents a failed validation containing one or more errors.
/// Errors accumulate when composed via <c>Apply</c>.
/// </summary>
/// <example>
/// <code>
/// Validated&lt;string&gt; invalid = new Invalid&lt;string&gt;(
///     new ValidationError("Email", "Email is required"),
///     new ValidationError("Password", "Password is too short"));
/// </code>
/// </example>
public readonly record struct Invalid<T>(params ValidationError[] Errors) : Validated<T>;
