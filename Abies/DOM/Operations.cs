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
using System.Collections.Concurrent;
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

// =============================================================================
// Batch Patching - Performance Optimization
// =============================================================================
// Converts patches to a JSON-serializable format for batch application.
// This allows sending multiple patches to JavaScript in a single interop call,
// dramatically reducing the overhead of N patches × N JS interop calls.
//
// Architecture Decision Records:
// - ADR-003: Virtual DOM (docs/adr/ADR-003-virtual-dom.md)
// - ADR-011: JavaScript Interop Strategy (docs/adr/ADR-011-javascript-interop.md)
// =============================================================================

/// <summary>
/// JSON-serializable patch data for batch patching.
/// All fields are nullable since different patch types use different fields.
/// </summary>
public record PatchData
{
    public string Type { get; init; } = "";
    public string? ParentId { get; init; }
    public string? TargetId { get; init; }
    public string? ChildId { get; init; }
    public string? Html { get; init; }
    public string? AttrName { get; init; }
    public string? AttrValue { get; init; }
    public string? NewId { get; init; }
    public string? Text { get; init; }
    public string? BeforeId { get; init; }
}


/// <summary>
/// Provides rendering utilities for the virtual DOM.
/// </summary>
public static class Render
{
    // StringBuilder pool to avoid allocations during rendering
    private static readonly ConcurrentQueue<StringBuilder> _stringBuilderPool = new();
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
        if (_stringBuilderPool.TryDequeue(out var sb))
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
            _stringBuilderPool.Enqueue(sb);
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
            // Handle other node types if necessary
            default:
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
    private static readonly ConcurrentQueue<List<Patch>> _patchListPool = new();
    private static readonly ConcurrentQueue<Dictionary<string, Attribute>> _attributeMapPool = new();
    private static readonly ConcurrentQueue<Dictionary<string, int>> _keyIndexMapPool = new();
    private static readonly ConcurrentQueue<List<int>> _intListPool = new();
    private static readonly ConcurrentQueue<List<(int, int)>> _intPairListPool = new();

