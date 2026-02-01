````markdown
# Debugging Guide

Strategies for debugging Abies applications.

## Overview

Debugging MVU applications follows a predictable pattern:

1. **Identify the symptom** — What's wrong?
2. **Find the message** — What triggered the issue?
3. **Trace the Update** — How did the model change?
4. **Check the View** — Is the DOM correct?
5. **Verify Commands** — Did side effects execute?

## Distributed Tracing (Recommended)

Abies includes built-in OpenTelemetry tracing that shows the complete flow from user interaction to API response.

### Quick Start

1. Open your app with Aspire AppHost running
2. Go to the Aspire dashboard (Traces tab)
3. Click through your app - traces appear automatically
4. Click a trace to see the full waterfall

### Verbosity Levels

Control how much detail is captured:

| Level | What's Traced | How to Enable |
|-------|---------------|---------------|
| `user` | UI Events + HTTP (default) | Production default |
| `debug` | Everything (DOM updates, etc.) | `?otel_verbosity=debug` in URL |
| `off` | Nothing | `<meta name="otel-verbosity" content="off">` |

### Quick Debug URL

Add `?otel_verbosity=debug` to see all spans including DOM mutations:

```
https://localhost:5209/?otel_verbosity=debug
```

### Runtime Toggle

Open browser console and run:

```javascript
// Enable debug mode
window.__otel.setVerbosity('debug');

// Check current level
window.__otel.getVerbosity();

// Force flush pending spans
await window.__otel.provider.forceFlush();
```

For the complete tracing tutorial, see [Tutorial: Distributed Tracing](../tutorials/08-tracing.md).

## Browser DevTools

### Console Logging

Add temporary logging to trace message flow:

```csharp
public static (Model, Command) Update(Message msg, Model model)
{
    Console.WriteLine($"[Update] Message: {msg.GetType().Name}");
    Console.WriteLine($"[Update] Model before: {model}");
    
    var (newModel, command) = msg switch
    {
        Increment => (model with { Count = model.Count + 1 }, Commands.None),
        Decrement => (model with { Count = model.Count - 1 }, Commands.None),
        _ => (model, Commands.None)
    };
    
    Console.WriteLine($"[Update] Model after: {newModel}");
    Console.WriteLine($"[Update] Command: {command.GetType().Name}");
    
    return (newModel, command);
}
```

View logs in the browser console (F12 → Console).

### Network Tab

Monitor API calls:

1. Open DevTools (F12)
2. Go to Network tab
3. Filter by "Fetch/XHR"
4. Click a request to see:
   - Request headers and body
   - Response status and body
   - Timing information

### Elements Tab

Inspect the rendered DOM:

1. Open DevTools (F12)
2. Go to Elements tab
3. Look for:
   - `id` attributes (Abies assigns unique IDs)
   - `data-event-*` attributes (event handlers)
   - Missing or incorrect elements

## Debugging Update Logic

### Print Model State

Add a debug view component:

```csharp
static Element<Model, Unit> DebugPanel(Model model)
    => details(
        attribute("open", ""),
        summary(text("Debug Info")),
        pre(
            style("background: #f0f0f0; padding: 1rem; font-size: 12px;"),
            text(System.Text.Json.JsonSerializer.Serialize(model, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            }))
        )
    );

public static Document View(Model model)
    => new("App", div(
        MainContent(model),
        DebugPanel(model)  // Add during development
    ));
```

### Trace Message History

Create a message log:

```csharp
public record Model(
    int Count,
    List<string> MessageLog  // Debug only
);

public static (Model, Command) Update(Message msg, Model model)
{
    var logEntry = $"{DateTime.Now:HH:mm:ss.fff} - {msg.GetType().Name}";
    var newLog = model.MessageLog.Append(logEntry).TakeLast(20).ToList();
    
    var (newModel, command) = msg switch
    {
        // ... normal handling
    };
    
    return (newModel with { MessageLog = newLog }, command);
}
```

### Breakpoint Debugging

Use Blazor's debugging support:

1. Run with `dotnet watch`
2. Open browser DevTools
3. Go to Sources tab
4. Find your C# files under `file://`
5. Set breakpoints in Update or View

## Debugging View Issues

### Check Element IDs

Every element has a unique ID:

```html
<div id="abc123">
  <button id="def456" data-event-click="ghi789">Click</button>
</div>
```

If elements aren't updating, IDs may be mismatched during diffing.

### Render to String

Debug virtual DOM structure:

```csharp
var dom = MyPage.View(model);
var html = Abies.DOM.Render.Html(dom.Body);
Console.WriteLine(html);
```

### Check Handler Registration

Verify event handlers are attached:

```csharp
public static Document View(Model model)
{
    var dom = div(
        button(
            onClick(new Increment()),
            text("+")
        )
    );
    
    // Debug: check for handlers
    var element = dom as Abies.DOM.Element;
    foreach (var attr in element?.Attributes ?? [])
    {
        Console.WriteLine($"Attribute: {attr.Name} = {attr.Value}");
    }
    
    return new("App", dom);
}
```

## Debugging Commands

### Log Command Execution

```csharp
public static async Task HandleCommand(Command command, Func<Message, ValueTuple> dispatch)
{
    Console.WriteLine($"[HandleCommand] Executing: {command.GetType().Name}");
    
    try
    {
        switch (command)
        {
            case LoadArticlesCommand load:
                Console.WriteLine($"[HandleCommand] Loading articles...");
                var articles = await api.GetArticles();
                Console.WriteLine($"[HandleCommand] Loaded {articles.Count} articles");
                dispatch(new ArticlesLoaded(articles));
                break;
            // ...
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[HandleCommand] Error: {ex.Message}");
        dispatch(new CommandFailed(ex.Message));
    }
}
```

