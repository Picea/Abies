// =============================================================================
// Virtual DOM Node Types
// =============================================================================
// Platform-independent node types that form the virtual DOM tree.
// These are the fundamental building blocks of the Abies rendering model.
//
// The tree structure:
//   Document
//     └── Body: Node (the root of the view tree)
//         ├── Element(Id, Tag, Attributes, Children...)
//         ├── Text(Id, Content)
//         ├── RawHtml(Id, Html)
//         ├── Empty
//         ├── Memo<TKey>(Id, Key, Node)
//         └── LazyMemo<TKey>(Id, Key, Factory, CachedNode?)
//
// Design decisions:
//   • All nodes carry an Id (typically from Praefixum compile-time generation)
//     for O(1) element lookup during diffing and DOM patching.
//   • Memo<TKey> and LazyMemo<TKey> enable O(1) subtree skip when the key
//     hasn't changed — the diff algorithm checks key equality and skips the
//     entire subtree if keys match.
//   • LazyMemo defers view construction until needed (key mismatch), reducing
//     allocation for unchanged subtrees.
//   • Children are stored as a flat array (params Node[]) rather than a list
//     for allocation efficiency and pattern matching.
//   • Document is the top-level container carrying the page title, head
//     content (meta tags, stylesheets), and the body node tree.
//
// See also:
//   - Element, Text, RawHtml, Empty — concrete node types below
//   - Memo<TKey>, LazyMemo<TKey> — memoization wrappers
//   - Patch.cs — diff output types
//   - Attribute.cs — element attribute types
// =============================================================================

namespace Picea.Abies.DOM;

/// <summary>
/// A complete document: title + head content + body node tree.
/// This is the return type of <c>Program.View(model)</c>.
/// </summary>
/// <param name="Title">The page title (rendered as &lt;title&gt; in the head).</param>
/// <param name="Body">The root of the view tree (rendered inside &lt;body&gt;).</param>
/// <param name="Head">Head elements: meta tags, stylesheets, canonical links, etc.</param>
public record Document(string Title, Node Body, params HeadContent[] Head);

/// <summary>
/// Base type for all virtual DOM nodes.
/// </summary>
public abstract record Node;

/// <summary>
/// An HTML element with a tag, attributes, and optional children.
/// </summary>
/// <param name="Id">Unique identifier for O(1) lookup during diffing.</param>
/// <param name="Tag">The HTML tag name (e.g., "div", "span", "button").</param>
/// <param name="Attributes">Attributes and event handlers on this element.</param>
/// <param name="Children">Child nodes (elements, text, raw HTML, etc.).</param>
public record Element(string Id, string Tag, Attribute[] Attributes, params Node[] Children) : Node;

/// <summary>
/// A text node. Content is HTML-encoded during rendering.
/// </summary>
/// <param name="Id">Unique identifier.</param>
/// <param name="Content">The text content (will be HTML-encoded).</param>
public record Text(string Id, string Content) : Node;

/// <summary>
/// A raw HTML node. Content is rendered as-is (not encoded).
/// Wrapped in a &lt;span&gt; with the Id for DOM patching.
/// </summary>
/// <param name="Id">Unique identifier.</param>
/// <param name="Html">Raw HTML content (rendered without encoding).</param>
public record RawHtml(string Id, string Html) : Node;

/// <summary>
/// An empty node that renders nothing. Used as a placeholder.
/// </summary>
public record Empty : Node;

// =============================================================================
// Memoization Nodes
// =============================================================================
// These nodes enable the diff algorithm to skip entire subtrees when the
// memoization key hasn't changed. This is the primary optimization for
// large lists and complex views.
//
// The generic MemoKeyEquals<TOtherKey> method avoids boxing overhead:
// instead of boxing the key to object for comparison, it uses
// EqualityComparer<TKey>.Default which the JIT can optimize for
// value types (no allocation, inlined comparison).
// =============================================================================

/// <summary>
/// Interface for memo nodes that cache their rendered subtree.
/// The diff algorithm checks <see cref="MemoKeyEquals{TOtherKey}"/> to decide
/// whether to skip diffing the subtree.
/// </summary>
public interface IMemoNode
{
    /// <summary>The memoization key (boxed for interface compatibility).</summary>
    object MemoKey { get; }

    /// <summary>The cached/wrapped node.</summary>
    Node Node { get; }

    /// <summary>
    /// Generic key comparison that avoids boxing for value-type keys.
    /// </summary>
    bool MemoKeyEquals<TOtherKey>(Memo<TOtherKey> other) where TOtherKey : notnull;
}

/// <summary>
/// A memoized node. If the key matches the previous render's key,
/// the diff algorithm skips the entire subtree.
/// </summary>
/// <typeparam name="TKey">The key type (must implement value equality).</typeparam>
/// <param name="Id">Unique identifier.</param>
/// <param name="Key">The memoization key.</param>
/// <param name="Node">The wrapped node.</param>
public record Memo<TKey>(string Id, TKey Key, Node Node) : Node, IMemoNode
    where TKey : notnull
{
    object IMemoNode.MemoKey => Key;
    Node IMemoNode.Node => Node;

    bool IMemoNode.MemoKeyEquals<TOtherKey>(Memo<TOtherKey> other) =>
        other.Key is TKey otherKey && EqualityComparer<TKey>.Default.Equals(Key, otherKey);
}

/// <summary>
/// Interface for lazy memo nodes that defer view construction.
/// </summary>
public interface ILazyMemoNode
{
    /// <summary>The memoization key (boxed for interface compatibility).</summary>
    object MemoKey { get; }

    /// <summary>
    /// Gets the rendered node, evaluating the factory if needed.
    /// </summary>
    Node GetNode();

    /// <summary>
    /// The cached node from the previous render (if available).
    /// Used by the diff algorithm as the "old" node when keys don't match.
    /// </summary>
    Node? CachedNode { get; }

    /// <summary>
    /// Generic key comparison that avoids boxing for value-type keys.
    /// </summary>
    bool MemoKeyEquals<TOtherKey>(LazyMemo<TOtherKey> other) where TOtherKey : notnull;
}

/// <summary>
/// A lazy memoized node. Defers view construction until the key changes.
/// If the key matches, the factory is never called — the previous render's
/// cached node is reused for diffing.
/// </summary>
/// <typeparam name="TKey">The key type (must implement value equality).</typeparam>
/// <param name="Id">Unique identifier.</param>
/// <param name="Key">The memoization key.</param>
/// <param name="Factory">Deferred view constructor, called only on key mismatch.</param>
/// <param name="CachedNode">Optional cached node from previous render.</param>
public record LazyMemo<TKey>(
    string Id,
    TKey Key,
    Func<Node> Factory,
    Node? CachedNode = null) : Node, ILazyMemoNode
    where TKey : notnull
{
    private Node? _evaluated;

    object ILazyMemoNode.MemoKey => Key;
    Node? ILazyMemoNode.CachedNode => CachedNode;

    Node ILazyMemoNode.GetNode() => _evaluated ??= Factory();

    bool ILazyMemoNode.MemoKeyEquals<TOtherKey>(LazyMemo<TOtherKey> other) =>
        other.Key is TKey otherKey && EqualityComparer<TKey>.Default.Equals(Key, otherKey);
}
