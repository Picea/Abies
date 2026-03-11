# Routing

Abies uses **plain C# pattern matching** on URL path segments for routing. There is no router library, no route registration table, and no parser combinator DSL — just a `switch` expression on `Url.Path`.

## Core Concepts

### The `Url` Type

Every URL in Abies is represented as a `Url` record:

```csharp
public sealed record Url(
    string[] Path,
    Dictionary<string, string> Query,
    Option<string> Fragment
);
```

- **`Path`** — Segments of the URL path. `/articles/hello-world` becomes `["articles", "hello-world"]`.
- **`Query`** — Query parameters as key-value pairs. `?page=2&tag=elm` becomes `{ "page": "2", "tag": "elm" }`.
- **`Fragment`** — The URL fragment (hash). `#comments` becomes `Some("comments")`.

### Routing is a Pure Function

Routing in Abies is a pure function: `Url → (Page, Command)`. It takes a URL and returns which page to show and what data to fetch:

```csharp
public static (Page Page, Command Command) FromUrl(Url url, Session? session, string apiUrl) =>
    url.Path switch
    {
        [] or [""] => (new Page.Home(HomeModel.Initial(session)), FetchHomeData(session, apiUrl)),
        ["login"] => (new Page.Login(LoginModel.Empty), Commands.None),
        ["register"] => (new Page.Register(RegisterModel.Empty), Commands.None),
        ["settings"] => RequireAuth(session, () => (new Page.Settings(SettingsModel.From(session!)), Commands.None)),
        ["editor"] => (new Page.Editor(EditorModel.New), Commands.None),
        ["editor", var slug] => (new Page.Editor(EditorModel.Loading(slug)), new FetchArticle(apiUrl, session?.Token, slug)),
        ["article", var slug] => (new Page.Article(ArticleModel.Loading(slug)), FetchArticleData(slug, session, apiUrl)),
        ["profile", var username] => (new Page.Profile(ProfileModel.Loading(username)), FetchProfileData(username, session, apiUrl)),
        ["profile", var username, "favorites"] => (new Page.Profile(ProfileModel.Loading(username, favorites: true)), FetchFavoritesData(username, session, apiUrl)),
        _ => (new Page.NotFound(), Commands.None)
    };
```

This is the entire router. No configuration, no middleware, no route table — just pattern matching.

### Reverse Routing

Convert pages back to URLs with another `switch` expression:

```csharp
public static Url ToUrl(Page page) => page switch
{
    Page.Home => MakeUrl([]),
    Page.Login => MakeUrl(["login"]),
    Page.Register => MakeUrl(["register"]),
    Page.Settings => MakeUrl(["settings"]),
    Page.Editor { Data.Slug: null } => MakeUrl(["editor"]),
    Page.Editor { Data.Slug: var slug } => MakeUrl(["editor", slug]),
    Page.Article { Data.Slug: var slug } => MakeUrl(["article", slug]),
    Page.Profile { Data: { ShowFavorites: true, Username: var u } } => MakeUrl(["profile", u, "favorites"]),
    Page.Profile { Data.Username: var u } => MakeUrl(["profile", u]),
    _ => MakeUrl([])
};

private static Url MakeUrl(string[] path) =>
    new(path, new Dictionary<string, string>(), Option<string>.None);
```

## The Navigation System

Abies provides two navigation channels that connect the browser's history API to the MVU loop:

### Commands (App → Browser)

Navigation commands are returned from your `Transition` or `OnUrlRequested` function:

| Command | Browser API | Effect |
|---|---|---|
| `Navigation.PushUrl(url)` | `history.pushState` | Adds a new history entry |
| `Navigation.ReplaceUrl(url)` | `history.replaceState` | Replaces current entry |
| `Navigation.Back` | `history.back()` | Goes back one step |
| `Navigation.Forward` | `history.forward()` | Goes forward one step |
| `Navigation.ExternalUrl(href)` | `window.location.href` | Full page navigation (leaves the app) |

```csharp
// In your Transition function:
static (Model, Command) Transition(Model model, Message message) =>
    message switch
    {
        ClickedArticle(var slug) =>
            (model, Navigation.PushUrl(Route.ToUrl(new Page.Article(ArticleModel.Loading(slug))))),

        ClickedBack =>
            (model, Navigation.Back),

        _ => (model, Commands.None)
    };
```

### Subscriptions (Browser → App)

