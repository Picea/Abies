# Tutorial 4: Routing

This tutorial teaches you to build multi-page applications with URL-based navigation.

**Prerequisites:** [Tutorial 3: API Integration](./03-api-integration.md)

**Time:** 30 minutes

## What You'll Build

A multi-page application with:

- Home, About, and Profile pages
- URL-based navigation
- Dynamic route parameters
- Browser back/forward support

## Routing in Abies

Abies provides two routing approaches:

1. **Parser Combinators** — Maximum control and type safety
2. **Templates** — Concise, ASP.NET-style syntax

Both produce a `RouteMatch` with captured parameters.

## Step 1: Define Routes

First, define your routes as a sum type:

```csharp
public record Route
{
    public sealed record Home : Route;
    public sealed record About : Route;
    public sealed record Profile(string Username) : Route;
    public sealed record Article(int Id) : Route;
    public sealed record NotFound : Route;
}
```

Each variant represents a page in your application.

## Step 2: Create the Router (Template Style)

The simplest approach uses template syntax:

```csharp
using RouteTemplates = Abies.Route.Templates;

public static class Router
{
    private static readonly RouteTemplates.TemplateRouter<Route> _router =
        RouteTemplates.Build<Route>(routes =>
        {
            routes.Map("/", _ => new Route.Home());
            routes.Map("/about", _ => new Route.About());
            routes.Map("/profile/{username}", m => 
                new Route.Profile(m.GetRequired<string>("username")));
            routes.Map("/article/{id:int}", m => 
                new Route.Article(m.GetRequired<int>("id")));
        });

    public static Route FromUrl(Url url)
    {
        if (_router.TryMatch(url.Path.Value, out var route))
            return route;
        return new Route.NotFound();
    }
}
```

Template syntax:

- `{name}` — String parameter
- `{name:int}` — Integer parameter
- `{name:double}` — Double parameter
- `{name?}` — Optional parameter

## Step 3: Create the Router (Parser Style)

For more control, use parser combinators:

```csharp
using RouteParse = Abies.Route.Parse;

public static class Router
{
    public static readonly Parser<Route> Match =
        // /profile/{username}
        RouteParse.Path(
            RouteParse.Segment.Literal("profile"),
            RouteParse.Segment.Parameter("username"))
        .Select(m => (Route)new Route.Profile(m.GetRequired<string>("username")))
        
        // /article/{id}
        | RouteParse.Path(
            RouteParse.Segment.Literal("article"),
            RouteParse.Segment.Parameter<int>("id", RouteParse.Int))
        .Select(m => (Route)new Route.Article(m.GetRequired<int>("id")))
        
        // /about
        | RouteParse.Path(RouteParse.Segment.Literal("about"))
        .Select(_ => (Route)new Route.About())
        
        // / (root)
        | RouteParse.Root.Select(_ => (Route)new Route.Home());

    public static Route FromUrl(Url url)
    {
        var result = Match.Parse(url.Path.Value);
        return result.Success ? result.Value : new Route.NotFound();
    }
}
```

The `|` operator tries parsers in order until one succeeds.

## Step 4: Model with Route

Include the current route in your model:

```csharp
public record Model(Route CurrentRoute, string? ProfileData);
```

## Step 5: Handle URL Changes

Implement `OnUrlChanged` to convert URLs to messages:

```csharp
public record UrlChanged(Url Url) : Message;

public static Message OnUrlChanged(Url url) => new UrlChanged(url);
```

Handle in Update:

```csharp
public static (Model model, Command command) Update(Message message, Model model)
    => message switch
    {
        UrlChanged changed =>
            (model with { CurrentRoute = Router.FromUrl(changed.Url) }, 
             Commands.None),
        
        // ... other messages
        
        _ => (model, Commands.None)
    };
```

## Step 6: Handle Link Clicks

Implement `OnLinkClicked` for internal navigation:

```csharp
public record LinkClicked(UrlRequest Request) : Message;

public static Message OnLinkClicked(UrlRequest urlRequest) => new LinkClicked(urlRequest);
```

