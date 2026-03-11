# Url API

The `Url` record provides a parsed, strongly-typed representation of a URL with path segments, query parameters, and an optional fragment.

## Definition

```csharp
public record Url(
    IReadOnlyList<string> Path,
    IReadOnlyDictionary<string, string> Query,
    Option<string> Fragment);
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `Path` | `IReadOnlyList<string>` | Path segments, split on `/` and URL-decoded. E.g., `/articles/my-slug` → `["articles", "my-slug"]` |
| `Query` | `IReadOnlyDictionary<string, string>` | Query parameters as key-value pairs. E.g., `?page=1&sort=date` → `{ "page": "1", "sort": "date" }` |
| `Fragment` | `Option<string>` | URL fragment (hash), if present. E.g., `#section-2` → `Some("section-2")` |

## Static Members

### Url.Root

```csharp
public static readonly Url Root
```

An empty URL representing the root path with no query or fragment:

```csharp
Url.Root
// Path: [], Query: {}, Fragment: None
```

### Url.FromUri

```csharp
public static Url FromUri(Uri uri)
```

Parses a `System.Uri` into an Abies `Url`:

```csharp
var url = Url.FromUri(new Uri("https://example.com/articles/my-slug?page=1#comments"));
// url.Path      → ["articles", "my-slug"]
// url.Query     → { "page": "1" }
// url.Fragment  → Some("comments")
```

Path segments are URL-decoded via `Uri.UnescapeDataString`. Query parameters are split on `&` and `=`, then URL-decoded.

### ToRelativeUri

```csharp
public string ToRelativeUri()
```

Converts back to a relative URI string:

```csharp
var url = new Url(["articles", "my-slug"], new Dictionary<string, string> { ["page"] = "1" }, Option<string>.None);
url.ToRelativeUri()  // "/articles/my-slug?page=1"

Url.Root.ToRelativeUri()  // "/"
```

## Constructing URLs

Create `Url` values directly using the record constructor:

```csharp
// Root path
var root = Url.Root;

// Simple path
var articleUrl = new Url(
    ["article", slug],
    new Dictionary<string, string>(),
    Option<string>.None);

// Path with query parameters
var searchUrl = new Url(
    ["search"],
    new Dictionary<string, string> { ["q"] = query, ["page"] = page.ToString() },
    Option<string>.None);

// Path with fragment
var sectionUrl = new Url(
    ["docs", "api"],
    new Dictionary<string, string>(),
    Option.Some("events"));
```

## Pattern Matching on URLs

The primary use of `Url` is pattern matching on `Path` for routing in `Transition`:

```csharp
private static (Model, Command) Route(Model model, Url url) =>
    url.Path.ToArray() switch
    {
        [] => (model with { Page = Page.Home }, Commands.None),
        ["login"] => (model with { Page = Page.Login }, Commands.None),
        ["register"] => (model with { Page = Page.Register }, Commands.None),
        ["article", var slug] => (model with { Page = Page.Article }, new LoadArticle(slug)),
        ["profile", var username] => (model with { Page = Page.Profile }, new LoadProfile(username)),
        ["editor"] => (model with { Page = Page.Editor }, Commands.None),
        ["editor", var slug] => (model with { Page = Page.Editor }, new LoadArticle(slug)),
        _ => (model with { Page = Page.NotFound }, Commands.None)
    };
```

### Accessing Query Parameters

```csharp
private static (Model, Command) Route(Model model, Url url) =>
    url.Path.ToArray() switch
    {
        [] =>
            url.Query.TryGetValue("tag", out var tag)
                ? (model with { Page = Page.Home, Filter = tag }, new LoadTaggedArticles(tag))
                : (model with { Page = Page.Home }, new LoadGlobalFeed()),
        // ...
    };
```

### Accessing the Fragment

```csharp
var section = url.Fragment.Match(
    some: fragment => fragment,  // e.g., "section-2"
    none: () => "top");
```

## URL in Navigation

Use `Url` values with navigation commands:

```csharp
// Navigate to a new URL
(model, Navigation.PushUrl(new Url(["article", slug], new Dictionary<string, string>(), Option<string>.None)))

// Replace current URL
(model, Navigation.ReplaceUrl(Url.Root))
```

## URL Change Messages

URLs arrive in the `Transition` function via `UrlChanged` messages:

```csharp
public static (Model, Command) Transition(Model model, Message message) =>
    message switch
    {
        UrlChanged(var url) => Route(model, url),
        UrlRequest.Internal(var url) => (model, Navigation.PushUrl(url)),
        UrlRequest.External(var href) => (model, Navigation.ExternalUrl(href)),
        _ => (model, Commands.None)
    };
```

The runtime dispatches `UrlChanged(initialUrl)` automatically after `Initialize`, so the application routes correctly on the initial page load.

## See Also

- [Navigation](navigation.md) — Commands and subscriptions for URL management
- [Routing](route.md) — How to implement routing with URL pattern matching
- [Program](program.md) — Where `Transition` handles URL messages
