# JavaScript Interop

This document describes how Abies communicates with the browser via JavaScript.

## Overview

Abies runs as WebAssembly in the browser but needs JavaScript for:

- DOM manipulation
- Browser navigation
- Event handling
- Storage APIs
- Subscriptions (timers, WebSocket, etc.)

The interop layer uses .NET's `[JSImport]` and `[JSExport]` attributes for type-safe communication.

## Architecture

```text
┌─────────────────────────────────────────────────────────────────┐
│                         Browser                                  │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                        DOM                                │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              ↑ ↓                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                     abies.js                              │   │
│  │  • Event delegation                                       │   │
│  │  • DOM operations                                         │   │
│  │  • Navigation API                                         │   │
│  │  • Subscriptions                                          │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              ↑ ↓                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                   .NET Interop                            │   │
│  │  [JSImport] ──────→ Call JS from .NET                     │   │
│  │  [JSExport] ←────── Call .NET from JS                     │   │
│  └──────────────────────────────────────────────────────────┘   │
│                              ↑ ↓                                 │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                   Abies Runtime                           │   │
│  │  • Interop.cs  (declarations)                             │   │
│  │  • Runtime.cs  (message dispatch)                         │   │
│  └──────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

## Importing JavaScript Functions

Use `[JSImport]` to call JavaScript from .NET:

```csharp
public static partial class Interop
{
    [JSImport("setAppContent", "abies.js")]
    public static partial Task SetAppContent(string html);
    
    [JSImport("updateAttribute", "abies.js")]
    public static partial Task UpdateAttribute(string id, string name, string value);
}
```

### Naming Convention

The first argument is the JavaScript function name, the second is the module name:

```csharp
[JSImport("functionName", "moduleName.js")]
```

### Return Types

| .NET Type | JavaScript Type |
| --------- | --------------- |
| `void` | `undefined` |
| `Task` | `Promise<void>` |
| `Task<T>` | `Promise<T>` |
| `string` | `string` |
| `int` | `number` |
| `bool` | `boolean` |
| `string?` | `string \| null` |

## Exporting .NET Functions

Use `[JSExport]` to call .NET from JavaScript:

```csharp
public static partial class Runtime
{
    [JSExport]
    public static void Dispatch(string messageId)
    {
        if (_handlers.TryGetValue(messageId, out var message))
        {
            _messageChannel.Writer.TryWrite(message);
        }
    }
    
    [JSExport]
    public static void DispatchData(string messageId, string? json)
    {
        if (_dataHandlers.TryGetValue(messageId, out var entry))
        {
            object? data = json is null 
                ? null 
                : JsonSerializer.Deserialize(json, entry.dataType);
            var message = entry.handler(data);
            _messageChannel.Writer.TryWrite(message);
        }
    }
}
```

### Calling Exported Functions from JS

```javascript
// In abies.js
const runtime = await dotnet.create();
const dispatch = runtime.getAssemblyExports("Abies").Abies.Runtime.Dispatch;

// Call .NET
dispatch("handler-id-123");
```

## DOM Operations

### Setting Content

```csharp
[JSImport("setAppContent", "abies.js")]
public static partial Task SetAppContent(string html);
```

JavaScript implementation:

```javascript
export function setAppContent(html) {
    document.body.innerHTML = html;
}
```

### Updating Attributes

```csharp
[JSImport("updateAttribute", "abies.js")]
public static partial Task UpdateAttribute(string id, string name, string value);

[JSImport("addAttribute", "abies.js")]
public static partial Task AddAttribute(string id, string name, string value);

[JSImport("removeAttribute", "abies.js")]
public static partial Task RemoveAttribute(string id, string name);
```

JavaScript:

```javascript
export function updateAttribute(id, name, value) {
    const el = document.getElementById(id);
    if (el) el.setAttribute(name, value);
}

export function addAttribute(id, name, value) {
    const el = document.getElementById(id);
    if (el) el.setAttribute(name, value);
}

export function removeAttribute(id, name) {
    const el = document.getElementById(id);
    if (el) el.removeAttribute(name);
}
```

### Child Operations

```csharp
[JSImport("addChildHtml", "abies.js")]
public static partial Task AddChildHtml(string parentId, string childHtml);

[JSImport("removeChild", "abies.js")]
public static partial Task RemoveChild(string parentId, string childId);

[JSImport("replaceChildHtml", "abies.js")]
public static partial Task ReplaceChildHtml(string oldNodeId, string newHtml);
```

JavaScript:

```javascript
export function addChildHtml(parentId, html) {
    const parent = document.getElementById(parentId);
    if (parent) parent.insertAdjacentHTML('beforeend', html);
}

export function removeChild(parentId, childId) {
    const child = document.getElementById(childId);
    if (child) child.remove();
}

export function replaceChildHtml(oldId, newHtml) {
    const old = document.getElementById(oldId);
    if (old) {
        old.insertAdjacentHTML('afterend', newHtml);
        old.remove();
    }
}
```

## Event Delegation

Abies uses event delegation for efficient event handling.

### Rendered HTML

Event handlers are rendered as data attributes:

```html
<button id="btn1" data-event-click="handler-123">Click me</button>
<input id="input1" data-event-input="handler-456" />
```

### Event Listener Setup

```javascript
document.addEventListener('click', (e) => {
    const handlerId = e.target.getAttribute('data-event-click');
    if (handlerId) {
        e.preventDefault();
        dispatch(handlerId);
    }
});

