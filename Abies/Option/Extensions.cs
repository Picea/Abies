namespace Abies.Option;

public static class Extensions
{
    public static Option<T> Some<T>(T t) => new Some<T>(t);
    public static Option<T> None<T>() => new None<T>();

    public static Option<T> Where<T>(this Option<T> option, Func<T, bool> predicate) =>
        option switch
        {
            Some<T> (var t) when predicate(t) => option,
            _ => None<T>()
        };

    public static Option<TResult> Select<T, TResult>(this Option<T> option, Func<T, TResult> selector) =>
        option switch
        {
            Some<T>(var t) => Some(selector(t)),
            None<T> => None<TResult>(),
            _ => throw new NotImplementedException()
        };

    public static Option<TResult> SelectMany<T, TResult>(this Option<T> option, Func<T, Option<TResult>> selector) =>
        option switch
        {
            Some<T>(var t) => selector(t),
            None<T> => None<TResult>(),
            _ => throw new NotImplementedException()
        };

    public static Option<TResult> SelectMany<T, TIntermediate, TResult>(this Option<T> option, Func<T, Option<TIntermediate>> selector, Func<T, TIntermediate, TResult> projector) =>
        option switch
        {
            Some<T>(var t) => selector(t).SelectMany(i => Some(projector(t, i))),
            None<T> => None<TResult>(),
            _ => throw new NotImplementedException()
        };
}
