// =============================================================================
// JavaScript Interop
// =============================================================================
// Declares all JavaScript functions imported from abies.js.
// Uses modern [JSImport]/[JSExport] attributes for type-safe interop.
//
// Architecture Decision Records:
// - ADR-011: JavaScript Interop Strategy (docs/adr/ADR-011-javascript-interop.md)
// - ADR-005: WebAssembly Runtime (docs/adr/ADR-005-webassembly-runtime.md)
// =============================================================================

using System.Runtime.InteropServices.JavaScript;

namespace Abies;

/// <summary>
/// JavaScript interop declarations for browser APIs.
/// </summary>
/// <remarks>
/// All browser interactions go through this class via JSImport.
/// The corresponding JavaScript implementation is in abies.js.
/// 
/// See ADR-011: JavaScript Interop Strategy
/// </remarks>
public static partial class Interop
{
    // Navigation APIs
    [JSImport("forward", "abies.js")]
    public static partial Task Forward(int steps);

    [JSImport("back", "abies.js")]
    public static partial Task Back(int steps);

    [JSImport("go", "abies.js")]
    public static partial Task Go(int steps);

    [JSImport("reload", "abies.js")]
    public static partial Task Reload();

    [JSImport("load", "abies.js")]
    public static partial Task Load(string url);

    [JSImport("pushState", "abies.js")]
    public static partial Task PushState(string url);

    [JSImport("replaceState", "abies.js")]
    public static partial Task ReplaceState(string url);

    // DOM manipulation APIs (see ADR-003: Virtual DOM)
    [JSImport("setAppContent", "abies.js")]
    public static partial Task SetAppContent(string html);

    [JSImport("addChildHtml", "abies.js")]
    public static partial Task AddChildHtml(string parentId, string childHtml);

    [JSImport("removeChild", "abies.js")]
    public static partial Task RemoveChild(string parentId, string childId);

    [JSImport("clearChildren", "abies.js")]
    public static partial Task ClearChildren(string parentId);

    [JSImport("replaceChildHtml", "abies.js")]
    public static partial Task ReplaceChildHtml(string oldNodeId, string newHtml);

    [JSImport("moveChild", "abies.js")]
    public static partial Task MoveChild(string parentId, string childId, string? beforeId);

    [JSImport("setChildrenHtml", "abies.js")]
    public static partial Task SetChildrenHtml(string parentId, string html);

    [JSImport("updateTextContent", "abies.js")]
    public static partial Task UpdateTextContent(string nodeId, string newText);

    [JSImport("updateAttribute", "abies.js")]
    public static partial Task UpdateAttribute(string id, string name, string value);

    [JSImport("addAttribute", "abies.js")]
    public static partial Task AddAttribute(string id, string name, string value);

    [JSImport("removeAttribute", "abies.js")]
    public static partial Task RemoveAttribute(string id, string name);

    /// <summary>
    /// Applies a binary render batch to the DOM.
    /// This uses a zero-copy protocol that avoids JSON serialization overhead.
    /// </summary>
    /// <param name="batch">The binary batch data as a memory view into WASM memory.</param>
    /// <remarks>
    /// The Span&lt;byte&gt; is marshaled as JSType.MemoryView, giving JavaScript direct
    /// access to the WASM memory without copying. The caller must ensure the memory
    /// remains valid (pinned) during the call.
    /// </remarks>
    [JSImport("applyBinaryBatch", "abies.js")]
    public static partial void ApplyBinaryBatch([JSMarshalAs<JSType.MemoryView>] Span<byte> batch);

    [JSImport("getValue", "abies.js")]
    public static partial string? GetValue(string id);

    // Storage APIs
    [JSImport("setLocalStorage", "abies.js")]
    public static partial Task SetLocalStorage(string key, string value);

    [JSImport("getLocalStorage", "abies.js")]
    public static partial string? GetLocalStorage(string key);

    [JSImport("removeLocalStorage", "abies.js")]
    public static partial Task RemoveLocalStorage(string key);

    [JSImport("setTitle", "abies.js")]
    public static partial Task SetTitle(string title);

    // Event handlers (callbacks from JS to C#)
    [JSImport("onUrlChange", "abies.js")]
    public static partial void OnUrlChange([JSMarshalAs<JSType.Function<JSType.String>>] Action<string> handler);

    [JSImport("getCurrentUrl", "abies.js")]
    public static partial string GetCurrentUrl();

    [JSImport("onLinkClick", "abies.js")]
    internal static partial void OnLinkClick([JSMarshalAs<JSType.Function<JSType.String>>] Action<string> value);

    [JSImport("onFormSubmit", "abies.js")]
    internal static partial void OnFormSubmit([JSMarshalAs<JSType.Function<JSType.String>>] Action<string> value);

    // Subscription APIs (see ADR-007: Subscriptions)
    [JSImport("subscribe", "abies.js")]
    internal static partial void Subscribe(string key, string kind, string? data);

    [JSImport("unsubscribe", "abies.js")]
    internal static partial void Unsubscribe(string key);
}
