# Navigation API

The `Navigation` static class provides commands and subscriptions for client-side URL management. Navigation in Abies is modeled as regular commands and messages — there are no special framework hooks for routing.

## Navigation Commands

Navigation commands are returned from `Transition` like any other command. The runtime's built-in navigation executor handles them — your interpreter never sees them.

### Navigation.PushUrl

```csharp
public static Command PushUrl(Url url)
```

Navigates to a new URL by pushing it onto the browser's history stack (equivalent to `history.pushState`).

```csharp
public static (Model, Command) Transition(Model model, Message message) =>
    message switch
    {
        ClickedArticle(var slug) =>
            (model, Navigation.PushUrl(new Url(["article", slug], new Dictionary<string, string>(), Option<string>.None))),
        _ => (model, Commands.None)
    };
```

### Navigation.ReplaceUrl

```csharp
public static Command ReplaceUrl(Url url)
```

Replaces the current URL without adding a history entry (equivalent to `history.replaceState`).

```csharp
// Replace URL when redirecting after login — no back-button entry
(model with { Page = Page.Home }, Navigation.ReplaceUrl(Url.Root))
```

### Navigation.Back

```csharp
public static readonly Command Back
```

Navigates back in the browser history (equivalent to `history.back()`). This is a `readonly` field, not a method.

```csharp
(model, Navigation.Back)
```

### Navigation.Forward

```csharp
public static readonly Command Forward
```

Navigates forward in the browser history (equivalent to `history.forward()`). This is a `readonly` field, not a method.

```csharp
(model, Navigation.Forward)
```

### Navigation.ExternalUrl

```csharp
public static Command ExternalUrl(string href)
```

Navigates to an external URL, causing a full page load (leaves the SPA).

```csharp
(model, Navigation.ExternalUrl("https://github.com/picea/abies"))
```

## Navigation Subscription

### Navigation.UrlChanges

```csharp
public static Subscription UrlChanges(Func<Url, Message> toMessage)
```

Subscribes to browser URL changes (popstate events, programmatic navigation). When the URL changes, the provided function maps the new `Url` to a `Message` that is dispatched into the MVU loop.

```csharp
public static Subscription Subscriptions(Model model) =>
    Navigation.UrlChanges(url => new UrlChanged(url));
```

This subscription is keyed as `"navigation:urlChanges"` internally, so only one URL change subscription can be active at a time.

## Navigation Messages

Navigation-related messages are regular `Message` types defined in the Abies framework. Handle them in `Transition` like any other message.

### UrlChanged

```csharp
public record UrlChanged(Url Url) : Message;
```

Dispatched when the URL changes — either from the `Navigation.UrlChanges` subscription, a programmatic navigation command, or the initial page load.

```csharp
public static (Model, Command) Transition(Model model, Message message) =>
    message switch
    {
        UrlChanged(var url) => Route(model, url),
        _ => (model, Commands.None)
    };
```

### UrlRequest

```csharp
public interface UrlRequest : Message
{
    record Internal(Url Url) : UrlRequest;
    record External(string Href) : UrlRequest;
}
```

Dispatched when a link is clicked. The application decides how to handle it:

| Variant | Description |
|---------|-------------|
| `UrlRequest.Internal` | A link within the application. The `Url` is already parsed. |
| `UrlRequest.External` | A link to an external site. Contains the raw URL string. |

```csharp
public static (Model, Command) Transition(Model model, Message message) =>
    message switch
    {
        UrlRequest.Internal(var url) => (model, Navigation.PushUrl(url)),
        UrlRequest.External(var href) => (model, Navigation.ExternalUrl(href)),
        UrlChanged(var url) => Route(model, url),
        _ => (model, Commands.None)
    };
```

## Url Type

```csharp
public record Url(
    IReadOnlyList<string> Path,
    IReadOnlyDictionary<string, string> Query,
    Option<string> Fragment)
```

A parsed URL with path segments, query parameters, and an optional fragment.

| Property | Type | Description |
|----------|------|-------------|
| `Path` | `IReadOnlyList<string>` | Path segments (e.g., `["articles", "my-slug"]`) |
| `Query` | `IReadOnlyDictionary<string, string>` | Query parameters as key-value pairs |
| `Fragment` | `Option<string>` | The URL fragment (hash), if present |

### Static Members

```csharp
public static readonly Url Root       // Empty path, no query, no fragment
public static Url FromUri(Uri uri)     // Parse a System.Uri into a Url
public string ToRelativeUri()          // Convert back to a relative URI string
```

## Routing Pattern

Abies has no built-in router component. Routing is implemented through URL pattern matching in `Transition`:

```csharp
private static (Model, Command) Route(Model model, Url url) =>
    url.Path.ToArray() switch
    {
        [] => (model with { Page = Page.Home }, new LoadFeed()),
        ["login"] => (model with { Page = Page.Login }, Commands.None),
        ["register"] => (model with { Page = Page.Register }, Commands.None),
        ["article", var slug] => (model with { Page = Page.Article }, new LoadArticle(slug)),
        ["profile", var username] => (model with { Page = Page.Profile }, new LoadProfile(username)),
        ["settings"] => (model with { Page = Page.Settings }, Commands.None),
        ["editor"] => (model with { Page = Page.Editor }, Commands.None),
        ["editor", var slug] => (model with { Page = Page.Editor }, new LoadArticle(slug)),
        _ => (model with { Page = Page.NotFound }, Commands.None)
    };
```

This approach uses C# pattern matching directly — no framework routing DSL, no route tables, no convention-based routing. The compiler verifies exhaustiveness, and refactoring is safe with IDE support.

## Internal Types

The following types exist in the source code but are framework internals — application code should not reference them directly:

| Type | Purpose |
|------|----------|
| `NavigationCommand` | Interface for navigation command variants |
| `NavigationCommand.Push(Url)` | Internal representation of `PushUrl` |
| `NavigationCommand.Replace(Url)` | Internal representation of `ReplaceUrl` |
| `NavigationCommand.GoBack` | Internal representation of `Back` |
| `NavigationCommand.GoForward` | Internal representation of `Forward` |
| `NavigationCommand.External(string)` | Internal representation of `ExternalUrl` |
| `NavigationCallbacks` | Static class bridging JS popstate events to subscription dispatch |

The `Navigation` static class is the public API — it creates `NavigationCommand` instances internally, but consumers only interact with `Command` values.

## See Also

- [Program](program.md) — Where navigation commands are returned from `Transition`
- [Command](command.md) — The command system that navigation builds on
- [Subscription](subscription.md) — The subscription system that `UrlChanges` uses
- [Url](url.md) — Detailed reference for the `Url` record type
