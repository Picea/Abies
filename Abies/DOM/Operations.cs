// =============================================================================
// Virtual DOM Implementation
// =============================================================================
// Provides diffing and patching utilities for the virtual DOM.
// The implementation is inspired by Elm's VirtualDom diff algorithm
// and is written with performance in mind (object pooling, minimal allocations).
//
// Key concepts:
// - Node: Base type for all DOM nodes (Element, Text, RawHtml)
// - Patch: Represents a single DOM modification operation
// - Diff: Computes patches needed to transform old tree to new tree
// - Apply: Applies patches to real DOM via JavaScript interop
//
// Performance optimizations inspired by Stephen Toub's .NET performance articles:
// - Pre-allocated index string cache to avoid string interpolation
// - stackalloc for small arrays to avoid ArrayPool overhead
// - StringBuilder pooling for HTML rendering
// - Append chains instead of string interpolation
// - SearchValues<char> fast-path for HTML encoding (skip when no special chars)
// - FrozenDictionary cache for "data-event-{name}" strings (eliminates interpolation)
//
// Architecture Decision Records:
// - ADR-003: Virtual DOM (docs/adr/ADR-003-virtual-dom.md)
// - ADR-008: Immutable State Management (docs/adr/ADR-008-immutable-state.md)
// - ADR-011: JavaScript Interop Strategy (docs/adr/ADR-011-javascript-interop.md)
// =============================================================================

using System.Buffers;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using System.Text;

namespace Abies.DOM;

/// <summary>
/// Represents a document in the Abies DOM.
/// </summary>
/// <param name="Title">The title of the document.</param>
/// <param name="Body">The body content of the document.</param>
public record Document(string Title, Node Body);

/// <summary>
/// Represents a node in the Abies DOM.
/// </summary>
/// <param name="Id">The unique identifier for the node.</param>
/// <remarks>
/// Nodes are immutable records used to build the virtual DOM tree.
/// See ADR-003: Virtual DOM and ADR-008: Immutable State Management.
/// </remarks>
public record Node(string Id);

/// <summary>
/// Represents a raw (unprocessed) HTML node in the Abies DOM.
/// </summary>
/// <param name="Id">The unique identifier for the node.</param>
/// <param name="Html">The raw HTML content.</param>
public record RawHtml(string Id, string Html) : Node(Id);

/// <summary>
/// Represents an attribute of an element in the Abies DOM.
/// </summary>
/// <param name="Id">The unique identifier for the attribute.</param>
/// <param name="Name">The name of the attribute.</param>
/// <param name="Value">The value of the attribute.</param>
public record Attribute(string Id, string Name, string Value);

/// <summary>
/// Represents an element in the Abies DOM.
/// </summary>
/// <param name="Id">The unique identifier for the element.</param>
/// <param name="Tag">The tag name of the element.</param>
/// <param name="Attributes">The attributes of the element.</param>
/// <param name="Children">The child nodes of the element.</param>
public record Element(string Id, string Tag, Attribute[] Attributes, params Node[] Children) : Node(Id);

/// <summary>
/// Interface for memoized nodes to avoid reflection in trimmed WASM.
/// </summary>
public interface IMemoNode
{
    /// <summary>The memo key used to determine if the node should be re-rendered.</summary>
    object MemoKey { get; }
    /// <summary>The actual node content to render.</summary>
    Node CachedNode { get; }
    /// <summary>Creates a new memo node with the same key but different cached content.</summary>
    Node WithCachedNode(Node newCachedNode);
    /// <summary>
    /// Compares memo keys without boxing overhead.
    /// Uses generic EqualityComparer for value types to avoid allocation.
    /// </summary>
    bool MemoKeyEquals(IMemoNode other);
}

/// <summary>
/// Represents a memoized node in the Abies DOM.
/// When diffing, if the memo key equals the previous key, the cached node is reused
/// without re-diffing the subtree. This is useful for expensive-to-render components
/// that don't change frequently.
/// </summary>
/// <param name="Id">The unique identifier for the node.</param>
/// <param name="Key">The memo key used to determine if the node should be re-rendered.
/// If the key equals the previous key, the cached node is reused.</param>
/// <param name="CachedNode">The actual node content to render.</param>
/// <remarks>
/// Inspired by Elm's lazy function. Use this to wrap list items or components
/// where re-rendering can be skipped when the underlying data hasn't changed.
/// Example: memo(row, TableRow(row, isSelected)) where row is the key.
/// </remarks>
public record Memo<TKey>(string Id, TKey Key, Node CachedNode) : Node(Id), IMemoNode where TKey : notnull
{
    /// <summary>Gets the memo key as object for interface implementation.</summary>
    object IMemoNode.MemoKey => Key;

    /// <summary>Creates a new memo with the same key but different cached content.</summary>
    public Node WithCachedNode(Node newCachedNode) => this with { CachedNode = newCachedNode };

    /// <summary>
    /// Compares memo keys without boxing overhead using generic EqualityComparer.
    /// </summary>
    public bool MemoKeyEquals(IMemoNode other) =>
        other is Memo<TKey> otherMemo && EqualityComparer<TKey>.Default.Equals(Key, otherMemo.Key);
}

/// <summary>
/// Interface for lazy memoized nodes that defer evaluation until needed.
/// This is the key performance optimization - the function is only called if the key differs.
/// </summary>
public interface ILazyMemoNode
{
    /// <summary>The memo key used to determine if the node should be re-rendered.</summary>
    object MemoKey { get; }
    /// <summary>The cached node content (null if not yet evaluated).</summary>
    Node? CachedNode { get; }
    /// <summary>Evaluates the lazy function to produce the node content.</summary>
    Node Evaluate();
    /// <summary>Creates a new lazy memo with the cached content populated.</summary>
    Node WithCachedNode(Node cachedNode);
    /// <summary>
    /// Compares memo keys without boxing overhead.
    /// Uses generic EqualityComparer for value types to avoid allocation.
    /// </summary>
    bool MemoKeyEquals(ILazyMemoNode other);
}

/// <summary>
/// Represents a lazily-evaluated memoized node in the Abies DOM.
/// Unlike Memo&lt;TKey&gt;, the node content is NOT created until actually needed.
/// During diffing, if the key matches the previous key, the function is never called.
/// This provides the true performance benefit of Elm's lazy function.
/// </summary>
/// <param name="Id">The unique identifier for the node.</param>
/// <param name="Key">The memo key used to determine if the node should be re-rendered.</param>
/// <param name="Factory">The function that produces the node content (only called if key differs).</param>
/// <param name="CachedNode">The cached node content after evaluation (null initially).</param>
public record LazyMemo<TKey>(string Id, TKey Key, Func<Node> Factory, Node? CachedNode = null) : Node(Id), ILazyMemoNode where TKey : notnull
{
    /// <summary>Gets the memo key as object for interface implementation.</summary>
    object ILazyMemoNode.MemoKey => Key;

    /// <summary>Gets the cached node content.</summary>
    Node? ILazyMemoNode.CachedNode => CachedNode;

    /// <summary>Evaluates the lazy function to produce the node content.</summary>
    public Node Evaluate() => Factory();

    /// <summary>Creates a new lazy memo with the cached content populated.</summary>
    public Node WithCachedNode(Node cachedNode) => this with { CachedNode = cachedNode };

    /// <summary>
    /// Compares memo keys without boxing overhead using generic EqualityComparer.
    /// </summary>
    public bool MemoKeyEquals(ILazyMemoNode other) =>
        other is LazyMemo<TKey> otherLazy && EqualityComparer<TKey>.Default.Equals(Key, otherLazy.Key);
}

