# Runtime Internals

The `Runtime<TProgram,TModel,TArgument>` class orchestrates the MVU execution loop. It composes the Automaton kernel's `AutomatonRuntime` with three MVU-specific concerns: **View** (render + diff + apply), **Subscriptions** (lifecycle management), and **Commands** (side effect interpretation).

## Architecture

```
┌──────────────────────────────────────────────────────────┐
│ Runtime<TProgram, TModel, TArgument>                     │
│                                                          │
│  ┌────────────────────────────────────────────────────┐  │
│  │ AutomatonRuntime (Automaton kernel)                 │  │
│  │                                                    │  │
│  │  Dispatch(message)                                 │  │
│  │    │                                               │  │
│  │    ▼                                               │  │
│  │  TProgram.Transition(model, message)               │  │
│  │    │                                               │  │
│  │    ├──▶ (newModel, command)                        │  │
│  │    │                                               │  │
│  │    ├──▶ Observer(newModel, message, command)       │  │
│  │    │     ├── View → Diff → Apply                  │  │
│  │    │     ├── HeadDiff → Apply                     │  │
│  │    │     ├── UpdateHandlerRegistry                │  │
│  │    │     └── SubscriptionManager.Update           │  │
│  │    │                                               │  │
│  │    └──▶ Interpreter(command)                      │  │
│  │          └── feedback Messages → re-enter loop    │  │
│  └────────────────────────────────────────────────────┘  │
│                                                          │
│  HandlerRegistry (per-runtime instance)                  │
│  SubscriptionState (current subscriptions)               │
│  Document? (current virtual DOM)                         │
└──────────────────────────────────────────────────────────┘
```

## The MVU Loop

Every dispatched `Message` triggers the following sequence:

### 1. Transition

```csharp
static (TModel, Command) Transition(TModel model, Message message);
```

The program's `Transition` function (inherited from `Automaton<TState,TEvent,TEffect,TParameters>`) produces a new model and an optional command. This is a **pure function** — no side effects.

### 2. Observer (View → Diff → Apply)

The observer is an instance method on `Runtime` that runs after every transition:

```csharp
private ValueTask<Result<Unit, PipelineError>> Observe(TModel state, Message _, Command __)
{
    // 1. Render new view
    var newDocument = TProgram.View(state);

    // 2. Diff body against previous
    var patches = Operations.Diff(_currentDocument?.Body, newDocument.Body);

    // 3. Diff head content
    var headPatches = HeadDiff.Diff(
        _currentDocument?.Head ?? [],
        newDocument.Head);

    // 4. Merge body + head patches into single list
    // 5. Update handler registry
    // 6. Apply all patches via single binary batch
    // 7. Update title if changed
    // 8. Update subscriptions
}
```

Key details:

- **Body and head patches are merged** into a single `IReadOnlyList<Patch>` and applied in one `Apply` call. This means one binary batch per render cycle carries both body and head mutations.
- **Handler registry updates happen before apply** — the runtime walks the patch list and registers/unregisters handlers so that event delegation can dispatch messages correctly as soon as the DOM is updated.
- **Title changes are detected** by string comparison and trigger a separate `titleChanged` callback.
- **Subscriptions are reconciled** after every render via `SubscriptionManager.Update`.

### 3. Handler Registry Update

The runtime walks the patch list and updates the `HandlerRegistry` based on what changed:

```csharp
private void UpdateHandlerRegistry(IReadOnlyList<Patch> patches)
{
    foreach (var patch in patches)
    {
        switch (patch)
        {
            case AddHandler p:      _handlerRegistry.Register(p.Handler); break;
            case RemoveHandler p:   _handlerRegistry.Unregister(p.Handler.CommandId); break;
            case UpdateHandler p:
                _handlerRegistry.Unregister(p.OldHandler.CommandId);
                _handlerRegistry.Register(p.NewHandler);
                break;
            case AddChild p:        _handlerRegistry.RegisterHandlers(p.Child); break;
            case AddRoot p:         _handlerRegistry.RegisterHandlers(p.Element); break;
            case ReplaceChild p:
                _handlerRegistry.UnregisterHandlers(p.OldElement);
                _handlerRegistry.RegisterHandlers(p.NewElement);
                break;
            case RemoveChild p:     _handlerRegistry.UnregisterHandlers(p.Child); break;
            case ClearChildren p:
                foreach (var child in p.OldChildren)
                    _handlerRegistry.UnregisterHandlers(child);
                break;
            case SetChildrenHtml p:
                foreach (var child in p.Children)
                    _handlerRegistry.RegisterHandlers(child);
                break;
            case AppendChildrenHtml p:
                foreach (var child in p.Children)
                    _handlerRegistry.RegisterHandlers(child);
                break;
        }
    }
}
```

