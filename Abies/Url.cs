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

public record Url
{
        
    private readonly Uri _uri;
    
    public Protocol? Scheme { get; }
    public Host? Host { get; }
    public Port? Port { get; }
    public Path Path { get; }
    public Query Query { get; }
    public Fragment Fragment { get; }
    
    private Url(Uri uri)
    {
        _uri = uri;

        if (uri.IsAbsoluteUri)
        {
            Scheme = uri.Scheme.ToLowerInvariant() switch
            {
                "http" => new Protocol.Http(),
                "https" => new Protocol.Https(),
                _ => throw new Exception($"Unsupported scheme: {uri.Scheme}")
            };

            Host = new Host(uri.Host);
            Port = uri.IsDefaultPort ? null : new Port(uri.Port);
            Path = new Path(uri.AbsolutePath);
            Query = new Query(uri.Query);
            Fragment = new Fragment(uri.Fragment);
        }
        else
        {
            Scheme = null;
            Host = null;
            Port = null;

            string originalString = _uri.OriginalString;
            string fragmentString = "";
            int fragmentIndex = originalString.IndexOf('#');
            if (fragmentIndex != -1)
            {
                fragmentString = originalString.Substring(fragmentIndex);
                originalString = originalString.Substring(0, fragmentIndex);
            }

            string pathString = originalString;
            string queryString = "";
            int queryIndex = originalString.IndexOf('?');
            if (queryIndex != -1)
            {
                queryString = originalString.Substring(queryIndex);
                pathString = originalString.Substring(0, queryIndex);
            }

            Path = new Path(pathString);
            Query = new Query(queryString);
            Fragment = new Fragment(fragmentString);
        }
    }
    
    public static Url Create(string url)
    {
        try
        {
            return new Url(new Uri(url, UriKind.RelativeOrAbsolute));
        }
        catch (UriFormatException)
        {
            throw new FormatException($"Invalid URL format: {url}");
        }
    }
    
    public override string ToString()
    {
        return _uri.ToString();
    }

    public interface Protocol
    {
        public sealed record Http : Protocol;
        public sealed record Https : Protocol;
    }
}