// =============================================================================
// Event Attribute Name Cache - Performance Optimization
// =============================================================================
// Caches "data-event-{name}" strings for all known event names to avoid
// string interpolation allocation on every Handler creation.
//
// Inspired by Stephen Toub's .NET performance articles on FrozenDictionary.
// Uses FrozenDictionary for O(1) lookup with minimal overhead.
// Falls back to string interpolation for custom/unknown event names.
// =============================================================================
internal static class EventAttributeNames
{
    // All known DOM event names - covers standard HTML events
    private static readonly FrozenDictionary<string, string> _cache = new Dictionary<string, string>
    {
        // Mouse events
        ["click"] = "data-event-click",
        ["dblclick"] = "data-event-dblclick",
        ["mousedown"] = "data-event-mousedown",
        ["mouseup"] = "data-event-mouseup",
        ["mouseover"] = "data-event-mouseover",
        ["mouseout"] = "data-event-mouseout",
        ["mouseenter"] = "data-event-mouseenter",
        ["mouseleave"] = "data-event-mouseleave",
        ["mousemove"] = "data-event-mousemove",
        ["contextmenu"] = "data-event-contextmenu",
        ["wheel"] = "data-event-wheel",

        // Keyboard events
        ["keydown"] = "data-event-keydown",
        ["keyup"] = "data-event-keyup",
        ["keypress"] = "data-event-keypress",

        // Form events
        ["input"] = "data-event-input",
        ["change"] = "data-event-change",
        ["submit"] = "data-event-submit",
        ["reset"] = "data-event-reset",
        ["focus"] = "data-event-focus",
        ["blur"] = "data-event-blur",
        ["invalid"] = "data-event-invalid",
        ["search"] = "data-event-search",

        // Touch events
        ["touchstart"] = "data-event-touchstart",
        ["touchend"] = "data-event-touchend",
        ["touchmove"] = "data-event-touchmove",
        ["touchcancel"] = "data-event-touchcancel",

        // Pointer events
        ["pointerdown"] = "data-event-pointerdown",
        ["pointerup"] = "data-event-pointerup",
        ["pointermove"] = "data-event-pointermove",
        ["pointercancel"] = "data-event-pointercancel",
        ["pointerover"] = "data-event-pointerover",
        ["pointerout"] = "data-event-pointerout",
        ["pointerenter"] = "data-event-pointerenter",
        ["pointerleave"] = "data-event-pointerleave",
        ["gotpointercapture"] = "data-event-gotpointercapture",
        ["lostpointercapture"] = "data-event-lostpointercapture",

        // Drag events
        ["drag"] = "data-event-drag",
        ["dragstart"] = "data-event-dragstart",
        ["dragend"] = "data-event-dragend",
        ["dragenter"] = "data-event-dragenter",
        ["dragleave"] = "data-event-dragleave",
        ["dragover"] = "data-event-dragover",
        ["drop"] = "data-event-drop",

        // Clipboard events
        ["copy"] = "data-event-copy",
        ["cut"] = "data-event-cut",
        ["paste"] = "data-event-paste",

        // Media events
        ["play"] = "data-event-play",
        ["pause"] = "data-event-pause",
        ["ended"] = "data-event-ended",
        ["timeupdate"] = "data-event-timeupdate",
        ["canplay"] = "data-event-canplay",
        ["canplaythrough"] = "data-event-canplaythrough",
        ["durationchange"] = "data-event-durationchange",
        ["emptied"] = "data-event-emptied",
        ["stalled"] = "data-event-stalled",
        ["suspend"] = "data-event-suspend",
        ["ratechange"] = "data-event-ratechange",
        ["volumechange"] = "data-event-volumechange",
        ["seeked"] = "data-event-seeked",
        ["seeking"] = "data-event-seeking",
        ["waiting"] = "data-event-waiting",
        ["loadeddata"] = "data-event-loadeddata",
        ["loadedmetadata"] = "data-event-loadedmetadata",
        ["loadstart"] = "data-event-loadstart",
        ["progress"] = "data-event-progress",
        ["encrypted"] = "data-event-encrypted",

        // Window/Document events
        ["load"] = "data-event-load",
        ["unload"] = "data-event-unload",
        ["beforeunload"] = "data-event-beforeunload",
        ["error"] = "data-event-error",
        ["resize"] = "data-event-resize",
        ["scroll"] = "data-event-scroll",
        ["online"] = "data-event-online",
        ["offline"] = "data-event-offline",
        ["storage"] = "data-event-storage",
        ["visibilitychange"] = "data-event-visibilitychange",

        // Animation/Transition events
        ["animationstart"] = "data-event-animationstart",
        ["animationend"] = "data-event-animationend",
        ["animationiteration"] = "data-event-animationiteration",
        ["transitionstart"] = "data-event-transitionstart",
        ["transitionend"] = "data-event-transitionend",

        // Fullscreen events
        ["fullscreenchange"] = "data-event-fullscreenchange",
        ["fullscreenerror"] = "data-event-fullscreenerror",

        // Dialog/Popover events
        ["close"] = "data-event-close",
        ["cancel"] = "data-event-cancel",
        ["toggle"] = "data-event-toggle",
        ["beforetoggle"] = "data-event-beforetoggle",
        ["show"] = "data-event-show",

        // Selection events
        ["selectionchange"] = "data-event-selectionchange",

        // Web Component events
        ["slotchange"] = "data-event-slotchange",

        // Print events
        ["afterprint"] = "data-event-afterprint",
        ["beforeprint"] = "data-event-beforeprint",

        // Misc events
        ["message"] = "data-event-message",
        ["messageerror"] = "data-event-messageerror",
        ["languagechange"] = "data-event-languagechange",
        ["rejectionhandled"] = "data-event-rejectionhandled",
        ["unhandledrejection"] = "data-event-unhandledrejection",
        ["securitypolicyviolation"] = "data-event-securitypolicyviolation",
        ["formdata"] = "data-event-formdata",
        ["finish"] = "data-event-finish",
        ["intersect"] = "data-event-intersect",
        ["audioprocess"] = "data-event-audioprocess",

        // Device events
        ["devicemotion"] = "data-event-devicemotion",
        ["deviceorientation"] = "data-event-deviceorientation",
        ["deviceorientationabsolute"] = "data-event-deviceorientationabsolute",

        // Gesture events (Safari)
        ["gesturestart"] = "data-event-gesturestart",
        ["gesturechange"] = "data-event-gesturechange",
        ["gestureend"] = "data-event-gestureend",

        // XR events
        ["beforexrselect"] = "data-event-beforexrselect",
    }.ToFrozenDictionary();

    /// <summary>
    /// Gets the cached "data-event-{name}" string for the given event name.
    /// Falls back to string interpolation for unknown event names.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Get(string eventName) =>
        _cache.TryGetValue(eventName, out var cached) ? cached : $"data-event-{eventName}";
}

/// <summary>
/// Represents a command handler for an element in the Abies DOM.
/// </summary>
/// <param name="Name">The name of the handler.</param>
/// <param name="CommandId">The unique identifier for the command.</param>
/// <param name="Command">The command associated with the handler.</param>
/// <param name="Id">The unique identifier for the handler.</param>
/// <param name="WithData">A function to provide additional data for the command.</param>
/// <param name="DataType">The type of the data provided by the WithData function.</param>
public record Handler(
    string Name,
    string CommandId,
    Message? Command,
    string Id,
    Func<object?, Message>? WithData = null,
    Type? DataType = null)
    : Attribute(Id, EventAttributeNames.Get(Name), CommandId);

/// <summary>
/// Represents a text node in the Abies DOM.
/// </summary>
/// <param name="Id">The unique identifier for the text node.</param>
/// <param name="Value">The text content of the node.</param>
public record Text(string Id, string Value) : Node(Id)
{
    public static implicit operator string(Text text) => text.Value;
    public static implicit operator Text(string text) => new(text, text);
};

/// <summary>
/// Represents an empty node in the Abies DOM.
/// </summary>
public record Empty() : Node("");

/// <summary>
/// Represents a patch operation in the Abies DOM.
/// </summary>
public interface Patch { }

/// <summary>
/// Represents a patch operation to add a root element in the Abies DOM.
/// </summary>
/// <param name="element">The element to add as the root.</param>
public readonly struct AddRoot(Element element) : Patch
{
    public readonly Element Element = element;
}

/// <summary>
/// Represents a patch operation to replace a child element in the Abies DOM.
/// </summary>
/// <param name="oldElement">The old child element to replace.</param>
/// <param name="newElement">The new child element to replace the old one.</param>
public readonly struct ReplaceChild(Element oldElement, Element newElement) : Patch
{
    public readonly Element OldElement = oldElement;
    public readonly Element NewElement = newElement;
}

/// <summary>
/// Represents a patch operation to add a child element in the Abies DOM.
/// </summary>
/// <param name="parent">The parent element.</param>
/// <param name="child">The child element to add.</param>
public readonly struct AddChild(Element parent, Element child) : Patch
{
    public readonly Element Parent = parent;
    public readonly Element Child = child;
}

/// <summary>
/// Represents a patch operation to remove a child element in the Abies DOM.
/// </summary>
/// <param name="parent">The parent element.</param>
/// <param name="child">The child element to remove.</param>
public readonly struct RemoveChild(Element parent, Element child) : Patch
{
    public readonly Element Parent = parent;
    public readonly Element Child = child;
}

/// <summary>
/// Represents a patch operation to clear all children from an element in the Abies DOM.
/// This is more efficient than multiple RemoveChild operations when removing all children.
/// </summary>
/// <param name="parent">The parent element to clear.</param>
/// <param name="oldChildren">The children being removed (needed for handler unregistration).</param>
public readonly struct ClearChildren(Element parent, Node[] oldChildren) : Patch
{
    public readonly Element Parent = parent;
    public readonly Node[] OldChildren = oldChildren;
}

/// <summary>
/// Represents a patch operation to update an attribute in the Abies DOM.
/// </summary>
/// <param name="element">The element to update.</param>
/// <param name="attribute">The attribute to update.</param>
/// <param name="value">The new value for the attribute.</param>
public readonly struct UpdateAttribute(Element element, Attribute attribute, string value) : Patch
{
    public readonly Element Element = element;
    public readonly Attribute Attribute = attribute;
    public readonly string Value = value;
}

/// <summary>
/// Represents a patch operation to add an attribute in the Abies DOM.
/// </summary>
/// <param name="element">The element to add the attribute to.</param>
/// <param name="attribute">The attribute to add.</param>
public readonly struct AddAttribute(Element element, Attribute attribute) : Patch
{
    public readonly Element Element = element;
    public readonly Attribute Attribute = attribute;
}

/// <summary>
/// Represents a patch operation to remove an attribute in the Abies DOM.
/// </summary>
/// <param name="element">The element to remove the attribute from.</param>
/// <param name="attribute">The attribute to remove.</param>
public readonly struct RemoveAttribute(Element element, Attribute attribute) : Patch
{
    public readonly Element Element = element;
    public readonly Attribute Attribute = attribute;
}

/// <summary>
/// Represents a patch operation to add an event handler in the Abies DOM.
/// </summary>
/// <param name="element">The element to add the event handler to.</param>
/// <param name="handler">The event handler to add.</param>
public readonly struct AddHandler(Element element, Handler handler) : Patch
{
    public readonly Element Element = element;
    public readonly Handler Handler = handler;
}

/// <summary>
/// Represents a patch operation to remove an event handler in the Abies DOM.
/// </summary>
/// <param name="element">The element to remove the event handler from.</param>
/// <param name="handler">The event handler to remove.</param>
public readonly struct RemoveHandler(Element element, Handler handler) : Patch
{
    public readonly Element Element = element;
    public readonly Handler Handler = handler;
}

/// <summary>
/// Represents a patch operation to update an event handler in the Abies DOM.
/// </summary>
/// <param name="element">The element to update.</param>
/// <param name="oldHandler">The old event handler to replace.</param>
/// <param name="newHandler">The new event handler to replace the old one.</param>
public readonly struct UpdateHandler(Element element, Handler oldHandler, Handler newHandler) : Patch
{
    public readonly Element Element = element;
    public readonly Handler OldHandler = oldHandler;
    public readonly Handler NewHandler = newHandler;
}

/// <summary>
/// Represents a patch operation to update a text node in the Abies DOM.
/// </summary>
/// <param name="node">The text node to update.</param>
/// <param name="text">The current text content of the node.</param>
/// <param name="newId">The new ID to assign to the node.</param>
public readonly struct UpdateText(Text node, string text, string newId) : Patch
{
    public readonly Text Node = node;
    public readonly string Text = text;
    public readonly string NewId = newId;
}

/// <summary>
/// Represents a patch operation to add a text node in the Abies DOM.
/// </summary>
/// <param name="parent">The parent element to add the text node to.</param>
/// <param name="child">The text node to add.</param>
public readonly struct AddText(Element parent, Text child) : Patch
{
    public readonly Element Parent = parent;
    public readonly Text Child = child;
}

