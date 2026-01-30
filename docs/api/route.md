# Route API Reference

The `Route` module provides type-safe URL routing using parser combinators.

## Usage

```csharp
using Abies;
using static Abies.Route.Parse;
```

## Overview

Abies offers two routing styles:

1. **Parser Combinators** — Functional, composable route parsers
2. **Templates** — ASP.NET-style route templates (e.g., `/article/{slug}`)

## Template Routing (Recommended)

The simplest way to define routes:

### Route.Templates.Path

Compiles a route template:

```csharp
var articleRoute = Route.Templates.Path("/article/{slug}");

if (articleRoute.Parse("/article/my-post").Success)
{
    // Matched!
}
```

### Template Syntax

| Pattern | Description | Example |
| ------- | ----------- | ------- |
| `/literal` | Exact match | `/home`, `/about` |
| `/{param}` | String parameter | `/{slug}`, `/{username}` |
| `/{param:int}` | Integer parameter | `/{id:int}` |
| `/{param:double}` | Float parameter | `/{price:double}` |
| `/{param?}` | Optional parameter | `/{page?}` |

### Route.Templates.Build

Creates a type-safe router:

```csharp
public interface Page
{
    record Home : Page;
    record Article(string Slug) : Page;
    record Profile(string Username) : Page;
    record User(int Id) : Page;
    record NotFound : Page;
}

var router = Route.Templates.Build<Page>(r => r
    .Map("/", _ => new Page.Home())
    .Map("/article/{slug}", m => new Page.Article(m.GetRequired<string>("slug")))
    .Map("/profile/{username}", m => new Page.Profile(m.GetRequired<string>("username")))
    .Map("/user/{id:int}", m => new Page.User(m.GetRequired<int>("id")))
);
```

### Using the Router

```csharp
if (router.TryMatch("/article/hello-world", out var page))
{
    // page is Page.Article("hello-world")
}

if (router.TryMatch("/user/42", out page, out var match))
{
    // page is Page.User(42)
    // match.Values contains {"id": 42}
}
```

### Complete Program Integration

```csharp
public class MyProgram : Program<Model, Unit>
{
    static readonly TemplateRouter<Page> Router = Route.Templates.Build<Page>(r => r
        .Map("/", _ => new Page.Home())
        .Map("/article/{slug}", m => new Page.Article(m.GetRequired<string>("slug")))
        .Map("/login", _ => new Page.Login())
    );

    public static (Model, Command) Initialize(Url url, Unit _)
    {
        var page = Router.TryMatch(url.Path.Value, out var p) ? p : new Page.NotFound();
        return (new Model(Page: page), Commands.None);
    }

    public static Message OnUrlChanged(Url url)
        => new UrlChanged(url);

    public static (Model, Command) Update(Message msg, Model model)
        => msg switch
        {
            UrlChanged changed => 
            {
                var page = Router.TryMatch(changed.Url.Path.Value, out var p) 
                    ? p 
                    : new Page.NotFound();
                return (model with { Page = page }, Commands.None);
            },
            NavigateTo nav =>
                (model, new Navigation.Command.PushState(Url.Create(nav.Path))),
            _ => (model, Commands.None)
        };
}
```

## RouteMatch

Result of a successful route match:

### Properties

```csharp
public readonly struct RouteMatch
{
    public IReadOnlyDictionary<string, object?> Values { get; }
    public object? this[string name] { get; }
}
```

### Methods

#### GetRequired<T>

Gets a required parameter (throws if missing):

```csharp
string slug = match.GetRequired<string>("slug");
int id = match.GetRequired<int>("id");
```

#### TryGetValue<T>

Gets an optional parameter:

```csharp
if (match.TryGetValue<int>("page", out var page))
{
    // Use page
}
else
{
    // Default to 1
    page = 1;
}
```

#### Indexer

Access by name (returns null if missing):

```csharp
var value = match["slug"];  // object? 
```

## Parser Combinator Routing

For more complex routing needs:

### Route.Parse.Root

Matches root path only:

```csharp
var rootRoute = Route.Parse.Root;
rootRoute.Parse("/").Success  // true
rootRoute.Parse("/home").Success  // false
```

### Route.Parse.Path

Builds from segments:

```csharp
using Seg = Route.Parse.Segment;

var route = Route.Parse.Path(
    Seg.Literal("article"),
    Seg.Parameter("slug")
);

var result = route.Parse("/article/my-post");
if (result.Success)
{
    var slug = result.Value.GetRequired<string>("slug");
}
```

### Segment Types

