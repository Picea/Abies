# URL API Reference

The `Url` record provides strongly-typed URL representation with immutable components.

## Usage

```csharp
using Abies;
```

## Creating URLs

### Url.Create

Parses a URL string:

```csharp
var url = Url.Create("https://example.com/articles?page=1#section");
var relative = Url.Create("/profile/johndoe");
```

## URL Components

| Property | Type | Description |
| -------- | ---- | ----------- |
| `Scheme` | `Protocol?` | HTTP/HTTPS protocol (null for relative URLs) |
| `Host` | `Host?` | Domain name (null for relative URLs) |
| `Port` | `Port?` | Port number (null if default) |
| `Path` | `Path` | URL path (always present) |
| `Query` | `Query` | Query string including `?` |
| `Fragment` | `Fragment` | Fragment including `#` |

## Component Types

### Protocol

Sum type for URL schemes:

```csharp
public interface Protocol
{
    public sealed record Http : Protocol;
    public sealed record Https : Protocol;
}
```

**Usage:**

```csharp
if (url.Scheme is Protocol.Https)
{
    // Secure connection
}
```

### Host

Domain name wrapper:

```csharp
public record Host(string Value);
```

**Implicit conversion to string:**

```csharp
string hostname = url.Host;  // "example.com"
```

### Port

Port number wrapper:

```csharp
public record Port(int Value);
```

**Null when default:**

- HTTP: 80
- HTTPS: 443

### Path

URL path wrapper:

```csharp
public record Path(string Value);
```

**Always includes leading slash:**

```csharp
string path = url.Path;  // "/articles/my-post"
```

### Query

Query string wrapper:

```csharp
public record Query(string Value);
```

**Includes the `?`:**

```csharp
string query = url.Query;  // "?page=1&sort=date"
```

### Fragment

Fragment wrapper:

```csharp
public record Fragment(string Value);
```

**Includes the `#`:**

```csharp
string fragment = url.Fragment;  // "#section-2"
```

## Examples

### Parse Absolute URL

```csharp
var url = Url.Create("https://api.example.com:8080/users/123?include=posts#bio");

// Components
url.Scheme   // Protocol.Https
url.Host     // "api.example.com"
url.Port     // 8080
url.Path     // "/users/123"
url.Query    // "?include=posts"
url.Fragment // "#bio"
```

### Parse Relative URL

```csharp
var url = Url.Create("/article/my-post?edit=true#comments");

// Components
url.Scheme   // null
url.Host     // null
url.Port     // null
url.Path     // "/article/my-post"
url.Query    // "?edit=true"
url.Fragment // "#comments"
```

### Check Protocol

```csharp
bool isSecure = url.Scheme switch
{
    Protocol.Https => true,
    Protocol.Http => false,
    null => false  // Relative URL
};
```

### Build URLs

While `Url` is immutable, you can construct URL strings:

```csharp
// Simple path
var articleUrl = Url.Create($"/article/{article.Slug}");

// With query parameters
var searchUrl = Url.Create($"/search?q={Uri.EscapeDataString(query)}&page={page}");

// Complex URL
var fullUrl = Url.Create($"https://example.com/api/v1/users/{userId}");
```

### URL in Navigation

```csharp
// In Update function
case NavigateToProfile profile:
    var url = Url.Create($"/profile/{profile.Username}");
    return (model, new Navigation.Command.PushState(url));
```

### URL Matching in Initialize

```csharp
public static (Model, Command) Initialize(Url url, Unit _)
{
    var route = url.Path.Value switch
    {
        "/" => Route.Home,
        "/login" => Route.Login,
        var path when path.StartsWith("/article/") => 
            Route.Article(path.Substring("/article/".Length)),
        _ => Route.NotFound
    };
    
    return (new Model(Route: route), Commands.None);
}
```

## ToString

Converts back to string:

```csharp
var url = Url.Create("https://example.com/path");
string str = url.ToString();  // "https://example.com/path"
```

## Error Handling

Invalid URLs throw `FormatException`:

```csharp
try
{
    var url = Url.Create("not a valid url ://");
}
catch (FormatException ex)
{
    // Handle invalid URL
}
```

## Integration with Routing

URLs work seamlessly with the routing system:

```csharp
var router = Route.Templates.Build<Page>(r => r
    .Map("/", _ => new Page.Home())
    .Map("/article/{slug}", m => new Page.Article(m.GetRequired<string>("slug")))
    .Map("/profile/{username}", m => new Page.Profile(m.GetRequired<string>("username")))
);

// In OnUrlChanged
public static Message OnUrlChanged(Url url)
{
    if (router.TryMatch(url.Path.Value, out var page))
    {
        return new RouteChanged(page);
    }
    return new RouteChanged(new Page.NotFound());
}
```

## See Also

- [Navigation API](./navigation.md) — URL navigation commands
- [Route API](./route.md) — URL routing
- [Concepts: Routing](../concepts/routing.md) — Deep dive
