// =============================================================================
// Interop — JavaScript ↔ .NET Bridge for Browser WASM
// =============================================================================
// This static partial class provides the [JSImport]/[JSExport] declarations
// that connect the .NET Abies runtime to the browser's DOM via abies.js.
//
// Architecture:
//   .NET → JS (JSImport):
//     ApplyBinaryBatch — applies a binary-encoded batch of DOM patches
//     SetTitle         — sets document.title
//     NavigateTo       — history.pushState for client-side routing
//     SetupEventDelegation — registers event listeners at the root
//     SetupNavigation  — registers popstate + link click interception
//     GetCurrentUrl    — retrieves window.location.href
//
//   JS → .NET (JSExport):
//     DispatchDomEvent — called by event delegation when a DOM event fires
//     OnUrlChanged     — called when browser URL changes
//
// Binary Protocol:
//   Patches are serialized by RenderBatchWriter into a compact binary format:
//
//     Header (8 bytes):
//       PatchCount:       int32 (4 bytes)
//       StringTableOffset: int32 (4 bytes)
//
//     Patch Entries (20 bytes each):
//       Type:  int32 (4 bytes) — BinaryPatchType enum value
//       Field1: int32 (4 bytes) — string table index (-1 = null)
//       Field2: int32 (4 bytes) — string table index (-1 = null)
//       Field3: int32 (4 bytes) — string table index (-1 = null)
//       Field4: int32 (4 bytes) — string table index (-1 = null)
//
//     String Table:
//       LEB128 length prefix + UTF-8 bytes for each string
//       String deduplication via Dictionary lookup
//
//   Transfer uses JSType.MemoryView (Span<byte>) for zero-copy interop.
//   The JS side must call .slice() to get a stable Uint8Array before the
//   interop call returns (the Span is stack-allocated).
//
// Event Delegation:
//   A single listener per event type is registered at the document level.
//   When an event fires, the handler walks up from the target looking for
//   a data-event-{eventType} attribute. The attribute value is the commandId
//   which maps to a Handler in the HandlerRegistry.
//
// See also:
//   - Picea.Abies/RenderBatchWriter.cs — binary serialization
//   - abies.js — browser-side runtime
//   - Runtime.cs — browser runtime bootstrap
// =============================================================================

using System.Runtime.InteropServices.JavaScript;
#if DEBUG
using Picea.Abies.Browser.Debugger;
using Picea.Abies.Debugger;
#endif

namespace Picea.Abies.Browser;

/// <summary>
/// JavaScript interop bridge for the Abies browser runtime.
/// </summary>
/// <remarks>
/// <para>
/// This class is split into two halves:
/// <list type="bullet">
///   <item><b>JSImport</b>: .NET calls into JavaScript (DOM mutations, navigation)</item>
///   <item><b>JSExport</b>: JavaScript calls into .NET (event dispatch)</item>
/// </list>
/// </para>
/// <para>
/// The module name <c>"Abies"</c> corresponds to the JS module loaded via
/// <c>JSHost.ImportAsync("Abies", "../abies.js")</c> at startup.
/// </para>
/// </remarks>
[System.Runtime.Versioning.SupportedOSPlatform("browser")]
public static partial class Interop
{
    // =========================================================================
    // .NET → JavaScript (JSImport)
    // =========================================================================

    /// <summary>
    /// Applies a binary-encoded batch of DOM patches.
    /// </summary>
    /// <remarks>
    /// The binary data is produced by <see cref="RenderBatchWriter"/> and transferred
    /// via <see cref="JSType.MemoryView"/> for zero-copy interop. The JS side must
    /// call <c>.slice()</c> on the MemoryView to get a stable <c>Uint8Array</c>
    /// before the interop call returns.
    /// </remarks>
    /// <param name="batchData">The binary patch data as a Span&lt;byte&gt;.</param>
    [JSImport("applyBinaryBatch", "Abies")]
    internal static partial void ApplyBinaryBatch(
        [JSMarshalAs<JSType.MemoryView>] Span<byte> batchData);

    /// <summary>
    /// Sets the document title.
    /// </summary>
    /// <param name="title">The new page title.</param>
    [JSImport("setTitle", "Abies")]
    internal static partial void SetTitle(string title);

    /// <summary>
    /// Navigates to a URL via <c>history.pushState</c>.
    /// </summary>
    /// <param name="url">The target URL (relative or absolute).</param>
    [JSImport("navigateTo", "Abies")]
    internal static partial void NavigateTo(string url);

