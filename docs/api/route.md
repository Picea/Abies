# Routing in Abies

Abies does not include a built-in router component, route table, or routing DSL. Instead, routing is implemented through **URL pattern matching** in the `Transition` function using standard C# pattern matching.

This is a deliberate design decision: routing is just message handling, and URLs are just data.

## How Routing Works

Routing in Abies is the combination of three features:

1. **`Navigation.UrlChanges`** — A subscription that dispatches a message when the URL changes
2. **`UrlChanged`** — A message carrying the new `Url`
3. **Pattern matching in `Transition`** — C# `switch` expressions on `Url.Path`

```
URL changes → UrlChanged(url) message → Transition → pattern match on url.Path → new model + commands
```

## Basic Example

```csharp
public static (Model, Command) Transition(Model model, Message message) =>
    message switch
    {
        UrlChanged(var url) => Route(model, url),
        UrlRequest.Internal(var url) => (model, Navigation.PushUrl(url)),
        UrlRequest.External(var href) => (model, Navigation.ExternalUrl(href)),
        // ... other messages
        _ => (model, Commands.None)
    };

private static (Model, Command) Route(Model model, Url url) =>
    url.Path.ToArray() switch
    {
        [] => (model with { Page = Page.Home }, Commands.None),
        ["login"] => (model with { Page = Page.Login }, Commands.None),
        ["register"] => (model with { Page = Page.Register }, Commands.None),
        ["article", var slug] => (model with { Page = Page.Article(slug) }, new LoadArticle(slug)),
        ["profile", var username] => (model with { Page = Page.Profile(username) }, new LoadProfile(username)),
        _ => (model with { Page = Page.NotFound }, Commands.None)
    };
```

## Setting Up Navigation

To receive URL change notifications, subscribe to `Navigation.UrlChanges` in your `Subscriptions` function:

```csharp
public static Subscription Subscriptions(Model model) =>
    Navigation.UrlChanges(url => new UrlChanged(url));
```

The runtime also dispatches `UrlChanged(initialUrl)` automatically after `Initialize` completes, so your application routes correctly on the initial page load without any additional setup.

## Handling Link Clicks

When a user clicks a link within the application, the runtime dispatches a `UrlRequest` message. Handle it in `Transition` to decide whether to navigate:

```csharp
UrlRequest.Internal(var url) => (model, Navigation.PushUrl(url)),
UrlRequest.External(var href) => (model, Navigation.ExternalUrl(href)),
```

For internal links, `Navigation.PushUrl` updates the browser URL (via `history.pushState`), which triggers the `Navigation.UrlChanges` subscription, which dispatches `UrlChanged`, which hits your `Route` function.

## URL Parameters

### Path Parameters

Extract path parameters using C# list patterns:

```csharp
url.Path.ToArray() switch
{
    ["article", var slug] => /* slug = "my-article" for /article/my-article */,
    ["profile", var username, "favorites"] => /* nested route */,
    ["page", var pageStr] when int.TryParse(pageStr, out var page) => /* typed parameter */,
    _ => /* not found */
};
```

### Query Parameters

Access query parameters via the `Url.Query` dictionary:

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

### Fragment (Hash)

Access the URL fragment via `Url.Fragment`:

```csharp
var section = url.Fragment.Match(
    some: fragment => fragment,  // e.g., "section-2"
    none: () => "top");
```

## Why No Built-In Router?

1. **Pattern matching is more powerful** — C# switch expressions handle complex routing logic (guards, nested patterns, type checks) that a route-table DSL cannot express.
2. **Compiler-verified** — The C# compiler checks pattern exhaustiveness. Typos in route strings are caught at compile time via the pattern structure.
3. **Refactoring-safe** — IDE rename/find-references works on route handler methods. No magic strings or convention-based routing.
4. **No framework lock-in** — Routing is just code in your `Transition` function. No framework types to learn, no routing middleware to configure.
5. **Composable** — Route matching is a pure function that returns `(Model, Command)`. You can compose, delegate, and test routing logic like any other function.

## See Also

- [Navigation](navigation.md) — The navigation API (commands + subscription)
- [Program](program.md) — Where `Transition` handles URL messages
- [Url](url.md) — The `Url` record type
