# Routing

Abies includes a small routing toolkit in `Abies.Route`. You can build routes
in two ways:

- Functional parsers (`Route.Parse.Path`)
- ASP.NET-style templates (`Route.Templates`)

## Functional routing

```csharp
using Abies;
using RouteParse = Abies.Route.Parse;

public abstract record Route
{
    public sealed record Home : Route;
    public sealed record Profile(string UserName) : Route;

    public static Parser<Route> Match =>
        RouteParse.Path(
                RouteParse.Segment.Literal("profile"),
                RouteParse.Segment.Parameter("userName"))
            .Map(match => new Profile(match.GetRequired<string>("userName")))
        | RouteParse.Root.Map(_ => new Home());

    public static Route FromUrl(Url url)
    {
        var result = Match.Parse(url.Path.Value);
        return result.Success ? result.Value : new Home();
    }
}
```

`RouteMatch` exposes captured values as typed accessors.

## Template routing

Templates are useful when you want concise route definitions:

```csharp
using RouteTemplates = Abies.Route.Templates;

var router = RouteTemplates.Build<Route>(routes =>
{
    routes.Map("/profile/{userName}", match => new Route.Profile(match.GetRequired<string>("userName")));
    routes.Map("/", _ => new Route.Home());
});

if (router.TryMatch(url.Path.Value, out var route))
{
    // route is typed
}
```

## Choosing a style

- Use functional parsers for maximum control and composition.
- Use templates for readability when routes are straightforward.