document.addEventListener('input', (e) => {
    const handlerId = e.target.getAttribute('data-event-input');
    if (handlerId) {
        const data = JSON.stringify({ value: e.target.value });
        dispatchData(handlerId, data);
    }
});
```

### Event Data

Events that need data (like input values) use `DispatchData`:

```csharp
// Event data types
public record InputEventData(string? Value);
public record CheckboxEventData(bool Checked);
public record KeyboardEventData(string Key, bool CtrlKey, bool ShiftKey, bool AltKey);
```

## Navigation

### Browser History

```csharp
[JSImport("pushState", "abies.js")]
public static partial Task PushState(string url);

[JSImport("replaceState", "abies.js")]
public static partial Task ReplaceState(string url);

[JSImport("back", "abies.js")]
public static partial Task Back(int steps);

[JSImport("forward", "abies.js")]
public static partial Task Forward(int steps);

[JSImport("load", "abies.js")]
public static partial Task Load(string url);
```

JavaScript:

```javascript
export function pushState(url) {
    history.pushState(null, '', url);
}

export function replaceState(url) {
    history.replaceState(null, '', url);
}

export function back(steps) {
    history.go(-steps);
}

export function forward(steps) {
    history.go(steps);
}

export function load(url) {
    window.location.href = url;
}
```

### URL Change Callback

```csharp
[JSImport("onUrlChange", "abies.js")]
public static partial void OnUrlChange(
    [JSMarshalAs<JSType.Function<JSType.String>>] Action<string> handler
);

[JSImport("getCurrentUrl", "abies.js")]
public static partial string GetCurrentUrl();
```

JavaScript:

```javascript
let urlChangeHandler = null;

export function onUrlChange(handler) {
    urlChangeHandler = handler;
}

window.addEventListener('popstate', () => {
    if (urlChangeHandler) {
        urlChangeHandler(window.location.href);
    }
});

export function getCurrentUrl() {
    return window.location.href;
}
```

## Link Interception

Abies intercepts link clicks for client-side routing:

```csharp
[JSImport("onLinkClick", "abies.js")]
internal static partial void OnLinkClick(
    [JSMarshalAs<JSType.Function<JSType.String>>] Action<string> handler
);
```

JavaScript:

```javascript
let linkClickHandler = null;

export function onLinkClick(handler) {
    linkClickHandler = handler;
}

document.addEventListener('click', (e) => {
    const link = e.target.closest('a[href]');
    if (link && linkClickHandler) {
        e.preventDefault();
        linkClickHandler(link.href);
    }
});
```

## Storage

```csharp
[JSImport("setLocalStorage", "abies.js")]
public static partial Task SetLocalStorage(string key, string value);

[JSImport("getLocalStorage", "abies.js")]
public static partial string? GetLocalStorage(string key);

[JSImport("removeLocalStorage", "abies.js")]
public static partial Task RemoveLocalStorage(string key);
```

JavaScript:

```javascript
export function setLocalStorage(key, value) {
    localStorage.setItem(key, value);
}

export function getLocalStorage(key) {
    return localStorage.getItem(key);
}

export function removeLocalStorage(key) {
    localStorage.removeItem(key);
}
```

## Subscriptions

Subscriptions let JavaScript call .NET when external events occur:

```csharp
[JSImport("subscribe", "abies.js")]
internal static partial void Subscribe(string key, string kind, string? data);

[JSImport("unsubscribe", "abies.js")]
internal static partial void Unsubscribe(string key);
```

### Timer Example

```javascript
const subscriptions = new Map();

export function subscribe(key, kind, data) {
    if (kind === 'interval') {
        const interval = JSON.parse(data).milliseconds;
        const id = setInterval(() => {
            dispatchSubscriptionData(key, null);
        }, interval);
        subscriptions.set(key, { kind, id });
    }
    // ... other subscription kinds
}

export function unsubscribe(key) {
    const sub = subscriptions.get(key);
    if (sub) {
        if (sub.kind === 'interval') {
            clearInterval(sub.id);
        }
        subscriptions.delete(key);
    }
}
```

## Error Handling

### Missing Elements

JavaScript gracefully handles missing elements:

```javascript
export function updateAttribute(id, name, value) {
    const el = document.getElementById(id);
    if (!el) {
        console.warn(`Element not found: ${id}`);
        return;
    }
    el.setAttribute(name, value);
}
```

### Missing Handlers

.NET gracefully handles missing handlers:

```csharp
[JSExport]
public static void Dispatch(string messageId)
{
    if (_handlers.TryGetValue(messageId, out var message))
    {
        _messageChannel.Writer.TryWrite(message);
        return;
    }
    // Handler may be gone during DOM updates - ignore
    Debug.WriteLine($"Missing handler: {messageId}");
}
```

## Performance

### Batching

DOM operations return `Task` to allow batching via await:

```csharp
// Applied sequentially
foreach (var patch in patches)
{
    await Operations.Apply(patch);  // Each await allows JS to batch
}
```

### Minimal Data

Event data is serialized as minimal JSON:

```javascript
// Only send needed data
const data = JSON.stringify({ value: e.target.value });
dispatchData(handlerId, data);
```

### Event Delegation

Single event listeners handle all elements:

```javascript
// One listener for all clicks, not one per button
document.addEventListener('click', handleClick);
```

## See Also

- [API: Runtime](../api/runtime.md) — Runtime API
- [Reference: Runtime Internals](./runtime-internals.md) — Internal implementation
- [ADR-011: JavaScript Interop](../adr/ADR-011-javascript-interop.md) — Design decision