Handle in Update:

```csharp
using static Abies.Navigation.Command;

LinkClicked { Request: UrlRequest.Internal @internal } =>
    (model with { CurrentRoute = Router.FromUrl(@internal.Url) },
     new PushState(@internal.Url)),  // Update browser URL

LinkClicked { Request: UrlRequest.External external } =>
    (model, new Load(Url.Create(external.Url))),  // Navigate away
```

## Step 7: Navigation Commands

Abies provides navigation commands:

| Command | Effect |
| ------- | ------ |
| `PushState(Url)` | Navigate, add to history |
| `ReplaceState(Url)` | Navigate, replace history entry |
| `Back(int steps)` | Go back in history |
| `Forward(int steps)` | Go forward in history |
| `Go(int steps)` | Relative history navigation |
| `Load(Url)` | Full page navigation (external) |
| `Reload()` | Reload current page |

Example: Programmatic navigation

```csharp
public record NavigateToProfile(string Username) : Message;

NavigateToProfile nav =>
    (model, new PushState(Url.Create($"/profile/{nav.Username}"))),
```

## Step 8: Build the View

Route to different views based on current route:

```csharp
public static Document View(Model model)
    => new(TitleFor(model.CurrentRoute),
        div([], [
            Navigation(),
            MainContent(model)
        ]));

static string TitleFor(Route route) => route switch
{
    Route.Home => "Home",
    Route.About => "About",
    Route.Profile p => $"{p.Username}'s Profile",
    Route.Article a => $"Article {a.Id}",
    _ => "Not Found"
};

static Node Navigation() =>
    nav([], [
        a([href("/")], [text("Home")]),
        a([href("/about")], [text("About")]),
        a([href("/profile/alice")], [text("Alice's Profile")])
    ]);

static Node MainContent(Model model) =>
    model.CurrentRoute switch
    {
        Route.Home => HomePage(),
        Route.About => AboutPage(),
        Route.Profile p => ProfilePage(p.Username),
        Route.Article a => ArticlePage(a.Id),
        Route.NotFound => NotFoundPage(),
        _ => NotFoundPage()
    };

static Node HomePage() =>
    main([], [h1([], [text("Welcome Home")])]);

static Node AboutPage() =>
    main([], [h1([], [text("About Us")])]);

static Node ProfilePage(string username) =>
    main([], [h1([], [text($"Profile: {username}")])]);

static Node ArticlePage(int id) =>
    main([], [h1([], [text($"Article #{id}")])]);

static Node NotFoundPage() =>
    main([], [
        h1([], [text("404 - Not Found")]),
        a([href("/")], [text("Go Home")])
    ]);
```

## Step 9: Complete Program

