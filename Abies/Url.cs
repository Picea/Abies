using System;

namespace Abies;

/// <summary>
///   https://example.com:8042/over/there?name=ferret#nose
///  \___/   \______________/\_________/ \_________/ \__/
///    |            |            |            |        |
///  scheme     authority       path        query   fragment
///  
///   example.com:8042
///  \_________/ \__/
///    |           |
///   host       port
///    
/// </summary>
public record Url
{
    public required Protocol Scheme { get; set; }
    public required string Host { get; set; }
    public required int? Port { get; set; }
    public required string Path { get; set; }
    public required string Query { get; set; }
    public required string Fragment { get; set; }

    public override string ToString()
      => ToString(this);
    

    public static Url FromSpan(ReadOnlySpan<char> input) =>
        Parser.Url(input).Value;

    public static Url FromString(string input) =>
        Parser.Url(input.AsSpan()).Value;

    public static string ToString(Url url) =>
        new UriBuilder
        {
            Scheme = url.Scheme switch
            {
                Protocol.Http _ => "http",
                Protocol.Https _ => "https",
                _ => throw new Exception("Unknown scheme")
            },
            Host = url.Host,
            Port = url.Port ?? -1,
            Path = url.Path,
            Query = url.Query,
            Fragment = url.Fragment
        }.Uri.ToString();
}

public interface Protocol
{
    public sealed record Http : Protocol;

    public sealed record Https : Protocol;
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
public static class Parser
{
    public static Parser<char> LetterOrDigitOrPlus =>
        Parse.Satisfy(c => char.IsLetterOrDigit(c) || c == '+');

    public static Parser<string> Scheme =>
        from schemeChars in Parse.Many1(LetterOrDigitOrPlus)
        from colon in Parse.Char(':')
        select new string([.. schemeChars]);

    public static Parser<string> Host =>
        from slashes in Parse.String("//")
        from hostChars in Parse.Many1(Parse.Satisfy(c => c != ':' && c != '/' && c != '?' && c != '#'))
        select new string([.. hostChars]);

    public static Parser<int?> Port =>
        input =>
        {
            if (input.Length > 0 && input[0] == ':')
            {
                var portParser =
                    from colon in Parse.Char(':')
                    from digits in Parse.Many1(Parse.Digit)
                    select int.Parse(new string(digits.ToArray()));

                var result = portParser(input);
                if (result.Success)
                    return ParseResult<int?>.Successful(result.Value, result.Remaining);
            }
            return ParseResult<int?>.Successful(null, input);
        };

    public static Parser<string> Path =>
        from pathChars in Parse.Many(Parse.Satisfy(c => c != '?' && c != '#'))
        select new string([.. pathChars]);

    public static Parser<string> Query =>
        input =>
        {
            if (input.Length > 0 && input[0] == '?')
            {
                var queryParser =
                    from question in Parse.Char('?')
                    from queryChars in Parse.Many(Parse.Satisfy(c => c != '#'))
                    select new string([.. queryChars]);

                var result = queryParser(input);
                if (result.Success)
                    return ParseResult<string>.Successful(result.Value, result.Remaining);
            }
            return ParseResult<string>.Successful(string.Empty, input);
        };

    public static Parser<string> Fragment =>
        input =>
        {
            if (input.Length > 0 && input[0] == '#')
            {
                var fragmentParser =
                    from hash in Parse.Char('#')
                    from fragmentChars in Parse.Many(Parse.Item())
                    select new string(fragmentChars.ToArray());

                var result = fragmentParser(input);
                if (result.Success)
                    return ParseResult<string>.Successful(result.Value, result.Remaining);
            }
            return ParseResult<string>.Successful(string.Empty, input);
        };

    public static Parser<Url> Url =>
        from scheme in Scheme
        from host in Host
        from port in Port
        from path in Path
        from query in Query
        from fragment in Fragment
        select new Url
        {
            Scheme = scheme switch
            {
                "http" => new Protocol.Http(),
                "https" => new Protocol.Https(),
                _ => throw new Exception("Unknown scheme")
            },
            Host = host,
            Port = port,
            Path = path,
            Query = query,
            Fragment = fragment
        };
}