/// <summary>
/// Represents a patch operation to remove a text node in the Abies DOM.
/// </summary>
/// <param name="parent">The parent element to remove the text node from.</param>
/// <param name="child">The text node to remove.</param>
public readonly struct RemoveText(Element parent, Text child) : Patch
{
    public readonly Element Parent = parent;
    public readonly Text Child = child;
}

/// <summary>
/// Represents a patch operation to add raw HTML in the Abies DOM.
/// </summary>
/// <param name="parent">The parent element to add the raw HTML to.</param>
/// <param name="child">The raw HTML to add.</param>
public readonly struct AddRaw(Element parent, RawHtml child) : Patch
{
    public readonly Element Parent = parent;
    public readonly RawHtml Child = child;
}

/// <summary>
/// Represents a patch operation to remove raw HTML in the Abies DOM.
/// </summary>
/// <param name="parent">The parent element to remove the raw HTML from.</param>
/// <param name="child">The raw HTML to remove.</param>
public readonly struct RemoveRaw(Element parent, RawHtml child) : Patch
{
    public readonly Element Parent = parent;
    public readonly RawHtml Child = child;
}

/// <summary>
/// Represents a patch operation to move a child element to a new position in the Abies DOM.
/// Uses insertBefore semantics: move child before the element with BeforeId, or append if null.
/// </summary>
/// <param name="parent">The parent element containing the child.</param>
/// <param name="child">The child element to move.</param>
/// <param name="beforeId">The ID of the element to insert before, or null to append.</param>
public readonly struct MoveChild(Element parent, Element child, string? beforeId) : Patch
{
    public readonly Element Parent = parent;
    public readonly Element Child = child;
    public readonly string? BeforeId = beforeId;
}

/// <summary>
/// Represents a patch operation to replace raw HTML in the Abies DOM.
/// </summary>
/// <param name="oldNode">The old raw HTML node to replace.</param>
/// <param name="newNode">The new raw HTML node to replace the old one.</param>
public readonly struct ReplaceRaw(RawHtml oldNode, RawHtml newNode) : Patch
{
    public readonly RawHtml OldNode = oldNode;
    public readonly RawHtml NewNode = newNode;
}

/// <summary>
/// Represents a patch operation to update raw HTML in the Abies DOM.
/// </summary>
/// <param name="node">The raw HTML node to update.</param>
/// <param name="html">The current HTML content of the node.</param>
/// <param name="newId">The new ID to assign to the node.</param>
public readonly struct UpdateRaw(RawHtml node, string html, string newId) : Patch
{
    public readonly RawHtml Node = node;
    public readonly string Html = html;
    public readonly string NewId = newId;
}

/// <summary>
/// Provides rendering utilities for the virtual DOM.
/// </summary>
public static class Render
{
    // StringBuilder pool to avoid allocations during rendering
    // Uses Stack<T> instead of ConcurrentQueue<T> since WASM is single-threaded
    private static readonly Stack<StringBuilder> _stringBuilderPool = new();
    private const int _maxPooledStringBuilderCapacity = 8192;

    // =============================================================================
    // HTML Encoding Optimization - SearchValues Fast Path
    // =============================================================================
    // Uses SearchValues<char> to quickly check if a string contains characters
    // that need HTML encoding. Most strings (class names, IDs, etc.) don't
    // contain special characters, so we can skip the expensive HtmlEncode call.
    //
    // Inspired by Stephen Toub's .NET performance articles on SearchValues.
    // Performance improvement: ~50-70% faster for strings without special chars.
    // =============================================================================
    private static readonly SearchValues<char> _htmlSpecialChars = SearchValues.Create("&<>\"'");

    /// <summary>
    /// Appends HTML-encoded value to StringBuilder, using fast-path when no encoding needed.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AppendHtmlEncoded(StringBuilder sb, string value)
    {
        // Fast path: if no special characters, append directly without encoding
        if (!value.AsSpan().ContainsAny(_htmlSpecialChars))
        {
            sb.Append(value);
            return;
        }

        // Slow path: encode the value (rare for most attribute values)
        sb.Append(System.Web.HttpUtility.HtmlEncode(value));
    }

    private static StringBuilder RentStringBuilder()
    {
        if (_stringBuilderPool.TryPop(out var sb))
        {
            sb.Clear();
            return sb;
        }
        return new StringBuilder(256);
    }

    private static void ReturnStringBuilder(StringBuilder sb)
    {
        if (sb.Capacity <= _maxPooledStringBuilderCapacity)
        {
            _stringBuilderPool.Push(sb);
        }
    }

    /// <summary>
    /// Renders a virtual DOM node to its HTML representation.
    /// </summary>
    /// <param name="node">The virtual DOM node to render.</param>
    /// <returns>The HTML representation of the virtual DOM node.</returns>
    public static string Html(Node node)
    {
        var sb = RentStringBuilder();
        try
        {
            RenderNode(node, sb);
            return sb.ToString();
        }
        finally
        {
            ReturnStringBuilder(sb);
        }
    }

    private static void RenderNode(Node node, StringBuilder sb)
    {
        switch (node)
        {
            case Element element:
                sb.Append('<').Append(element.Tag).Append(" id=\"").Append(element.Id).Append('"');
                foreach (var attr in element.Attributes)
                {
                    // For handlers, only render the data-event-* attribute; do not render a raw event name attribute
                    sb.Append(' ').Append(attr.Name).Append("=\"");
                    AppendHtmlEncoded(sb, attr.Value);
                    sb.Append('"');
                }
                sb.Append('>');
                foreach (var child in element.Children)
                {
                    RenderNode(child, sb);
                }
                sb.Append("</").Append(element.Tag).Append('>');
                break;
            case Text text:
                sb.Append("<span id=\"").Append(text.Id).Append("\">");
                AppendHtmlEncoded(sb, text.Value);
                sb.Append("</span>");
                break;
            case RawHtml raw:
                sb.Append("<span id=\"").Append(raw.Id).Append("\">")
                  .Append(raw.Html).Append("</span>");
                break;
            // Handle LazyMemo<T> nodes by evaluating and rendering their content
            case ILazyMemoNode lazyMemo:
                RenderNode(lazyMemo.CachedNode ?? lazyMemo.Evaluate(), sb);
                break;
            // Handle Memo<T> nodes by rendering their cached content
            case IMemoNode memo:
                RenderNode(memo.CachedNode, sb);
                break;
        }
    }
}

/// <summary>
/// Provides diffing and patching utilities for the virtual DOM.
/// The implementation is inspired by Elm's VirtualDom diff algorithm
/// and is written with performance in mind.
/// </summary>
public static class Operations
{
    // Pre-allocated index strings to avoid string interpolation allocation.
    // Inspired by Stephen Toub's .NET performance articles on avoiding allocations.
    // Cache covers 99% of real-world use cases (elements with >256 children are rare).
    private const int _indexStringCacheSize = 256;
    private static readonly string[] _indexStringCache = InitializeIndexStringCache();

    private static string[] InitializeIndexStringCache()
    {
        var cache = new string[_indexStringCacheSize];
        for (int i = 0; i < _indexStringCacheSize; i++)
        {
            cache[i] = $"__index:{i}";
        }
        return cache;
    }

    /// <summary>
    /// Gets a cached index string for the given index to avoid allocation.
    /// For indices >= 256, falls back to string interpolation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetIndexString(int index) =>
        (uint)index < _indexStringCacheSize ? _indexStringCache[index] : $"__index:{index}";

    // Object pools to reduce allocations
    // Uses Stack<T> instead of ConcurrentQueue<T> since WASM is single-threaded
    private static readonly Stack<List<Patch>> _patchListPool = new();
    private static readonly Stack<Dictionary<string, Attribute>> _attributeMapPool = new();
    private static readonly Stack<Dictionary<string, int>> _keyIndexMapPool = new();
    private static readonly Stack<List<int>> _intListPool = new();
    private static readonly Stack<List<(int, int)>> _intPairListPool = new();

    private static List<Patch> RentPatchList()
    {
        if (_patchListPool.TryPop(out var list))
        {
            list.Clear();
            return list;
        }
        return [];
    }

    private static void ReturnPatchList(List<Patch> list)
    {
        if (list.Count < 1000) // Prevent memory bloat
        {
            _patchListPool.Push(list);
        }
    }

    private static Dictionary<string, Attribute> RentAttributeMap()
    {
        if (_attributeMapPool.TryPop(out var map))
        {
            map.Clear();
            return map;
        }
        return [];
    }

    private static void ReturnAttributeMap(Dictionary<string, Attribute> map)
    {
        if (map.Count < 100) // Prevent memory bloat
        {
            _attributeMapPool.Push(map);
        }
    }

    private static Dictionary<string, int> RentKeyIndexMap()
    {
        if (_keyIndexMapPool.TryPop(out var map))
        {
            map.Clear();
            return map;
        }
        return [];
    }

    private static void ReturnKeyIndexMap(Dictionary<string, int> map)
    {
        if (map.Count < 200) // Prevent memory bloat
        {
            _keyIndexMapPool.Push(map);
        }
    }

    private static List<int> RentIntList()
    {
        if (_intListPool.TryPop(out var list))
        {
            list.Clear();
            return list;
        }
        return [];
    }

    private static void ReturnIntList(List<int> list)
    {
        if (list.Count < 500) // Prevent memory bloat
        {
            _intListPool.Push(list);
        }
    }

    private static List<(int, int)> RentIntPairList()
    {
        if (_intPairListPool.TryPop(out var list))
        {
            list.Clear();
            return list;
        }
        return [];
    }

    private static void ReturnIntPairList(List<(int, int)> list)
    {
        if (list.Count < 500) // Prevent memory bloat
        {
            _intPairListPool.Push(list);
        }
    }

