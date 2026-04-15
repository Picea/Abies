# JavaScript Interop Architecture

Abies uses .NET's `System.Runtime.InteropServices.JavaScript` (`JSImport`/`JSExport`) to bridge C# and the browser. All interop flows through a single JavaScript module and a binary batch protocol — there is no JSON serialization, no runtime reflection, and no per-event round-trip overhead.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│ .NET (WASM)                                                 │
│                                                             │
│  Program.Transition ──▶ View ──▶ Diff ──▶ RenderBatchWriter │
│       ▲                                        │            │
│       │                              binary batch (byte[])  │
│       │                                        │            │
│  HandlerRegistry.CreateMessage                 ▼            │
│       ▲                              ApplyBinaryBatch       │
│       │                              [JSImport]             │
├───────┼────────────────────────────────────┼─────────────────┤
│       │         JS Module "Abies"         │                 │
│       │                                   ▼                 │
│  DispatchDomEvent                  Parse binary batch       │
│  [JSExport]                        Apply DOM mutations      │
│       ▲                                                     │
│       │                                                     │
│  Event delegation (document-level listeners)                │
│  data-event-{name} attribute ──▶ commandId lookup           │
└─────────────────────────────────────────────────────────────┘
```

Key design decisions:

- **One binary batch per render cycle** — all DOM mutations from a single transition are serialized into one `Span<byte>` and applied in a single `JSImport` call
- **Event delegation** — one document-level listener per event type, not per-element listeners
- **Self-bootstrapping** — `abies.js` imports `dotnet.js` and calls `dotnet.run()` automatically, so the consumer's `index.html` only needs `<script type="module" src="abies.js"></script>`

## Module Loading

The browser runtime loads the JavaScript module during startup:

```csharp
await JSHost.ImportAsync("Abies", "../abies.js");
```

The module name `"Abies"` is used in all `[JSImport]` and `[JSExport]` attributes to bind .NET methods to JavaScript functions.

## JSImport Functions (.NET → JavaScript)

These are C# methods that call into JavaScript. Defined in `Abies.Browser/Interop.cs`:

### DOM Patching

```csharp
[JSImport("applyBinaryBatch", "Abies")]
internal static partial void ApplyBinaryBatch(Span<byte> data);
```

Receives the entire patch batch as a binary `Span<byte>` via `JSType.MemoryView` (zero-copy transfer). The JavaScript side parses the binary format and applies DOM mutations.

### Title Management

```csharp
[JSImport("setTitle", "Abies")]
internal static partial void SetTitle(string title);
```

Sets `document.title`. Called by the runtime when `Document.Title` changes between renders.

### Navigation

```csharp
[JSImport("navigateTo", "Abies")]
internal static partial void NavigateTo(string url);

[JSImport("replaceUrl", "Abies")]
internal static partial void ReplaceUrl(string url);

[JSImport("historyBack", "Abies")]
internal static partial void HistoryBack();

[JSImport("historyForward", "Abies")]
internal static partial void HistoryForward();

[JSImport("externalNavigate", "Abies")]
internal static partial void ExternalNavigate(string href);
```

These map directly to `NavigationCommand` variants:

| NavigationCommand | JSImport | Browser API |
|---|---|---|
| `Push(url)` | `NavigateTo` | `history.pushState` |
| `Replace(url)` | `ReplaceUrl` | `history.replaceState` |
| `GoBack` | `HistoryBack` | `history.back()` |
| `GoForward` | `HistoryForward` | `history.forward()` |
| `External(href)` | `ExternalNavigate` | `window.location.href =` |

### Event System Setup

```csharp
[JSImport("setupEventDelegation", "Abies")]
internal static partial void SetupEventDelegation();

[JSImport("setupNavigation", "Abies")]
internal static partial void SetupNavigation();

[JSImport("getCurrentUrl", "Abies")]
internal static partial string GetCurrentUrl();
```

### Callback Wiring

```csharp
[JSImport("setDispatchCallback", "Abies")]
internal static partial void SetDispatchCallback(
    [JSMarshalAs<JSType.Function<JSType.String, JSType.String, JSType.String>>]
    Action<string, string, string> callback);

[JSImport("setOnUrlChangedCallback", "Abies")]
internal static partial void SetOnUrlChangedCallback(
    [JSMarshalAs<JSType.Function<JSType.String>>]
    Action<string> callback);
