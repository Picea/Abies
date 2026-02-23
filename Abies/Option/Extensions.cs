// =============================================================================
// Option Extensions
// =============================================================================
// Combinators for composing Option values. Supports LINQ query syntax.
// =============================================================================

using System.Diagnostics;

namespace Abies.Option;

/// <summary>
/// Combinators and LINQ support for <see cref="Option{T}"/>.
/// </summary>
public static class Extensions
{
    // -------------------------------------------------------------------------
    // Factory helpers
    // -------------------------------------------------------------------------

    /// <summary>Creates an Option containing a value.</summary>
    public static Option<T> Some<T>(T t) => new Some<T>(t);

    /// <summary>Creates an empty Option.</summary>
    public static Option<T> None<T>() => new None<T>();

    // -------------------------------------------------------------------------
    // Combinators
    // -------------------------------------------------------------------------

    /// <summary>Filters an option by a predicate.</summary>
    public static Option<T> Where<T>(this Option<T> option, Func<T, bool> predicate) =>
        option switch
        {
            Some<T>(var t) when predicate(t) => option,
            _ => None<T>()
        };

    /// <summary>Returns the contained value or a default.</summary>
    public static T DefaultValue<T>(this Option<T> option, T defaultValue) =>
        option switch
        {
            Some<T>(var t) => t,
            _ => defaultValue
        };

    /// <summary>Returns the contained value or lazily computes a default.</summary>
    public static T DefaultWith<T>(this Option<T> option, Func<T> defaultFactory) =>
        option switch
        {
            Some<T>(var t) => t,
            _ => defaultFactory()
        };

    // -------------------------------------------------------------------------
    // Conversion
    // -------------------------------------------------------------------------

    /// <summary>
    /// Converts an Option to a Result, using the provided error for None.
    /// </summary>
    /// <example>
    /// <code>
    /// Result&lt;User, string&gt; result = maybeUser.ToResult("User not found");
    /// </code>
    /// </example>
    public static Result<T, TError> ToResult<T, TError>(this Option<T> option, TError error) =>
        option switch
        {
            Some<T>(var t) => new Ok<T, TError>(t),
            _ => new Error<T, TError>(error)
        };

    // -------------------------------------------------------------------------
    // LINQ query syntax (select / from)
    // -------------------------------------------------------------------------

    /// <summary>Enables <c>select</c> clause in LINQ query syntax.</summary>
    public static Option<TResult> Select<T, TResult>(this Option<T> option, Func<T, TResult> selector) =>
        option switch
        {
            Some<T>(var t) => Some(selector(t)),
            None<T> => None<TResult>(),
            _ => throw new UnreachableException()
        };

    /// <summary>Enables <c>from ... from ...</c> (flatMap) in LINQ query syntax.</summary>
    public static Option<TResult> SelectMany<T, TResult>(this Option<T> option, Func<T, Option<TResult>> selector) =>
        option switch
        {
            Some<T>(var t) => selector(t),
            None<T> => None<TResult>(),
            _ => throw new UnreachableException()
        };

    /// <summary>Enables <c>from x in ... from y in ... select f(x, y)</c> in LINQ query syntax.</summary>
    public static Option<TResult> SelectMany<T, TIntermediate, TResult>(
        this Option<T> option,
        Func<T, Option<TIntermediate>> selector,
        Func<T, TIntermediate, TResult> projector) =>
        option switch
        {
            Some<T>(var t) => selector(t).SelectMany(i => Some(projector(t, i))),
            None<T> => None<TResult>(),
            _ => throw new UnreachableException()
        };

    // -------------------------------------------------------------------------
    // Async combinators
    // -------------------------------------------------------------------------

    /// <summary>Asynchronous Map — transforms the value of a Task-wrapped option.</summary>
    public static async Task<Option<TResult>> Select<T, TResult>(
        this Task<Option<T>> optionTask,
        Func<T, TResult> selector) =>
        (await optionTask).Select(selector);
}