    /// <summary>
    /// Apply a patch to the real DOM by invoking JavaScript interop.
    /// </summary>
    /// <param name="patch">The patch to apply.</param>
    public static async Task Apply(Patch patch)
    {
        switch (patch)
        {
            case AddRoot addRoot:
                await Interop.SetAppContent(Render.Html(addRoot.Element));
                Runtime.RegisterHandlers(addRoot.Element);
                break;
            case ReplaceChild replaceChild:
                // Unregister old handlers before replacing DOM
                Runtime.UnregisterHandlers(replaceChild.OldElement);
                await Interop.ReplaceChildHtml(replaceChild.OldElement.Id, Render.Html(replaceChild.NewElement));
                Runtime.RegisterHandlers(replaceChild.NewElement);
                break;
            case AddChild addChild:
                await Interop.AddChildHtml(addChild.Parent.Id, Render.Html(addChild.Child));
                Runtime.RegisterHandlers(addChild.Child);
                break;
            case RemoveChild removeChild:
                Runtime.UnregisterHandlers(removeChild.Child);
                await Interop.RemoveChild(removeChild.Parent.Id, removeChild.Child.Id);
                break;
            case ClearChildren clearChildren:
                // Single DOM operation to clear all children - much faster than N RemoveChild operations
                await Interop.ClearChildren(clearChildren.Parent.Id);
                break;
            case UpdateAttribute updateAttribute:
                await Interop.UpdateAttribute(updateAttribute.Element.Id, updateAttribute.Attribute.Name, updateAttribute.Value);
                break;
            case AddAttribute addAttribute:
                await Interop.AddAttribute(addAttribute.Element.Id, addAttribute.Attribute.Name, addAttribute.Attribute.Value);
                break;
            case RemoveAttribute removeAttribute:
                await Interop.RemoveAttribute(removeAttribute.Element.Id, removeAttribute.Attribute.Name);
                break;
            case AddHandler addHandler:
                // First register the handler so events dispatched immediately after DOM update are handled
                Runtime.RegisterHandler(addHandler.Handler);
                await Interop.AddAttribute(addHandler.Element.Id, addHandler.Handler.Name, addHandler.Handler.Value);
                break;
            case RemoveHandler removeHandler:
                // First remove the DOM attribute to avoid dispatching with stale IDs, then unregister
                await Interop.RemoveAttribute(removeHandler.Element.Id, removeHandler.Handler.Name);
                Runtime.UnregisterHandler(removeHandler.Handler);
                break;
            case UpdateHandler updateHandler:
                // Atomically update handler mapping and DOM attribute value to avoid gaps
                // 1) Register the new handler command id
                Runtime.RegisterHandler(updateHandler.NewHandler);
                // 2) Update the DOM attribute value to the new command id
                var attrNameToUpdate = updateHandler.NewHandler.Name;
                await Interop.UpdateAttribute(updateHandler.Element.Id, attrNameToUpdate, updateHandler.NewHandler.Value);
                // 3) Unregister the old handler command id
                Runtime.UnregisterHandler(updateHandler.OldHandler);
                break;
            case UpdateText updateText:
                // Update the text content first using the old ID
                await Interop.UpdateTextContent(updateText.Node.Id, updateText.Text);
                // If the text node's ID changed, update the DOM element's id attribute
                if (!string.Equals(updateText.Node.Id, updateText.NewId, StringComparison.Ordinal))
                {
                    await Interop.UpdateAttribute(updateText.Node.Id, "id", updateText.NewId);
                }
                break;
            case AddText addText:
                await Interop.AddChildHtml(addText.Parent.Id, Render.Html(addText.Child));
                break;
            case RemoveText removeText:
                await Interop.RemoveChild(removeText.Parent.Id, removeText.Child.Id);
                break;
            case AddRaw addRaw:
                await Interop.AddChildHtml(addRaw.Parent.Id, Render.Html(addRaw.Child));
                break;
            case RemoveRaw removeRaw:
                await Interop.RemoveChild(removeRaw.Parent.Id, removeRaw.Child.Id);
                break;
            case ReplaceRaw replaceRaw:
                await Interop.ReplaceChildHtml(replaceRaw.OldNode.Id, Render.Html(replaceRaw.NewNode));
                break;
            case UpdateRaw updateRaw:
                await Interop.ReplaceChildHtml(updateRaw.Node.Id, Render.Html(new RawHtml(updateRaw.NewId, updateRaw.Html)));
                break;
            case MoveChild moveChild:
                await Interop.MoveChild(moveChild.Parent.Id, moveChild.Child.Id, moveChild.BeforeId);
                break;
            default:
                throw new InvalidOperationException("Unknown patch type");
        }
    }

    /// <summary>
    /// Apply a batch of patches to the real DOM using a binary protocol for zero-copy transfer.
    /// This eliminates JSON serialization overhead by writing patches directly to a binary buffer
    /// that JavaScript can read directly from WASM memory.
    /// </summary>
    /// <param name="patches">The list of patches to apply.</param>
    public static async ValueTask ApplyBatch(List<Patch> patches)
    {
        if (patches.Count == 0)
        {
            return;
        }

        // Step 1: Pre-process - register new handlers BEFORE DOM changes
        // This includes handlers in newly added subtrees (AddChild, ReplaceChild, AddRoot)
        foreach (var patch in patches)
        {
            switch (patch)
            {
                case AddRoot addRoot:
                    Runtime.RegisterHandlers(addRoot.Element);
                    break;
                case ReplaceChild replaceChild:
                    Runtime.RegisterHandlers(replaceChild.NewElement);
                    break;
                case AddChild addChild:
                    Runtime.RegisterHandlers(addChild.Child);
                    break;
                case AddHandler addHandler:
                    Runtime.RegisterHandler(addHandler.Handler);
                    break;
                case UpdateHandler updateHandler:
                    Runtime.RegisterHandler(updateHandler.NewHandler);
                    break;
            }
        }

        // Step 2: Build binary batch
        var writer = RenderBatchWriterPool.Rent();
        try
        {
            foreach (var patch in patches)
            {
                WritePatchToBinary(writer, patch);
            }

            // Step 3: Apply the binary batch via zero-copy memory transfer
            var memory = writer.ToMemory();
            Interop.ApplyBinaryBatch(memory.Span);
        }
        finally
        {
            RenderBatchWriterPool.Return(writer);
        }

        // Step 4: Post-process - unregister old handlers AFTER DOM changes
        // This includes handlers in removed subtrees (RemoveChild, ReplaceChild)
        foreach (var patch in patches)
        {
            switch (patch)
            {
                case ReplaceChild replaceChild:
                    Runtime.UnregisterHandlers(replaceChild.OldElement);
                    break;
                case RemoveChild removeChild:
                    Runtime.UnregisterHandlers(removeChild.Child);
                    break;
                case RemoveHandler removeHandler:
                    Runtime.UnregisterHandler(removeHandler.Handler);
                    break;
                case UpdateHandler updateHandler:
                    Runtime.UnregisterHandler(updateHandler.OldHandler);
                    break;
            }
        }
    }

    /// <summary>
    /// Write a single patch to the binary batch writer.
    /// </summary>
    private static void WritePatchToBinary(RenderBatchWriter writer, Patch patch)
    {
        switch (patch)
        {
            case AddRoot addRoot:
                writer.WriteSetAppContent(Render.Html(addRoot.Element));
                break;

            case ReplaceChild replaceChild:
                writer.WriteReplaceChild(replaceChild.OldElement.Id, Render.Html(replaceChild.NewElement));
                break;

            case AddChild addChild:
                writer.WriteAddChild(addChild.Parent.Id, Render.Html(addChild.Child));
                break;

            case RemoveChild removeChild:
                writer.WriteRemoveChild(removeChild.Parent.Id, removeChild.Child.Id);
                break;

            case ClearChildren clearChildren:
                writer.WriteClearChildren(clearChildren.Parent.Id);
                break;

            case UpdateAttribute updateAttribute:
                writer.WriteUpdateAttribute(updateAttribute.Element.Id, updateAttribute.Attribute.Name, updateAttribute.Value);
                break;

            case AddAttribute addAttribute:
                writer.WriteAddAttribute(addAttribute.Element.Id, addAttribute.Attribute.Name, addAttribute.Attribute.Value);
                break;

            case RemoveAttribute removeAttribute:
                writer.WriteRemoveAttribute(removeAttribute.Element.Id, removeAttribute.Attribute.Name);
                break;

            case AddHandler addHandler:
                writer.WriteAddAttribute(addHandler.Element.Id, addHandler.Handler.Name, addHandler.Handler.Value);
                break;

            case RemoveHandler removeHandler:
                writer.WriteRemoveAttribute(removeHandler.Element.Id, removeHandler.Handler.Name);
                break;

            case UpdateHandler updateHandler:
                writer.WriteUpdateAttribute(updateHandler.Element.Id, updateHandler.NewHandler.Name, updateHandler.NewHandler.Value);
                break;

            case UpdateText updateText:
                if (updateText.Node.Id == updateText.NewId)
                {
                    writer.WriteUpdateText(updateText.Node.Id, updateText.Text);
                }
                else
                {
                    writer.WriteUpdateTextWithId(updateText.Node.Id, updateText.Text, updateText.NewId);
                }
                break;

            case AddText addText:
                writer.WriteAddChild(addText.Parent.Id, Render.Html(addText.Child));
                break;

            case RemoveText removeText:
                writer.WriteRemoveChild(removeText.Parent.Id, removeText.Child.Id);
                break;

            case AddRaw addRaw:
                writer.WriteAddChild(addRaw.Parent.Id, Render.Html(addRaw.Child));
                break;

            case RemoveRaw removeRaw:
                writer.WriteRemoveChild(removeRaw.Parent.Id, removeRaw.Child.Id);
                break;

            case ReplaceRaw replaceRaw:
                writer.WriteReplaceChild(replaceRaw.OldNode.Id, Render.Html(replaceRaw.NewNode));
                break;

            case UpdateRaw updateRaw:
                writer.WriteReplaceChild(updateRaw.Node.Id, Render.Html(new RawHtml(updateRaw.NewId, updateRaw.Html)));
                break;

            case MoveChild moveChild:
                writer.WriteMoveChild(moveChild.Parent.Id, moveChild.Child.Id, moveChild.BeforeId);
                break;

            default:
                throw new InvalidOperationException($"Unknown patch type: {patch.GetType().Name}");
        }
    }