    /// <summary>
    /// Sets up event delegation on the document for all common event types.
    /// </summary>
    [JSImport("setupEventDelegation", "Abies")]
    internal static partial void SetupEventDelegation();

    // =========================================================================
    // Navigation (.NET → JavaScript)
    // =========================================================================

    /// <summary>
    /// Replaces the current URL in the browser history via <c>history.replaceState</c>.
    /// Unlike <see cref="NavigateTo"/>, this does not add a new history entry.
    /// </summary>
    /// <param name="url">The URL to replace the current entry with.</param>
    [JSImport("replaceUrl", "Abies")]
    internal static partial void ReplaceUrl(string url);

    /// <summary>
    /// Navigates the browser back one step in history via <c>history.back()</c>.
    /// </summary>
    [JSImport("historyBack", "Abies")]
    internal static partial void HistoryBack();

    /// <summary>
    /// Navigates the browser forward one step in history via <c>history.forward()</c>.
    /// </summary>
    [JSImport("historyForward", "Abies")]
    internal static partial void HistoryForward();

    /// <summary>
    /// Navigates to an external URL by setting <c>window.location.href</c>.
    /// This triggers a full page reload — the WASM application is unloaded.
    /// </summary>
    /// <param name="href">The external URL to navigate to.</param>
    [JSImport("externalNavigate", "Abies")]
    internal static partial void ExternalNavigate(string href);

    /// <summary>
    /// Sets up navigation interception: registers a <c>popstate</c> listener and
    /// intercepts internal <c>&lt;a&gt;</c> link clicks. Calls back to .NET via
    /// <see cref="OnUrlChanged"/> when the URL changes.
    /// </summary>
    [JSImport("setupNavigation", "Abies")]
    internal static partial void SetupNavigation();

#if DEBUG
    internal static DebuggerMachine? Debugger { get; set; }
    internal static Func<object?, bool>? ApplyDebuggerSnapshot { get; set; }

    /// <summary>
    /// Mounts the debugger UI by injecting a mount point div into the document.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Called automatically in Debug builds during Runtime.Run(). In Release builds,
    /// this method is compiled out and never called.
    /// </para>
    /// <para>
    /// The debugger mount point is idempotent — calling multiple times has no effect.
    /// </para>
    /// </remarks>
    [JSImport("mountDebugger", "AbiesDebugger")]
    internal static partial void MountDebugger();

    [JSImport("setRuntimeBridge", "AbiesDebugger")]
    internal static partial void SetRuntimeBridge(
        [JSMarshalAs<JSType.Function<JSType.String, JSType.Number, JSType.String>>]
        Func<string, int, string> callback);

    /// <summary>
    /// Called from C# after each <c>CaptureMessage</c> to push a timeline refresh into the
    /// debugger UI. The JS side fetches the latest timeline via the existing bridge.
    /// </summary>
    [JSImport("notifyTimelineChanged", "AbiesDebugger")]
    internal static partial void NotifyTimelineChanged();

    [JSExport]
    public static string DispatchDebuggerMessage(string messageType, int entryId)
    {
        if (Debugger is null)
        {
            return System.Text.Json.JsonSerializer.Serialize(new DebuggerAdapterResponse
            {
                Status = "unavailable",
                CursorPosition = -1,
                TimelineSize = 0,
                ModelSnapshotPreview = string.Empty
            }, DebuggerAdapterJsonContext.Default.DebuggerAdapterResponse);
        }

        try
        {
            if (string.IsNullOrWhiteSpace(messageType))
            {
                return System.Text.Json.JsonSerializer.Serialize(new DebuggerAdapterResponse
                {
                    Status = "error",
                    CursorPosition = Debugger.CursorPosition,
                    TimelineSize = Debugger.Timeline.Count,
                    AtStart = Debugger.AtStart,
                    AtEnd = Debugger.AtEnd,
                    ModelSnapshotPreview = Debugger.CurrentModelSnapshotPreview
                }, DebuggerAdapterJsonContext.Default.DebuggerAdapterResponse);
            }

            var message = new DebuggerAdapterMessage
            {
                Type = messageType,
                EntryId = entryId >= 0 ? entryId : null
            };

            var response = DebuggerRuntimeBridge.Execute(message, Debugger);

            if (ApplyDebuggerSnapshot is not null)
            {
                _ = ApplyDebuggerSnapshot(Debugger.CurrentModelSnapshot);
            }

            return System.Text.Json.JsonSerializer.Serialize(response,
                DebuggerAdapterJsonContext.Default.DebuggerAdapterResponse);
        }
        catch
        {
            return System.Text.Json.JsonSerializer.Serialize(new DebuggerAdapterResponse
            {
                Status = "error",
                CursorPosition = Debugger.CursorPosition,
                TimelineSize = Debugger.Timeline.Count,
                AtStart = Debugger.AtStart,
                AtEnd = Debugger.AtEnd,
                ModelSnapshotPreview = Debugger.CurrentModelSnapshotPreview
            }, DebuggerAdapterJsonContext.Default.DebuggerAdapterResponse);
        }
    }
#endif

