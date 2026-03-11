# Tutorial 4: Routing

Learn how to handle client-side navigation with URLs, route parsing, and the browser history API.

**Prerequisites:** [Tutorial 3: API Integration](03-api-integration.md)

**Time:** 25 minutes

**What you'll learn:**

- How Abies handles navigation as regular messages
- Parsing URLs into application routes
- Programmatic navigation with navigation commands
- The `UrlChanged` and `UrlRequest` message types
- Using `Navigation.UrlChanges` as a subscription

## Navigation in Abies

Unlike frameworks where routing is a separate subsystem, Abies treats navigation as **regular messages** flowing through the same MVU loop. When the URL changes:

1. The runtime dispatches a `UrlChanged(Url)` message
2. Your `Transition` function handles it like any other message
3. You return a new model (e.g., switch to a different page)

This means routing is just pattern matching — no router configuration, no route tables, no middleware.

## The URL Type

Abies represents URLs with a structured `Url` record:

```csharp
public record Url(
    string[] Path,                              // ["article", "hello-world"]
    IReadOnlyDictionary<string, string> Query,   // { "page": "2" }
    Option<string> Fragment);                    // Some("comments") or None
```

The `Path` is already split into segments — no string parsing needed. You pattern-match directly on the segments:

```csharp
url.Path switch
{
    [] or [""] => /* home page */,
    ["about"]  => /* about page */,
    ["users", var id] => /* user profile with captured id */,
    _ => /* 404 */
};
```

## Building a Multi-Page App

Let's build a simple app with Home, About, and User Profile pages.

### Model

```csharp
using Abies.DOM;
using Abies.Subscriptions;
using Automaton;
using static Abies.Html.Attributes;
using static Abies.Html.Elements;
using static Abies.Html.Events;

namespace RoutingApp;

/// <summary>Discriminated union for pages.</summary>
public abstract record Page
{
    private Page() { }
    public sealed record Home : Page;
    public sealed record About : Page;
    public sealed record UserProfile(string Username, bool IsLoading) : Page;
    public sealed record NotFound : Page;
}

public record Model(Page CurrentPage);
```

The `Page` type is a discriminated union (sealed hierarchy). Each variant holds the data specific to that page.

### Messages

```csharp
public interface AppMessage : Message;

/// <summary>User profile data loaded from the API.</summary>
public record UserLoaded(string Username, string Bio) : AppMessage;

/// <summary>API request failed.</summary>
public record LoadFailed(string Error) : AppMessage;
```

Notice what's **not** here: there's no `Navigate` message. Navigation is handled by `UrlChanged` and `UrlRequest`, which are built-in framework message types.

### Commands

```csharp
/// <summary>Fetch a user's profile from the API.</summary>
public record FetchUser(string Username) : Command;
```

### Route Parsing

Create a pure function that converts a URL into a page with optional commands:

```csharp
public static class Route
{
    public static (Page Page, Command Command) FromUrl(Url url) =>
        url.Path switch
        {
            [] or [""] => (new Page.Home(), Commands.None),
            ["about"]  => (new Page.About(), Commands.None),
            ["users", var username] =>
                (new Page.UserProfile(username, IsLoading: true),
                 new FetchUser(username)),
            _ => (new Page.NotFound(), Commands.None)
        };
}
```

**Key insight:** Route parsing is a **pure function**. It takes a URL and returns a page + commands. No side effects, no state mutation. When a route needs data (like a user profile), it returns both the loading-state page and the fetch command.

### Transition

```csharp
public sealed class App : Program<Model, Unit>
{
    public static (Model, Command) Initialize(Unit _)
    {
        // Start with home page. The runtime will dispatch UrlChanged
        // with the actual browser URL as the first message.
        return (new Model(new Page.Home()), Commands.None);
    }

    public static (Model, Command) Transition(Model model, Message message) =>
        message switch
        {
            // Browser URL changed (back/forward, initial load, link click)
            UrlChanged url => HandleUrlChanged(url.Url),

            // API response
            UserLoaded msg when model.CurrentPage is Page.UserProfile profile =>
                (model with
                {
                    CurrentPage = new Page.UserProfile(
                        profile.Username, IsLoading: false)
                }, Commands.None),

            LoadFailed when model.CurrentPage is Page.UserProfile =>
                (model with { CurrentPage = new Page.NotFound() },
                 Commands.None),

            _ => (model, Commands.None)
        };

    private static (Model, Command) HandleUrlChanged(Url url)
    {
        var (page, command) = Route.FromUrl(url);
        return (new Model(page), command);
    }
```

