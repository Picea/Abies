// =============================================================================
// Routing Utilities
// =============================================================================
// High-level routing built on top of the parser combinator library.
// Provides both functional-style route parsing and ASP.NET-style template
// routing (e.g., "/article/{slug}" or "/profile/{id:int}").
//
// Architecture Decision Records:
// - ADR-004: Parser Combinators for Routing (docs/adr/ADR-004-parser-combinators.md)
// - ADR-009: Sum Types for State Representation (docs/adr/ADR-009-sum-types.md)
// =============================================================================

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Linq;

namespace Abies;

/// <summary>
/// Provides routing utilities built on top of the parser combinator library.
/// </summary>
/// <remarks>
/// Routes are defined as parser combinators that can be composed together.
/// This approach provides type-safety and composability without regex.
/// 
/// See ADR-004: Parser Combinators for Routing
/// </remarks>
public static class Route
{
    /// <summary>
    /// Represents a successful route match, exposing captured values by name.
    /// </summary>
    public readonly struct RouteMatch
    {
        private static readonly IReadOnlyDictionary<string, object?> EmptyDictionary =
            new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>(0));

        private readonly Dictionary<string, object?>? _values;

        internal RouteMatch(Dictionary<string, object?>? values)
        {
            _values = values;
        }

        /// <summary>
        /// Gets an empty match (used when the route contains no captured values).
        /// </summary>
        public static RouteMatch Empty => new(null);

        /// <summary>
        /// Gets a read-only view of the captured values.
        /// </summary>
        public IReadOnlyDictionary<string, object?> Values => _values ?? EmptyDictionary;