    /// <summary>
    /// Gets the current browser URL (window.location.href).
    /// </summary>
    [JSImport("getCurrentUrl", "Abies")]
    internal static partial string GetCurrentUrl();

    /// <summary>
    /// Gets the browser window's origin (e.g., "http://localhost:5000").
    /// Used by WASM apps to resolve API URLs relative to the hosting server.
    /// </summary>
    [JSImport("getOrigin", "Abies")]
    internal static partial string GetOrigin();

    /// <summary>
    /// Gets a sessionStorage item by key.
    /// </summary>
    [JSImport("getSessionStorageItem", "Abies")]
    internal static partial string? GetSessionStorageItem(string key);

    /// <summary>
    /// Sets a sessionStorage item.
    /// </summary>
    [JSImport("setSessionStorageItem", "Abies")]
    internal static partial void SetSessionStorageItem(string key, string value);

    /// <summary>
    /// Removes a sessionStorage item.
    /// </summary>
    [JSImport("removeSessionStorageItem", "Abies")]
    internal static partial void RemoveSessionStorageItem(string key);

    // =========================================================================
    // Callback Wiring (.NET → JavaScript)
    // =========================================================================
    // These methods wire the [JSExport] callbacks into abies.js. The .NET side
    // passes managed delegates to JS during Runtime.Run(), so the consumer's
    // index.html only needs: <script type="module" src="abies.js"></script>
    // =========================================================================

    /// <summary>
    /// Passes the dispatch callback to abies.js so event delegation can call
    /// back into .NET when a DOM event fires.
    /// </summary>
    /// <param name="callback">The <see cref="DispatchDomEvent"/> method.</param>
    [JSImport("setDispatchCallback", "Abies")]
    internal static partial void SetDispatchCallback(
        [JSMarshalAs<JSType.Function<JSType.String, JSType.String, JSType.String>>]
        Action<string, string, string> callback);

    /// <summary>
    /// Passes the URL-changed callback to abies.js so navigation events can
    /// call back into .NET when the browser URL changes.
    /// </summary>
    /// <param name="callback">The <see cref="OnUrlChanged"/> method.</param>
    [JSImport("setOnUrlChangedCallback", "Abies")]
    internal static partial void SetOnUrlChangedCallback(
        [JSMarshalAs<JSType.Function<JSType.String>>]
        Action<string> callback);

    // =========================================================================
    // JavaScript → .NET (JSExport)
    // =========================================================================

    /// <summary>
    /// The handler registry instance for the browser runtime.
    /// Set during bootstrap by <see cref="Runtime.Run"/>. This static field
    /// is safe because WASM is single-threaded — there is exactly one runtime
    /// and one handler registry per browser tab.
    /// </summary>
    internal static HandlerRegistry? Handlers { get; set; }

    /// <summary>
    /// Called by abies.js when a DOM event fires on an element with a
    /// <c>data-event-{eventType}</c> attribute. The commandId maps to
    /// a handler in the <see cref="HandlerRegistry"/>.
    /// </summary>
    /// <param name="commandId">The handler command ID from the data-event attribute.</param>
    /// <param name="eventName">The DOM event name (e.g., "click", "input").</param>
    /// <param name="eventData">Serialized event data (e.g., input value, key name).</param>
    [JSExport]
    public static void DispatchDomEvent(string commandId, string eventName, string eventData)
    {
        var message = Handlers?.CreateMessage(commandId, eventData);
        if (message is not null)
        {
            Handlers?.Dispatch?.Invoke(message);
        }
    }

    /// <summary>
    /// Called by abies.js when the browser URL changes (popstate event or
    /// intercepted link click). Delegates to <see cref="NavigationCallbacks"/>
    /// which routes the URL change to the navigation subscription.
    /// </summary>
    /// <param name="url">The new URL as a string from the browser (e.g., "/articles/my-slug").</param>
    [JSExport]
    public static void OnUrlChanged(string url) =>
        NavigationCallbacks.HandleUrlChanged(url);
}