**How it works:**

1. The runtime dispatches `UrlChanged(url)` whenever the browser URL changes
2. `Transition` delegates to `HandleUrlChanged`, which calls the pure `Route.FromUrl`
3. The returned page becomes the new model; any commands trigger data loading

### View with Navigation Links

```csharp
    public static Document View(Model model)
    {
        var title = model.CurrentPage switch
        {
            Page.Home => "Home",
            Page.About => "About",
            Page.UserProfile p => $"{p.Username}'s Profile",
            _ => "Not Found"
        };

        return new(title,
            div([],
            [
                Nav(),
                Content(model.CurrentPage)
            ]));
    }

    static Node Nav() =>
        nav([class_("navbar")],
        [
            a([href("/")], [text("Home")]),
            a([href("/about")], [text("About")]),
            a([href("/users/alice")], [text("Alice's Profile")])
        ]);

    static Node Content(Page page) =>
        page switch
        {
            Page.Home => div([], [h1([], [text("Welcome Home")])]),
            Page.About => div([], [h1([], [text("About Us")])]),
            Page.UserProfile { IsLoading: true } =>
                div([], [text("Loading profile...")]),
            Page.UserProfile p =>
                div([], [h1([], [text($"{p.Username}'s Profile")])]),
            Page.NotFound => div([], [h1([], [text("404 — Not Found")])]),
            _ => text("")
        };
```

**How links work:** Regular `<a href="...">` links are intercepted by the runtime. Instead of triggering a full page reload, the runtime:

1. Prevents the default browser navigation
2. Updates the browser URL via the History API
3. Dispatches `UrlChanged(newUrl)` into your `Transition` function

You don't need to use special link components — regular `a` elements with `href` just work.

### Subscriptions

To receive URL change notifications, subscribe to `Navigation.UrlChanges`:

```csharp
    public static Subscription Subscriptions(Model model) =>
        Navigation.UrlChanges(url => new UrlChanged(url));
}
```

`Navigation.UrlChanges` listens for browser `popstate` events (back/forward navigation) and intercepted link clicks, dispatching them as `UrlChanged` messages.

## Programmatic Navigation

Sometimes you need to navigate in response to an action (e.g., redirect after login). Use navigation commands:

```csharp
// In Transition:
LoginSucceeded =>
    (model with { CurrentUser = user },
     Navigation.PushUrl(new Url(["dashboard"],
         new Dictionary<string, string>(), Option<string>.None)))
```

Available navigation commands:

| Command | Effect |
| --- | --- |
| `Navigation.PushUrl(url)` | Navigate to URL, add to history |
| `Navigation.ReplaceUrl(url)` | Navigate to URL, replace current history entry |
| `Navigation.Back` | Go back one entry |
| `Navigation.Forward` | Go forward one entry |
| `Navigation.ExternalUrl(href)` | Navigate to an external URL (full page load) |

### Push vs. Replace

- **PushUrl**: Adds a new entry to the browser history. The user can press Back to return.
- **ReplaceUrl**: Replaces the current history entry. Useful for redirects where you don't want the user to "go back" to the redirect page.

```csharp
// After login: redirect to dashboard (replace login page in history)
LoginSucceeded =>
    (model with { Session = session },
     Navigation.ReplaceUrl(dashboardUrl))

// After creating an article: navigate to the new article (push to history)
ArticleCreated slug =>
    (model,
     Navigation.PushUrl(new Url(["article", slug],
         new Dictionary<string, string>(), Option<string>.None)))
```

## External Links

For links to external sites, use `Navigation.ExternalUrl`:

```csharp
OpenDocs =>
    (model, Navigation.ExternalUrl("https://docs.example.com"))
```

Or simply use an `<a>` tag with a full URL — the runtime only intercepts same-origin links:

```csharp
a([href("https://docs.example.com"), target_("_blank")],
    [text("Documentation")])
```

## Advanced: Query Parameters

Use the `Query` dictionary on `Url` for search, filters, and pagination:

```csharp
public static (Page Page, Command Command) FromUrl(Url url) =>
    url.Path switch
    {
        ["search"] =>
            (new Page.Search(
                Query: url.Query.GetValueOrDefault("q", ""),
                PageNumber: int.TryParse(
                    url.Query.GetValueOrDefault("page", "1"), out var p) ? p : 1
             ),
             new FetchSearchResults(
                 url.Query.GetValueOrDefault("q", ""),
                 int.TryParse(url.Query.GetValueOrDefault("page", "1"), out var pg) ? pg : 1)),
        // ...
    };
```

## Real-World Example: Conduit Routing

The Conduit demo uses the same pattern at scale:

```csharp
// From Abies.Conduit.App/Route.cs
public static (Page Page, Command Command) FromUrl(
    Url url, Session? session, string apiUrl) =>
    url.Path switch
    {
        [] or [""]             => HomeRoute(session, apiUrl),
        ["login"]              => LoginRoute(),
        ["register"]           => RegisterRoute(),
        ["settings"]           => SettingsRoute(session),
        ["editor"]             => EditorRoute(null, session?.Token, apiUrl),
        ["editor", var slug]   => EditorRoute(slug, session?.Token, apiUrl),
        ["article", var slug]  => ArticleRoute(slug, session?.Token, apiUrl),
        ["profile", var user]  => ProfileRoute(user, false, session?.Token, apiUrl),
        ["profile", var user, "favorites"]
                               => ProfileRoute(user, true, session?.Token, apiUrl),
        _                      => (new Page.NotFound(), Commands.None)
    };
```

Notice how route parameters (`slug`, `user`) are captured directly in the pattern match. No route parameter parsing library needed.

## Testing

```csharp
[Fact]
public void FromUrl_Home_ReturnsHomePage()
{
    var url = new Url([], new Dictionary<string, string>(),
        Option<string>.None);

    var (page, command) = Route.FromUrl(url);

    Assert.IsType<Page.Home>(page);
    Assert.Equal(Commands.None, command);
}

[Fact]
public void FromUrl_UserProfile_ReturnsLoadingPage_AndFetchCommand()
{
    var url = new Url(["users", "alice"],
        new Dictionary<string, string>(), Option<string>.None);

    var (page, command) = Route.FromUrl(url);

    var profile = Assert.IsType<Page.UserProfile>(page);
    Assert.Equal("alice", profile.Username);
    Assert.True(profile.IsLoading);
    Assert.IsType<FetchUser>(command);
}

[Fact]
public void UrlChanged_UpdatesPage()
{
    var model = new Model(new Page.Home());
    var url = new Url(["about"],
        new Dictionary<string, string>(), Option<string>.None);

    var (newModel, _) = App.Transition(model, new UrlChanged(url));

    Assert.IsType<Page.About>(newModel.CurrentPage);
}

[Fact]
public void FromUrl_UnknownPath_ReturnsNotFound()
{
    var url = new Url(["nonexistent", "path"],
        new Dictionary<string, string>(), Option<string>.None);

    var (page, _) = Route.FromUrl(url);

    Assert.IsType<Page.NotFound>(page);
}
```

## Exercises

1. **Add a search page** — Create a `/search?q=term` route that reads the query parameter and triggers a search command.

2. **Protected routes** — Add a `Session?` parameter to your route function. Redirect unauthenticated users to `/login` when they try to access protected pages.

3. **Breadcrumbs** — Build a breadcrumb component that derives navigation links from the current URL path segments.

4. **404 with suggestions** — On the NotFound page, show links to routes that are similar to the attempted path.

## Key Concepts

| Concept | In This Tutorial |
| --- | --- |
| `UrlChanged(Url)` | Built-in message for URL changes |
| `Url.Path` | Array of path segments for pattern matching |
| `Route.FromUrl(url)` | Pure function: URL → (Page, Command) |
| `Navigation.PushUrl` | Programmatic navigation command |
| `Navigation.ReplaceUrl` | Replace current history entry |
| `Navigation.UrlChanges` | Subscription for URL change events |
| Link interception | Regular `<a href>` links are intercepted automatically |

## Next Steps

→ [Tutorial 5: Forms](05-forms.md) — Learn form input handling, validation, and submission