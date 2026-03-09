using Picea.Abies.DOM;
using Picea.Abies.Subscriptions;
using Picea;

namespace Picea.Abies;

public interface Program<TModel, in TArgument> : Automaton<TModel, Message, Command, TArgument>
{
    static abstract Document View(TModel model);

    static abstract Subscription Subscriptions(TModel model);
}

public record Url(IReadOnlyList<string> Path, Dictionary<string, string> Query, Option<string> Fragment)
{
    public static Url Root => new([], [], Option<string>.None);

    public static Url FromUri(Uri uri)
    {
        var path = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.UnescapeDataString)
            .ToList();

        var query = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(uri.Query))
        {
            foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split('=', 2);
                if (parts.Length == 2)
                    query[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
            }
        }

        var fragment = string.IsNullOrEmpty(uri.Fragment)
            ? Option<string>.None
            : Option.Some(uri.Fragment.TrimStart('#'));

        return new Url(path, query, fragment);
    }

    public string ToRelativeUri()
    {
        var path = Path.Count > 0 ? "/" + string.Join("/", Path) : "/";

        if (Query.Count > 0)
        {
            var queryString = string.Join("&", Query.Select(kv =>
                Uri.EscapeDataString(kv.Key) + "=" + Uri.EscapeDataString(kv.Value)));
            path += "?" + queryString;
        }

        if (Fragment.IsSome)
            path += "#" + Fragment.Value;

        return path;
    }
}

public interface UrlRequest : Message;
public record UrlRequest
{
    public sealed record Internal(Url Url) : UrlRequest;
    public sealed record External(string Href) : UrlRequest;
}

public sealed record UrlChanged(Url Url) : Message;
