# Debugging Guide

Strategies for debugging Abies applications.

## Overview

Debugging MVU applications follows a predictable pattern:

1. **Identify the symptom** — What's wrong?
2. **Find the message** — What triggered the issue?
3. **Trace the Transition** — How did the model change?
4. **Check the View** — Is the DOM correct?
5. **Verify Commands** — Did side effects execute?

## Distributed Tracing (Recommended)

Abies includes built-in OpenTelemetry tracing that shows the complete flow from user interaction to API response.

### Quick Start

1. Open your app with Aspire AppHost running
2. Go to the Aspire dashboard (Traces tab)
3. Click through your app — traces appear automatically
4. Click a trace to see the full waterfall

### Verbosity Levels

| Level | What's Traced | How to Enable |
| ----- | ------------- | ------------- |
| `user` | UI Events + HTTP (default) | Production default |
| `debug` | Everything (DOM updates, etc.) | `?otel_verbosity=debug` in URL |
| `off` | Nothing | `<meta name="otel-verbosity" content="off">` |

### Runtime Toggle

Open browser console:

```javascript
window.__otel.setVerbosity('debug');
window.__otel.getVerbosity();
await window.__otel.provider.forceFlush();
```

For the complete tracing tutorial, see [Tutorial: Distributed Tracing](../tutorials/08-tracing.md).

## Console Logging

Add temporary logging to trace message flow:

```csharp
public static (Model, Command) Transition(Model model, Message msg)
{
    Console.WriteLine($"[Transition] Message: {msg.GetType().Name}");
    Console.WriteLine($"[Transition] Model before: {model}");

    var (newModel, command) = msg switch
    {
        CounterMessage.Increment => (model with { Count = model.Count + 1 }, Commands.None),
        CounterMessage.Decrement => (model with { Count = model.Count - 1 }, Commands.None),
        _ => (model, Commands.None)
    };

    Console.WriteLine($"[Transition] Model after: {newModel}");
    Console.WriteLine($"[Transition] Command: {command.GetType().Name}");

    return (newModel, command);
}
```

In WASM, logs appear in the browser console (F12). On the server, logs go to stdout.

## Debugging by Platform

### Browser (WASM)

1. **Console** (F12) — `Console.WriteLine` output
2. **Network** — API calls and WebSocket traffic
3. **Elements** — Inspect `data-event-*` attributes for event handlers
4. **Sources** — Set breakpoints in C# files (Blazor debugging)

### Server (InteractiveServer)

1. **Server logs** — `Console.WriteLine` goes to server stdout
2. **Browser DevTools** — WebSocket tab shows DOM patches sent to client
3. **Aspire dashboard** — Full distributed traces
4. **Debugger** — Attach to the Kestrel process normally

## Hot Reload Workflow (Debug)

Use this workflow when iterating on view functions.

Prerequisite (in the app assembly that defines your `Program<TModel, TArg>`):

```csharp
using System.Reflection.Metadata;

#if DEBUG
[assembly: MetadataUpdateHandler(typeof(Picea.Abies.AbiesMetadataUpdateHandler))]
#endif
```

### 1. Start the correct host with `dotnet watch`

Server host:

```bash
dotnet watch run --project MyApp.Server
```

WASM host (split structure):

```bash
dotnet watch run --project MyApp.Wasm.Host
```

WASM host (single-project structure):

```bash
dotnet watch run --project MyApp.Wasm
```

### 2. Edit view code and save

- Change `View` or view helper functions.
- Save the file and wait for hot reload to apply.

### 3. Verify state and UI

- Expected: Abies preserves current model state and re-renders the UI.
- Example: if a counter is at `42`, it should stay at `42` after a pure view change.

### Supported Modes

- Server host sessions (`*.Server`, interactive modes)
- Browser host runtime (`*.Wasm.Host` or `*.Wasm`)

### Known Limitations

- This workflow is for view-function edits.
- Changes to `Initialize`, `Transition`, command/interpreter code, or subscriptions require restart.
- Some .NET edits are not hot-reloadable (rude edits); when that happens, restart the process.

### Release Impact

Release builds are unaffected. Hot reload guidance in this section is Debug workflow only.

## Debugging Transition

### Print Model State

Add a debug panel to your view:

```csharp
static Node DebugPanel(Model model) =>
    details([], [
        summary([], [text("Debug Info")]),
        pre([], [text(System.Text.Json.JsonSerializer.Serialize(model,
            new JsonSerializerOptions { WriteIndented = true }))])
    ]);

public static Document View(Model model) =>
    new("App", div([], [
        MainContent(model),
        #if DEBUG
        DebugPanel(model)
        #endif
    ]));
```

### Trace Message History

```csharp
public record Model(
    int Count,
    List<string> MessageLog  // Debug only
);

public static (Model, Command) Transition(Model model, Message msg)
{
    var logEntry = $"{DateTime.Now:HH:mm:ss.fff} - {msg.GetType().Name}";
    var newLog = model.MessageLog.Append(logEntry).TakeLast(20).ToList();

    var (newModel, command) = msg switch { /* ... */ };

    return (newModel with { MessageLog = newLog }, command);
}
```

## Debugging Commands and Interpreters

```csharp
Interpreter<Command, Message> interpreter = async command =>
{
    Console.WriteLine($"[Interpreter] Executing: {command.GetType().Name}");

    try
    {
        switch (command)
        {
            case LoadArticles:
                var articles = await api.GetArticles();
                Console.WriteLine($"[Interpreter] Loaded {articles.Length} articles");
                return Result<Message[], PipelineError>.Ok([new ArticlesLoaded(articles)]);

            default:
                Console.WriteLine($"[Interpreter] Unhandled command: {command.GetType().Name}");
                return Result<Message[], PipelineError>.Ok([]);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Interpreter] Error: {ex.Message}");
        return Result<Message[], PipelineError>.Ok([new CommandFailed(ex.Message)]);
    }
};
```

## Common Issues

### Event Handler Not Firing

**Symptoms:** Clicking a button does nothing.

**Check:**
1. Inspect the element — does it have a `data-event-click` attribute?
2. Add `Console.WriteLine` in Transition — is the message arriving?
3. Ensure the handler is correctly attached: `onclick(new MyMessage())`

### Model Not Updating

**Symptoms:** UI doesn't reflect expected state.

```csharp
// ❌ Mutation (records are immutable, this is a no-op)
model.Count = model.Count + 1;
return (model, Commands.None);

// ✅ Create new record with `with`
return (model with { Count = model.Count + 1 }, Commands.None);
```

### View Not Reflecting Model

**Symptoms:** Model is correct but UI is stale.

1. Log the model in View: `Console.WriteLine($"View: {model}")`
2. Check if View is pure (no `DateTime.Now`, no external state)
3. For lists, verify that keys/IDs are stable

### API Calls Failing Silently

**Symptoms:** Data never loads, no errors shown.

1. Check Network tab for the request
2. Add try/catch with logging in the interpreter
3. Check for CORS errors in the console
4. Ensure the interpreter returns feedback messages

### Navigation Not Working

**Symptoms:** URL changes but page doesn't update.

1. Verify `Navigation.UrlChanges(...)` is in `Subscriptions`
2. Log in the `UrlChanged` message handler
3. Check route matching logic

## Removing Debug Code

```csharp
#if DEBUG
    Console.WriteLine($"[Transition] {msg.GetType().Name}");
#endif
```

## See Also

- [MVU Architecture](../concepts/mvu-architecture.md) — Understanding message flow
- [Testing](./testing.md) — Catch bugs before they reach production
- [Tutorial: Distributed Tracing](../tutorials/08-tracing.md) — Full tracing setup
