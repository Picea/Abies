using Abies;
using System.Globalization;

using static Abies.Parse;

namespace Abies;

public record Scheme(string Value)
{
    public static implicit operator string(Scheme scheme) => scheme.Value;
}
public record Host(string Value)
{
    public static implicit operator string(Host host) => host.Value;
}
public record Port(int Value)
{
    public static implicit operator int(Port port) => port.Value;
    public static implicit operator int?(Port? port) => port?.Value;
}
public record Path(string Value)
{
    public static implicit operator string(Path path) => path.Value;
};
public record Query(string Value)
{
    public static implicit operator string(Query query) => query.Value;
}
public record Fragment(string Value)
{
    public static implicit operator string(Fragment fragment) => fragment.Value;
}

public partial interface Parser<T>
{
    public static Parser<T> operator /(Parser<T> first, Parser<T> second) =>
        first.Slash(second);

    public static Parser<int> operator /(Parser<T> first, int second) =>
        first.Slash(Abies.Parse.Int(second));

    public static Parser<T> operator /(int first, Parser<T> second) =>
        Abies.Parse.Int(first).Slash(second);

    public static Parser<string> operator /(Parser<T> first, string second) =>
        first.Slash(Abies.Parse.String(second));

    public static Parser<T> operator /(string first, Parser<T> second) =>
        Abies.Parse.String(first).Slash(second);

    // for the rest of the primitive types
    public static Parser<T> operator /(Parser<T> first, T second) =>
        first.Slash(second.Return());

    public static Parser<T> operator /(T first, Parser<T> second) =>
        first.Return().Slash(second);

    public static Parser<T> operator /(Parser<T> first, Func<T, T> second) =>
        first.Select(second);

}

/// <summary>
///   https://example.com:8042/over/there?name=ferret#nose
///  \___/   \______________/\_________/ \_________/ \__/
///    |            |            |            |        |
///  scheme     authority       path        query   fragment
///  
/// 
///   /over/there
///   \___/ \__/
///    |     | 
///    segment
///  
///   example.com:8042
///  \_________/ \__/
///    |           |
///   host       port
///    
/// </summary>
public record Url
{
    private Url()
    {
        Scheme = default!;
        Host = default!;
        Port = default!;
        Path = default!;
        Query = default!;
        Fragment = default!;
    }
    public required Protocol Scheme { get; init; }
    public required Host Host { get; init; }
    public required Port? Port { get; init; }
    public required Path Path { get; init; }
    public required Query Query { get; init; }
    public required Fragment Fragment { get; init; }

    public override string ToString() => ToString(this);

    public static Url Create(Decoded.String input) =>
        FromSpanInternal(input.Value.AsSpan());

    public static string ToString(Url url)
    {
        UriBuilder uriBuilder = new UriBuilder
        {
            Scheme = url.Scheme switch
            {
                Protocol.Http _ => "http",
                Protocol.Https _ => "https",
                _ => throw new Exception("Unknown scheme")
            },
            Host = url.Host.Value,
            Path = url.Path.Value,
            Query = url.Query.Value,
            Fragment = url.Fragment.Value
        };
        if (url.Port is not null) uriBuilder.Port = url.Port.Value;
        return uriBuilder.Uri.ToString();
    }


    private static Parser<Url> Parser =>
        from scheme in Parse.Scheme
        from host in Parse.Host
        from port in Parse.Port
        from path in Parse.Path
        from query in Parse.Query
        from fragment in Parse.Fragment
        select new Url
        {
            Scheme = scheme switch
            {
                "http" => new Protocol.Http(),
                "https" => new Protocol.Https(),
                _ => throw new Exception("Unknown scheme")
            },
            Host = new(host),
            Port = port is not null ? new Port(port.Value) : null,
            Path = new(Uri.UnescapeDataString(path)),
            Query = new(query),
            Fragment = new(fragment)
        };



    private static Url FromSpanInternal(ReadOnlySpan<char> input)
    {
        var result = Parser.Parse(input);
        if (!result.Success)
        {
            throw new FormatException("Invalid URL format.");
        }

        var parsedUrl = result.Value;

        // Assign default ports if none are specified
        int? effectivePort = parsedUrl.Port?.Value == -1
        ? (parsedUrl.Scheme switch
        {
            Protocol.Http _ => 80,
            Protocol.Https _ => 443,
            _ => throw new Exception("Unknown scheme for default port assignment.")
        })
        : parsedUrl.Port?.Value;

        // Normalize scheme and host to lowercase and convert to Punycode
        var idn = new IdnMapping();
        string punycodeHost = idn.GetAscii(parsedUrl.Host.Value.ToLowerInvariant());

        // Normalize scheme and host to lowercase
        return parsedUrl with
        {
            Scheme = parsedUrl.Scheme switch
            {
                Protocol.Http _ => new Protocol.Http(),
                Protocol.Https _ => new Protocol.Https(),
                _ => throw new Exception("Unknown scheme")
            },
            Host = new(punycodeHost),
            Port =effectivePort.HasValue ? new(effectivePort.Value) : null,
            Path = parsedUrl.Path,
            Query = parsedUrl.Query,
            Fragment = parsedUrl.Fragment
        };
    }

    public static class Encoded
    {
        public readonly struct String(string value)
        {
            public string Value { get; } = Uri.EscapeDataString(value);

            public override string ToString() => Value;

            public static Decoded.String Decode(string value) =>
                new(Uri.UnescapeDataString(value));

            public static implicit operator string(String value) => value.Value;
        }
    }