For tree-level patches (`AddChild`, `ReplaceChild`, `ClearChildren`, `SetChildrenHtml`, `AppendChildrenHtml`), the registry recursively walks the virtual DOM subtree to register/unregister all handlers, including those inside `MemoNode` and `LazyMemoNode` wrappers.

### 4. Command Interpretation

After the observer runs, the Automaton kernel passes the command to the interpreter. The runtime wraps the caller-supplied interpreter with structural command handling:

```csharp
static async ValueTask<Result<Message[], PipelineError>> InterpretCommand(
    Command command,
    Interpreter<Command, Message> interpreter,
    Action<NavigationCommand>? navigationExecutor)
{
    switch (command)
    {
        case Command.None:
            return Result<Message[], PipelineError>.Ok([]);

        case Command.Batch batch:
            var allMessages = new List<Message>();
            foreach (var sub in batch.Commands)
            {
                var result = await InterpretCommand(sub, interpreter, navigationExecutor);
                if (result.IsErr) return result;
                if (result.Value.Length > 0) allMessages.AddRange(result.Value);
            }
            return Result<Message[], PipelineError>.Ok(allMessages.ToArray());

        case NavigationCommand navCommand:
            navigationExecutor?.Invoke(navCommand);
            return Result<Message[], PipelineError>.Ok([]);

        default:
            return await interpreter(command);
    }
}
```

The interpretation order:

1. **`Command.None`** — no-op (identity element of the command monoid)
2. **`Command.Batch`** — recursively interpret each sub-command, collecting all feedback messages
3. **`NavigationCommand`** — handled by the runtime's built-in navigation executor (browser: calls JS interop; server: sends over transport)
4. **All other commands** — fall through to the caller-supplied interpreter

Feedback messages from the interpreter are dispatched back into the MVU loop, triggering new transitions.

## Navigation Commands

Navigation is modeled as `NavigationCommand` records that implement the `Command` interface:

```csharp
public interface NavigationCommand : Command
{
    sealed record Push(Url Url) : NavigationCommand;
    sealed record Replace(Url Url) : NavigationCommand;
    sealed record GoBack : NavigationCommand;
    sealed record GoForward : NavigationCommand;
    sealed record External(string Href) : NavigationCommand;
}
```

Application code uses the `Navigation` static class for convenience:

```csharp
Navigation.PushUrl(url)       // → NavigationCommand.Push(url)
Navigation.ReplaceUrl(url)    // → NavigationCommand.Replace(url)
Navigation.Back               // → NavigationCommand.GoBack
Navigation.Forward             // → NavigationCommand.GoForward
Navigation.ExternalUrl(href)  // → NavigationCommand.External(href)
```

The runtime handles these before the caller-supplied interpreter ever sees them. Application code never needs to pattern-match on navigation commands.

## Head Content Management

The `Document` record carries optional `HeadContent[]` alongside the body:

```csharp
public record Document(string Title, Node Body, params HeadContent[] Head);
```

`HeadContent` is a sum type with variants for each kind of `<head>` element:

| Variant | Key Format | HTML Output |
|---|---|---|
| `Meta(name, content)` | `"meta:{name}"` | `<meta name="..." content="..." data-abies-head="...">` |
| `MetaProperty(property, content)` | `"property:{property}"` | `<meta property="..." content="..." data-abies-head="...">` |
| `Link(rel, href, type?)` | `"link:{rel}:{href}"` | `<link rel="..." href="..." data-abies-head="...">` |
| `Script(type, content)` | `"script:{type}"` | `<script type="..." data-abies-head="...">...</script>` |
| `Base(href)` | `"base"` | `<base href="..." data-abies-head="base">` |

Head diffing uses `HeadDiff.Diff(oldHead, newHead)` which produces standard `Patch` types (`AddHeadElement`, `UpdateHeadElement`, `RemoveHeadElement`) that flow through the same binary batch protocol as body patches.

Convenience factories are available via `using static Abies.Head`:

```csharp
meta("description", "My page")   // <meta name="description" content="My page">
og("title", "My Page")            // <meta property="og:title" content="My Page">
twitter("card", "summary")        // <meta name="twitter:card" content="summary">
canonical("https://example.com")  // <link rel="canonical" href="...">
stylesheet("/css/app.css")        // <link rel="stylesheet" href="...">
jsonLd(structuredData)            // <script type="application/ld+json">...</script>
```

## Startup Sequence

The `Runtime.Start` factory method performs a multi-phase initialization:

```
Phase 1: Create runtime shell (observer needs `this` reference)
    │
    ▼
Phase 2: Wire HandlerRegistry.Dispatch to runtime
    │
    ▼
Phase 3: TProgram.Initialize(argument) → (model, initialCommand)
    │
    ▼
Phase 4: TProgram.View(model) → initial Document
    │
    ▼
Phase 5: Diff(null, document.Body) → AddRoot patch
         HeadDiff.Diff([], document.Head) → head patches
         Merge + RegisterHandlers + Apply
    │
    ▼
Phase 6: Set initial title
    │
    ▼
Phase 7: Create AutomatonRuntime with wrapped interpreter
    │
    ▼
Phase 8: SubscriptionManager.Start(initialSubscriptions)
    │
    ▼
Phase 9: InterpretEffect(initialCommand)
    │
    ▼
Phase 10: Dispatch UrlChanged(initialUrl) if provided
```

**Two-phase initialization** — The runtime instance is constructed before the `AutomatonRuntime` so the observer can be an instance method. This avoids the stale-closure problem where lambda-captured local variables would diverge from the runtime's fields after startup.

## Subscription Lifecycle

Subscriptions are declared as a function of the model:

```csharp
static Subscription Subscriptions(TModel model);
```

The `SubscriptionManager` handles the lifecycle:

- **Start**: called during initialization with the initial subscription set
- **Update**: called after every render. Compares desired subscriptions (by key) against running subscriptions:
  - New keys → start subscription task
  - Removed keys → cancel via `CancellationToken`
  - Unchanged keys → keep running
- **Stop**: called during disposal, cancels all running subscriptions

Subscriptions dispatch messages via a `DispatchFromSubscription` method that calls `_core.Dispatch` (fire-and-forget).

## Thread Safety

| Environment | `threadSafe` | Behavior |
|---|---|---|
| WASM (browser) | `false` | No synchronization overhead. Single-threaded by nature. |
| Server (SSR) | `true` | `SemaphoreSlim` serializes dispatch calls. Required for async I/O. |

Each server-side session creates its own `Runtime` instance with its own `HandlerRegistry`, providing complete isolation between concurrent sessions.

## Observability

The runtime uses `System.Diagnostics.ActivitySource` with the name `"Abies.Runtime"` for OpenTelemetry instrumentation:

| Activity Name | Tags | When |
|---|---|---|
| `Abies.Start` | `abies.program` | Runtime startup |
| `Abies.Render` | `abies.patches` (count) | Each render cycle |
| `Abies.Stop` | — | Runtime disposal |

## Disposal

`Runtime<TProgram,TModel,TArgument>` implements `IDisposable`. Disposal:

1. Stops all running subscriptions via `SubscriptionManager.Stop`
2. Clears the `HandlerRegistry.Dispatch` callback
3. Clears all registered handlers
4. Disposes the underlying `AutomatonRuntime`

## Source Files

| File | Role |
|---|---|
| `Abies/Runtime.cs` | MVU runtime, observer, command interpretation, startup |
| `Abies/Program.cs` | `Program<TModel,TArgument>` interface, `Url`, `UrlChanged`, `UrlRequest` |
| `Abies/Navigation.cs` | `NavigationCommand` types, `Navigation` convenience API, URL subscriptions |
| `Abies/HandlerRegistry.cs` | Per-runtime event handler mapping |
| `Abies/Head.cs` | `HeadContent` sum type and factory functions |
| `Abies/Diff.cs` | Virtual DOM diff algorithm |
| `Abies/RenderBatchWriter.cs` | Binary patch serializer |
| `Abies/Subscriptions/` | Subscription infrastructure |
| `Abies.Browser/Runtime.cs` | Browser-specific bootstrap |
| `Abies.Browser/Interop.cs` | JSImport/JSExport declarations |