The `Navigation.UrlChanges` subscription listens for URL changes:

```csharp
static Subscription Subscriptions(Model model) =>
    Navigation.UrlChanges(url => new UrlChanged(url));
```

This captures two kinds of browser events:

1. **`popstate`** — Browser back/forward button, programmatic history changes
2. **Internal link clicks** — Same-origin `<a>` elements are intercepted, the URL is pushed via `history.pushState`, and a `UrlChanged` message is dispatched

External links (cross-origin or `target="_blank"`) are not intercepted.

## The Program Interface

The `Program<TModel, TArgument>` interface includes two navigation-specific methods:

### `OnUrlRequested`

Called when the user clicks a link. You decide what to do:

```csharp
static (Model, Command) OnUrlRequested(Model model, UrlRequest request) =>
    request switch
    {
        // Internal link — navigate within the app
        UrlRequest.Internal(var url) => (model, Navigation.PushUrl(url)),

        // External link — leave the app
        UrlRequest.External(var href) => (model, Navigation.ExternalUrl(href)),

        _ => (model, Commands.None)
    };
```

### `OnUrlChanged`

Called when the URL actually changes (after `PushUrl`, `ReplaceUrl`, or browser back/forward). This is where you invoke your router:

```csharp
static (Model, Command) OnUrlChanged(Model model, Url url)
{
    var (page, command) = Route.FromUrl(url, model.Session, model.ApiUrl);
    return (model with { CurrentPage = page }, command);
}
```

## Complete Navigation Flow

```text
User clicks <a href="/article/hello">
        │
        ▼
  OnUrlRequested(Internal("/article/hello"))
        │
        ├── Returns Navigation.PushUrl(url)
        │
        ▼
  Runtime calls history.pushState
        │
        ▼
  OnUrlChanged(Url(["article", "hello"], {}, None))
        │
        ├── Route.FromUrl(url, ...) → (Page.Article, FetchArticle)
        │
        ├── Model updated with new page
        │
        ├── FetchArticle command sent to handler
        │
        ▼
  View(model) renders the article page skeleton
        │
        ▼
  FetchArticle completes → ArticleLoaded message
        │
        ▼
  Transition updates model with article data
        │
        ▼
  View(model) renders the full article
```

## Why Pattern Matching Over Parser Combinators

Abies originally used parser combinators for routing (see [ADR-004](../adr/ADR-004-parser-combinators.md), now deprecated). The switch to plain pattern matching was motivated by:

1. **Simplicity** — `url.Path switch { ["article", var slug] => ... }` is immediately readable to any C# developer. No DSL to learn.

2. **Exhaustiveness** — The compiler warns if you miss a case (with the `_` default). Parser combinators silently fail on unmatched routes.

3. **Zero abstraction cost** — Pattern matching compiles to efficient IL. No allocator pressure from parser state, no intermediate result objects.

4. **IDE support** — Full IntelliSense, refactoring, and go-to-definition. Parser combinator chains are opaque to tooling.

5. **Bidirectional** — Writing `FromUrl` (URL → Page) and `ToUrl` (Page → URL) as mirrored switch expressions is natural. Parser combinators only solve one direction.

## Query Parameters and Fragments

Access query parameters and fragments directly from the `Url` record:

```csharp
// URL: /articles?page=2&tag=elm#comments
url.Path switch
{
    ["articles"] => HandleArticleList(
        page: url.Query.GetValueOrDefault("page", "1"),
        tag: url.Query.GetValueOrDefault("tag"),
        scrollTo: url.Fragment
    ),
    ...
};
```

## Server-Side Routing

The routing system works identically across all render modes (Static, InteractiveServer, InteractiveWasm, InteractiveAuto). The `Url` type and `Route.FromUrl` function are pure — they have no browser dependencies.

In server render modes, the initial URL comes from the HTTP request rather than `window.location`, but once the MVU loop starts, navigation works the same way via the `Navigation` commands and subscriptions.

## References

- [ADR-004: Parser Combinators (Deprecated)](../adr/ADR-004-parser-combinators.md) — Historical context on why parser combinators were removed
- [Navigation.cs source](https://github.com/Picea/Abies/blob/main/Picea.Abies/Navigation.cs) — Navigation commands and subscriptions
- [Elm Browser.Navigation](https://package.elm-lang.org/packages/elm/browser/latest/Browser.Navigation) — The Elm module that inspired this design