        /// <summary>
        /// Attempts to retrieve a typed value from the match result.
        /// </summary>
        public bool TryGetValue<T>(string name, [MaybeNullWhen(false)] out T value)
        {
            if (_values is { } dictionary && dictionary.TryGetValue(name, out var raw) && raw is T typed)
            {
                value = typed;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Retrieves a typed value from the match result or throws when it is missing.
        /// </summary>
        public T GetRequired<T>(string name)
        {
            if (TryGetValue<T>(name, out var value))
            {
                return value;
            }

            throw new KeyNotFoundException($"Route value '{name}' was not present or was not of type {typeof(T).Name}.");
        }

        /// <summary>
        /// Indexer access to captured values (returns <c>null</c> when missing).
        /// </summary>
        public object? this[string name] =>
            _values is { } dictionary && dictionary.TryGetValue(name, out var value) ? value : null;

        internal bool HasValues => _values is { Count: > 0 };
    }

    /// <summary>
    /// Contains combinators for building functional route parsers.
    /// </summary>
    public static class Parse
    {
        /// <summary>
        /// Represents a compiled path segment (literal or parameter).
        /// </summary>
        public readonly struct PathSegment
        {
            internal PathSegment(Parser<object?> parser, string? name, bool capture, bool optional)
            {
                Parser = parser;
                Name = name;
                Capture = capture;
                Optional = optional;
            }

            internal Parser<object?> Parser { get; }
            internal string? Name { get; }
            internal bool Capture { get; }
            internal bool Optional { get; }
        }

        /// <summary>
        /// Builds a parser from the provided path segments.
        /// </summary>
        public static Parser<RouteMatch> Path(params PathSegment[] segments)
            => new PathParser(segments);

        /// <summary>
        /// Matches the root path (<c>/</c> or empty).
        /// </summary>
        public static Parser<RouteMatch> Root => new PathParser([]);

        /// <summary>
        /// Provides helpers for creating path segments.
        /// </summary>
        public static class Segment
        {
            /// <summary>
            /// Creates a literal segment that must match exactly.
            /// </summary>
            public static PathSegment Literal(string value, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
                => new(new LiteralSegmentParser(value, comparison), null, capture: false, optional: false);

            /// <summary>
            /// Creates a string parameter segment.
            /// </summary>
            public static PathSegment Parameter(string name, bool optional = false)
                => Parameter(name, Route.Parse.String, optional);

            /// <summary>
            /// Creates a typed parameter segment backed by a custom parser.
            /// </summary>
            public static PathSegment Parameter<T>(string name, Parser<T> parser, bool optional = false, Func<T, object?>? projection = null)
            {
                Func<T, object?> projector = projection ?? (value => (object?)value);
                return new PathSegment(new ParameterSegmentParser<T>(parser, projector), name, capture: true, optional: optional);
            }
        }

        /// <summary>
        /// Parses a segment containing arbitrary non-slash characters (at least one).
        /// </summary>
        public static Parser<string> String => new StringSegmentParser();

        /// <summary>
        /// Parses a numeric segment containing digits (at least one).
        /// </summary>
        public static Parser<int> Int => new IntSegmentParser();

        /// <summary>
        /// Parses a floating point segment.
        /// </summary>
        public static Parser<double> Double => new DoubleSegmentParser();

        public static class Strict
        {
            /// <summary>
            /// Parses an integer segment and returns <c>null</c> when the conversion fails.
            /// </summary>
            public static Parser<int?> Int => new StrictIntSegmentParser();

            /// <summary>
            /// Parses a floating point segment and returns <c>null</c> when the conversion fails.
            /// </summary>
            public static Parser<double?> Double => new StrictDoubleSegmentParser();
        }

        private sealed class PathParser : Parser<RouteMatch>
        {
            private readonly PathSegment[] _segments;

            public PathParser(PathSegment[] segments)
            {
                _segments = segments;
            }

            public ParseResult<RouteMatch> Parse(ReadOnlySpan<char> input)
            {
                if (_segments.Length == 0)
                {
                    if (input.IsEmpty || (input.Length == 1 && input[0] == '/'))
                    {
                        return ParseResult<RouteMatch>.Successful(RouteMatch.Empty, ReadOnlySpan<char>.Empty);
                    }

                    return ParseResult<RouteMatch>.Failure();
                }

                var remaining = input;
                Dictionary<string, object?>? values = null;

                foreach (var segment in _segments)
                {
                    var segmentResult = segment.Parser.Parse(remaining);

                    if (!segmentResult.Success)
                    {
                        if (segment.Optional)
                        {
                            continue;
                        }

                        return ParseResult<RouteMatch>.Failure();
                    }

                    if (segment.Capture && segment.Name is { Length: > 0 })
                    {
                        values ??= new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                        values[segment.Name] = segmentResult.Value;
                    }

                    remaining = segmentResult.Remaining;
                }

                remaining = TrimTrailingSlash(remaining);

                if (!remaining.IsEmpty)
                {
                    return ParseResult<RouteMatch>.Failure();
                }

                return ParseResult<RouteMatch>.Successful(
                    values is null ? RouteMatch.Empty : new RouteMatch(values),
                    remaining);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadOnlySpan<char> TrimTrailingSlash(ReadOnlySpan<char> span)
        {
            if (span.Length == 1 && span[0] == '/')
            {
                return ReadOnlySpan<char>.Empty;
            }

            return span;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryReadSegment(ReadOnlySpan<char> input, bool allowEmpty, out ReadOnlySpan<char> segment, out ReadOnlySpan<char> remaining)
        {
            var index = 0;

            while (index < input.Length)
            {
                var ch = input[index];
                if (ch == '/')
                {
                    break;
                }

                if (char.IsControl(ch))
                {
                    segment = default;
                    remaining = default;
                    return false;
                }

                index++;
            }

            if (index == 0 && !allowEmpty)
            {
                segment = default;
                remaining = default;
                return false;
            }

            segment = input[..index];
            remaining = input[index..];
            return true;
        }

        private sealed class LiteralSegmentParser : Parser<object?>
        {
            private readonly string _segment;
            private readonly StringComparison _comparison;

            public LiteralSegmentParser(string segment, StringComparison comparison)
            {
                _segment = segment ?? throw new ArgumentNullException(nameof(segment));
                _comparison = comparison;
            }

            public ParseResult<object?> Parse(ReadOnlySpan<char> input)
            {
                if (input.IsEmpty || input[0] != '/')
                {
                    return ParseResult<object?>.Failure();
                }

                var span = input.Slice(1);

                if (!TryReadSegment(span, allowEmpty: false, out var segment, out var remaining))
                {
                    return ParseResult<object?>.Failure();
                }

                if (!segment.Equals(_segment.AsSpan(), _comparison))
                {
                    return ParseResult<object?>.Failure();
                }

                return ParseResult<object?>.Successful(null, remaining);
            }
        }

        private sealed class ParameterSegmentParser<T> : Parser<object?>
        {
            private readonly Parser<T> _parser;
            private readonly Func<T, object?> _projection;

            public ParameterSegmentParser(Parser<T> parser, Func<T, object?> projection)
            {
                _parser = parser ?? throw new ArgumentNullException(nameof(parser));
                _projection = projection ?? throw new ArgumentNullException(nameof(projection));
            }

            public ParseResult<object?> Parse(ReadOnlySpan<char> input)
            {
                if (input.IsEmpty || input[0] != '/')
                {
                    return ParseResult<object?>.Failure();
                }

                var span = input.Slice(1);
                var result = _parser.Parse(span);

                if (!result.Success)
                {
                    return ParseResult<object?>.Failure();
                }

                if (result.Remaining.Length == span.Length)
                {
                    // The inner parser did not consume any input; treat as failure to avoid infinite loops.
                    return ParseResult<object?>.Failure();
                }

                if (!result.Remaining.IsEmpty && result.Remaining[0] != '/')
                {
                    return ParseResult<object?>.Failure();
                }

                return ParseResult<object?>.Successful(_projection(result.Value), result.Remaining);
            }
        }

        private sealed class StringSegmentParser : Parser<string>
        {
            public ParseResult<string> Parse(ReadOnlySpan<char> input)
            {
                if (!TryReadSegment(input, allowEmpty: false, out var segment, out var remaining))
                {
                    return ParseResult<string>.Failure();
                }

                return ParseResult<string>.Successful(new string(segment), remaining);
            }
        }

        private sealed class IntSegmentParser : Parser<int>
        {
            public ParseResult<int> Parse(ReadOnlySpan<char> input)
            {
                if (!TryReadSegment(input, allowEmpty: false, out var segment, out var remaining))
                {
                    return ParseResult<int>.Failure();
                }

                if (!int.TryParse(segment, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                {
                    return ParseResult<int>.Failure();
                }

                return ParseResult<int>.Successful(value, remaining);
            }
        }

        private sealed class DoubleSegmentParser : Parser<double>
        {
            public ParseResult<double> Parse(ReadOnlySpan<char> input)
            {
                if (!TryReadSegment(input, allowEmpty: false, out var segment, out var remaining))
                {
                    return ParseResult<double>.Failure();
                }

                if (!double.TryParse(segment, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var value))
                {
                    return ParseResult<double>.Failure();
                }

                return ParseResult<double>.Successful(value, remaining);
            }
        }

        private sealed class StrictIntSegmentParser : Parser<int?>
        {
            public ParseResult<int?> Parse(ReadOnlySpan<char> input)
            {
                if (!TryReadSegment(input, allowEmpty: true, out var segment, out var remaining))
                {
                    return ParseResult<int?>.Failure();
                }

                if (segment.IsEmpty)
                {
                    return ParseResult<int?>.Successful(null, remaining);
                }

                return ParseResult<int?>.Successful(
                    int.TryParse(segment, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : (int?)null,
                    remaining);
            }
        }

        private sealed class StrictDoubleSegmentParser : Parser<double?>
        {
            public ParseResult<double?> Parse(ReadOnlySpan<char> input)
            {
                if (!TryReadSegment(input, allowEmpty: true, out var segment, out var remaining))
                {
                    return ParseResult<double?>.Failure();
                }

                if (segment.IsEmpty)
                {
                    return ParseResult<double?>.Successful(null, remaining);
                }

                return ParseResult<double?>.Successful(
                    double.TryParse(segment, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var value) ? value : (double?)null,
                    remaining);
            }
        }
    }

    /// <summary>
    /// Provides an ASP.NET-style template parser built on top of the functional routing library.
    /// </summary>
    public static class Templates
    {
        /// <summary>
        /// Compiles a route template (for example "/article/{slug}" or "/profile/{id:int}") into a parser.
        /// </summary>
        public static Parser<RouteMatch> Path(string template)
        {
            var segments = ParseTemplate(template);
            return Parse.Path(segments);
        }

        /// <summary>
        /// Creates a template-based router that evaluates templates in registration order.
        /// </summary>
        public static TemplateRouter<TResult> Build<TResult>(Action<TemplateRouterBuilder<TResult>> configure)
        {
            if (configure is null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var builder = new TemplateRouterBuilder<TResult>();
            configure(builder);
            return builder.Build();
        }

        private static Parse.PathSegment[] ParseTemplate(string template)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                throw new ArgumentException("Template cannot be null or empty.", nameof(template));
            }

            if (template[0] != '/')
            {
                throw new ArgumentException("Route templates must start with '/'.", nameof(template));
            }

            List<Parse.PathSegment> segments = [];
            var optionalEncountered = false;
            var index = 0;

            while (index < template.Length)
            {
                if (template[index] != '/')
                {
                    throw new ArgumentException($"Invalid route template '{template}'.", nameof(template));
                }

                index++; // skip '/'

                var start = index;
                while (index < template.Length && template[index] != '/')
                {
                    index++;
                }

                if (index == start)
                {
                    continue; // ignore duplicate slashes
                }

                var token = template[start..index];
                var segment = ParseSegment(token);

                if (optionalEncountered && !segment.Optional)
                {
                    throw new ArgumentException("Optional segments must appear at the end of the template.", nameof(template));
                }

                optionalEncountered |= segment.Optional;
                segments.Add(segment);
            }

            return segments.ToArray();

            static Parse.PathSegment ParseSegment(string token)
            {
                token = token.Trim();

                if (token.Length >= 2 && token[0] == '{' && token[^1] == '}')
                {
                    return ParseParameter(token);
                }

                if (token.Contains('{') || token.Contains('}'))
                {
                    throw new ArgumentException($"Invalid literal segment '{token}'.", nameof(token));
                }

                return Parse.Segment.Literal(token);
            }

            static Parse.PathSegment ParseParameter(string token)
            {
                var content = token[1..^1].Trim();
                var optional = false;

                if (content.EndsWith("?", StringComparison.Ordinal))
                {
                    optional = true;
                    content = content[..^1];
                }

                var colonIndex = content.IndexOf(':');
                string name;
                string? constraint = null;

                if (colonIndex >= 0)
                {
                    name = content[..colonIndex].Trim();
                    constraint = content[(colonIndex + 1)..].Trim();
                }
                else
                {
                    name = content.Trim();
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new ArgumentException($"Invalid parameter segment '{token}': name is required.", nameof(token));
                }

                return constraint switch
                {
                    null or "" => optional
                        ? Parse.Segment.Parameter(name, optional: true)
                        : Parse.Segment.Parameter(name),
                    "int" => optional
                        ? Parse.Segment.Parameter<int>(name, Parse.Int, optional: true)
                        : Parse.Segment.Parameter<int>(name, Parse.Int),
                    "double" or "float" => optional
                        ? Parse.Segment.Parameter<double>(name, Parse.Double, optional: true)
                        : Parse.Segment.Parameter<double>(name, Parse.Double),
                    "string" => optional
                        ? Parse.Segment.Parameter(name, optional: true)
                        : Parse.Segment.Parameter(name),
                    _ => throw new ArgumentException($"Unsupported route constraint '{constraint}' in segment '{token}'.", nameof(token)),
                };
            }
        }

        /// <summary>
        /// Builder used to configure a template router.
        /// </summary>
        public sealed class TemplateRouterBuilder<TResult>
        {
            private readonly List<TemplateEntry<TResult>> _entries = new();

            public TemplateRouterBuilder<TResult> Map(string template, Func<RouteMatch, TResult> projector)
            {
                if (string.IsNullOrWhiteSpace(template))
                {
                    throw new ArgumentException("Template cannot be null or empty.", nameof(template));
                }

                if (projector is null)
                {
                    throw new ArgumentNullException(nameof(projector));
                }

                var parser = Path(template);
                _entries.Add(new TemplateEntry<TResult>(parser, projector));
                return this;
            }

            internal TemplateRouter<TResult> Build()
            {
                return new TemplateRouter<TResult>(_entries);
            }
        }

        /// <summary>
        /// Represents a compiled template router.
        /// </summary>
        public sealed class TemplateRouter<TResult>
        {
            private readonly TemplateEntry<TResult>[] _entries;

            internal TemplateRouter(IEnumerable<TemplateEntry<TResult>> entries)
            {
                _entries = entries.ToArray();
            }

            public bool TryMatch(string path, out TResult result)
                => TryMatch(path.AsSpan(), out result, out _);

            public bool TryMatch(ReadOnlySpan<char> path, out TResult result)
                => TryMatch(path, out result, out _);

            public bool TryMatch(string path, out TResult result, out RouteMatch match)
                => TryMatch(path.AsSpan(), out result, out match);

            public bool TryMatch(ReadOnlySpan<char> path, out TResult result, out RouteMatch match)
            {
                foreach (var entry in _entries)
                {
                    var parseResult = entry.Parser.Parse(path);
                    if (!parseResult.Success)
                    {
                        continue;
                    }

                    match = parseResult.Value;
                    result = entry.Projector(parseResult.Value);
                    return true;
                }

                result = default!;
                match = RouteMatch.Empty;
                return false;
            }
        }

        internal readonly struct TemplateEntry<TResult>
        {
            public TemplateEntry(Parser<RouteMatch> parser, Func<RouteMatch, TResult> projector)
            {
                Parser = parser;
                Projector = projector;
            }

            public Parser<RouteMatch> Parser { get; }
            public Func<RouteMatch, TResult> Projector { get; }
        }
    }
}
