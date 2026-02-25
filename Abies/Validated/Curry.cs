// =============================================================================
// Currying Helpers
// =============================================================================
// Converts multi-argument functions into chains of single-argument functions.
// Required for the Apply combinator to work with multi-field constructors.
//
// Example: Func<A, B, C> becomes Func<A, Func<B, C>>
// This enables: Valid(f).Apply(a).Apply(b) where f takes (A, B) → C
//
// First() splits off only the first argument, leaving the rest uncurried.
// This is more efficient than full currying for Apply chains because each
// Apply only needs to extract one argument at a time.
//
// These methods are designed to be used as method group references with .Map():
//   fValidated.Map(Curry.First)
// =============================================================================

namespace Abies.Validated;

/// <summary>
/// Currying helpers for multi-argument function application.
/// Used by <see cref="Extensions.Apply{T1, T2, TResult}"/> and related overloads.
/// </summary>
/// <remarks>
/// All methods are designed to be used as method group references:
/// <code>fValidated.Map(Curry.First)</code>
/// </remarks>
public static class Curry
{
    // -------------------------------------------------------------------------
    // First (splits off the first argument — used by Apply overloads)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Curries a 2-argument function by splitting off the first argument.
    /// <c>(T1, T2) → R</c> becomes <c>T1 → (T2 → R)</c>.
    /// </summary>
    public static Func<T1, Func<T2, TResult>> First<T1, T2, TResult>(
        Func<T1, T2, TResult> f) =>
        t1 => t2 => f(t1, t2);

    /// <summary>
    /// Curries a 3-argument function by splitting off the first argument.
    /// <c>(T1, T2, T3) → R</c> becomes <c>T1 → ((T2, T3) → R)</c>.
    /// </summary>
    public static Func<T1, Func<T2, T3, TResult>> First<T1, T2, T3, TResult>(
        Func<T1, T2, T3, TResult> f) =>
        t1 => (t2, t3) => f(t1, t2, t3);

    /// <summary>
    /// Curries a 4-argument function by splitting off the first argument.
    /// <c>(T1, T2, T3, T4) → R</c> becomes <c>T1 → ((T2, T3, T4) → R)</c>.
    /// </summary>
    public static Func<T1, Func<T2, T3, T4, TResult>> First<T1, T2, T3, T4, TResult>(
        Func<T1, T2, T3, T4, TResult> f) =>
        t1 => (t2, t3, t4) => f(t1, t2, t3, t4);

    /// <summary>
    /// Curries a 5-argument function by splitting off the first argument.
    /// <c>(T1, T2, T3, T4, T5) → R</c> becomes <c>T1 → ((T2, T3, T4, T5) → R)</c>.
    /// </summary>
    public static Func<T1, Func<T2, T3, T4, T5, TResult>> First<T1, T2, T3, T4, T5, TResult>(
        Func<T1, T2, T3, T4, T5, TResult> f) =>
        t1 => (t2, t3, t4, t5) => f(t1, t2, t3, t4, t5);

    // -------------------------------------------------------------------------
    // Full (fully curries all arguments — useful for REPL/exploratory use)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fully curries a 2-argument function.
    /// <c>(T1, T2) → R</c> becomes <c>T1 → (T2 → R)</c>.
    /// </summary>
    public static Func<T1, Func<T2, TResult>> Full<T1, T2, TResult>(
        Func<T1, T2, TResult> f) =>
        t1 => t2 => f(t1, t2);

    /// <summary>
    /// Fully curries a 3-argument function.
    /// <c>(T1, T2, T3) → R</c> becomes <c>T1 → (T2 → (T3 → R))</c>.
    /// </summary>
    public static Func<T1, Func<T2, Func<T3, TResult>>> Full<T1, T2, T3, TResult>(
        Func<T1, T2, T3, TResult> f) =>
        t1 => t2 => t3 => f(t1, t2, t3);
}