    private static List<Patch> RentPatchList()
    {
        if (_patchListPool.TryDequeue(out var list))
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
            _patchListPool.Enqueue(list);
        }
    }

    private static Dictionary<string, Attribute> RentAttributeMap()
    {
        if (_attributeMapPool.TryDequeue(out var map))
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
            _attributeMapPool.Enqueue(map);
        }
    }

    private static Dictionary<string, int> RentKeyIndexMap()
    {
        if (_keyIndexMapPool.TryDequeue(out var map))
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
            _keyIndexMapPool.Enqueue(map);
        }
    }

    private static List<int> RentIntList()
    {
        if (_intListPool.TryDequeue(out var list))
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
            _intListPool.Enqueue(list);
        }
    }

    private static List<(int, int)> RentIntPairList()
    {
        if (_intPairListPool.TryDequeue(out var list))
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
            _intPairListPool.Enqueue(list);
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
    /// Apply a batch of patches to the real DOM in a single JavaScript interop call.
    /// This is more efficient than calling Apply() for each patch individually
    /// because it reduces the number of JS interop boundary crossings.
    /// </summary>
    /// <param name="patches">The list of patches to apply.</param>
    public static async Task ApplyBatch(List<Patch> patches)
    {
        if (patches.Count == 0)
        {
            return;
        }

        // Step 1: Pre-register all new handlers BEFORE making any DOM changes
        // This ensures events dispatched immediately after DOM updates are handled correctly
        foreach (var patch in patches)
        {
            switch (patch)
            {
                case AddRoot addRoot:
                    Runtime.RegisterHandlers(addRoot.Element);
                    break;
                case ReplaceChild replaceChild:
                    Runtime.UnregisterHandlers(replaceChild.OldElement);
                    Runtime.RegisterHandlers(replaceChild.NewElement);
                    break;
                case AddChild addChild:
                    Runtime.RegisterHandlers(addChild.Child);
                    break;
                case RemoveChild removeChild:
                    Runtime.UnregisterHandlers(removeChild.Child);
                    break;
                case AddHandler addHandler:
                    Runtime.RegisterHandler(addHandler.Handler);
                    break;
                case RemoveHandler:
                    // Don't unregister yet - wait until after DOM update
                    break;
                case UpdateHandler updateHandler:
                    Runtime.RegisterHandler(updateHandler.NewHandler);
                    break;
            }
        }

        // Step 2: Convert all patches to JSON-serializable format
        var patchDataList = new List<PatchData>(patches.Count);
        foreach (var patch in patches)
        {
            patchDataList.Add(ConvertToPatchData(patch));
        }

        // Step 3: Apply all patches in a single JS interop call
        var json = System.Text.Json.JsonSerializer.Serialize(patchDataList, AbiesJsonContext.Default.ListPatchData);
        await Interop.ApplyPatches(json);

        // Step 4: Post-process - unregister old handlers AFTER DOM changes
        foreach (var patch in patches)
        {
            switch (patch)
            {
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
    /// Convert a Patch to a JSON-serializable PatchData record.
    /// </summary>
    private static PatchData ConvertToPatchData(Patch patch)
    {
        return patch switch
        {
            AddRoot addRoot => new PatchData
            {
                Type = "SetAppContent",
                Html = Render.Html(addRoot.Element)
            },
            ReplaceChild replaceChild => new PatchData
            {
                Type = "ReplaceChild",
                TargetId = replaceChild.OldElement.Id,
                Html = Render.Html(replaceChild.NewElement)
            },
            AddChild addChild => new PatchData
            {
                Type = "AddChild",
                ParentId = addChild.Parent.Id,
                Html = Render.Html(addChild.Child)
            },
            RemoveChild removeChild => new PatchData
            {
                Type = "RemoveChild",
                ParentId = removeChild.Parent.Id,
                ChildId = removeChild.Child.Id
            },
            UpdateAttribute updateAttribute => new PatchData
            {
                Type = "UpdateAttribute",
                TargetId = updateAttribute.Element.Id,
                AttrName = updateAttribute.Attribute.Name,
                AttrValue = updateAttribute.Value
            },
            AddAttribute addAttribute => new PatchData
            {
                Type = "AddAttribute",
                TargetId = addAttribute.Element.Id,
                AttrName = addAttribute.Attribute.Name,
                AttrValue = addAttribute.Attribute.Value
            },
            RemoveAttribute removeAttribute => new PatchData
            {
                Type = "RemoveAttribute",
                TargetId = removeAttribute.Element.Id,
                AttrName = removeAttribute.Attribute.Name
            },
            AddHandler addHandler => new PatchData
            {
                Type = "AddAttribute",
                TargetId = addHandler.Element.Id,
                AttrName = addHandler.Handler.Name,
                AttrValue = addHandler.Handler.Value
            },
            RemoveHandler removeHandler => new PatchData
            {
                Type = "RemoveAttribute",
                TargetId = removeHandler.Element.Id,
                AttrName = removeHandler.Handler.Name
            },
            UpdateHandler updateHandler => new PatchData
            {
                Type = "UpdateAttribute",
                TargetId = updateHandler.Element.Id,
                AttrName = updateHandler.NewHandler.Name,
                AttrValue = updateHandler.NewHandler.Value
            },
            UpdateText updateText => updateText.Node.Id == updateText.NewId
                ? new PatchData
                {
                    Type = "UpdateText",
                    TargetId = updateText.Node.Id,
                    Text = updateText.Text
                }
                : new PatchData
                {
                    Type = "UpdateTextWithId",
                    TargetId = updateText.Node.Id,
                    Text = updateText.Text,
                    NewId = updateText.NewId
                },
            AddText addText => new PatchData
            {
                Type = "AddChild",
                ParentId = addText.Parent.Id,
                Html = Render.Html(addText.Child)
            },
            RemoveText removeText => new PatchData
            {
                Type = "RemoveChild",
                ParentId = removeText.Parent.Id,
                ChildId = removeText.Child.Id
            },
            AddRaw addRaw => new PatchData
            {
                Type = "AddChild",
                ParentId = addRaw.Parent.Id,
                Html = Render.Html(addRaw.Child)
            },
            RemoveRaw removeRaw => new PatchData
            {
                Type = "RemoveChild",
                ParentId = removeRaw.Parent.Id,
                ChildId = removeRaw.Child.Id
            },
            ReplaceRaw replaceRaw => new PatchData
            {
                Type = "ReplaceChild",
                TargetId = replaceRaw.OldNode.Id,
                Html = Render.Html(replaceRaw.NewNode)
            },
            UpdateRaw updateRaw => new PatchData
            {
                Type = "ReplaceChild",
                TargetId = updateRaw.Node.Id,
                Html = Render.Html(new RawHtml(updateRaw.NewId, updateRaw.Html))
            },
            MoveChild moveChild => new PatchData
            {
                Type = "MoveChild",
                ParentId = moveChild.Parent.Id,
                ChildId = moveChild.Child.Id,
                BeforeId = moveChild.BeforeId
            },
            _ => throw new InvalidOperationException($"Unknown patch type: {patch.GetType().Name}")
        };
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

    private static void DiffInternal(Node oldNode, Node newNode, Element? parent, List<Patch> patches)
    {
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

        // Check if keys differ at all
        if (!oldKeys.SequenceEqual(newKeys))
        {
            // Build lookup maps for efficient matching - use pooled dictionaries
            var oldKeyToIndex = RentKeyIndexMap();
            var newKeyToIndex = RentKeyIndexMap();

            try
            {
                oldKeyToIndex.EnsureCapacity(oldLength);
                for (int i = 0; i < oldLength; i++)
                {
                    oldKeyToIndex[oldKeys[i]] = i;
                }

                newKeyToIndex.EnsureCapacity(newLength);
                for (int i = 0; i < newLength; i++)
                {
                    newKeyToIndex[newKeys[i]] = i;
                }

                // Check if this is a reorder (same keys, different order) or a membership change
                // Avoid allocating HashSets - use dictionaries we already have
                var isReorder = oldLength == newLength && AreKeysSameSet(oldKeys, newKeyToIndex);

                if (isReorder)
                {
                    // Reorder detected: use LIS algorithm to minimize DOM moves
                    // 1. Build sequence of old indices in new order
                    // 2. Find LIS - elements in LIS don't need to be moved
                    // 3. Move elements NOT in LIS to their correct positions
                    // 4. Diff each matched element pair for attribute/child changes

                    // Build sequence: for each position in newChildren, get the old index
                    var oldIndices = ArrayPool<int>.Shared.Rent(newLength);
                    try
                    {
                        for (int i = 0; i < newLength; i++)
                        {
                            oldIndices[i] = oldKeyToIndex[newKeys[i]];
                        }

                        // Find LIS of old indices - elements in LIS are already in correct relative order
                        var lisIndices = ComputeLIS(oldIndices.AsSpan(0, newLength));

                        // Create a set of new positions that are in the LIS (don't need moving)
                        var inLIS = new HashSet<int>(lisIndices);

                        // First, diff all elements (they all exist in both old and new)
                        for (int i = 0; i < newLength; i++)
                        {
                            var oldIndex = oldIndices[i];
                            DiffInternal(oldChildren[oldIndex], newChildren[i], oldParent, patches);
                        }

                        // Move elements NOT in LIS to their correct positions
                        // Process in reverse order so we can use "insertBefore" with known reference
                        // IMPORTANT: We must use OLD element IDs since those are what exist in the DOM
                        for (int i = newLength - 1; i >= 0; i--)
                        {
                            if (!inLIS.Contains(i))
                            {
                                // This element needs to be moved
                                // Use OLD element since it has the ID currently in the DOM
                                var oldIndex = oldIndices[i];
                                var oldNode = oldChildren[oldIndex];
                                if (oldNode is Element oldChildElement)
                                {
                                    // Find the element to insert before (the next sibling in new order)
                                    // Also use OLD element ID for the reference element
                                    string? beforeId = null;
                                    if (i + 1 < newLength)
                                    {
                                        // Get the OLD element for position i+1 (its ID is in the DOM)
                                        var nextOldIndex = oldIndices[i + 1];
                                        beforeId = oldChildren[nextOldIndex].Id;
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
                    for (int i = 0; i < oldLength; i++)
                    {
                        if (newKeyToIndex.TryGetValue(oldKeys[i], out var newIndex))
                        {
                            keysToDiff.Add((i, newIndex));
                        }
                        else
                        {
                            keysToRemove.Add(i);
                        }
                    }

                    for (int i = 0; i < newLength; i++)
                    {
                        if (!oldKeyToIndex.ContainsKey(newKeys[i]))
                        {
                            keysToAdd.Add(i);
                        }
                    }

                    // Remove old children that don't exist in new (iterate backwards to maintain order)
                    for (int i = keysToRemove.Count - 1; i >= 0; i--)
                    {
                        var idx = keysToRemove[i];
                        if (oldChildren[idx] is Element oldChild)
                        {
                            patches.Add(new RemoveChild(oldParent, oldChild));
                        }
                        else if (oldChildren[idx] is RawHtml oldRaw)
                        {
                            patches.Add(new RemoveRaw(oldParent, oldRaw));
                        }
                        else if (oldChildren[idx] is Text oldText)
                        {
                            patches.Add(new RemoveText(oldParent, oldText));
                        }
                    }

                    // Diff children that exist in both trees
                    foreach (var (oldIndex, newIndex) in keysToDiff)
                    {
                        DiffInternal(oldChildren[oldIndex], newChildren[newIndex], oldParent, patches);
                    }

                    // Add new children that don't exist in old
                    foreach (var idx in keysToAdd)
                    {
                        if (newChildren[idx] is Element newChild)
                        {
                            patches.Add(new AddChild(newParent, newChild));
                        }
                        else if (newChildren[idx] is RawHtml newRaw)
                        {
                            patches.Add(new AddRaw(newParent, newRaw));
                        }
                        else if (newChildren[idx] is Text newText)
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

        // Keys are identical: diff in place
        var shared = Math.Min(oldLength, newLength);
        for (int i = 0; i < shared; i++)
        {
            DiffInternal(oldChildren[i], newChildren[i], oldParent, patches);
        }

        // Remove extra old children (iterate backwards to maintain DOM order)
        for (int i = oldLength - 1; i >= shared; i--)
        {
            if (oldChildren[i] is Element oldChild)
            {
                patches.Add(new RemoveChild(oldParent, oldChild));
            }
            else if (oldChildren[i] is RawHtml oldRaw)
            {
                patches.Add(new RemoveRaw(oldParent, oldRaw));
            }
            else if (oldChildren[i] is Text oldText)
            {
                patches.Add(new RemoveText(oldParent, oldText));
            }
        }

        // Add additional new children
        for (int i = shared; i < newLength; i++)
        {
            if (newChildren[i] is Element newChild)
            {
                patches.Add(new AddChild(newParent, newChild));
            }
            else if (newChildren[i] is RawHtml newRaw)
            {
                patches.Add(new AddRaw(newParent, newRaw));
            }
            else if (newChildren[i] is Text newText)
            {
                patches.Add(new AddText(newParent, newText));
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
    /// Computes the Longest Increasing Subsequence (LIS) of the input array.
    /// Returns the indices in the input array that form the LIS.
    /// Used for optimal DOM reordering - elements in the LIS don't need to be moved.
    /// 
    /// Algorithm: O(n log n) using binary search with patience sorting.
    /// Inspired by Inferno's virtual DOM implementation.
    /// </summary>
    /// <param name="arr">Array of old indices in new order.</param>
    /// <returns>Indices in the input array that form the LIS.</returns>
    private static int[] ComputeLIS(ReadOnlySpan<int> arr)
    {
        var len = arr.Length;
        if (len == 0)
        {
            return [];
        }

        // result[i] = index in arr of smallest ending element of LIS of length i+1
        var result = new int[len];
        // p[i] = predecessor index in arr for element at arr[i] in the LIS
        var p = new int[len];
        var k = 0; // Length of longest LIS found - 1

        for (int i = 0; i < len; i++)
        {
            var arrI = arr[i];

            // Binary search for position to insert arrI
            if (k > 0 && arr[result[k]] < arrI)
            {
                // arrI extends the longest LIS
                p[i] = result[k];
                result[++k] = i;
            }
            else
            {
                // Binary search to find the smallest LIS ending value >= arrI
                int lo = 0, hi = k;
                while (lo < hi)
                {
                    var mid = (lo + hi) >> 1;
                    if (arr[result[mid]] < arrI)
                    {
                        lo = mid + 1;
                    }
                    else
                    {
                        hi = mid;
                    }
                }

                // Update result and predecessor
                if (lo > 0)
                {
                    p[i] = result[lo - 1];
                }
                result[lo] = i;
                if (lo > k)
                {
                    k = lo;
                }
            }
        }

        // Reconstruct LIS by following predecessor chain
        var lisLength = k + 1;
        var lis = new int[lisLength];
        var idx = result[k];
        for (int i = lisLength - 1; i >= 0; i--)
        {
            lis[i] = idx;
            idx = p[idx];
        }

        return lis;
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
    /// Per ADR-016: Element Id is the primary key, with data-key/key attribute as fallback.
    /// </summary>
    private static string? GetKey(Node node)
    {
        if (node is not Element element)
        {
            return null;
        }

        // ADR-016: Use element Id as the primary key for diffing.
        // This allows developers to set stable IDs on elements,
        // and the diff algorithm will correctly match elements by ID.
        // The Id is always present, so we use it directly.
        // Only treat auto-generated IDs (from Praefixum) as non-keyed
        // by checking for data-key attribute as an explicit override.

        // First, check for explicit data-key attribute (backward compatibility)
        foreach (var attr in element.Attributes)
        {
            if (attr.Name == "data-key" || attr.Name == "key")
            {
                return attr.Value;
            }
        }

        // Use element Id as the key
        // Element IDs are always unique, making them ideal for keyed diffing
        return element.Id;
    }
}
