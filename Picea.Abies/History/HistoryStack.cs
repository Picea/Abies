using System.Collections;

namespace Picea.Abies.History;

/// <summary>
/// An immutable stack backed by a single array, copied on every mutating operation.
/// <para>
/// Every operation is worst-case O(Count) — a fresh array is allocated and the
/// surviving elements are copied into it. This is deliberate, not an oversight: the
/// stacks this type backs (<c>Past</c> and <c>Future</c> in
/// <see cref="History{TModel}"/>) are kept trimmed to a small, caller-supplied depth
/// (see <see cref="Trim"/>), so the array copy an operation pays is bounded by that
/// depth, not by how long the application has run. An amortised structure — a
/// doubling array, a persistent linked list — earns its bound by growing without
/// limit; a value that is trimmed back down after (almost) every write never runs
/// long enough for that amortisation to pay for itself, and its worst case is
/// already as small as the depth allows. Stated as worst-case O(Depth), not
/// amortised O(1).
/// </para>
/// <para>
/// Every returned stack is a new instance. No operation mutates the array an older
/// instance still references, so an older instance stays observably unchanged
/// forever — the type is trivially persistent.
/// </para>
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
internal sealed class HistoryStack<T> : IEnumerable<T>, IEquatable<HistoryStack<T>>
{
    private readonly T[] _items;

    private HistoryStack(T[] items) => _items = items;

    /// <summary>The empty stack.</summary>
    internal static HistoryStack<T> Empty { get; } = new([]);

    /// <summary>The number of retained elements.</summary>
    internal int Count => _items.Length;

    /// <summary>Whether the stack holds no elements.</summary>
    internal bool IsEmpty => _items.Length == 0;

    /// <summary>The most recently pushed element.</summary>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    internal T Peek() =>
        _items.Length == 0
            ? throw new InvalidOperationException("Cannot peek an empty history stack.")
            : _items[^1];

    /// <summary>Returns a new stack with <paramref name="item"/> pushed on top.</summary>
    internal HistoryStack<T> Push(T item)
    {
        var next = new T[_items.Length + 1];
        Array.Copy(_items, next, _items.Length);
        next[^1] = item;
        return new HistoryStack<T>(next);
    }

    /// <summary>Returns a new stack with the top element removed.</summary>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    internal HistoryStack<T> Pop() =>
        _items.Length == 0
            ? throw new InvalidOperationException("Cannot pop an empty history stack.")
            : new HistoryStack<T>(_items[..^1]);

    /// <summary>Returns a new stack with the top element replaced by <paramref name="item"/>.
    /// The element beneath the top, and everything below it, is unchanged.</summary>
    /// <exception cref="InvalidOperationException">The stack is empty.</exception>
    internal HistoryStack<T> ReplaceTop(T item)
    {
        if (_items.Length == 0)
            throw new InvalidOperationException("Cannot replace the top of an empty history stack.");

        var next = new T[_items.Length];
        Array.Copy(_items, next, _items.Length);
        next[^1] = item;
        return new HistoryStack<T>(next);
    }

    /// <summary>
    /// Returns a new stack holding at most <paramref name="depth"/> of the most
    /// recently pushed elements, evicting from the far end — the oldest elements —
    /// first. A no-op, returning <c>this</c> unchanged, when the stack already holds
    /// at most <paramref name="depth"/> elements.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="depth"/> is negative.</exception>
    internal HistoryStack<T> Trim(int depth)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(depth);

        if (_items.Length <= depth)
            return this;

        var next = new T[depth];
        Array.Copy(_items, _items.Length - depth, next, 0, depth);
        return new HistoryStack<T>(next);
    }

    /// <summary>Enumerates from the oldest retained element to the most recently pushed.</summary>
    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Structural equality: two stacks are equal when they hold the same elements in
    /// the same order, regardless of which instance produced them. Every mutating
    /// operation above returns a fresh instance, so <see cref="History{TModel}"/>'s
    /// record-synthesized equality — which compares its <c>Past</c> and
    /// <c>Future</c> fields through this method — would otherwise treat two
    /// historically-identical histories as different.
    /// </summary>
    public bool Equals(HistoryStack<T>? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        if (_items.Length != other._items.Length)
            return false;

        var comparer = EqualityComparer<T>.Default;
        for (var i = 0; i < _items.Length; i++)
        {
            if (!comparer.Equals(_items[i], other._items[i]))
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj) => Equals(obj as HistoryStack<T>);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in _items)
            hash.Add(item);
        return hash.ToHashCode();
    }
}
