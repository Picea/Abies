// =============================================================================
// Validated Extensions
// =============================================================================
// Combinators for composing Validated values.
// Supports both applicative (Apply — error accumulation) and monadic
// (Bind — short-circuit) composition, plus LINQ query syntax.
//
// The Apply combinator is the key differentiator: it runs ALL validations
// and accumulates ALL errors, unlike Bind which stops at the first failure.
// =============================================================================

using System.Diagnostics;

namespace Abies.Validated;

/// <summary>
/// Combinators and LINQ support for <see cref="Validated{T}"/>.
/// </summary>
public static class Extensions
{
    // -------------------------------------------------------------------------
    // Factory helpers
    // -------------------------------------------------------------------------

    /// <summary>Creates a successful validation.</summary>
    public static Validated<T> Valid<T>(T value) =>
        new Valid<T>(value);

    /// <summary>Creates a failed validation with structured errors.</summary>
    public static Validated<T> Invalid<T>(params ValidationError[] errors) =>
        new Invalid<T>(errors);

    /// <summary>Creates a failed validation from a field name and message.</summary>
    public static Validated<T> Invalid<T>(string field, string message) =>
        new Invalid<T>(new ValidationError(field, message));

    // -------------------------------------------------------------------------
    // Map (functor)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Transforms the success value, leaving errors untouched.
    /// </summary>
    /// <example>
    /// <code>
    /// Validated&lt;int&gt; length = Valid("hello").Map(s => s.Length); // Valid(5)
    /// </code>
    /// </example>
    public static Validated<TResult> Map<T, TResult>(
        this Validated<T> validated,
        Func<T, TResult> mapper) =>
        validated switch
        {
            Valid<T>(var value) => new Valid<TResult>(mapper(value)),
            Invalid<T>(var errors) => new Invalid<TResult>(errors),
            _ => throw new UnreachableException()
        };

    // -------------------------------------------------------------------------
    // Bind (monad — short-circuits on first error)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Chains a validation that depends on the previous result.
    /// Short-circuits on the first error (monadic behavior).
    /// Use <see cref="Apply{T, TResult}"/> for independent validations that
    /// should accumulate errors.
    /// </summary>
    /// <example>
    /// <code>
    /// // Dependent validation: can only check password strength if parsing succeeded
    /// Validated&lt;StrongPassword&gt; result = parsePassword(input).Bind(CheckStrength);
    /// </code>
    /// </example>
    public static Validated<TResult> Bind<T, TResult>(
        this Validated<T> validated,
        Func<T, Validated<TResult>> binder) =>
        validated switch
        {
            Valid<T>(var value) => binder(value),
            Invalid<T>(var errors) => new Invalid<TResult>(errors),
            _ => throw new UnreachableException()
        };

    // -------------------------------------------------------------------------
    // Apply (applicative — accumulates ALL errors)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Applies a validated function to a validated argument, accumulating errors
    /// from both sides. This is the core applicative combinator.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="Bind{T, TResult}"/> which stops at the first error,
    /// <c>Apply</c> collects errors from both the function and argument.
    /// Use with curried constructors to validate multiple fields:
    /// <code>
    /// Valid((string name, string email) => new User(name, email))
    ///     .Apply(validateName(input.Name))
    ///     .Apply(validateEmail(input.Email))
    /// // If both fail, BOTH errors are in the result.
    /// </code>
    /// </remarks>
    public static Validated<TResult> Apply<T, TResult>(
        this Validated<Func<T, TResult>> fValidated,
        Validated<T> xValidated) =>
        (fValidated, xValidated) switch
        {
            (Valid<Func<T, TResult>>(var f), Valid<T>(var x)) =>
                new Valid<TResult>(f(x)),

            (Invalid<Func<T, TResult>>(var fErrors), Valid<T>) =>
                new Invalid<TResult>(fErrors),

            (Valid<Func<T, TResult>>, Invalid<T>(var xErrors)) =>
                new Invalid<TResult>(xErrors),

            (Invalid<Func<T, TResult>>(var fErrors), Invalid<T>(var xErrors)) =>
                new Invalid<TResult>([.. fErrors, .. xErrors]),

            _ => throw new UnreachableException()
        };

    // -------------------------------------------------------------------------
    // Multi-argument Apply overloads (curried)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Applies a 2-argument validated function to the first argument,
    /// returning a validated 1-argument function for the second.
    /// </summary>
    public static Validated<Func<T2, TResult>> Apply<T1, T2, TResult>(
        this Validated<Func<T1, T2, TResult>> fValidated,
        Validated<T1> arg) =>
        Apply(fValidated.Map(Curry.First), arg);