```

These pass .NET function pointers to JavaScript so the JS event system can call back into .NET. The dispatch callback receives `(commandId, eventName, eventData)`. The URL-changed callback receives the new URL string.

## JSExport Functions (JavaScript → .NET)

These are C# methods callable from JavaScript:

### Event Dispatch

```csharp
[JSExport]
internal static void DispatchDomEvent(string commandId, string eventName, string eventData)
{
    if (Handlers is null) return;

    var message = Handlers.CreateMessage(commandId, eventData);
    if (message is not null)
    {
        Handlers.Dispatch?.Invoke(message);
    }
}
```

When a DOM event fires, JavaScript's event delegation system reads the `data-event-{eventName}` attribute from the target element to get the `commandId`, extracts event data (e.g., `input.value`), and calls this function.

The `HandlerRegistry` looks up the handler by `commandId` and creates the appropriate `Message`.

### URL Change Notification

```csharp
[JSExport]
internal static void OnUrlChanged(string url)
{
    NavigationCallbacks.HandleUrlChanged(url);
}
```

Called when the browser URL changes (popstate, link click interception). Parses the URL string into a `Url` record and dispatches it through the navigation subscription.

## Event Delegation System

Abies uses **event delegation** rather than per-element listeners. This is the same pattern used by React and virtual-DOM frameworks:

1. **Single listener per event type** — JavaScript registers one `click` listener at the document level, one `input` listener, etc.
2. **Attribute-based routing** — Each element that handles events has a `data-event-{eventName}="{commandId}"` attribute rendered into the HTML
3. **Bubbling** — When an event fires, it bubbles up to the document listener. The listener walks up the DOM tree looking for `data-event-{eventName}` attributes
4. **CommandId lookup** — The `commandId` from the attribute is passed to `DispatchDomEvent`, which looks up the handler in the `HandlerRegistry`

### Handler Record

The `Handler` record carries everything needed to process an event:

```csharp
public record Handler(
    string EventName,      // DOM event name ("click", "input", etc.)
    string CommandId,       // Unique ID linking DOM attribute to handler
    Message? Command,       // Static message (null for data-carrying handlers)
    string Id,              // Attribute node ID (from Praefixum)
    Func<object?, Message>? WithData = null,      // Factory for data-carrying messages
    Func<string, object?>?  Deserializer = null)   // Trim-safe JSON deserializer
    : Attribute(Id, $"data-event-{EventName}", CommandId);
```

Handlers support two modes:

| Mode | Fields Used | Example |
|---|---|---|
| **Static** | `Command` is set | `onclick(() => new Increment())` — dispatches the same message every time |
| **Data-carrying** | `WithData` + `Deserializer` | `oninput<InputEventData>(e => new TextChanged(e.Value))` — deserializes event data, passes to factory |

The `Deserializer` field is a `Func<string, object?>` that uses a source-generated `JsonSerializerContext` for trim-safe deserialization. No reflection is used.

### HandlerRegistry

The `HandlerRegistry` is a per-runtime instance that maps `commandId` strings to `Handler` records:

```csharp
public sealed class HandlerRegistry
{
    private readonly Dictionary<string, Handler> _handlers = new();
    internal Action<Message>? Dispatch { get; set; }

    public void Register(Handler handler);
    public void Unregister(string commandId);
    public Message? CreateMessage(string commandId, string eventData);
    public void RegisterHandlers(Node? node);    // Recursive tree scan
    public void UnregisterHandlers(Node? node);  // Recursive tree scan
    internal void Clear();
}
```

Key design points:

- **Per-runtime isolation** — Each `Runtime<TProgram,TModel,TArgument>` owns its own `HandlerRegistry`, enabling concurrent server-side sessions with isolated handler state
- **Non-concurrent dictionary** — Uses `Dictionary<string, Handler>` (not `ConcurrentDictionary`) because the MVU loop is serialized: only one transition runs at a time per runtime instance
- **Recursive registration** — `RegisterHandlers` and `UnregisterHandlers` walk the virtual DOM tree (including `MemoNode` and `LazyMemoNode` wrappers) to batch-register/unregister all handlers in a subtree
- **CreateMessage logic** — Checks `Command` first (static handler), then `WithData`/`Deserializer` (data-carrying handler). Returns `null` if the `commandId` is not found

### Event Flow (Complete Path)

```
User clicks button
  │
  ▼
document click listener fires (event delegation)
  │
  ▼
JS walks up DOM tree, finds data-event-click="cmd_42"
  │
  ▼
JS extracts event data (if needed)
  │
  ▼
JS calls dispatchDomEvent("cmd_42", "click", "")
  │
  ▼