```csharp
using Abies;
using Abies.DOM;
using static Abies.Html.Elements;
using static Abies.Html.Attributes;
using static Abies.Html.Events;
using static Abies.Navigation.Command;
using RouteTemplates = Abies.Route.Templates;

await Runtime.Run<App, Arguments, Model>(new Arguments());

public record Arguments;

// Routes
public record Route
{
    public sealed record Home : Route;
    public sealed record About : Route;
    public sealed record Profile(string Username) : Route;
    public sealed record Article(int Id) : Route;
    public sealed record NotFound : Route;
}

// Router
public static class Router
{
    private static readonly RouteTemplates.TemplateRouter<Route> _router =
        RouteTemplates.Build<Route>(routes =>
        {
            routes.Map("/", _ => new Route.Home());
            routes.Map("/about", _ => new Route.About());
            routes.Map("/profile/{username}", m => 
                new Route.Profile(m.GetRequired<string>("username")));
            routes.Map("/article/{id:int}", m => 
                new Route.Article(m.GetRequired<int>("id")));
        });

    public static Route FromUrl(Url url)
    {
        if (_router.TryMatch(url.Path.Value, out var route))
            return route;
        return new Route.NotFound();
    }
}

// Model
public record Model(Route CurrentRoute);

// Messages
public record UrlChanged(Url Url) : Message;
public record LinkClicked(UrlRequest Request) : Message;

public class App : Program<Model, Arguments>
{
    public static (Model, Command) Initialize(Url url, Arguments argument)
        => (new Model(Router.FromUrl(url)), Commands.None);

    public static (Model model, Command command) Update(Message message, Model model)
        => message switch
        {
            UrlChanged changed =>
                (model with { CurrentRoute = Router.FromUrl(changed.Url) }, 
                 Commands.None),
            
            LinkClicked { Request: UrlRequest.Internal @internal } =>
                (model with { CurrentRoute = Router.FromUrl(@internal.Url) },
                 new PushState(@internal.Url)),
            
            LinkClicked { Request: UrlRequest.External external } =>
                (model, new Load(Url.Create(external.Url))),
            
            _ => (model, Commands.None)
        };

    public static Document View(Model model)
        => new(TitleFor(model.CurrentRoute),
            div([class_("app")], [
                nav([], [
                    a([href("/")], [text("Home")]),
                    text(" | "),
                    a([href("/about")], [text("About")]),
                    text(" | "),
                    a([href("/profile/alice")], [text("Alice")]),
                    text(" | "),
                    a([href("/article/42")], [text("Article 42")])
                ]),
                hr([], []),
                model.CurrentRoute switch
                {
                    Route.Home => main([], [h1([], [text("Welcome Home")])]),
                    Route.About => main([], [h1([], [text("About Us")])]),
                    Route.Profile p => main([], [h1([], [text($"Profile: {p.Username}")])]),
                    Route.Article a => main([], [h1([], [text($"Article #{a.Id}")])]),
                    _ => main([], [h1([], [text("404 - Not Found")])])
                }
            ]));

    static string TitleFor(Route route) => route switch
    {
        Route.Home => "Home",
        Route.About => "About",
        Route.Profile p => $"{p.Username}'s Profile",
        Route.Article a => $"Article {a.Id}",
        _ => "Not Found"
    };

    public static Message OnUrlChanged(Url url) => new UrlChanged(url);
    public static Message OnLinkClicked(UrlRequest urlRequest) => new LinkClicked(urlRequest);
    public static Subscription Subscriptions(Model model) => SubscriptionModule.None;
    public static Task HandleCommand(Command command, Func<Message, ValueTuple> dispatch)
        => Task.CompletedTask;
}
```

## What You Learned

| Concept | Application |
| ------- | ----------- |
| Route sum types | Each page is a route variant |
| Template routing | Quick, readable route definitions |
| Parser routing | Composable, type-safe parsing |
| Navigation commands | PushState, ReplaceState, Back, etc. |
| URL handling | OnUrlChanged for browser navigation |
| Link handling | OnLinkClicked for anchor clicks |

## Advanced Topics

### Nested routes

```csharp
public record Route
{
    public sealed record Settings(SettingsTab Tab) : Route;
}

public record SettingsTab
{
    public sealed record Profile : SettingsTab;
    public sealed record Security : SettingsTab;
    public sealed record Notifications : SettingsTab;
}
```

### Route-based data loading

```csharp
UrlChanged changed =>
    var route = Router.FromUrl(changed.Url);
    var command = route switch
    {
        Route.Profile p => new LoadProfile(p.Username),
        Route.Article a => new LoadArticle(a.Id),
        _ => Commands.None
    };
    return (model with { CurrentRoute = route }, command);
```

### Query parameters

```csharp
// Parse ?page=2&sort=date from url.Query
var page = ParseQueryParam(url.Query, "page", 1);
var sort = ParseQueryParam(url.Query, "sort", "date");
```

## Exercises

1. **Add query params**: Support `/articles?page=2`
2. **Protected routes**: Redirect to login if not authenticated
3. **Route transitions**: Add loading states during navigation
4. **Breadcrumbs**: Show navigation path

## Next Tutorial

→ [Tutorial 5: Forms](./05-forms.md) — Learn form handling and validation