    public static class Decoded
    {
        public readonly struct String(string value)
        {
            public string Value { get; } = // Check if the input is already decoded
                Uri.IsWellFormedUriString(value, UriKind.Absolute)
                ? value
                : Uri.UnescapeDataString(value);

            public override string ToString() => Value;

            public static Encoded.String Encode(string value) =>
                new(Uri.EscapeDataString(value));

            public static implicit operator string(String value) =>
                value.Value;
        }
    }


    // // Usage example
    // var input = "https://www.example.com:8080/path/to/resource?query=abc#section";
    // var result = UrlParser.Url(input.AsSpan());

    // if (result.Success)
    // {
    //     var urlComponents = result.Value;
    //     // Access urlComponents.Scheme, urlComponents.Host, etc.
    // }
    // else
    // {
    //     // Handle parsing failure
    public static class Parse
    {
        public static Parser<char> LetterOrDigitOrPlus =>
            Abies.Parse.Satisfy(c => char.IsLetterOrDigit(c) || c == '+');

        public static Parser<string> Scheme =>
            from schemeChars in Abies.Parse.Many1(LetterOrDigitOrPlus)
            from colon in Abies.Parse.Char(':')
            select new string(schemeChars.ToArray());

        public static Parser<string> Host =>
            from slashes in Abies.Parse.String("//")
            from hostChars in Abies.Parse.Many1(Abies.Parse.Satisfy(c => c != ':' && c != '/' && c != '?' && c != '#'))
            select new string([.. hostChars]);

        public static Parser<string> Query =>
            (from question in Abies.Parse.Char('?')
             from parameters in Abies.Parse.Many1(ParseParameter())
             select string.Join("&", parameters))
            .Optional()
            .Select(query => query ?? string.Empty);

        private static Parser<string> ParseParameter() =>
            from key in Abies.Parse.Many1(Abies.Parse.Satisfy(c => char.IsLetterOrDigit(c) || c == '_'))
            from _ in Abies.Parse.Char('=')
            from value in Abies.Parse.Many1(Abies.Parse.Satisfy(c => char.IsLetterOrDigit(c) || c == '_'))
            from ampersand in Abies.Parse.Char('&').Optional()
            select $"{new string(key.ToArray())}={new string(value.ToArray())}";

        public static Parser<string> Fragment =>
            (from hash in Abies.Parse.Char('#')
             from fragmentChars in Abies.Parse.Many(Abies.Parse.Item())
             select new string(fragmentChars.ToArray()))
            .Optional()
            .Select(fragment => fragment ?? string.Empty);

        public static Parser<int?> Port =>
            (from colon in Abies.Parse.Char(':')
             from portChars in Abies.Parse.Many1(Abies.Parse.Satisfy(char.IsDigit))
             select (int?)int.Parse(new string(portChars.ToArray())))
            .Optional();

        public static Parser<string> Path =>
            Abies.Parse.Many(Abies.Parse.Satisfy(c => c != '?' && c != '#'))
                .Select(chars => new string(chars.ToArray()))
                .Optional()
                .Select(path => string.IsNullOrEmpty(path) ? "/" : path);

        public static class Segment
        {
            public static Parser<string> String(string segment) =>
                from slash in Abies.Parse.Char('/')
                from @string in Abies.ParserExtensions.String(segment)
                select @string;

            /// <summary>
            /// A parser that parses a sequence of one or more characters that are not a '/' character.
            /// For example, the parser succeeds when input is "foo" or "bar", but fails when input is "foo/bar".
            /// </summary>
            public static Parser<string> Any =>
                from segmentChars in Abies.Parse.Many1(Abies.Parse.Satisfy(c => c != '/'))
                select new string([.. segmentChars]);

            /// <summary>
            /// Parses an empty segment, which is represented by a single forward slash and returns an empty string.
            /// Applications can use this parser to represent the root path of a URL.
            /// </summary>
            public static Parser<string> Empty =>
                from empty in Abies.Parse.Char('/')
                select string.Empty;

            // parse ONLY the root segment
            public static Parser<string> Root =>
                from root in Abies.Parse.Char('/')
                select "/";
        }




        /// <summary>
        /// A parser that parses a sequence of one or more digits (0-9) that are not preceded by a '/' character.
        /// If the input does not contain at least one digit or contains a '/' character, the parser fails.
        /// </summary>
        /// <remarks>
        /// This parser uses the `Parse.Many1` combinator to ensure that at least one digit is present in the input.
        /// It uses the `Parse.Satisfy` combinator to check that each character is a digit and not a '/' character.
        /// When the parser succeeds, it returns an integer parsed from the sequence of digits.
        /// </remarks>
        public static Parser<int> Int =>
            from digits in Abies.Parse.Many1(Abies.Parse.Satisfy(c => c != '/' && char.IsDigit(c)))
            select int.Parse(new string([.. digits]));

        public static Parser<double> Double =>
            from digits in Abies.Parse.Many1(Abies.Parse.Satisfy(c => c != '/' && char.IsDigit(c)))
            select double.Parse(new string([.. digits]));


        public static class Strict
        {
            public static Parser<int?> Int =>
                from items in Segment.Any
                let digits = int.TryParse(new string([.. items]), out var result) ? result : (int?)null
                select digits;

            public static Parser<double?> Double =>
                from items in Segment.Any
                let digits = double.TryParse(new string([.. items]), out var result) ? result : (double?)null
                select digits;
        }

        /// <summary>
        /// A parser that parses a sequence of one or more characters that are not a '/' character.
        /// If the input does not contain at least one character or contains a '/' character, the parser fails.
        /// </summary>
        public static Parser<string> String =>
            from chars in Abies.Parse.Many(Abies.Parse.Satisfy(c => c != '/'))
            select new string([.. chars]);

    }

    public interface Protocol
    {
        public sealed record Http : Protocol;
        public sealed record Https : Protocol;
    }
}