[JSExport] DispatchDomEvent receives (commandId, eventName, eventData)
  │
  ▼
HandlerRegistry.CreateMessage("cmd_42", "")
  ├── handler.Command is set → return the static Message
  └── handler.WithData is set → deserialize eventData, call factory
  │
  ▼
Handlers.Dispatch?.Invoke(message)
  │
  ▼
Runtime._core.Dispatch(message)
  │
  ▼
TProgram.Transition(model, message) → (newModel, command)
  │
  ▼
Observer: View → Diff → Binary batch → ApplyBinaryBatch
```

## Binary Batch Protocol

The `RenderBatchWriter` serializes all patches from a render cycle into a compact binary format transferred via `Span<byte>` (zero-copy `MemoryView`).

### Format

```
┌──────────────────────────────────────────┐
│ Header (8 bytes)                         │
│   PatchCount:        int32 LE (4 bytes)  │
│   StringTableOffset: int32 LE (4 bytes)  │
├──────────────────────────────────────────┤
│ Patch Entries (16 bytes each)            │
│   Type:   int32 LE — BinaryPatchType     │
│   Field1: int32 LE — string table index  │
│   Field2: int32 LE — string table index  │
│   Field3: int32 LE — string table index  │
│   (unused fields = -1)                   │
├──────────────────────────────────────────┤
│ String Table                             │
│   LEB128-length-prefixed UTF-8 strings   │
│   Deduplicated (same string = same idx)  │
└──────────────────────────────────────────┘
```

### Design Decisions

- **3 fields per entry** — covers the widest patch (e.g., `MoveChild`: parentId, childId, beforeId). Narrower patches leave trailing fields as `-1`
- **Fixed-size entries** — O(1) random access, trivial `DataView` parsing in JavaScript
- **String deduplication** — Element IDs are frequently repeated (parent + child). Dedup via `Dictionary<string, int>` reduces payload size significantly
- **LEB128 length encoding** — compact for short strings (IDs, tag names), no wasted bytes on alignment padding

### Binary Patch Protocol Contract (C# ↔ JavaScript)

Each patch entry is `20` bytes:

- `type` (`int32`, little-endian) — opcode value from `BinaryPatchType`
- `field1` (`int32`) — string table index or `-1`
- `field2` (`int32`) — string table index or `-1`
- `field3` (`int32`) — string table index or `-1`
- `field4` (`int32`) — string table index or `-1`

All patch fields are string-table indices. `-1` means null/unused.

The protocol must stay synchronized across:

- `/home/runner/work/Abies/Abies/Picea.Abies/RenderBatchWriter.cs` (`BinaryPatchType`, `WritePatch`)
- `/home/runner/work/Abies/Abies/Picea.Abies.Browser/wwwroot/abies.js` (`OP_*`, `applyBinaryBatch`)
- `/home/runner/work/Abies/Abies/Picea.Abies.Server.Kestrel/wwwroot/_abies/abies-server.js` (`OP_*`, batch reader)

### Patch Type Opcodes and Field Mapping

| Opcode | Type | field1 | field2 | field3 | field4 |
|---|---|---|---|---|---|
| 0 | `AddRoot` | elementId | html | — | — |
| 1 | `ReplaceChild` | oldId | newId | html | — |
| 2 | `AddChild` | parentId | childId | html | — |
| 3 | `RemoveChild` | parentId | childId | — | — |
| 4 | `ClearChildren` | parentId | — | — | — |
| 5 | `SetChildrenHtml` | parentId | html | — | — |
| 6 | `MoveChild` | parentId | childId | beforeId | — |
| 7 | `UpdateAttribute` | elementId | name | value | — |
| 8 | `AddAttribute` | elementId | name | value | — |
| 9 | `RemoveAttribute` | elementId | name | — | — |
| 10 | `AddHandler` | elementId | name | commandId | — |
| 11 | `RemoveHandler` | elementId | name | commandId | — |
| 12 | `UpdateHandler` | elementId | oldName | newCommandId | — |
| 13 | `UpdateText` | parentId | nodeId | text | newId |
| 14 | `AddText` | parentId | value | id | — |
| 15 | `RemoveText` | parentId | id | — | — |
| 16 | `AddRaw` | parentId | html | id | — |
| 17 | `RemoveRaw` | parentId | id | — | — |
| 18 | `ReplaceRaw` | oldId | newId | html | — |
| 19 | `UpdateRaw` | nodeId | html | newId | — |
| 20 | `AddHeadElement` | key | html | — | — |
| 21 | `UpdateHeadElement` | key | html | — | — |
| 22 | `RemoveHeadElement` | key | — | — | — |
| 23 | `AppendChildrenHtml` | parentId | html | — | — |

### Contributor Update Checklist (Protocol Changes)

Use this checklist for any opcode/field/encoding change:

1. Update `BinaryPatchType` and `RenderBatchWriter.WritePatch(...)` in `/home/runner/work/Abies/Abies/Picea.Abies/RenderBatchWriter.cs`.
2. Update opcode constants and decode/apply logic in `/home/runner/work/Abies/Abies/Picea.Abies.Browser/wwwroot/abies.js`.
3. Apply the same opcode and decode/apply changes in `/home/runner/work/Abies/Abies/Picea.Abies.Server.Kestrel/wwwroot/_abies/abies-server.js`.
4. Update protocol docs in this file (contract + opcode table + changed field semantics).
5. Run protocol tests: `/home/runner/work/Abies/Abies/Picea.Abies.Tests/RenderBatchWriterTests.cs`.
6. If `Picea.Abies.Browser/wwwroot/abies.js` changed, verify downstream synced copies/templates:
   - `/home/runner/work/Abies/Abies/Picea.Abies.Presentation/wwwroot/abies.js`
   - `/home/runner/work/Abies/Abies/Picea.Abies.SubscriptionsDemo/wwwroot/abies.js`
   - `/home/runner/work/Abies/Abies/Picea.Abies.UI.Demo/wwwroot/abies.js`
   - `/home/runner/work/Abies/Abies/Picea.Abies.Templates/templates/abies-browser/wwwroot/abies.js`
   - `/home/runner/work/Abies/Abies/Picea.Abies.Templates/templates/abies-browser-empty/wwwroot/abies.js`

## Navigation Interop

Navigation commands flow from the MVU loop through the runtime's built-in interpreter to JavaScript interop calls:

```csharp
// In Browser/Runtime.cs:
void NavigationExecutor(NavigationCommand command)
{
    switch (command)
    {
        case NavigationCommand.Push push:
            Interop.NavigateTo(push.Url.ToRelativeUri());
            break;
        case NavigationCommand.Replace replace:
            Interop.ReplaceUrl(replace.Url.ToRelativeUri());
            break;
        case NavigationCommand.GoBack:
            Interop.HistoryBack();
            break;
        case NavigationCommand.GoForward:
            Interop.HistoryForward();
            break;
        case NavigationCommand.External ext:
            Interop.ExternalNavigate(ext.Href);
            break;
    }
}
```

URL changes flow back from JavaScript to .NET via the `OnUrlChanged` JSExport, which dispatches through `NavigationCallbacks` to the `Navigation.UrlChanges` subscription.

## Browser Runtime Bootstrap

The `Abies.Browser.Runtime.Run<TProgram,TModel,TArgument>()` method performs the complete bootstrap sequence:

1. Load `abies.js` via `JSHost.ImportAsync("Abies", "../abies.js")`
2. Wire dispatch callback: `SetDispatchCallback(DispatchDomEvent)`
3. Wire URL-changed callback: `SetOnUrlChangedCallback(OnUrlChanged)`
4. Set up event delegation at document level
5. Set up navigation interception (popstate + link clicks)
6. Create `RenderBatchWriter` and `BrowserApply` delegate
7. Parse current URL for initial routing
8. Start core `Runtime<TProgram,TModel,TArgument>.Start(...)`
9. Wire `Interop.Handlers = runtime.Handlers` (static reference for JSExport)
10. Block with `Task.Delay(Timeout.Infinite)` to keep WASM alive

This replaces ~30 lines of boilerplate that every WASM consumer previously needed. The consumer's `Program.cs` is a single line:

```csharp
await Abies.Browser.Runtime.Run<CounterProgram, CounterModel, Unit>();
```

## Source Files

| File | Role |
|---|---|
| `Abies.Browser/Interop.cs` | `[JSImport]`/`[JSExport]` declarations |
| `Abies.Browser/Runtime.cs` | Browser bootstrap sequence |
| `Abies.Browser/wwwroot/abies.js` | JavaScript runtime (event delegation, DOM patching, navigation) |
| `Abies/HandlerRegistry.cs` | CommandId → Handler mapping |
| `Abies/RenderBatchWriter.cs` | Binary patch serializer |
| `Abies/DOM/Attribute.cs` | `Handler` record definition |
