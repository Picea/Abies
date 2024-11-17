using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Abies;

[StructLayout(LayoutKind.Sequential)]
public readonly ref struct ParseResult<T>
    {
        public readonly T Value { get; }
        public readonly ReadOnlySpan<char> Remaining { get; }
        public readonly bool Success { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ParseResult(T value, ReadOnlySpan<char> remaining)
        {
            Value = value;
            Remaining = remaining;
            Success = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParseResult<T> Successful(T value, ReadOnlySpan<char> remaining)
            => new(value, remaining);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ParseResult<T> Failure()
            => default;
    }

    public delegate ParseResult<T> Parser<T>(ReadOnlySpan<char> input);

    public static class Parse
    {
        public static Parser<char> Item() =>
            static input =>
                !input.IsEmpty
                    ? ParseResult<char>.Successful(input[0], input.Slice(1))
                    : ParseResult<char>.Failure();


        public static Parser<char> Satisfy(Func<char, bool> predicate) =>
            input =>
                !input.IsEmpty && predicate(input[0])
                    ? ParseResult<char>.Successful(input[0], input.Slice(1))
                    : ParseResult<char>.Failure();

        public static Parser<char> Char(char expected) =>
            input =>
                !input.IsEmpty && input[0] == expected
                    ? ParseResult<char>.Successful(expected, input.Slice(1))
                    : ParseResult<char>.Failure();

        public static Parser<char> Digit =>
            Satisfy(char.IsDigit);

        public static Parser<char> Letter =>
            Satisfy(char.IsLetter);


        public static Parser<T> Return<T>(T value) =>
            input =>
                ParseResult<T>.Successful(value, input);

        public static Parser<T> Or<T>(this Parser<T> first, Parser<T> second) =>
            input =>
            {
                var result = first(input);
                return result.Success
                    ? result
                    : second(input);
            };

        public static Parser<TResult> Bind<TSource, TResult>(
        this Parser<TSource> parser,
        Func<TSource, Parser<TResult>> binder) =>
            input =>
            {
                var result = parser(input);
                return !result.Success
                    ? ParseResult<TResult>.Failure()
                    : binder(result.Value)(result.Remaining);
            };

        public static Parser<U> SelectMany<T, U>(this Parser<T> parser, Func<T, Parser<U>> f) =>
            Bind(parser, f);

        public static Parser<V> SelectMany<T, U, V>(this Parser<T> parser, Func<T, Parser<U>> f, Func<T, U, V> project) =>
            Bind(parser, x => Bind(f(x), y => Return(project(x, y))));


        public static Parser<U> Map<T, U>(this Parser<T> parser, Func<T, U> f) =>
            Bind(parser, x => Return(f(x)));

        public static Parser<U> Select<T, U>(this Parser<T> parser, Func<T, U> f) =>
            Map(parser, f);

        /// <summary>
        /// Parses the given string.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static Parser<string> String(string expected) =>
            input =>
            {
                var expectedSpan = expected.AsSpan();
                return input.StartsWith(expectedSpan)
                    ? ParseResult<string>.Successful(expected, input.Slice(expectedSpan.Length))
                    : ParseResult<string>.Failure();
            };

        public static Parser<List<T>> Many<T>(this Parser<T> parser)
        {
            return input =>
            {
                var results = new List<T>();
                var remainder = input;
                while (true)
                {
                    var result = parser(remainder);
                    if (!result.Success)
                        break;
                    results.Add(result.Value);
                    remainder = result.Remaining;
                }
                return ParseResult<List<T>>.Successful(results, remainder);
            };
        }

        public static Parser<List<T>> Many1<T>(this Parser<T> parser)
        {
            return parser.Bind(first =>
                parser.Many().Select(rest =>
                {
                    rest.Insert(0, first);
                    return rest;
                })
            );
        }


        /// <summary>
        /// Parses an integer.
        /// </summary>
        public static Parser<int> Integer =>
            from digits in Many1(Digit)
            select int.Parse(new string([.. digits]));

        public static Parser<object> EndOfInput = static input =>
            input.IsEmpty
            ? ParseResult<object>.Successful(new(), input)
            : ParseResult<object>.Failure();
    }
