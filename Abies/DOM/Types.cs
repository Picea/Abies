// =============================================================================
// Virtual DOM Types
// =============================================================================
// Platform-independent types that describe the virtual DOM tree.
// These types form an Abstract Syntax Tree (AST) that can be interpreted
// by different renderers (browser, server, test).
//
// Architecture Decision Records:
// - ADR-003: Virtual DOM (docs/adr/ADR-003-virtual-dom.md)
// - ADR-008: Immutable State Management (docs/adr/ADR-008-immutable-state.md)
// =============================================================================

namespace Abies.DOM;

/// <summary>
/// Represents a document in the Abies DOM.
/// </summary>
/// <param name="Title">The title of the document.</param>
/// <param name="Body">The body content of the document.</param>
/// <param name="Head">Optional managed <c>&lt;head&gt;</c> elements (meta tags, OG, links, JSON-LD).</param>
/// <remarks>
/// The <paramref name="Head"/> parameter is optional for backward compatibility:
/// <c>new Document("title", body)</c> continues to work unchanged.
/// </remarks>
public record Document(string Title, Node Body, params HeadContent[] Head);

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
public interface MemoNode
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
    bool MemoKeyEquals(MemoNode other);
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
public record Memo<TKey>(string Id, TKey Key, Node CachedNode) : Node(Id), MemoNode where TKey : notnull
{
    /// <summary>Gets the memo key as object for interface implementation.</summary>
    object MemoNode.MemoKey => Key;

    /// <summary>Creates a new memo with the same key but different cached content.</summary>
    public Node WithCachedNode(Node newCachedNode) => this with { CachedNode = newCachedNode };

    /// <summary>
    /// Compares memo keys without boxing overhead using generic EqualityComparer.
    /// </summary>
    public bool MemoKeyEquals(MemoNode other) =>
        other is Memo<TKey> otherMemo && EqualityComparer<TKey>.Default.Equals(Key, otherMemo.Key);
}

/// <summary>
/// Interface for lazy memoized nodes that defer evaluation until needed.
/// This is the key performance optimization - the function is only called if the key differs.
/// </summary>
public interface LazyMemoNode
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
    bool MemoKeyEquals(LazyMemoNode other);
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
public record LazyMemo<TKey>(string Id, TKey Key, Func<Node> Factory, Node? CachedNode = null) : Node(Id), LazyMemoNode where TKey : notnull
{
    /// <summary>Gets the memo key as object for interface implementation.</summary>
    object LazyMemoNode.MemoKey => Key;

    /// <summary>Gets the cached node content.</summary>
    Node? LazyMemoNode.CachedNode => CachedNode;

    /// <summary>Evaluates the lazy function to produce the node content.</summary>
    public Node Evaluate() => Factory();

    /// <summary>Creates a new lazy memo with the cached content populated.</summary>
    public Node WithCachedNode(Node cachedNode) => this with { CachedNode = cachedNode };

    /// <summary>
    /// Compares memo keys without boxing overhead using generic EqualityComparer.
    /// </summary>
    public bool MemoKeyEquals(LazyMemoNode other) =>
        other is LazyMemo<TKey> otherLazy && EqualityComparer<TKey>.Default.Equals(Key, otherLazy.Key);
}

/// <summary>
/// Represents a text node in the Abies DOM.
/// </summary>
/// <param name="Id">The unique identifier for the text node.</param>
/// <param name="Value">The text content of the node.</param>
public record Text(string Id, string Value) : Node(Id)
{
    public static implicit operator string(Text text) => text.Value;
    public static implicit operator Text(string text) => new(text, text);
}

/// <summary>
/// Represents an empty node in the Abies DOM.
/// </summary>
public record Empty() : Node("");
