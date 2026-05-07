# Browser Runtime API Reference (abies.js)

This document defines the public browser runtime surface in:

- `Picea.Abies.Browser/wwwroot/abies.js` (canonical source)

Important repository rule:

- Do not document or edit copied `abies.js` files in sample/template projects.
- Build sync copies the canonical file to consuming projects.

## Audience and Scope

This page is for contributors changing browser runtime behavior or C# interop bindings.

Scope:

- ES module exports in `abies.js`
- C# interop calls in `Picea.Abies.Browser/Interop.cs`
- JS to .NET callbacks used by event dispatch and navigation

Out of scope:

- Debugger module exports from `AbiesDebugger` (documented separately)

## Stability Contract

Use these expectations when changing the API:

| Surface | Stability | Notes |
| --- | --- | --- |
| Exports used by `Interop.cs` `[JSImport]` | High | Treat as compatibility contract between C# and JS. Change only with synchronized C# + JS update. |
| Callback shape for `setDispatchCallback` and `setOnUrlChangedCallback` | High | Signature mismatches break runtime event/navigation flow. |
| Internal helpers not exported | Internal | Refactor freely if external contracts stay intact. |
| Boot guard `globalThis.__ABIES_DOTNET_STARTED` | Medium | Required for InteractiveWasm/Auto double-start prevention. |
| Observability globals (`window.__otel`, `window.__ABIES_DEBUG_HANDLERS`) | Medium | Operational tooling contract; avoid breaking without migration note. |

## Export Surface

All exports below are defined in `Picea.Abies.Browser/wwwroot/abies.js`.

### DOM and Rendering

| Export | Called from .NET | Contract |
| --- | --- | --- |
| `applyBinaryBatch(batchData)` | `Interop.ApplyBinaryBatch` | `batchData` is `JSType.MemoryView` from `Span<byte>`. JS must copy to stable bytes via `batchData.slice()` before interop returns. Parses header + entries + string table and applies patches in order. |
| `renderInitial(rootId, html)` | Not currently imported from `Interop.cs` | Sets `innerHTML` on `document.getElementById(rootId)` when found. Kept as runtime utility export. |
| `setTitle(title)` | `Interop.SetTitle` | Sets `document.title`. |

### Navigation

| Export | Called from .NET | Contract |
| --- | --- | --- |
| `navigateTo(url)` | `Interop.NavigateTo` | Calls `history.pushState(null, "", url)` then dispatches synthetic `PopStateEvent("popstate")`. |
| `replaceUrl(url)` | `Interop.ReplaceUrl` | Calls `history.replaceState(null, "", url)` then dispatches synthetic `PopStateEvent("popstate")`. |
| `historyBack()` | `Interop.HistoryBack` | Calls `history.back()`. |
| `historyForward()` | `Interop.HistoryForward` | Calls `history.forward()`. |
| `externalNavigate(href)` | `Interop.ExternalNavigate` | Sets `window.location.href` for full navigation. |
| `setupNavigation()` | `Interop.SetupNavigation` | Registers `popstate` listener and internal-link click interception. Uses .NET callback previously registered by `setOnUrlChangedCallback`. |
| `getCurrentUrl()` | `Interop.GetCurrentUrl` | Returns `window.location.href`. |
| `getOrigin()` | `Interop.GetOrigin` | Returns `globalThis.location?.origin ?? ""`. |

### Event and Runtime Wiring

| Export | Called from .NET | Contract |
| --- | --- | --- |
| `setupEventDelegation()` | `Interop.SetupEventDelegation` | Registers document-level listeners for all `COMMON_EVENT_TYPES`; starts optional OTel init. |
| `setDispatchCallback(callback)` | `Interop.SetDispatchCallback` | Expects callable matching `(commandId: string, eventName: string, eventData: string) => void`. Usually receives JS-exported `DispatchDomEvent`. |
| `setOnUrlChangedCallback(callback)` | `Interop.SetOnUrlChangedCallback` | Expects callable matching `(url: string) => void`. Usually receives JS-exported `OnUrlChanged`. |

### Session Storage Helpers

| Export | Called from .NET | Contract |
| --- | --- | --- |
| `getSessionStorageItem(key)` | `Interop.GetSessionStorageItem` | Returns `string | null`; swallows storage exceptions. |
| `setSessionStorageItem(key, value)` | `Interop.SetSessionStorageItem` | Writes storage key/value; swallows storage exceptions. |
| `removeSessionStorageItem(key)` | `Interop.RemoveSessionStorageItem` | Removes storage key; swallows storage exceptions. |

## Callback Contracts (JS -> .NET)

The JS runtime receives .NET callbacks through setter exports, then invokes them from event/navigation flow.

### Dispatch callback

- Set by: `setDispatchCallback`
- Function shape: `(commandId, eventName, eventData) => void`
- Source .NET method: `Interop.DispatchDomEvent`
- Required behavior: must not throw for unknown `commandId`; handler lookup may yield no message.

### URL changed callback

- Set by: `setOnUrlChangedCallback`
- Function shape: `(url) => void`
- Source .NET method: `Interop.OnUrlChanged`
- Input format: relative path emitted by navigation code, or browser URL-derived value.

## Startup and Boot Contract

`abies.js` is a self-bootstrapping module.

- It imports `./_framework/dotnet.js`.
- If `globalThis.__ABIES_DOTNET_STARTED` is not set, it sets the flag and calls `dotnet.run()` (without top-level await).

Do not convert this to top-level awaited startup. The runtime intentionally avoids deadlock between module evaluation and `JSHost.ImportAsync`.

## Change Rules for Contributors

1. If you rename or remove an exported function used by `[JSImport]`, update `Picea.Abies.Browser/Interop.cs` in the same commit.
2. If you change callback parameter order or count, update both JS setter docs and C# delegate marshalling attributes.
3. If you add a new stable export, add it to this page and wire it from `Interop.cs` or explicitly mark it internal-only.
4. Keep comments and naming aligned with actual behavior in `abies.js`.

## Related References

- [JavaScript Interop Architecture](./js-interop.md)
- [Binary Patch Protocol Maintenance Guide](./binary-patch-protocol.md)
- [Runtime Internals](./runtime-internals.md)
