namespace Picea.Abies.DOM;

/// <summary>
/// A complete document with title, body content, and optional head elements.
/// </summary>
public record Document(string Title, Node Body, params HeadContent[] Head);

/// <summary>
/// Base type for all virtual DOM nodes. Every node carries a stable Id
/// for efficient diffing and DOM addressing.
/// </summary>
public record Node(string Id);

/// <summary>
/// An HTML element with a tag name, attributes, and children.
/// </summary>
public record Element(string Id, string Tag, Attribute[] Attributes, params Node[] Children) : Node(Id);

/// <summary>
/// A text node containing a string value.
/// </summary>
public record Text(string Id, string Value) : Node(Id)
{
    public static implicit operator string(Text text) => text.Value;
    public static implicit operator Text(string text) => new(text, text);
}

/// <summary>
/// A raw (unprocessed) HTML node. Inserted as-is — no encoding or escaping.
/// </summary>
public record RawHtml(string Id, string Html) : Node(Id);

/// <summary>
/// An empty node — renders nothing.
/// </summary>
public record Empty() : Node("");

/// <summary>
/// Interface for memoized nodes — enables polymorphic key comparison.
/// </summary>
public interface MemoNode
{
    object MemoKey { get; }
    Node CachedNode { get; }
    Node WithCachedNode(Node newCachedNode);
    bool MemoKeyEquals(MemoNode other);
}

/// <summary>
/// A memoized node. When diffing, if the memo key equals the previous key,
/// the cached node is reused without re-diffing the subtree.
/// </summary>
public record Memo<TKey>(string Id, TKey Key, Node CachedNode) : Node(Id), MemoNode where TKey : notnull
{
    object MemoNode.MemoKey => Key;
    public Node WithCachedNode(Node newCachedNode) => this with { CachedNode = newCachedNode };
    public bool MemoKeyEquals(MemoNode other) =>
        other is Memo<TKey> otherMemo &&
        EqualityComparer<TKey>.Default.Equals(Key, otherMemo.Key);
}

/// <summary>
/// Interface for lazy memoized nodes that defer evaluation until needed.
/// </summary>
public interface LazyMemoNode
{
    object MemoKey { get; }
    Node? CachedNode { get; }
    Node Evaluate();
    Node WithCachedNode(Node cachedNode);
    bool MemoKeyEquals(LazyMemoNode other);
}

/// <summary>
/// A lazily-evaluated memoized node. The factory is only called if the key differs.
/// </summary>
public record LazyMemo<TKey>(string Id, TKey Key, Func<Node> Factory, Node? CachedNode = null)
    : Node(Id), LazyMemoNode where TKey : notnull
{
    object LazyMemoNode.MemoKey => Key;
    Node? LazyMemoNode.CachedNode => CachedNode;
    public Node Evaluate() => Factory();
    public Node WithCachedNode(Node cachedNode) => this with { CachedNode = cachedNode };
    public bool MemoKeyEquals(LazyMemoNode other) =>
        other is LazyMemo<TKey> otherLazy &&
        EqualityComparer<TKey>.Default.Equals(Key, otherLazy.Key);
}
