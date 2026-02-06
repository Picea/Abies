// =============================================================================
// Parser Combinators
// =============================================================================
// A functional parser combinator library used for URL routing.
// Parsers are composable building blocks that can be combined using
// LINQ syntax (from/select) or fluent methods (Slash, Or, Many).
//
// This approach is inspired by functional parsing in Haskell (Parsec)
// and Elm's URL parser, providing type-safe routing without regex.
//
// Architecture Decision Records:
// - ADR-004: Parser Combinators for Routing (docs/adr/ADR-004-parser-combinators.md)
// - ADR-002: Pure Functional Programming (docs/adr/ADR-002-pure-functional-programming.md)
// =============================================================================

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Abies;


[StructLayout(LayoutKind.Sequential)]
public readonly ref struct ParseResult<T>
{
    public readonly T Value;
    public readonly ReadOnlySpan<char> Remaining;
    public readonly bool Success;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ParseResult(T value, ReadOnlySpan<char> remaining, bool success)
    {
        Value = value;
        Remaining = remaining;
        Success = success;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ParseResult<T> Successful(T value, ReadOnlySpan<char> remaining)
        => new(value, remaining, true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ParseResult<T> Failure()
        => default;

    // implicit conversion from ParseResult<T> to T
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T(ParseResult<T> result) => result.Value;
}


/// <summary>
/// Represents the result of a parsing operation.
/// </summary>
/// <typeparam name="T">The type of the parsed value.</typeparam>

/// <summary>
/// Represents a parser that can parse input and produce a result of type T.
/// </summary>
/// <typeparam name="T">The type of the parsed value.</typeparam>
public partial interface Parser<T>
{
    /// <summary>
    /// Parses the input and returns the parse result.
    /// </summary>
    /// <param name="input">The input span to parse.</param>
    /// <returns>The result of parsing.</returns>
    ParseResult<T> Parse(ReadOnlySpan<char> input);

    /// <summary>
    /// Overloads the | operator to represent an alternative between two parsers.
    /// </summary>
    /// <param name="first">The first parser.</param>
    /// <param name="second">The second parser.</param>
    /// <returns>A new parser representing the alternative.</returns>
    public static Parser<T> operator |(Parser<T> first, Parser<T> second) =>
        first.Or(second);
}

/// <summary>
/// Provides extension methods for parser combinators.
/// </summary>
public static class ParserExtensions
{
    /// <summary>
    /// Represents a parser that always returns a specific value without consuming any input.
    /// </summary>
    /// <typeparam name="T">The type of the parsed value.</typeparam>
    private readonly struct ReturnParser<T> : Parser<T>
    {
        private readonly T _value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReturnParser(T value)
        {
            _value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParseResult<T> Parse(ReadOnlySpan<char> input)
            => ParseResult<T>.Successful(_value, input);
    }

    private readonly struct FailParser<T> : Parser<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParseResult<T> Parse(ReadOnlySpan<char> input)
            => ParseResult<T>.Failure();
    }

    /// <summary>
    /// Represents a parser that consumes multiple occurrences of a parser.
    /// </summary>
    /// <typeparam name="T">The type of the parsed values.</typeparam>
    private readonly struct ManyParser<T> : Parser<List<T>>
    {
        private readonly Parser<T> _parser;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ManyParser(Parser<T> parser)
        {
            _parser = parser;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParseResult<List<T>> Parse(ReadOnlySpan<char> input)
        {
            var results = new List<T>();
            var remaining = input;

            while (true)
            {
                var result = _parser.Parse(remaining);
                if (!result.Success)
                    break;

                results.Add(result.Value);
                remaining = result.Remaining;
            }

            return ParseResult<List<T>>.Successful(results, remaining);
        }
    }

    /// <summary>
    /// Represents a parser that consumes one or more occurrences of a parser.
    /// </summary>
    /// <typeparam name="T">The type of the parsed values.</typeparam>
    private readonly struct Many1Parser<T> : Parser<List<T>>
    {
        private readonly Parser<T> _parser;

        public Many1Parser(Parser<T> parser)
        {
            _parser = parser;
        }

        public ParseResult<List<T>> Parse(ReadOnlySpan<char> input)
        {
            var results = new List<T>();
            var remaining = input;

            // Must parse at least once
            var firstResult = _parser.Parse(remaining);
            if (!firstResult.Success)
                return ParseResult<List<T>>.Failure();

            if (remaining.Length == firstResult.Remaining.Length)
                throw new InvalidOperationException("Parser did not consume any input. This leads to an infinite loop.");

            results.Add(firstResult.Value);
            remaining = firstResult.Remaining;

            // Continue parsing
            while (true)
            {
                var previousLength = remaining.Length;
                var result = _parser.Parse(remaining);
                if (!result.Success)
                    break;

                if (remaining.Length == result.Remaining.Length)
                    break; // Prevent infinite loop

                results.Add(result.Value);
                remaining = result.Remaining;
            }

            return ParseResult<List<T>>.Successful(results, remaining);
        }
    }




    /// <summary>
    /// Represents a parser that attempts to parse input using the first parser, and if it fails, tries the second parser.
    /// </summary>
    /// <typeparam name="T">The type of the parsed value.</typeparam>
    private readonly struct OrParser<T> : Parser<T>
    {
        private readonly Parser<T> _first;
        private readonly Parser<T> _second;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public OrParser(Parser<T> first, Parser<T> second)
        {
            _first = first;
            _second = second;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParseResult<T> Parse(ReadOnlySpan<char> input)
        {
            var firstResult = _first.Parse(input);
            if (firstResult.Success)
                return firstResult;

            return _second.Parse(input);
        }
    }

    /// <summary>
    /// Represents a parser that expects a '/' separator between two parsers.
    /// </summary>
    /// <typeparam name="T">The output type of the first parser.</typeparam>
    /// <typeparam name="U">The output type of the second parser.</typeparam>
    private readonly struct SlashParser<T, U> : Parser<U>
    {
        private readonly Parser<T> _firstParser;
        private readonly Parser<U> _secondParser;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SlashParser(Parser<T> firstParser, Parser<U> secondParser)
        {
            _firstParser = firstParser;
            _secondParser = secondParser;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParseResult<U> Parse(ReadOnlySpan<char> input)
        {
            // Parse the first parser
            var firstResult = _firstParser.Parse(input);
            if (!firstResult.Success)
                return ParseResult<U>.Failure();

            var remaining = firstResult.Remaining;

            // Attempt to consume '/'
            var slashParser = new CharParser('/');
            var slashResult = slashParser.Parse(remaining);
            if (!slashResult.Success)
                return ParseResult<U>.Failure();

            remaining = slashResult.Remaining;

            // Parse the second parser
            var secondResult = _secondParser.Parse(remaining);
            if (!secondResult.Success)
                return ParseResult<U>.Failure();

            return ParseResult<U>.Successful(secondResult.Value, secondResult.Remaining);
        }
    }

    /// <summary>
    /// Represents a parser that maps the result of another parser using a selector function.
    /// </summary>
    /// <typeparam name="T">The input type of the parser.</typeparam>
    /// <typeparam name="U">The output type of the parser.</typeparam>
    private readonly struct SelectParser<T, U> : Parser<U>
    {
        private readonly Parser<T> _parser;
        private readonly Func<T, U> _selector;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SelectParser(Parser<T> parser, Func<T, U> selector)
        {
            _parser = parser;
            _selector = selector;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParseResult<U> Parse(ReadOnlySpan<char> input)
        {
            var result = _parser.Parse(input);
            if (!result.Success)
                return ParseResult<U>.Failure();

            U mappedValue = _selector(result.Value);
            return ParseResult<U>.Successful(mappedValue, result.Remaining);
        }
    }

    public readonly struct WhereParser<T> : Parser<T>
    {
        private readonly Parser<T> _parser;
        private readonly Func<T, bool> _predicate;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WhereParser(Parser<T> parser, Func<T, bool> predicate)
        {
            _parser = parser;
            _predicate = predicate;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParseResult<T> Parse(ReadOnlySpan<char> input)
        {
            var result = _parser.Parse(input);
            if (!result.Success || !_predicate(result.Value))
                return ParseResult<T>.Failure();

            return result;
        }
    }

    /// <summary>
    /// Represents a parser that combines two parsers and projects their results into a new value.
    /// </summary>
    /// <typeparam name="T">The type of the first parser's result.</typeparam>
    /// <typeparam name="U">The type of the second parser's result.</typeparam>
    /// <typeparam name="V">The type of the projected result.</typeparam>
    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    /// <summary>
    /// Represents a parser that combines two parsers and projects their results into a new value.
    /// </summary>
    /// <typeparam name="T">The type of the first parser's result.</typeparam>
    /// <typeparam name="U">The type of the second parser's result.</typeparam>
    /// <typeparam name="V">The type of the projected result.</typeparam>
    private readonly struct SelectManyParser<T, U, V>(Parser<T> parser, Func<T, Parser<U>> binder, Func<T, U, V> projector) : Parser<V>
    {
        private readonly Parser<T> _parser = parser;
        private readonly Func<T, Parser<U>> _binder = binder;
        private readonly Func<T, U, V> _projector = projector;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParseResult<V> Parse(ReadOnlySpan<char> input)
        {
            var result1 = _parser.Parse(input);
            if (!result1.Success)
                return ParseResult<V>.Failure();

            var parser2 = _binder(result1.Value);
            var result2 = parser2.Parse(result1.Remaining);
            if (!result2.Success)
                return ParseResult<V>.Failure();

            V projected = _projector(result1.Value, result2.Value);
            return ParseResult<V>.Successful(projected, result2.Remaining);
        }
    }

    /// <summary>
    /// Represents a parser that consumes a single character from the input.
    /// </summary>
    public readonly struct ItemParser : Parser<char>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParseResult<char> Parse(ReadOnlySpan<char> input)
        {
            if (!input.IsEmpty)
            {
                char firstChar = input[0];
                ReadOnlySpan<char> remaining = input[1..];
                return ParseResult<char>.Successful(firstChar, remaining);
            }
            else
            {
                return ParseResult<char>.Failure();
            }
        }
    }

    /// <summary>
    /// Represents a parser that consumes a single character if it satisfies a given predicate.
    /// </summary>
    public readonly struct SatisfyParser : Parser<char>
    {
        private readonly Func<char, bool> _predicate;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SatisfyParser(Func<char, bool> predicate)
        {
            _predicate = predicate;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParseResult<char> Parse(ReadOnlySpan<char> input)
        {
            if (!input.IsEmpty && _predicate(input[0]))
            {
                return ParseResult<char>.Successful(input[0], input.Slice(1));
            }
            else
            {
                return ParseResult<char>.Failure();
            }
        }
    }

    /// <summary>
    /// Represents a parser that consumes a specific expected character.
    /// </summary>
    public readonly struct CharParser : Parser<char>
    {
        private readonly char _expected;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CharParser(char expected)
        {
            _expected = expected;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParseResult<char> Parse(ReadOnlySpan<char> input)
        {
            if (!input.IsEmpty && input[0] == _expected)
            {
                return ParseResult<char>.Successful(_expected, input.Slice(1));
            }
            else
            {
                return ParseResult<char>.Failure();
            }
        }
    }

    /// <summary>
    /// Represents a parser that parses a specific string.
    /// </summary>
    public readonly struct StringValueParser : Parser<string>
    {
        private readonly string _expected;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringValueParser(string expected)
        {
            _expected = expected;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParseResult<string> Parse(ReadOnlySpan<char> input)
        {
            var expectedSpan = _expected.AsSpan();
            if (input.StartsWith(expectedSpan))
            {
                return ParseResult<string>.Successful(_expected, input.Slice(expectedSpan.Length));
            }
            else
            {
                return ParseResult<string>.Failure();
            }
        }
    }

    public readonly struct IntValueParser : Parser<int>
    {
        private readonly int _expected;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntValueParser(int expected)
        {
            _expected = expected;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParseResult<int> Parse(ReadOnlySpan<char> input)
        {
            var expectedSpan = _expected.ToString().AsSpan();
            if (input.StartsWith(expectedSpan))
            {
                return ParseResult<int>.Successful(_expected, input.Slice(expectedSpan.Length));
            }
            else
            {
                return ParseResult<int>.Failure();
            }
        }
    }


    /// <summary>
    /// Represents a parser that parses an integer from the input.
    /// </summary>
    public readonly struct IntParser : Parser<int>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParseResult<int> Parse(ReadOnlySpan<char> input)
        {
            int i = 0;
            while (i < input.Length && char.IsDigit(input[i]))
                i++;

            if (i == 0)
                return ParseResult<int>.Failure();

            int value = int.Parse(input[..i]);
            return ParseResult<int>.Successful(value, input[i..]);
        }
    }

        
    

    public readonly struct OptionalParser<T> : Parser<T?> 
    {
        private readonly Parser<T> _parser;

        public OptionalParser(Parser<T> parser)
        {
            _parser = parser;
        }

        public ParseResult<T?> Parse(ReadOnlySpan<char> input)
        {
            var result = _parser.Parse(input);
            if (result.Success)
            {
                return ParseResult<T?>.Successful(result.Value, result.Remaining);
            }
            else
            {
                // Parsing failed, but we return null and do not consume input
                return ParseResult<T?>.Successful(default, input);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parser<T?> Optional<T>(this Parser<T> parser)
    {
        return new OptionalParser<T>(parser);
    }

    /// <summary>
    /// Combines two parsers with an alternative (logical OR).
    /// </summary>
    /// <typeparam name="T">The output type of the parsers.</typeparam>
    /// <param name="first">The first parser.</param>
    /// <param name="second">The second parser.</param>
    /// <returns>A new parser representing the alternative between the two parsers.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parser<T> Or<T>(this Parser<T> first, Parser<T> second)
    {
        return new OrParser<T>(first, second);
    }

    /// <summary>
    /// Creates a parser that consumes zero or more occurrences of the given parser.
    /// </summary>
    /// <typeparam name="T">The type of the parsed values.</typeparam>
    /// <param name="parser">The parser to apply repeatedly.</param>
    /// <returns>A parser that returns a list of parsed values.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parser<List<T>> Many<T>(this Parser<T> parser)
        => new ManyParser<T>(parser);

    /// <summary>
    /// Creates a parser that consumes one or more occurrences of the given parser.
    /// </summary>
    /// <typeparam name="T">The type of the parsed values.</typeparam>
    /// <param name="parser">The parser to apply repeatedly.</param>
    /// <returns>A parser that returns a list of parsed values.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parser<List<T>> Many1<T>(this Parser<T> parser)
        => new Many1Parser<T>(parser);


    /// <summary>
    /// Combines two parsers with a '/' separator between them.
    /// </summary>
    /// <typeparam name="T">The output type of the first parser.</typeparam>
    /// <typeparam name="U">The output type of the second parser.</typeparam>
    /// <param name="first">The first parser.</param>
    /// <param name="second">The second parser.</param>
    /// <returns>A new parser that combines the two parsers with a '/' separator.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parser<U> Slash<T, U>(this Parser<T> first, Parser<U> second)
    {
        return new SlashParser<T, U>(first, second);
    }

    /// <summary>
    /// Creates a parser that always returns the specified value without consuming any input.
    /// </summary>
    /// <typeparam name="T">The type of the parsed value.</typeparam>
    /// <param name="value">The value to return.</param>
    /// <returns>A parser that always returns the specified value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parser<T> Return<T>(T value)
    {
        return new ReturnParser<T>(value);
    }

    public static Parser<T> Fail<T>()
    {
        return new FailParser<T>();
    }

    /// <summary>
    /// Creates a parser that consumes a single character from the input.
    /// </summary>
    /// <returns>A parser that consumes a single character.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parser<char> Item()
        => new ItemParser();


    /// <summary>
    /// Creates a parser that consumes a single character if it satisfies the given predicate.
    /// </summary>
    /// <param name="predicate">The predicate to test the character.</param>
    /// <returns>A parser that consumes a single character if it satisfies the predicate.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parser<char> Satisfy(Func<char, bool> predicate)
        => new SatisfyParser(predicate);

    /// <summary>
    /// Creates a parser that consumes the specific expected character.
    /// </summary>
    /// <param name="expected">The character to consume.</param>
    /// <returns>A parser that consumes the specific expected character.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parser<char> Char(char expected)
        => new CharParser(expected);

    /// <summary>
    /// Creates a parser that parses a specific string.
    /// </summary>
    /// <param name="expected">The string to parse.</param>
    /// <returns>A parser that parses the specific string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parser<string> String(string expected)
        => new StringValueParser(expected);

    /// <summary>
    /// Creates a parser that parses a specific string.
    /// </summary>
    /// <param name="expected">The string to parse.</param>
    /// <returns>A parser that parses the specific string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parser<string> S(string expected)
        => String(expected);

    /// <summary>
    /// Creates a parser that parses an integer from the input.
    /// </summary>
    /// <returns>A parser that parses an integer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parser<int> Int()
        => new IntParser();

    /// <summary>
    /// Creates a parser that parses a specific integer from the input.
    /// </summary>
    /// <returns>A parser that parses an integer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parser<int> Int(int i)
        => new IntValueParser(i);

    /// <summary>
    /// Enables LINQ's Select functionality for parser combinators.
    /// </summary>
    /// <typeparam name="T">The input type of the parser.</typeparam>
    /// <typeparam name="U">The output type after selection.</typeparam>
    /// <param name="parser">The parser to project.</param>
    /// <param name="selector">The selector function.</param>
    /// <returns>A new parser with the selected result.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parser<U> Select<T, U>(this Parser<T> parser, Func<T, U> selector)
        => new SelectParser<T, U>(parser, selector);

    // support for the where linq operator
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parser<T> Where<T>(this Parser<T> parser, Func<T, bool> predicate)
        => new WhereParser<T>(parser, predicate);

    /// <summary>
    /// Alias for the Select method.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="U"></typeparam>
    /// <param name="parser"></param>
    /// <param name="selector"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parser<U> Map<T, U>(this Parser<T> parser, Func<T, U> selector)
        => parser.Select(selector);

    /// <summary>
    /// Enables LINQ's SelectMany functionality for parser combinators.
    /// </summary>
    /// <typeparam name="T">The type of the first parser's result.</typeparam>
    /// <typeparam name="U">The type of the second parser's result.</typeparam>
    /// <typeparam name="V">The type of the projected result.</typeparam>
    /// <param name="parser">The first parser.</param>
    /// <param name="binder">The binder function that returns the second parser.</param>
    /// <param name="projector">The projector function that combines the results.</param>
    /// <returns>A new parser with the projected result.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parser<V> SelectMany<T, U, V>(
        this Parser<T> parser,
        Func<T, Parser<U>> binder,
        Func<T, U, V> projector)
        => new SelectManyParser<T, U, V>(parser, binder, projector);
}

/// <summary>
/// Provides high-level parser factory methods.
/// </summary>
public static class Parse
{
    /// <summary>
    /// Creates a parser that always returns the specified value without consuming any input.
    /// </summary>
    /// <typeparam name="T">The type of the parsed value.</typeparam>
    /// <param name="value">The value to return.</param>
    /// <returns>A parser that always returns the specified value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parser<T> Return<T>(this T value)
        => ParserExtensions.Return(value);

    /// <summary>
    /// Creates a parser that consumes a single character from the input.
    /// </summary>
    /// <returns>A parser that consumes a single character.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parser<char> Item()
        => ParserExtensions.Item();



    /// <summary>
    /// Creates a parser that consumes a single character if it satisfies the given predicate.
    /// </summary>
    /// <param name="predicate">The predicate to test the character.</param>
    /// <returns>A parser that consumes a single character if it satisfies the predicate.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parser<char> Satisfy(Func<char, bool> predicate)
        => ParserExtensions.Satisfy(predicate);

    /// <summary>
    /// Creates a parser that consumes the specific expected character.
    /// </summary>
    /// <param name="expected">The character to consume.</param>
    /// <returns>A parser that consumes the specific expected character.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parser<char> Char(char expected)
        => ParserExtensions.Char(expected);

    /// <summary>
    /// Creates a parser that parses a specific string.
    /// </summary>
    /// <param name="expected">The string to parse.</param>
    /// <returns>A parser that parses the specific string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parser<string> S(string expected)
        => ParserExtensions.S(expected);

    /// <summary>
    /// Creates a parser that parses a specific string.
    /// </summary>
    /// <param name="expected">The string to parse.</param>
    /// <returns>A parser that parses the specific string.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parser<string> String(string expected)
        => ParserExtensions.S(expected);

    /// <summary>
    /// Creates a parser that consumes zero or more occurrences of the given parser.
    /// </summary>
    /// <typeparam name="T">The type of the parsed values.</typeparam>
    /// <param name="parser">The parser to apply repeatedly.</param>
    /// <returns>A parser that returns a list of parsed values.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parser<List<T>> Many<T>(Parser<T> parser)
        => ParserExtensions.Many(parser);

    /// <summary>
    /// Creates a parser that consumes one or more occurrences of the given parser.
    /// </summary>
    /// <typeparam name="T">The type of the parsed values.</typeparam>
    /// <param name="parser">The parser to apply repeatedly.</param>
    /// <returns>A parser that returns a list of parsed values.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parser<List<T>> Many1<T>( Parser<T> parser)
        => ParserExtensions.Many1(parser);

    /// <summary>
    /// Creates a parser that parses an integer from the input.
    /// </summary>
    /// <returns>A parser that parses an integer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parser<int> Int()
        => ParserExtensions.Int();

    /// <summary>
    /// Creates a parser that parses a specific integer from the input.
    /// </summary>
    /// <returns>A parser that parses an integer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Parser<int> Int(int i)
        => ParserExtensions.Int(i);
}

