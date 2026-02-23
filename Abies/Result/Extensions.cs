// =============================================================================
// Result Extensions
// =============================================================================
// Combinators for composing Result values using Railway-Oriented Programming.
// Supports LINQ query syntax for ergonomic chaining.
// =============================================================================

using System.Diagnostics;

namespace Abies.Result;

/// <summary>
/// Combinators and LINQ support for <see cref="Result{TSuccess, TError}"/>.
/// </summary>
public static class Extensions
{
    // -------------------------------------------------------------------------
    // Factory helpers
    // -------------------------------------------------------------------------

    /// <summary>Creates a successful result.</summary>
    public static Result<TSuccess, TError> Ok<TSuccess, TError>(TSuccess value) =>
        new Ok<TSuccess, TError>(value);

    /// <summary>Creates a failed result.</summary>
    public static Result<TSuccess, TError> Error<TSuccess, TError>(TError error) =>
        new Error<TSuccess, TError>(error);

    // -------------------------------------------------------------------------
    // Map (functor)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Transforms the success value, leaving errors untouched.
    /// </summary>
    public static Result<TResult, TError> Map<TSuccess, TError, TResult>(
        this Result<TSuccess, TError> result,
        Func<TSuccess, TResult> mapper) =>
        result switch
        {
            Ok<TSuccess, TError>(var value) => new Ok<TResult, TError>(mapper(value)),
            Error<TSuccess, TError>(var err) => new Error<TResult, TError>(err),
            _ => throw new UnreachableException()
        };

    /// <summary>
    /// Transforms the error value, leaving successes untouched.
    /// </summary>
    public static Result<TSuccess, TResult> MapError<TSuccess, TError, TResult>(
        this Result<TSuccess, TError> result,
        Func<TError, TResult> mapper) =>
        result switch
        {
            Ok<TSuccess, TError>(var value) => new Ok<TSuccess, TResult>(value),
            Error<TSuccess, TError>(var err) => new Error<TSuccess, TResult>(mapper(err)),
            _ => throw new UnreachableException()
        };

    // -------------------------------------------------------------------------
    // Bind (monad)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Chains an operation that may fail, short-circuiting on errors.
    /// </summary>
    /// <example>
    /// <code>
    /// Result&lt;Profile, Error&gt; result = loginResult.Bind(user => LoadProfile(user.Id));
    /// </code>
    /// </example>
    public static Result<TResult, TError> Bind<TSuccess, TError, TResult>(
        this Result<TSuccess, TError> result,
        Func<TSuccess, Result<TResult, TError>> binder) =>
        result switch
        {
            Ok<TSuccess, TError>(var value) => binder(value),
            Error<TSuccess, TError>(var err) => new Error<TResult, TError>(err),
            _ => throw new UnreachableException()
        };

    // -------------------------------------------------------------------------
    // LINQ query syntax (select / from)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Enables <c>select</c> clause in LINQ query syntax.
    /// </summary>
    public static Result<TResult, TError> Select<TSuccess, TError, TResult>(
        this Result<TSuccess, TError> result,
        Func<TSuccess, TResult> selector) =>
        result.Map(selector);

    /// <summary>
    /// Enables <c>from ... from ...</c> (flatMap) in LINQ query syntax.
    /// </summary>
    public static Result<TResult, TError> SelectMany<TSuccess, TError, TResult>(
        this Result<TSuccess, TError> result,
        Func<TSuccess, Result<TResult, TError>> selector) =>
        result.Bind(selector);

    /// <summary>
    /// Enables <c>from x in ... from y in ... select f(x, y)</c> in LINQ query syntax.
    /// </summary>
    public static Result<TResult, TError> SelectMany<TSuccess, TError, TIntermediate, TResult>(
        this Result<TSuccess, TError> result,
        Func<TSuccess, Result<TIntermediate, TError>> selector,
        Func<TSuccess, TIntermediate, TResult> projector) =>
        result.Bind(value => selector(value).Map(intermediate => projector(value, intermediate)));

    // -------------------------------------------------------------------------
    // Async combinators
    // -------------------------------------------------------------------------

    /// <summary>
    /// Asynchronous Map — transforms the success value of a Task-wrapped result.
    /// </summary>
    public static async Task<Result<TResult, TError>> Map<TSuccess, TError, TResult>(
        this Task<Result<TSuccess, TError>> resultTask,
        Func<TSuccess, TResult> mapper) =>
        (await resultTask).Map(mapper);

    /// <summary>
    /// Asynchronous Bind — chains an async operation that may fail.
    /// </summary>
    public static async Task<Result<TResult, TError>> Bind<TSuccess, TError, TResult>(
        this Task<Result<TSuccess, TError>> resultTask,
        Func<TSuccess, Task<Result<TResult, TError>>> binder)
    {
        var result = await resultTask;
        if (result is Ok<TSuccess, TError>(var value))
        {
            return await binder(value);
        }
        if (result is Error<TSuccess, TError>(var err))
        {
            return new Error<TResult, TError>(err);
        }
        throw new UnreachableException();
    }

    // -------------------------------------------------------------------------
    // Conversion
    // -------------------------------------------------------------------------

    /// <summary>
    /// Converts a nullable value to a Result, using the provided error for null values.
    /// </summary>
    public static Result<TSuccess, TError> ToResult<TSuccess, TError>(
        this TSuccess? value,
        TError error) where TSuccess : class =>
        value is not null
            ? new Ok<TSuccess, TError>(value)
            : new Error<TSuccess, TError>(error);

    /// <summary>
    /// Converts a nullable value type to a Result, using the provided error for null values.
    /// </summary>
    public static Result<TSuccess, TError> ToResult<TSuccess, TError>(
        this TSuccess? value,
        TError error) where TSuccess : struct =>
        value.HasValue
            ? new Ok<TSuccess, TError>(value.Value)
            : new Error<TSuccess, TError>(error);
}
