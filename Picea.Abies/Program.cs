using Picea;
using Picea.Abies.DOM;
using Picea.Abies.Subscriptions;

namespace Picea.Abies;

public interface Program<TModel, TArgument> : Decider<TModel, Message, Message, Command, Message, TArgument>
{
    new static abstract Result<Message[], Message> Decide(TModel state, Message command);

    new static abstract bool IsTerminal(TModel state);

    static abstract Document View(TModel model);

    static abstract Subscription Subscriptions(TModel model);
}

public record Url(IReadOnlyList<string> Path, IReadOnlyDictionary<string, string> Query, Option<string> Fragment)
{
    public static readonly Url Root = new(
        Array.Empty<string>(),
        new Dictionary<string, string>(),
        Option<string>.None);

    public static Url FromUri(Uri uri)
    {
        var path = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();

        var query = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(uri.Query))
        {
            var queryString = uri.Query.TrimStart('?');
            foreach (var pair in queryString.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split('=', 2);
                var key = Uri.UnescapeDataString(parts[0]);
                var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
                query[key] = value;
            }
        }

        var fragment = string.IsNullOrEmpty(uri.Fragment)
            ? Option<string>.None
            : Option.Some(uri.Fragment.TrimStart('#'));

        return new Url(path, query, fragment);
    }

    public string ToRelativeUri()
    {
        var pathPart = Path.Count > 0 ? "/" + string.Join("/", Path) : "/";
        var queryPart = Query.Count > 0
            ? "?" + string.Join("&", Query.Select(kv =>
                $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"))
            : string.Empty;
        var fragmentPart = Fragment.Match(f => "#" + f, () => string.Empty);
        return $"{pathPart}{queryPart}{fragmentPart}";
    }
}

public interface UrlRequest : Message
{
    record Internal(Url Url) : UrlRequest;
    record External(string Href) : UrlRequest;
}

public sealed record UrlChanged(Url Url) : Message;