    /// <summary>
    /// Compute the list of patches that transform <paramref name="oldNode"/> into <paramref name="newNode"/>.
    /// </summary>
    /// <param name="oldNode">The previous virtual DOM node. Can be <c>null</c> when rendering for the first time.</param>
    /// <param name="newNode">The new virtual DOM node.</param>
    public static List<Patch> Diff(Node? oldNode, Node newNode)
    {
        var patches = RentPatchList();
        try
        {
            if (oldNode is null)
            {
                patches.Add(new AddRoot((Element)newNode));
                var result = new List<Patch>(patches);
                return result;
            }

            DiffInternal(oldNode, newNode, null, patches);
            var finalResult = new List<Patch>(patches);
            return finalResult;
        }
        finally
        {
            ReturnPatchList(patches);
        }
    }

    // =============================================================================
    // Memo Node Helpers
    // =============================================================================
    // Memo node diffing - uses IMemoNode interface for trim-safe access
    // =============================================================================

    // Debug counters for memo performance analysis
    internal static int MemoHits = 0;
    internal static int MemoMisses = 0;

    internal static void ResetMemoCounters()
    {
        MemoHits = 0;
        MemoMisses = 0;
    }

    /// <summary>
    /// Unwraps a memo node (lazy or regular) to get its actual content.
    /// For lazy memos, this evaluates the factory function.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Node UnwrapMemoNode(Node node)
    {
        return node switch
        {
            ILazyMemoNode lazy => lazy.CachedNode ?? lazy.Evaluate(),
            IMemoNode memo => memo.CachedNode,
            _ => node
        };
    }

    private static void DiffInternal(Node oldNode, Node newNode, Element? parent, List<Patch> patches)
    {
        // Quick bailout: if both nodes are the exact same reference, nothing to diff
        // This can happen when a cached node is reused across renders
        if (ReferenceEquals(oldNode, newNode))
        {
            return;
        }

        // Lazy memo nodes: defer evaluation until keys differ
        // This provides the true performance benefit - we don't even construct the node if unchanged
        // Uses MemoKeyEquals to avoid boxing overhead for value type keys
        if (oldNode is ILazyMemoNode oldLazy && newNode is ILazyMemoNode newLazy)
        {
            if (oldLazy.MemoKeyEquals(newLazy))
            {
                // Keys match - skip evaluation AND diffing entirely
                // Simple increment since WASM is single-threaded (no Interlocked needed)
                MemoHits++;
                return;
            }

            // Keys differ - evaluate the new lazy and diff
            MemoMisses++;
            var oldCached = oldLazy.CachedNode ?? oldLazy.Evaluate();
            var newCached = newLazy.Evaluate();
            DiffInternal(oldCached, newCached, parent, patches);
            return;
        }

        // Regular memo nodes: skip diffing subtree if keys are equal
        // This is similar to Elm's lazy function - major performance win for list items
        // Uses MemoKeyEquals to avoid boxing overhead for value type keys
        if (oldNode is IMemoNode oldMemo && newNode is IMemoNode newMemo)
        {
            if (oldMemo.MemoKeyEquals(newMemo))
            {
                // Keys match - skip diffing the subtree entirely
                MemoHits++;
                return;
            }

            // Keys differ - diff the cached nodes
            MemoMisses++;
            DiffInternal(oldMemo.CachedNode, newMemo.CachedNode, parent, patches);
            return;
        }

        // Unwrap any type of memo node (lazy or regular)
        var effectiveOld = UnwrapMemoNode(oldNode);
        var effectiveNew = UnwrapMemoNode(newNode);

        // If either was a memo, recurse with the unwrapped nodes
        if (!ReferenceEquals(effectiveOld, oldNode) || !ReferenceEquals(effectiveNew, newNode))
        {
            DiffInternal(effectiveOld, effectiveNew, parent, patches);
            return;
        }

        // Text nodes only need an update when the value changes
        if (oldNode is Text oldText && newNode is Text newText)
        {
            if (!string.Equals(oldText.Value, newText.Value, StringComparison.Ordinal) || !string.Equals(oldText.Id, newText.Id, StringComparison.Ordinal))
            {
                patches.Add(new UpdateText(oldText, newText.Value, newText.Id));
            }

            return;
        }

        if (oldNode is RawHtml oldRaw && newNode is RawHtml newRaw)
        {
            if (!string.Equals(oldRaw.Html, newRaw.Html, StringComparison.Ordinal) || !string.Equals(oldRaw.Id, newRaw.Id, StringComparison.Ordinal))
            {
                patches.Add(new UpdateRaw(oldRaw, newRaw.Html, newRaw.Id));
            }

            return;
        }

        // Elements may need to be replaced when the tag differs or the node type changed
        if (oldNode is Element oldElement && newNode is Element newElement)
        {
            // Early exit for reference equality only for elements with same tag
            if (ReferenceEquals(oldElement, newElement))
            {
                return;
            }

            if (!string.Equals(oldElement.Tag, newElement.Tag, StringComparison.Ordinal))
            {
                if (parent is null)
                {
                    patches.Add(new AddRoot(newElement));
                }
                else
                {
                    patches.Add(new ReplaceChild(oldElement, newElement));
                }

                return;
            }

            DiffAttributes(oldElement, newElement, patches);
            DiffChildren(oldElement, newElement, patches);
            return;
        }

        // Fallback for node type mismatch
        if (parent is not null)
        {
            if (oldNode is Element oe && newNode is Element ne)
            {
                patches.Add(new ReplaceChild(oe, ne));
            }
            else if (oldNode is RawHtml oldRaw2 && newNode is RawHtml newRaw2)
            {
                patches.Add(new ReplaceRaw(oldRaw2, newRaw2));
            }
            else if (oldNode is RawHtml r && newNode is Element ne2)
            {
                patches.Add(new ReplaceRaw(r, new RawHtml(ne2.Id, Render.Html(ne2))));
            }
            else if (oldNode is Element oe2 && newNode is RawHtml r2)
            {
                patches.Add(new ReplaceRaw(new RawHtml(oe2.Id, Render.Html(oe2)), r2));
            }
            else if (oldNode is Text ot && newNode is Text nt)
            {
                patches.Add(new UpdateText(ot, nt.Value, nt.Id));
            }
            else if (oldNode is Text ot2 && newNode is Element ne3)
            {
                // Replace text with element via raw representation
                patches.Add(new ReplaceRaw(new RawHtml(ot2.Id, Render.Html(ot2)), new RawHtml(ne3.Id, Render.Html(ne3))));
            }
            else if (oldNode is Element oe3 && newNode is Text nt2)
            {
                // Replace element with text via raw representation
                patches.Add(new ReplaceRaw(new RawHtml(oe3.Id, Render.Html(oe3)), new RawHtml(nt2.Id, Render.Html(nt2))));
            }
        }
    }

    // Diff attribute collections using dictionaries for O(n) lookup
    private static void DiffAttributes(Element oldElement, Element newElement, List<Patch> patches)
    {
        var oldAttrs = oldElement.Attributes;
        var newAttrs = newElement.Attributes;

        // Early exit for identical attribute arrays
        if (ReferenceEquals(oldAttrs, newAttrs))
        {
            return;
        }

        // Early exit for both empty
        if (oldAttrs.Length == 0 && newAttrs.Length == 0)
        {
            return;
        }

        // If old is empty, just add all new attributes
        if (oldAttrs.Length == 0)
        {
            foreach (var newAttr in newAttrs)
            {
                if (newAttr is Handler handler)
                {
                    patches.Add(new AddHandler(newElement, handler));
                }
                else
                {
                    patches.Add(new AddAttribute(newElement, newAttr));
                }
            }
            return;
        }

        // If new is empty, remove all old attributes
        if (newAttrs.Length == 0)
        {
            foreach (var oldAttr in oldAttrs)
            {
                if (oldAttr is Handler handler)
                {
                    patches.Add(new RemoveHandler(oldElement, handler));
                }
                else
                {
                    patches.Add(new RemoveAttribute(oldElement, oldAttr));
                }
            }
            return;
        }

        // =============================================================================
        // Same-Order Fast Path - Skip dictionary building when attributes match in order
        // =============================================================================
        // Most renders don't change attribute order or count. When old and new have
        // the same count, try comparing them positionally first. This avoids:
        // - Dictionary allocation and building (O(n) time + allocations)
        // - Dictionary lookups (hash computation overhead)
        // Only fall back to dictionary approach if names don't match.
        // =============================================================================
        if (oldAttrs.Length == newAttrs.Length)
        {
            var sameOrder = true;
            for (int i = 0; i < oldAttrs.Length; i++)
            {
                var oldAttrName = oldAttrs[i] is Handler oh ? oh.Name : oldAttrs[i].Name;
                var newAttrName = newAttrs[i] is Handler nh ? nh.Name : newAttrs[i].Name;
                if (!string.Equals(oldAttrName, newAttrName, StringComparison.Ordinal))
                {
                    sameOrder = false;
                    break;
                }
            }

            if (sameOrder)
            {
                // Same order: diff each attribute pair positionally
                for (int i = 0; i < oldAttrs.Length; i++)
                {
                    var oldAttr = oldAttrs[i];
                    var newAttr = newAttrs[i];
                    if (!newAttr.Equals(oldAttr))
                    {
                        if (oldAttr is Handler oldHandler && newAttr is Handler newHandler)
                        {
                            patches.Add(new UpdateHandler(newElement, oldHandler, newHandler));
                        }
                        else if (newAttr is Handler newHandler2)
                        {
                            if (oldAttr is Handler oldHandler2)
                            {
                                patches.Add(new UpdateHandler(newElement, oldHandler2, newHandler2));
                            }
                            else
                            {
                                patches.Add(new RemoveAttribute(oldElement, oldAttr));
                                patches.Add(new AddHandler(newElement, newHandler2));
                            }
                        }
                        else if (oldAttr is Handler oldHandler3)
                        {
                            patches.Add(new RemoveHandler(oldElement, oldHandler3));
                            patches.Add(new AddAttribute(newElement, newAttr));
                        }
                        else
                        {
                            patches.Add(new UpdateAttribute(oldElement, newAttr, newAttr.Value));
                        }
                    }
                }
                return;
            }
        }

        // Fall back to dictionary-based diffing for different order/count
        var oldMap = RentAttributeMap();
        try
        {
            // Use initial capacity hint
            if (oldMap.Count == 0 && oldAttrs.Length > 0)
            {
                oldMap.EnsureCapacity(oldAttrs.Length);
            }

            foreach (var attr in oldAttrs)
            {
                var attrName = attr is Handler h ? h.Name : attr.Name;
                oldMap[attrName] = attr;
            }

            foreach (var newAttr in newAttrs)
            {
                var newAttrName = newAttr is Handler nh ? nh.Name : newAttr.Name;
                if (oldMap.TryGetValue(newAttrName, out var oldAttr))
                {
                    oldMap.Remove(newAttrName);
                    if (!newAttr.Equals(oldAttr))
                    {
                        if (oldAttr is Handler oldHandler && newAttr is Handler newHandler)
                        {
                            // Same attribute id and name, but handler value changed: update atomically
                            patches.Add(new UpdateHandler(newElement, oldHandler, newHandler));
                        }
                        else
                        {
                            if (oldAttr is Handler oldHandler2)
                            {
                                patches.Add(new RemoveHandler(oldElement, oldHandler2));
                            }
                            else if (newAttr is Handler)
                            {
                                patches.Add(new RemoveAttribute(oldElement, oldAttr));
                            }

                            if (newAttr is Handler newHandler2)
                            {
                                patches.Add(new AddHandler(newElement, newHandler2));
                            }
                            else
                            {
                                patches.Add(new UpdateAttribute(oldElement, newAttr, newAttr.Value));
                            }
                        }
                    }
                }
                else
                {
                    if (newAttr is Handler handler)
                    {
                        patches.Add(new AddHandler(newElement, handler));
                    }
                    else
                    {
                        patches.Add(new AddAttribute(newElement, newAttr));
                    }
                }
            }

            // Any remaining old attributes must be removed
            foreach (var remaining in oldMap.Values)
            {
                if (remaining is Handler handler)
                {
                    patches.Add(new RemoveHandler(oldElement, handler));
                }
                else
                {
                    patches.Add(new RemoveAttribute(oldElement, remaining));
                }
            }
        }
        finally
        {
            ReturnAttributeMap(oldMap);
        }
    }

