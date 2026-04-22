# Browser Runtime API (`abies.js`)

This page documents the **public ES module exports** from the browser runtime at:

- `Picea.Abies.Browser/wwwroot/abies.js` (**source of truth**)

Abies imports this module as `"Abies"` via `JSHost.ImportAsync("Abies", "../abies.js")`. The exports below are the interop contract used by `Picea.Abies.Browser/Interop.cs`.

## Scope

- ✅ Covered: exported functions in `abies.js` that are part of Abies runtime interop.
- ❌ Not covered: internal helper functions/constants (`applyPatch`, `readLeb128`, opcodes, etc.).

## Public Exports

| Export | Parameters | Return | Called from | Contract |
| --- | --- | --- | --- | --- |
| `applyBinaryBatch` | `(batchData)` | `void` | `Interop.ApplyBinaryBatch` | Receives `JSType.MemoryView` (Span<byte>) and applies a full binary DOM patch batch. |
| `setTitle` | `(title: string)` | `void` | `Interop.SetTitle` | Sets `document.title`. |
| `navigateTo` | `(url: string)` | `void` | `Interop.NavigateTo` | Calls `history.pushState`, then dispatches synthetic `popstate`. |
| `replaceUrl` | `(url: string)` | `void` | `Interop.ReplaceUrl` | Calls `history.replaceState`, then dispatches synthetic `popstate`. |
| `historyBack` | `()` | `void` | `Interop.HistoryBack` | Calls `history.back()`. |
| `historyForward` | `()` | `void` | `Interop.HistoryForward` | Calls `history.forward()`. |
| `externalNavigate` | `(href: string)` | `void` | `Interop.ExternalNavigate` | Sets `window.location.href` (full page navigation). |
| `getCurrentUrl` | `()` | `string` | `Interop.GetCurrentUrl` | Returns `window.location.href`. |
| `getOrigin` | `()` | `string` | `Interop.GetOrigin` | Returns `window.location.origin` or `""` if unavailable. |
| `getSessionStorageItem` | `(key: string)` | `string \| null` | `Interop.GetSessionStorageItem` | Reads sessionStorage. Returns `null` on missing key or storage access failure. |
| `setSessionStorageItem` | `(key: string, value: string)` | `void` | `Interop.SetSessionStorageItem` | Writes sessionStorage. Silently ignores storage failures. |
| `removeSessionStorageItem` | `(key: string)` | `void` | `Interop.RemoveSessionStorageItem` | Removes sessionStorage key. Silently ignores storage failures. |
| `setupEventDelegation` | `()` | `void` | `Interop.SetupEventDelegation` | Registers document-level listeners for supported event types and starts OTel initialization (non-blocking). |
| `setupNavigation` | `()` | `void` | `Interop.SetupNavigation` | Wires popstate + internal anchor-click interception and emits URL changes to .NET callback. |
| `setDispatchCallback` | `(callback: (commandId, eventName, eventData) => void)` | `void` | `Interop.SetDispatchCallback` | Stores .NET `DispatchDomEvent` callback used by delegated event handlers. |
| `setOnUrlChangedCallback` | `(callback: (url) => void)` | `void` | `Interop.SetOnUrlChangedCallback` | Stores .NET `OnUrlChanged` callback used by navigation integration. |
| `renderInitial` | `(rootId: string, html: string)` | `void` | Compatibility export | Sets `root.innerHTML` for a root element. Not currently used by `Picea.Abies.Browser.Runtime.Run`. |

## Callback Contracts

### `setDispatchCallback`

The callback **must** accept:

```text
(commandId: string, eventName: string, eventDataJson: string)
```

- `commandId`: maps to `HandlerRegistry` entry on the .NET side.
- `eventName`: DOM event name (for diagnostics/telemetry context).
- `eventDataJson`: JSON produced by `extractEventData(event)`.

### `setOnUrlChangedCallback`

The callback **must** accept:

```text
(url: string)
```

`url` is the path/search/hash payload emitted by navigation handling.

## Expected Call Order (Runtime Bootstrap)

`Picea.Abies.Browser.Runtime.Run` wires the module in this order:

1. `setDispatchCallback`
2. `setOnUrlChangedCallback`
3. `setupEventDelegation`
4. `setupNavigation`
5. `getCurrentUrl`
6. Ongoing runtime calls (`applyBinaryBatch`, `setTitle`, navigation/storage helpers)

## Stability Expectations

| Surface | Stability |
| --- | --- |
| Exports used by `Picea.Abies.Browser/Interop.cs` | **Stable runtime contract** for Abies consumers. Changes require coordinated updates to both `abies.js` and `Interop.cs`, and should be treated as breaking when behavior/signatures change. |
| `renderInitial` | **Compatibility surface**. Kept exported for backwards compatibility; not part of current primary runtime flow. |
| Internal, non-exported helpers/constants | **Internal implementation details**. May change at any time without notice. |

## Related Docs

- [JavaScript Interop Architecture](./js-interop.md)
- [Runtime API](../api/runtime.md)
- [Runtime Internals](./runtime-internals.md)
