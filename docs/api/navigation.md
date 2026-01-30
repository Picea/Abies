# Navigation API Reference

The `Navigation` module provides commands for browser history manipulation.

## Usage

```csharp
using Abies;
```

## Navigation Commands

All navigation is done through commands returned from `Update`:

```csharp
public static (Model, Command) Update(Message msg, Model model)
    => msg switch
    {
        GoHome => (model, new Navigation.Command.PushState(Url.Create("/"))),
        _ => (model, Commands.None)
    };
```

## Command Types

### Navigation.Command.PushState

Navigates to a new URL, adding an entry to browser history (SPA navigation):

```csharp
new Navigation.Command.PushState(Url.Create("/profile/johndoe"))
```

**When to use:**

- Standard navigation between pages
- User-initiated navigation
- Links and buttons that change pages

**Behavior:**

- Adds new entry to history stack
- User can go back to previous page
- Dispatches URL change message

### Navigation.Command.ReplaceState

Replaces the current URL without adding to history:

```csharp
new Navigation.Command.ReplaceState(Url.Create("/login"))
```

**When to use:**

- Redirects (e.g., after login/logout)
- Correcting URLs without adding history
- Replacing error pages

**Behavior:**

- Replaces current history entry
- User cannot go back to replaced URL
- Dispatches URL change message

### Navigation.Command.Load

Performs a full page navigation (not SPA):

```csharp
new Navigation.Command.Load(Url.Create("https://external-site.com"))
```

**When to use:**

- External links
- Full page refresh needed
- OAuth redirects

**Behavior:**

- Full browser navigation
- Page reloads completely
- WebAssembly app restarts (if navigating within app)

### Navigation.Command.Back

Navigates back in history:

```csharp
new Navigation.Command.Back(1)   // Go back 1 page
new Navigation.Command.Back(2)   // Go back 2 pages
```

**Behavior:**

- Same as browser back button
- Does nothing if no history

### Navigation.Command.Forward

Navigates forward in history:

```csharp
new Navigation.Command.Forward(1)   // Go forward 1 page
```

**Behavior:**

- Same as browser forward button
- Does nothing if at end of history

### Navigation.Command.Go

Navigates by a relative position in history:

```csharp
new Navigation.Command.Go(-2)   // Go back 2 pages
new Navigation.Command.Go(1)    // Go forward 1 page
new Navigation.Command.Go(0)    // Reload current page
```

### Navigation.Command.Reload

Reloads the current page:

```csharp
new Navigation.Command.Reload()
```

**Behavior:**

- Full page reload
- WebAssembly app restarts

## Complete Examples

### Basic Navigation

```csharp
public static (Model, Command) Update(Message msg, Model model)
    => msg switch
    {
        NavigateToHome => 
            (model, new Navigation.Command.PushState(Url.Create("/"))),
        
        NavigateToArticle article => 
            (model, new Navigation.Command.PushState(Url.Create($"/article/{article.Slug}"))),
        
        NavigateToProfile profile => 
            (model, new Navigation.Command.PushState(Url.Create($"/profile/{profile.Username}"))),
        
        _ => (model, Commands.None)
    };
```

### Redirect After Action

```csharp
case LoginSuccess success:
    // Replace login page with home (can't go back to login)
    return (
        model with { User = success.User },
        new Navigation.Command.ReplaceState(Url.Create("/"))
    );

case LogoutClicked:
    return (
        model with { User = null },
        new Navigation.Command.ReplaceState(Url.Create("/login"))
    );
```

### External Navigation

```csharp
case OpenExternalLink link:
    return (model, new Navigation.Command.Load(Url.Create(link.Url)));
```

### Handle Link Clicks

The `OnLinkClicked` callback receives `UrlRequest` for all link clicks:

```csharp
public static Message OnLinkClicked(UrlRequest urlRequest)
    => urlRequest switch
    {
        // Internal link - navigate within app
        UrlRequest.Internal @internal => new InternalNavigation(@internal.Url),
        
        // External link - open in new tab or navigate away
        UrlRequest.External external => new ExternalNavigation(external.Url),
        
        _ => throw new NotImplementedException()
    };
```

Then handle in Update:

```csharp
case InternalNavigation nav:
    // Process URL and update route
    var route = ParseRoute(nav.Url);
    return (model with { Route = route }, new Navigation.Command.PushState(nav.Url));

case ExternalNavigation nav:
    // Could open in new tab via JS interop, or navigate away
    return (model, new Navigation.Command.Load(Url.Create(nav.Url)));
```

### Handle URL Changes

The `OnUrlChanged` callback handles browser back/forward:

```csharp
public static Message OnUrlChanged(Url url)
    => new UrlChanged(url);

// In Update
case UrlChanged changed:
    var route = ParseRoute(changed.Url);
    return (model with { Route = route }, Commands.None);
```

### Building URLs with Parameters

```csharp
// Simple path parameter
var url = Url.Create($"/article/{article.Slug}");

// Multiple parameters
var url = Url.Create($"/user/{userId}/posts/{postId}");

// Query parameters
var query = Uri.EscapeDataString(searchTerm);
var url = Url.Create($"/search?q={query}&page={page}");

// Complex URL building
var builder = new UriBuilder
{
    Path = "/articles",
    Query = $"tag={tag}&author={author}&page={page}"
};
var url = Url.Create(builder.ToString());
```

## Navigation Patterns

### Tab/Section Navigation

For tabs within a page, update model state without URL:

```csharp
case TabClicked tab:
    return (model with { ActiveTab = tab.Index }, Commands.None);
```

For URL-tracked tabs:

```csharp
case TabClicked tab:
    return (
        model with { ActiveTab = tab.Index },
        new Navigation.Command.PushState(Url.Create($"/settings/{tab.Name}"))
    );
```

### Scroll Position

Navigation doesn't automatically scroll. Handle in command:

```csharp
public record ScrollToTopCommand : Command;

case NavigateToPage:
    return (model with { Page = page }, Commands.Batch([
        new Navigation.Command.PushState(url),
        new ScrollToTopCommand()
    ]));

// In HandleCommand
case ScrollToTopCommand:
    await js.InvokeVoidAsync("window.scrollTo", 0, 0);
    break;
```

### Modal URLs

Modals can have URLs for direct linking:

```csharp
case OpenModal modal:
    return (
        model with { Modal = modal },
        new Navigation.Command.PushState(Url.Create($"/article/{slug}/edit"))
    );

case CloseModal:
    return (
        model with { Modal = null },
        new Navigation.Command.Back(1)  // Or ReplaceState to article URL
    );
```

## See Also

- [URL API](./url.md) — URL types
- [Route API](./route.md) — URL routing
- [Concepts: Routing](../concepts/routing.md) — Deep dive