    private static void DiffChildren(Element oldParent, Element newParent, List<Patch> patches)
    {
        var oldChildren = oldParent.Children;
        var newChildren = newParent.Children;

        // Early exit for identical child arrays
        if (ReferenceEquals(oldChildren, newChildren))
        {
            return;
        }

        var oldLength = oldChildren.Length;
        var newLength = newChildren.Length;

        // Early exit for both empty - avoids ArrayPool rent/return overhead
        if (oldLength == 0 && newLength == 0)
        {
            return;
        }

        // ADR-016: Use ID-based keyed diffing for all elements.
        // Build maps of old and new children by their keys (element Id or data-key).
        // Use ArrayPool to avoid allocations
        var oldKeysArray = ArrayPool<string>.Shared.Rent(oldLength);
        var newKeysArray = ArrayPool<string>.Shared.Rent(newLength);

        try
        {
            BuildKeySequenceInto(oldChildren, oldKeysArray);
            BuildKeySequenceInto(newChildren, newKeysArray);

            var oldKeys = oldKeysArray.AsSpan(0, oldLength);
            var newKeys = newKeysArray.AsSpan(0, newLength);
            DiffChildrenCore(oldParent, newParent, oldChildren, newChildren, oldKeys, newKeys, patches);
        }
        finally
        {
            // Clear the arrays before returning to pool (avoid memory leaks of string references)
            Array.Clear(oldKeysArray, 0, oldLength);
            Array.Clear(newKeysArray, 0, newLength);
            ArrayPool<string>.Shared.Return(oldKeysArray);
            ArrayPool<string>.Shared.Return(newKeysArray);
        }
    }

    // =============================================================================
    // Small Count Fast Path Threshold
    // =============================================================================
    // For child counts below this threshold, use O(n²) linear scan instead of
    // building dictionaries. This eliminates dictionary allocation overhead for
    // common cases (most elements have < 8 children).
    //
    // Based on profiling: Dictionary allocation + hashing overhead exceeds O(n²)
    // scan cost for small n. Threshold of 8 chosen based on benchmarks.
    // =============================================================================
    private const int SmallChildCountThreshold = 8;