    /// <summary>
    /// Applies a 3-argument validated function to the first argument.
    /// </summary>
    public static Validated<Func<T2, T3, TResult>> Apply<T1, T2, T3, TResult>(
        this Validated<Func<T1, T2, T3, TResult>> fValidated,
        Validated<T1> arg) =>
        Apply(fValidated.Map(Curry.First), arg);

    /// <summary>
    /// Applies a 4-argument validated function to the first argument.
    /// </summary>
    public static Validated<Func<T2, T3, T4, TResult>> Apply<T1, T2, T3, T4, TResult>(
        this Validated<Func<T1, T2, T3, T4, TResult>> fValidated,
        Validated<T1> arg) =>
        Apply(fValidated.Map(Curry.First), arg);

    /// <summary>
    /// Applies a 5-argument validated function to the first argument.
    /// </summary>
    public static Validated<Func<T2, T3, T4, T5, TResult>> Apply<T1, T2, T3, T4, T5, TResult>(
        this Validated<Func<T1, T2, T3, T4, T5, TResult>> fValidated,
        Validated<T1> arg) =>
        Apply(fValidated.Map(Curry.First), arg);

    // -------------------------------------------------------------------------
    // LINQ query syntax (select / from)
    // -------------------------------------------------------------------------

    /// <summary>Enables <c>select</c> clause in LINQ query syntax.</summary>
    public static Validated<TResult> Select<T, TResult>(
        this Validated<T> validated,
        Func<T, TResult> selector) =>
        validated.Map(selector);

    /// <summary>Enables <c>from ... from ...</c> (flatMap) in LINQ query syntax.</summary>
    /// <remarks>
    /// Note: LINQ syntax uses Bind semantics (short-circuit), not Apply.
    /// For error accumulation, use the Apply combinator directly.
    /// </remarks>
    public static Validated<TResult> SelectMany<T, TResult>(
        this Validated<T> validated,
        Func<T, Validated<TResult>> selector) =>
        validated.Bind(selector);

    /// <summary>Enables <c>from x in ... from y in ... select f(x, y)</c> in LINQ query syntax.</summary>
    public static Validated<TResult> SelectMany<T, TIntermediate, TResult>(
        this Validated<T> validated,
        Func<T, Validated<TIntermediate>> selector,
        Func<T, TIntermediate, TResult> projector) =>
        validated.Bind(value => selector(value).Map(intermediate => projector(value, intermediate)));

    // -------------------------------------------------------------------------
    // Match (exhaustive pattern matching with side effects)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Matches on the validated value, invoking the appropriate handler.
    /// </summary>
    /// <example>
    /// <code>
    /// validated.Match(
    ///     valid: user => Console.WriteLine($"Welcome {user.Name}"),
    ///     invalid: errors => errors.ForEach(e => Console.WriteLine($"{e.Field}: {e.Message}"))
    /// );
    /// </code>
    /// </example>
    public static TResult Match<T, TResult>(
        this Validated<T> validated,
        Func<T, TResult> valid,
        Func<ValidationError[], TResult> invalid) =>
        validated switch
        {
            Valid<T>(var value) => valid(value),
            Invalid<T>(var errors) => invalid(errors),
            _ => throw new UnreachableException()
        };

    // -------------------------------------------------------------------------
    // Traverse (validate a collection of items)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Applies a validation function to each element and accumulates all errors.
    /// If all elements are valid, returns a <c>Valid</c> containing all results.
    /// If any are invalid, returns an <c>Invalid</c> with all accumulated errors.
    /// </summary>
    /// <example>
    /// <code>
    /// var emails = new[] { "a@b.com", "invalid", "c@d.com" };
    /// Validated&lt;IEnumerable&lt;string&gt;&gt; result = emails.Traverse(Validate.Email);
    /// // Invalid with error for "invalid"
    /// </code>
    /// </example>
    public static Validated<IEnumerable<TResult>> Traverse<T, TResult>(
        this IEnumerable<T> values,
        Func<T, Validated<TResult>> validator) =>
        values.Aggregate(
            Valid(Enumerable.Empty<TResult>()),
            (acc, item) =>
                Valid<Func<IEnumerable<TResult>, TResult, IEnumerable<TResult>>>(
                    (list, x) => list.Append(x))
                .Apply(acc)
                .Apply(validator(item)));

    /// <summary>
    /// Validates all items in a collection using a validation function,
    /// returning the validated items if all pass.
    /// </summary>
    public static Validated<IEnumerable<T>> Sequence<T>(
        this IEnumerable<Validated<T>> validatedItems) =>
        validatedItems.Aggregate(
            Valid(Enumerable.Empty<T>()),
            (acc, item) =>
                Valid<Func<IEnumerable<T>, T, IEnumerable<T>>>(
                    (list, x) => list.Append(x))
                .Apply(acc)
                .Apply(item));