#### Literal

Exact match segment:

```csharp
Seg.Literal("articles")  // Matches "articles" exactly
Seg.Literal("API", StringComparison.Ordinal)  // Case-sensitive
```

#### Parameter

String parameter:

```csharp
Seg.Parameter("slug")  // Captures any string
Seg.Parameter("slug", optional: true)  // Optional parameter
```

#### Typed Parameters

```csharp
Seg.Parameter<int>("id", Route.Parse.Int)  // Integer
Seg.Parameter<double>("price", Route.Parse.Double)  // Floating point
```

#### Custom Parser

```csharp
Seg.Parameter<Guid>("id", new GuidParser())
```

### Combining Routes

```csharp
var homeRoute = Route.Parse.Root;
var articlesRoute = Route.Parse.Path(Seg.Literal("articles"));
var articleRoute = Route.Parse.Path(
    Seg.Literal("article"),
    Seg.Parameter("slug")
);

// Try routes in order
Page MatchRoute(string path)
{
    if (homeRoute.Parse(path).Success) return new Page.Home();
    if (articlesRoute.Parse(path).Success) return new Page.Articles();
    
    var articleResult = articleRoute.Parse(path);
    if (articleResult.Success) 
        return new Page.Article(articleResult.Value.GetRequired<string>("slug"));
    
    return new Page.NotFound();
}
```

## Built-in Parsers

### Route.Parse.String

Parses any non-slash characters:

```csharp
var parser = Route.Parse.String;
parser.Parse("hello-world").Value  // "hello-world"
```

### Route.Parse.Int

Parses integers:

```csharp
var parser = Route.Parse.Int;
parser.Parse("42").Value  // 42
parser.Parse("abc").Success  // false
```

### Route.Parse.Double

Parses floating-point numbers:

```csharp
var parser = Route.Parse.Double;
parser.Parse("3.14").Value  // 3.14
```

### Route.Parse.Strict.Int

Returns null for invalid integers instead of failing:

```csharp
var parser = Route.Parse.Strict.Int;
parser.Parse("42").Value  // 42
parser.Parse("abc").Value  // null (but Success is true)
```

## Route Generation

For generating URLs from routes:

```csharp
public static class Routes
{
    public static string Home => "/";
    public static string Articles => "/articles";
    public static string Article(string slug) => $"/article/{slug}";
    public static string Profile(string username) => $"/profile/{username}";
    public static string User(int id) => $"/user/{id}";
}

// Usage
var url = Url.Create(Routes.Article("my-post"));
```

## Query Parameters

Routes focus on paths. For query parameters, access the URL directly:

```csharp
public static (Model, Command) Initialize(Url url, Unit _)
{
    // Parse path
    var page = Router.TryMatch(url.Path.Value, out var p) ? p : new Page.NotFound();
    
    // Parse query parameters
    var query = System.Web.HttpUtility.ParseQueryString(url.Query.Value);
    var searchTerm = query["q"];
    var pageNum = int.TryParse(query["page"], out var n) ? n : 1;
    
    return (new Model(Page: page, Search: searchTerm, PageNumber: pageNum), Commands.None);
}
```

## Best Practices

### 1. Define Routes Centrally

```csharp
public static class AppRoutes
{
    public static readonly TemplateRouter<Page> Router = Route.Templates.Build<Page>(r => r
        .Map("/", _ => new Page.Home())
        .Map("/article/{slug}", m => new Page.Article(m.GetRequired<string>("slug")))
        // ... all routes
    );
    
    // URL generation
    public static string Home => "/";
    public static string Article(string slug) => $"/article/{slug}";
}
```

### 2. Use Sum Types for Pages

```csharp
public interface Page
{
    record Home : Page;
    record Article(string Slug) : Page;
    record NotFound : Page;
}
```

### 3. Handle Not Found

Always have a fallback:

```csharp
var page = router.TryMatch(path, out var p) ? p : new Page.NotFound();
```

### 4. Keep Routes Simple

Complex logic belongs in Update, not routing:

```csharp
// Route just captures the ID
.Map("/user/{id:int}", m => new Page.User(m.GetRequired<int>("id")))

// Update loads user data
case Page.User userPage:
    return (model, new LoadUserCommand(userPage.Id));
```

## See Also

- [URL API](./url.md) — URL types
- [Navigation API](./navigation.md) — Navigation commands
- [Concepts: Routing](../concepts/routing.md) — Deep dive
- [Tutorial: Navigation](../tutorials/05-navigation.md) — Hands-on examples