    /// <summary>
    /// Core diffing logic for child elements.
    /// </summary>
    private static void DiffChildrenCore(
        Element oldParent,
        Element newParent,
        Node[] oldChildren,
        Node[] newChildren,
        ReadOnlySpan<string> oldKeys,
        ReadOnlySpan<string> newKeys,
        List<Patch> patches)
    {
        var oldLength = oldChildren.Length;
        var newLength = newChildren.Length;

        // =============================================================================
        // Clear Fast Path - O(1) detection before building any dictionaries
        // =============================================================================
        // When clearing all children (oldLength > 0, newLength == 0), we can skip:
        // - Building key dictionaries (O(n) time + allocations)
        // - Key categorization loops
        // - The existing ClearChildren optimization is too late (after dict building)
        // =============================================================================
        if (oldLength > 0 && newLength == 0)
        {
            patches.Add(new ClearChildren(oldParent, oldChildren));
            return;
        }

        // =============================================================================
        // Add-All Fast Path - O(n) when starting from empty
        // =============================================================================
        // When adding all new children (oldLength == 0, newLength > 0), skip dict building
        // and directly add all children. This is common when initializing a list.
        // =============================================================================
        if (oldLength == 0 && newLength > 0)
        {
            foreach (var child in newChildren)
            {
                var effectiveNode = UnwrapMemoNode(child);
                if (effectiveNode is Element newChild)
                {
                    patches.Add(new AddChild(newParent, newChild));
                }
                else if (effectiveNode is RawHtml newRaw)
                {
                    patches.Add(new AddRaw(newParent, newRaw));
                }
                else if (effectiveNode is Text newText)
                {
                    patches.Add(new AddText(newParent, newText));
                }
            }
            return;
        }

        // =============================================================================
        // Head/Tail Skip - Three-Phase Keyed Diff Optimization
        // =============================================================================
        // Before building key maps, skip common prefix (head) and suffix (tail).
        // This optimization is especially effective for:
        // - Append-only scenarios (chat, logs, feeds) - O(1) instead of O(n)
        // - Prepend scenarios - O(1) head mismatch, tail skip handles most
        // - Single item changes - minimal middle section to diff
        //
        // For swap benchmark (swap positions 1 and 998 of 1000 items):
        // - Head: 1 element matches (index 0)
        // - Tail: 1 element matches (index 999)
        // - Middle: 998 elements still need key map + LIS
        // Even though swap only saves 2 elements, this prepares for append-only fast path.
        // =============================================================================
        int headSkip = 0;
        int tailSkip = 0;
        int minLength = Math.Min(oldLength, newLength);

        // Skip matching head (common prefix)
        while (headSkip < minLength && oldKeys[headSkip] == newKeys[headSkip])
        {
            // Diff the matching elements in place (they may have attribute/child changes)
            DiffInternal(oldChildren[headSkip], newChildren[headSkip], oldParent, patches);
            headSkip++;
        }

        // If all elements matched, handle length differences
        if (headSkip == minLength)
        {
            // Remove extra old children (from end)
            for (int i = oldLength - 1; i >= headSkip; i--)
            {
                var effectiveOld = UnwrapMemoNode(oldChildren[i]);
                if (effectiveOld is Element oldChild)
                {
                    patches.Add(new RemoveChild(oldParent, oldChild));
                }
                else if (effectiveOld is RawHtml oldRaw)
                {
                    patches.Add(new RemoveRaw(oldParent, oldRaw));
                }
                else if (effectiveOld is Text oldText)
                {
                    patches.Add(new RemoveText(oldParent, oldText));
                }
            }

            // Add extra new children
            for (int i = headSkip; i < newLength; i++)
            {
                var effectiveNew = UnwrapMemoNode(newChildren[i]);
                if (effectiveNew is Element newChild)
                {
                    patches.Add(new AddChild(newParent, newChild));
                }
                else if (effectiveNew is RawHtml newRaw)
                {
                    patches.Add(new AddRaw(newParent, newRaw));
                }
                else if (effectiveNew is Text newText)
                {
                    patches.Add(new AddText(newParent, newText));
                }
            }
            return;
        }

        // Skip matching tail (common suffix)
        // Be careful not to overlap with head
        int oldEnd = oldLength - 1;
        int newEnd = newLength - 1;
        while (oldEnd > headSkip && newEnd > headSkip &&
               oldKeys[oldEnd] == newKeys[newEnd])
        {
            // Diff the matching elements (they may have attribute/child changes)
            DiffInternal(oldChildren[oldEnd], newChildren[newEnd], oldParent, patches);
            tailSkip++;
            oldEnd--;
            newEnd--;
        }

        // Calculate middle section bounds
        int oldMiddleStart = headSkip;
        int oldMiddleEnd = oldLength - tailSkip; // exclusive
        int newMiddleStart = headSkip;
        int newMiddleEnd = newLength - tailSkip; // exclusive
        int oldMiddleLength = oldMiddleEnd - oldMiddleStart;
        int newMiddleLength = newMiddleEnd - newMiddleStart;

        // If middle is empty after skip, we're done
        if (oldMiddleLength == 0 && newMiddleLength == 0)
        {
            return;
        }

        // Handle middle-only clear (all middle elements removed)
        if (oldMiddleLength > 0 && newMiddleLength == 0)
        {
            for (int i = oldMiddleEnd - 1; i >= oldMiddleStart; i--)
            {
                var effectiveOld = UnwrapMemoNode(oldChildren[i]);
                if (effectiveOld is Element oldChild)
                {
                    patches.Add(new RemoveChild(oldParent, oldChild));
                }
                else if (effectiveOld is RawHtml oldRaw)
                {
                    patches.Add(new RemoveRaw(oldParent, oldRaw));
                }
                else if (effectiveOld is Text oldText)
                {
                    patches.Add(new RemoveText(oldParent, oldText));
                }
            }
            return;
        }

        // Handle middle-only add (all middle elements are new)
        if (oldMiddleLength == 0 && newMiddleLength > 0)
        {
            for (int i = newMiddleStart; i < newMiddleEnd; i++)
            {
                var effectiveNew = UnwrapMemoNode(newChildren[i]);
                if (effectiveNew is Element newChild)
                {
                    patches.Add(new AddChild(newParent, newChild));
                }
                else if (effectiveNew is RawHtml newRaw)
                {
                    patches.Add(new AddRaw(newParent, newRaw));
                }
                else if (effectiveNew is Text newText)
                {
                    patches.Add(new AddText(newParent, newText));
                }
            }
            return;
        }

        // Create slices for the middle section only
        var oldMiddleChildren = oldChildren.AsSpan(oldMiddleStart, oldMiddleLength);
        var newMiddleChildren = newChildren.AsSpan(newMiddleStart, newMiddleLength);
        var oldMiddleKeys = oldKeys.Slice(oldMiddleStart, oldMiddleLength);
        var newMiddleKeys = newKeys.Slice(newMiddleStart, newMiddleLength);

        // Fast path for small middle counts: use O(n²) linear scan instead of dictionaries
        // This eliminates dictionary allocation overhead for common cases
        if (oldMiddleLength <= SmallChildCountThreshold && newMiddleLength <= SmallChildCountThreshold)
        {
            // Use array-based version with ToArray() since we need Node[] not Span
            DiffChildrenSmall(oldParent, newParent, oldMiddleChildren.ToArray(), newMiddleChildren.ToArray(), oldMiddleKeys, newMiddleKeys, patches);
            return;
        }

        // Check if middle keys differ at all
        if (!oldMiddleKeys.SequenceEqual(newMiddleKeys))
        {
            // Build lookup maps for efficient matching - use pooled dictionaries
            // Note: Maps are built for MIDDLE section only (head/tail already handled)
            var oldKeyToIndex = RentKeyIndexMap();
            var newKeyToIndex = RentKeyIndexMap();

            try
            {
                oldKeyToIndex.EnsureCapacity(oldMiddleLength);
                for (int i = 0; i < oldMiddleLength; i++)
                {
                    oldKeyToIndex[oldMiddleKeys[i]] = i;
                }

                newKeyToIndex.EnsureCapacity(newMiddleLength);
                for (int i = 0; i < newMiddleLength; i++)
                {
                    newKeyToIndex[newMiddleKeys[i]] = i;
                }

                // Check if this is a reorder (same keys, different order) or a membership change
                // Avoid allocating HashSets - use dictionaries we already have
                var isReorder = oldMiddleLength == newMiddleLength && AreKeysSameSet(oldMiddleKeys, newKeyToIndex);

                if (isReorder)
                {
                    // Reorder detected: use LIS algorithm to minimize DOM moves
                    // 1. Build sequence of old indices in new order
                    // 2. Find LIS - elements in LIS don't need to be moved
                    // 3. Move elements NOT in LIS to their correct positions
                    // 4. Diff each matched element pair for attribute/child changes

                    // Build sequence: for each position in newMiddleChildren, get the old index
                    var oldIndices = ArrayPool<int>.Shared.Rent(newMiddleLength);
                    // Rent a bool array instead of allocating HashSet<int>
                    var inLIS = ArrayPool<bool>.Shared.Rent(newMiddleLength);
                    try
                    {
                        // Clear inLIS to avoid stale data from pool (ArrayPool doesn't zero memory)
                        inLIS.AsSpan(0, newMiddleLength).Clear();

                        for (int i = 0; i < newMiddleLength; i++)
                        {
                            oldIndices[i] = oldKeyToIndex[newMiddleKeys[i]];
                        }

                        // Find LIS of old indices - elements in LIS are already in correct relative order
                        // The indices returned are positions in oldIndices that form the LIS
                        // We mark those positions as "in LIS" (don't need moving)
                        ComputeLISInto(oldIndices.AsSpan(0, newMiddleLength), inLIS.AsSpan(0, newMiddleLength));

                        // First, diff all elements (they all exist in both old and new)
                        for (int i = 0; i < newMiddleLength; i++)
                        {
                            var oldIndex = oldIndices[i];
                            DiffInternal(oldMiddleChildren[oldIndex], newMiddleChildren[i], oldParent, patches);
                        }

                        // Move elements NOT in LIS to their correct positions
                        // Process in reverse order so we can use "insertBefore" with known reference
                        // IMPORTANT: We must use OLD element IDs since those are what exist in the DOM
                        for (int i = newMiddleLength - 1; i >= 0; i--)
                        {
                            if (!inLIS[i])
                            {
                                // This element needs to be moved
                                // Use OLD element since it has the ID currently in the DOM
                                // Unwrap memo nodes to get the actual element for patch creation
                                var oldIndex = oldIndices[i];
                                var oldNode = UnwrapMemoNode(oldMiddleChildren[oldIndex]);
                                if (oldNode is Element oldChildElement)
                                {
                                    // Find the element to insert before (the next sibling in new order)
                                    // Also use OLD element ID for the reference element
                                    // Unwrap memo nodes for the reference element too
                                    string? beforeId = null;
                                    if (i + 1 < newMiddleLength)
                                    {
                                        // Get the OLD element for position i+1 (its ID is in the DOM)
                                        var nextOldIndex = oldIndices[i + 1];
                                        var nextOldNode = UnwrapMemoNode(oldMiddleChildren[nextOldIndex]);
                                        beforeId = nextOldNode.Id;
                                    }
                                    else if (tailSkip > 0)
                                    {
                                        // Insert before the first tail element (which wasn't moved)
                                        var firstTailNode = UnwrapMemoNode(oldChildren[oldMiddleEnd]);
                                        beforeId = firstTailNode.Id;
                                    }
                                    patches.Add(new MoveChild(oldParent, oldChildElement, beforeId));
                                }
                                // Note: Text and RawHtml nodes would need similar handling
                                // but they typically don't have stable keys for reordering
                            }
                        }
                    }
                    finally
                    {
                        // No need to clear before returning - we clear on rent for safety
                        ArrayPool<bool>.Shared.Return(inLIS);
                        ArrayPool<int>.Shared.Return(oldIndices);
                    }
                    return;
                }

                // Membership change: some keys added, some removed
                // Find keys that exist in old but not in new (to remove)
                // Find keys that exist in new but not in old (to add)
                // Find keys that exist in both (to diff)
                // Use pooled lists
                var keysToRemove = RentIntList();
                var keysToAdd = RentIntList();
                var keysToDiff = RentIntPairList();

                try
                {
                    for (int i = 0; i < oldMiddleLength; i++)
                    {
                        if (newKeyToIndex.TryGetValue(oldMiddleKeys[i], out var newIndex))
                        {
                            keysToDiff.Add((i, newIndex));
                        }
                        else
                        {
                            keysToRemove.Add(i);
                        }
                    }

                    for (int i = 0; i < newMiddleLength; i++)
                    {
                        if (!oldKeyToIndex.ContainsKey(newMiddleKeys[i]))
                        {
                            keysToAdd.Add(i);
                        }
                    }

                    // Remove old children that don't exist in new (iterate backwards to maintain order)
                    // Note: ClearChildren case (newLength == 0) is handled by early exit above
                    for (int i = keysToRemove.Count - 1; i >= 0; i--)
                    {
                        var idx = keysToRemove[i];
                        // Unwrap memo nodes to get the actual content for patch creation
                        var effectiveOld = UnwrapMemoNode(oldMiddleChildren[idx]);

                        if (effectiveOld is Element oldChild)
                        {
                            patches.Add(new RemoveChild(oldParent, oldChild));
                        }
                        else if (effectiveOld is RawHtml oldRaw)
                        {
                            patches.Add(new RemoveRaw(oldParent, oldRaw));
                        }
                        else if (effectiveOld is Text oldText)
                        {
                            patches.Add(new RemoveText(oldParent, oldText));
                        }
                    }

                    // Diff children that exist in both trees
                    foreach (var (oldIndex, newIndex) in keysToDiff)
                    {
                        DiffInternal(oldMiddleChildren[oldIndex], newMiddleChildren[newIndex], oldParent, patches);
                    }

                    // Add new children that don't exist in old
                    foreach (var idx in keysToAdd)
                    {
                        // Unwrap memo nodes to get the actual content for patch creation
                        var effectiveNode = UnwrapMemoNode(newMiddleChildren[idx]);

                        if (effectiveNode is Element newChild)
                        {
                            patches.Add(new AddChild(newParent, newChild));
                        }
                        else if (effectiveNode is RawHtml newRaw)
                        {
                            patches.Add(new AddRaw(newParent, newRaw));
                        }
                        else if (effectiveNode is Text newText)
                        {
                            patches.Add(new AddText(newParent, newText));
                        }
                    }
                }
                finally
                {
                    ReturnIntList(keysToRemove);
                    ReturnIntList(keysToAdd);
                    ReturnIntPairList(keysToDiff);
                }
                return;
            }
            finally
            {
                ReturnKeyIndexMap(oldKeyToIndex);
                ReturnKeyIndexMap(newKeyToIndex);
            }
        }

        // Middle keys are identical: diff in place
        for (int i = 0; i < oldMiddleLength; i++)
        {
            DiffInternal(oldMiddleChildren[i], newMiddleChildren[i], oldParent, patches);
        }
    }