    // -------------------------------------------------------------------------
    // Filtering
    // -------------------------------------------------------------------------

    /// <summary>Extracts all valid values from a collection of validated items.</summary>
    public static IEnumerable<T> WhereValid<T>(this IEnumerable<Validated<T>> items)
    {
        foreach (var item in items)
        {
            if (item is Valid<T>(var value))
            {
                yield return value;
            }
        }
    }

    /// <summary>Extracts all errors from a collection of validated items.</summary>
    public static IEnumerable<ValidationError[]> WhereInvalid<T>(this IEnumerable<Validated<T>> items)
    {
        foreach (var item in items)
        {
            if (item is Invalid<T>(var errors))
            {
                yield return errors;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Composition (validate a single value with multiple validators)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Runs multiple validators against a value and accumulates all errors.
    /// </summary>
    /// <example>
    /// <code>
    /// Validated&lt;string&gt; result = email.Validate(
    ///     e => Validate.NonEmpty(e, "email"),
    ///     e => Validate.MaxLength(e, 255, "email"),
    ///     e => Validate.Email(e, "email"));
    /// </code>
    /// </example>
    public static Validated<T> Validate<T>(
        this T value,
        params Func<T, Validated<T>>[] validators) =>
        validators
            .Select(v => v(value))
            .Aggregate(
                Valid(value),
                (acc, result) =>
                    (acc, result) switch
                    {
                        (Valid<T>, Valid<T>) => acc,
                        (Valid<T>, Invalid<T>(var errors)) => new Invalid<T>(errors),
                        (Invalid<T>(var existing), Valid<T>) => new Invalid<T>(existing),
                        (Invalid<T>(var existing), Invalid<T>(var errors)) =>
                            new Invalid<T>([.. existing, .. errors]),
                        _ => throw new UnreachableException()
                    });

    // -------------------------------------------------------------------------
    // Conversion
    // -------------------------------------------------------------------------

    /// <summary>
    /// Converts a <see cref="Validated{T}"/> to a <see cref="Result{TSuccess, TError}"/>
    /// using the specified error mapper for the failure case.
    /// </summary>
    public static Result<T, TError> ToResult<T, TError>(
        this Validated<T> validated,
        Func<ValidationError[], TError> errorMapper) =>
        validated switch
        {
            Valid<T>(var value) => new Ok<T, TError>(value),
            Invalid<T>(var errors) => new Error<T, TError>(errorMapper(errors)),
            _ => throw new UnreachableException()
        };

    /// <summary>
    /// Converts a <see cref="Result{TSuccess, TError}"/> to a <see cref="Validated{T}"/>
    /// using the specified error mapper to create validation errors.
    /// </summary>
    public static Validated<T> ToValidated<T, TError>(
        this Result<T, TError> result,
        Func<TError, ValidationError[]> errorMapper) =>
        result switch
        {
            Ok<T, TError>(var value) => new Valid<T>(value),
            Error<T, TError>(var error) => new Invalid<T>(errorMapper(error)),
            _ => throw new UnreachableException()
        };

    // -------------------------------------------------------------------------
    // Async combinators
    // -------------------------------------------------------------------------

    /// <summary>Asynchronous Map — transforms the success value of a Task-wrapped validated.</summary>
    public static async Task<Validated<TResult>> Map<T, TResult>(
        this Task<Validated<T>> validatedTask,
        Func<T, TResult> mapper) =>
        (await validatedTask).Map(mapper);

    /// <summary>Asynchronous Bind — chains an async validation operation.</summary>
    public static async Task<Validated<TResult>> Bind<T, TResult>(
        this Task<Validated<T>> validatedTask,
        Func<T, Task<Validated<TResult>>> binder)
    {
        var validated = await validatedTask;
        return validated switch
        {
            Valid<T>(var value) => await binder(value),
            Invalid<T>(var errors) => new Invalid<TResult>(errors),
            _ => throw new UnreachableException()
        };
    }

    /// <summary>
    /// Asynchronous Traverse — applies an async validation to each element.
    /// </summary>
    public static async Task<Validated<TResult>> Traverse<T, TResult>(
        this Validated<T> validated,
        Func<T, Task<TResult>> f) =>
        validated switch
        {
            Valid<T>(var value) => new Valid<TResult>(await f(value)),
            Invalid<T>(var errors) => new Invalid<TResult>(errors),
            _ => throw new UnreachableException()
        };
}