### Verify Dispatch

Ensure messages are dispatched after commands:

```csharp
case FetchDataCommand:
    var data = await httpClient.GetFromJsonAsync<Data>("/api/data");
    Console.WriteLine($"[HandleCommand] Fetched data, dispatching DataLoaded");
    dispatch(new DataLoaded(data));
    Console.WriteLine($"[HandleCommand] Dispatch complete");
    break;
```

## Debugging Subscriptions

### Log Subscription Lifecycle

```csharp
public static Subscription Subscriptions(Model model)
{
    Console.WriteLine($"[Subscriptions] IsRunning={model.IsTimerRunning}");
    
    if (model.IsTimerRunning)
    {
        Console.WriteLine("[Subscriptions] Starting timer subscription");
        return Every(TimeSpan.FromSeconds(1), () => new Tick());
    }
    
    Console.WriteLine("[Subscriptions] No subscriptions");
    return Subscription.None;
}
```

### Check Subscription Messages

```csharp
public static (Model, Command) Update(Message msg, Model model)
    => msg switch
    {
        Tick t =>
        {
            Console.WriteLine($"[Update] Tick received at {DateTime.Now:HH:mm:ss}");
            return (model with { Seconds = model.Seconds + 1 }, Commands.None);
        },
        // ...
    };
```

## Debugging Navigation

### Log URL Changes

```csharp
public static Message OnUrlChanged(Url url)
{
    Console.WriteLine($"[OnUrlChanged] New URL: {url}");
    Console.WriteLine($"[OnUrlChanged] Path: {url.Path}");
    Console.WriteLine($"[OnUrlChanged] Query: {url.Query}");
    return new UrlChanged(url);
}

public static Message OnLinkClicked(UrlRequest request)
{
    Console.WriteLine($"[OnLinkClicked] Request: {request}");
    return request switch
    {
        UrlRequest.Internal i => new NavigateTo(i.Url),
        UrlRequest.External e => new ExternalLink(e.Url),
        _ => new NoOp()
    };
}
```

### Verify Route Matching

```csharp
Page MatchRoute(string path)
{
    Console.WriteLine($"[MatchRoute] Matching path: {path}");
    
    if (Router.TryMatch(path, out var page, out var match))
    {
        Console.WriteLine($"[MatchRoute] Matched: {page.GetType().Name}");
        foreach (var (key, value) in match.Values)
        {
            Console.WriteLine($"[MatchRoute] Param: {key} = {value}");
        }
        return page;
    }
    
    Console.WriteLine("[MatchRoute] No match, returning NotFound");
    return new Page.NotFound();
}
```

## Common Issues

### Event Handler Not Firing

**Symptoms:** Clicking a button does nothing.

**Causes:**
1. Handler not attached to element
2. Element replaced during render (ID mismatch)
3. Event prevented by parent element

**Debug Steps:**
1. Check `data-event-click` attribute in Elements tab
2. Add console log in Update
3. Check for `onclick` capturing in parent

### Model Not Updating

**Symptoms:** UI doesn't reflect expected state.

**Causes:**
1. Update not returning new model
2. Pattern match not covering message type
3. Using mutation instead of `with`

**Debug Steps:**

```csharp
// Wrong: mutation
model.Count = model.Count + 1;
return (model, Commands.None);

// Right: new record
return (model with { Count = model.Count + 1 }, Commands.None);
```

### View Not Reflecting Model

**Symptoms:** Model is correct but UI is stale.

**Causes:**
1. View function has side effects
2. DOM diff not detecting changes
3. Keyed children with wrong keys

**Debug Steps:**
1. Log model in View function
2. Check if View is pure
3. Verify key attributes on list items

### API Calls Failing Silently

**Symptoms:** Data never loads, no errors shown.

**Causes:**
1. Command not dispatching result message
2. Exception swallowed in HandleCommand
3. CORS issues

**Debug Steps:**
1. Check Network tab for requests
2. Add try/catch with logging
3. Check console for CORS errors

### Navigation Not Working

**Symptoms:** URL changes but page doesn't update.

**Causes:**
1. OnUrlChanged not returning correct message
2. Route not matching
3. Update not handling navigation message

**Debug Steps:**
1. Log in OnUrlChanged
2. Log route matching
3. Add catch-all in Update

## Performance Debugging

### Slow Renders

If updates feel sluggish:

```csharp
public static Document View(Model model)
{
    var sw = System.Diagnostics.Stopwatch.StartNew();
    
    var result = new("App", ActualView(model));
    
    Console.WriteLine($"[View] Rendered in {sw.ElapsedMilliseconds}ms");
    return result;
}
```

### Large DOM Trees

Check virtual DOM size:

```csharp
int CountNodes(Node node) => node switch
{
    Element e => 1 + e.Children.Sum(CountNodes),
    _ => 1
};

var dom = View(model);
Console.WriteLine($"[View] Node count: {CountNodes(dom.Body)}");
```

## Removing Debug Code

Before deploying, remove debug code:

```csharp
#if DEBUG
    Console.WriteLine($"[Update] {msg.GetType().Name}");
#endif
```

Or use a feature flag:

```csharp
public static bool EnableDebugLogging = false;

public static void DebugLog(string message)
{
    if (EnableDebugLogging)
        Console.WriteLine(message);
}
```

## See Also

- [Concepts: MVU Architecture](../concepts/mvu-architecture.md) — Understanding message flow
- [Guide: Testing](./testing.md) — Write tests to catch bugs
- [API: Runtime](../api/runtime.md) — Runtime internals