    /// <summary>
    /// Fast path for diffing small child lists using O(n²) linear scan.
    /// Avoids dictionary allocation overhead which dominates for small n.
    /// </summary>
    private static void DiffChildrenSmall(
        Element oldParent,
        Element newParent,
        Node[] oldChildren,
        Node[] newChildren,
        ReadOnlySpan<string> oldKeys,
        ReadOnlySpan<string> newKeys,
        List<Patch> patches)
    {
        var oldLength = oldChildren.Length;
        var newLength = newChildren.Length;

        // Optimization: if removing ALL children and adding none, use ClearChildren
        // This is the same optimization as in DiffChildrenCore
        if (oldLength > 0 && newLength == 0)
        {
            patches.Add(new ClearChildren(oldParent, oldChildren));
            return;
        }

        // Fast path: keys are identical (use SequenceEqual for small spans)
        if (oldKeys.SequenceEqual(newKeys))
        {
            // Keys match: diff children in place
            for (int i = 0; i < oldLength; i++)
            {
                DiffInternal(oldChildren[i], newChildren[i], oldParent, patches);
            }
            return;
        }

        // Use stackalloc for tracking matched indices (no heap allocation)
        // -1 = not matched, otherwise = index in the other array
        Span<int> oldMatched = stackalloc int[oldLength];
        Span<int> newMatched = stackalloc int[newLength];
        oldMatched.Fill(-1);
        newMatched.Fill(-1);

        // O(n²) matching: for each old key, find its position in new keys
        for (int i = 0; i < oldLength; i++)
        {
            var oldKey = oldKeys[i];
            for (int j = 0; j < newLength; j++)
            {
                if (newMatched[j] == -1 && string.Equals(oldKey, newKeys[j], StringComparison.Ordinal))
                {
                    oldMatched[i] = j;
                    newMatched[j] = i;
                    break;
                }
            }
        }

        // Check if this is a pure reorder (all keys matched)
        var allMatched = true;
        for (int i = 0; i < oldLength; i++)
        {
            if (oldMatched[i] == -1)
            {
                allMatched = false;
                break;
            }
        }

        if (allMatched && oldLength == newLength)
        {
            // Pure reorder: use simple approach for small lists
            // For small lists, just diff each matched pair and emit moves
            // Build old indices in new order for LIS
            Span<int> oldIndices = stackalloc int[newLength];
            for (int i = 0; i < newLength; i++)
            {
                oldIndices[i] = newMatched[i];
            }

            // For small lists, use simple in-LIS detection with stackalloc
            // Note: stackalloc doesn't zero-initialize, so we must clear it
            Span<bool> inLIS = stackalloc bool[newLength];
            inLIS.Clear();
            ComputeLISIntoSmall(oldIndices, inLIS);

            // Diff all matched pairs
            for (int i = 0; i < newLength; i++)
            {
                var oldIndex = oldIndices[i];
                DiffInternal(oldChildren[oldIndex], newChildren[i], oldParent, patches);
            }

            // Move elements not in LIS
            for (int i = newLength - 1; i >= 0; i--)
            {
                if (!inLIS[i])
                {
                    var oldIndex = oldIndices[i];
                    var oldNode = UnwrapMemoNode(oldChildren[oldIndex]);
                    if (oldNode is Element oldChildElement)
                    {
                        string? beforeId = null;
                        if (i + 1 < newLength)
                        {
                            var nextOldIndex = oldIndices[i + 1];
                            var nextOldNode = UnwrapMemoNode(oldChildren[nextOldIndex]);
                            beforeId = nextOldNode.Id;
                        }
                        patches.Add(new MoveChild(oldParent, oldChildElement, beforeId));
                    }
                }
            }
            return;
        }

        // Membership change: some added, some removed
        // Remove unmatched old children (backwards to maintain order)
        for (int i = oldLength - 1; i >= 0; i--)
        {
            if (oldMatched[i] == -1)
            {
                var effectiveOld = UnwrapMemoNode(oldChildren[i]);
                if (effectiveOld is Element oldChild)
                {
                    patches.Add(new RemoveChild(oldParent, oldChild));
                }
                else if (effectiveOld is RawHtml oldRaw)
                {
                    patches.Add(new RemoveRaw(oldParent, oldRaw));
                }
                else if (effectiveOld is Text oldText)
                {
                    patches.Add(new RemoveText(oldParent, oldText));
                }
            }
        }

        // Diff matched children
        for (int i = 0; i < oldLength; i++)
        {
            if (oldMatched[i] != -1)
            {
                DiffInternal(oldChildren[i], newChildren[oldMatched[i]], oldParent, patches);
            }
        }

        // Add unmatched new children
        for (int i = 0; i < newLength; i++)
        {
            if (newMatched[i] == -1)
            {
                var effectiveNode = UnwrapMemoNode(newChildren[i]);
                if (effectiveNode is Element newChild)
                {
                    patches.Add(new AddChild(newParent, newChild));
                }
                else if (effectiveNode is RawHtml newRaw)
                {
                    patches.Add(new AddRaw(newParent, newRaw));
                }
                else if (effectiveNode is Text newText)
                {
                    patches.Add(new AddText(newParent, newText));
                }
            }
        }
    }

    /// <summary>
    /// Simplified LIS computation for small arrays using stackalloc.
    /// Same algorithm as ComputeLISInto but avoids ArrayPool overhead.
    /// </summary>
    private static void ComputeLISIntoSmall(ReadOnlySpan<int> arr, Span<bool> inLIS)
    {
        var len = arr.Length;
        if (len == 0)
        {
            return;
        }

        // For small arrays, use stackalloc instead of ArrayPool
        Span<int> result = stackalloc int[len];
        Span<int> p = stackalloc int[len];

        var lisLen = 0;

        for (int i = 0; i < len; i++)
        {
            var val = arr[i];

            int lo = 0, hi = lisLen;
            while (lo < hi)
            {
                var mid = (lo + hi) >> 1;
                if (arr[result[mid]] < val)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                }
            }

            if (lo > 0)
            {
                p[i] = result[lo - 1];
            }

            result[lo] = i;

            if (lo == lisLen)
            {
                lisLen++;
            }
        }

        if (lisLen > 0)
        {
            var idx = result[lisLen - 1];
            for (int i = lisLen - 1; i >= 0; i--)
            {
                inLIS[idx] = true;
                idx = p[idx];
            }
        }
    }

    /// <summary>
    /// Checks if all keys in oldKeys exist in newKeyToIndex (same set, possibly different order).
    /// </summary>
    private static bool AreKeysSameSet(ReadOnlySpan<string> oldKeys, Dictionary<string, int> newKeyToIndex)
    {
        foreach (var key in oldKeys)
        {
            if (!newKeyToIndex.ContainsKey(key))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Computes the Longest Increasing Subsequence (LIS) of the input array and marks
    /// the positions that are in the LIS in the output bool span.
    /// Used for optimal DOM reordering - elements in the LIS don't need to be moved.
    ///
    /// Algorithm: O(n log n) using binary search with patience sorting.
    /// Inspired by Inferno's virtual DOM implementation.
    /// 
    /// This version uses ArrayPool to avoid allocations on the hot path.
    /// </summary>
    /// <param name="arr">Array of old indices in new order.</param>
    /// <param name="inLIS">Output span where inLIS[i] = true if position i is in the LIS.</param>
    private static void ComputeLISInto(ReadOnlySpan<int> arr, Span<bool> inLIS)
    {
        var len = arr.Length;
        if (len == 0)
        {
            return;
        }

        // Rent pooled arrays to avoid allocations
        // result[j] = index in arr of smallest ending value for LIS of length j+1
        // p[i] = predecessor index for position i in the LIS chain
        var result = ArrayPool<int>.Shared.Rent(len);
        var p = ArrayPool<int>.Shared.Rent(len);

        try
        {
            var lisLen = 0; // Actual length of longest LIS found

            for (int i = 0; i < len; i++)
            {
                var val = arr[i];

                // Binary search to find position where val fits in result[0..lisLen)
                // We want the leftmost position where arr[result[pos]] >= val
                int lo = 0, hi = lisLen;
                while (lo < hi)
                {
                    var mid = (lo + hi) >> 1;
                    if (arr[result[mid]] < val)
                    {
                        lo = mid + 1;
                    }
                    else
                    {
                        hi = mid;
                    }
                }

                // lo is the position where val fits
                // Set predecessor (element before this in the LIS chain)
                if (lo > 0)
                {
                    p[i] = result[lo - 1];
                }

                // Update result - this position i has the smallest ending value for LIS of length lo+1
                result[lo] = i;

                // If we extended the LIS, increment length
                if (lo == lisLen)
                {
                    lisLen++;
                }
            }

            // Mark LIS positions by following predecessor chain backwards
            // Start from the last element of the LIS (result[lisLen-1])
            if (lisLen > 0)
            {
                var idx = result[lisLen - 1];
                for (int i = lisLen - 1; i >= 0; i--)
                {
                    inLIS[idx] = true;
                    idx = p[idx];
                }
            }
        }
        finally
        {
            ArrayPool<int>.Shared.Return(result);
            ArrayPool<int>.Shared.Return(p);
        }
    }

    /// <summary>
    /// Builds key sequence into a pre-allocated array (avoiding allocation).
    /// Uses cached index strings for non-keyed children to eliminate string interpolation.
    /// </summary>
    private static void BuildKeySequenceInto(Node[] children, string[] keys)
    {
        for (int i = 0; i < children.Length; i++)
        {
            keys[i] = GetKey(children[i]) ?? GetIndexString(i);
        }
    }

    /// <summary>
    /// Gets the key for a node used in keyed diffing.
    /// Per ADR-016: data-key/key attribute is an explicit override; element Id is the default key.
    /// Optimized with fast paths for common node types to avoid interface dispatch.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string? GetKey(Node node)
    {
        // Fast path for common case: Element (vast majority of nodes)
        // Check this first to avoid interface dispatch overhead for IMemoNode/ILazyMemoNode
        if (node is Element element)
        {
            return GetElementKey(element);
        }

        // Fast path: Text and RawHtml nodes have no key
        if (node is Text or RawHtml)
        {
            return null;
        }

        // Slow path: Memo nodes (rare in practice)
        if (node is ILazyMemoNode)
        {
            // Use the lazy node's ID as the key - don't evaluate just for key lookup
            return node.Id;
        }

        if (node is IMemoNode memo)
        {
            // Recurse into the cached content
            return GetKey(memo.CachedNode);
        }

        return null;
    }

    /// <summary>
    /// Gets the key for an Element node.
    /// Checks for explicit data-key/key attribute first, then falls back to element Id.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetElementKey(Element element)
    {
        // ADR-016: Use element Id as the primary key for diffing.
        // This allows developers to set stable IDs on elements,
        // and the diff algorithm will correctly match elements by ID.
        // The Id is always present, so we use it directly.
        // Only treat auto-generated IDs (from Praefixum) as non-keyed
        // by checking for data-key attribute as an explicit override.

        // First, check for explicit data-key attribute (backward compatibility)
        var attrs = element.Attributes;
        for (int i = 0; i < attrs.Length; i++)
        {
            var name = attrs[i].Name;
            if (name == "data-key" || name == "key")
            {
                return attrs[i].Value;
            }
        }

        // Use element Id as the key
        // Element IDs are always unique, making them ideal for keyed diffing
        return element.Id;
    }
}
