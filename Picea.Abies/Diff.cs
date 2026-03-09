// =============================================================================
// Virtual DOM Diff Algorithm — Pure Algorithm
// =============================================================================
// Computes the minimal set of Patch instructions to transform an old virtual
// DOM tree into a new one. This is a pure function — no browser or JS interop
// dependencies — making it usable for both WASM and future SSR scenarios.
//
// Algorithm design (inspired by Elm's VirtualDom and Inferno):
//   1. Reference equality bailout — O(1) for unchanged subtrees
//   2. Memo/LazyMemo key comparison — skip subtree diff when key matches
//   3. Same-tag elements: DiffAttributes + DiffChildren
//   4. DiffChildren: head/tail skip → small fast path (O(n²)) or
//      keyed reconciliation with LIS (O(n log n)) for minimal DOM moves
//
// Performance optimizations (inspired by Stephen Toub's .NET perf articles):
//   • Object pools (Stack<T>) for List<Patch>, Dictionary, etc.
//   • ArrayPool<T> for key arrays and LIS computation
//   • stackalloc for small child count fast paths
//   • Pre-allocated index string cache to avoid string interpolation
//   • SearchValues / FrozenSet for spec-level HTML knowledge
//   • Same-order attribute fast path — skip dictionary when order matches
//
// Architecture Decision Records:
//   • ADR-003: Virtual DOM
//   • ADR-008: Immutable State Management
// =============================================================================

using System.Buffers;
using System.Runtime.CompilerServices;
using Picea.Abies.DOM;

namespace Picea.Abies;

/// <summary>
/// Provides diffing utilities for the virtual DOM.
/// The implementation is inspired by Elm's VirtualDom diff algorithm
/// and is written with performance in mind.
/// </summary>
public static class Operations
{
    // =========================================================================
    // Pre-allocated Index String Cache
    // =========================================================================
    // Avoids string interpolation allocation for non-keyed children.
    // Cache covers 99% of real-world use cases (elements with >256 children
    // are rare).
    // =========================================================================

    private const int IndexStringCacheSize = 256;
    private static readonly string[] IndexStringCache = InitializeIndexStringCache();

    private static string[] InitializeIndexStringCache()
    {
        var cache = new string[IndexStringCacheSize];
        for (int i = 0; i < IndexStringCacheSize; i++)
        {
            cache[i] = $"__index:{i}";
        }

        return cache;
    }

    /// <summary>
    /// Gets a cached index string for the given index to avoid allocation.
    /// For indices &gt;= 256, falls back to string interpolation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetIndexString(int index) =>
        (uint)index < IndexStringCacheSize ? IndexStringCache[index] : $"__index:{index}";

    // =========================================================================
    // Object Pools — Stack<T> (WASM is single-threaded)
    // =========================================================================

    private static readonly Stack<List<Patch>> PatchListPool = new();
    private static readonly Stack<Dictionary<string, DOM.Attribute>> AttributeMapPool = new();
    private static readonly Stack<Dictionary<string, int>> KeyIndexMapPool = new();
    private static readonly Stack<List<int>> IntListPool = new();
    private static readonly Stack<List<(int, int)>> IntPairListPool = new();

    private static List<Patch> RentPatchList()
    {
        if (PatchListPool.TryPop(out var list))
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
            PatchListPool.Push(list);
        }
    }

    private static Dictionary<string, DOM.Attribute> RentAttributeMap()
    {
        if (AttributeMapPool.TryPop(out var map))
        {
            map.Clear();
            return map;
        }

        return [];
    }

    private static void ReturnAttributeMap(Dictionary<string, DOM.Attribute> map)
    {
        if (map.Count < 100)
        {
            AttributeMapPool.Push(map);
        }
    }

    private static Dictionary<string, int> RentKeyIndexMap()
    {
        if (KeyIndexMapPool.TryPop(out var map))
        {
            map.Clear();
            return map;
        }

        return [];
    }

    private static void ReturnKeyIndexMap(Dictionary<string, int> map)
    {
        if (map.Count < 200)
        {
            KeyIndexMapPool.Push(map);
        }
    }

    private static List<int> RentIntList()
    {
        if (IntListPool.TryPop(out var list))
        {
            list.Clear();
            return list;
        }

        return [];
    }

    private static void ReturnIntList(List<int> list)
    {
        if (list.Count < 500)
        {
            IntListPool.Push(list);
        }
    }

    private static List<(int, int)> RentIntPairList()
    {
        if (IntPairListPool.TryPop(out var list))
        {
            list.Clear();
            return list;
        }

        return [];
    }

    private static void ReturnIntPairList(List<(int, int)> list)
    {
        if (list.Count < 500)
        {
            IntPairListPool.Push(list);
        }
    }

    // =========================================================================
    // Memo Diagnostics
    // =========================================================================

    /// <summary>Number of memo key hits (subtree diff skipped).</summary>
    internal static int MemoHits;

    /// <summary>Number of memo key misses (subtree diff required).</summary>
    internal static int MemoMisses;

    internal static void ResetMemoCounters()
    {
        MemoHits = 0;
        MemoMisses = 0;
    }

    // =========================================================================
    // Public API
    // =========================================================================

    /// <summary>
    /// Compute the list of patches that transform <paramref name="oldNode"/>
    /// into <paramref name="newNode"/>.
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

    // =========================================================================
    // Memo Node Helpers
    // =========================================================================

    /// <summary>
    /// Unwraps a memo node (lazy or regular) to get its actual content.
    /// For lazy memos, this evaluates the factory function.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Node UnwrapMemoNode(Node node) =>
        node switch
        {
            LazyMemoNode lazy => lazy.CachedNode ?? lazy.Evaluate(),
            MemoNode memo => memo.CachedNode,
            _ => node
        };

    /// <summary>
    /// Pre-evaluates any lazy memo or memo wrapper nodes in a children array,
    /// returning a new array with all nodes materialized to their concrete forms.
    /// <para>
    /// This is critical for <see cref="SetChildrenHtml"/>: both
    /// <c>RegisterHandlers</c> (which registers command IDs in the handler map)
    /// and <c>Render.HtmlChildren</c> (which renders data-event-* attributes
    /// into HTML) must see the <strong>same</strong> concrete nodes with the
    /// <strong>same</strong> CommandIds. Without materialization, each call to
    /// <c>LazyMemo.Evaluate()</c> produces a fresh node with a new CommandId,
    /// causing the handler map and rendered HTML to diverge — clicks are
    /// silently dropped.
    /// </para>
    /// <para>
    /// As a side effect, this method backfills <c>CachedNode</c> on LazyMemo
    /// entries in the <strong>original</strong> array. This ensures the stored
    /// virtual DOM tree has populated caches so that <c>UnregisterHandlers</c>
    /// can later traverse them to clean up handlers, and <c>PreserveIds</c>
    /// can carry cached content forward across render cycles.
    /// </para>
    /// </summary>
    private static Node[] MaterializeChildren(Node[] children)
    {
        // Fast path: check if any children need materialization
        bool needsMaterialization = false;
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] is LazyMemoNode or MemoNode)
            {
                needsMaterialization = true;
                break;
            }
        }

        if (!needsMaterialization)
        {
            return children;
        }

        // Materialize: evaluate lazy/memo nodes so the concrete result is shared
        // by both RegisterHandlers (handler map) and Render.HtmlChildren (HTML output).
        var materialized = new Node[children.Length];
        for (int i = 0; i < children.Length; i++)
        {
            var child = children[i];
            if (child is LazyMemoNode lazyMemo)
            {
                var evaluated = lazyMemo.CachedNode ?? lazyMemo.Evaluate();
                materialized[i] = evaluated;
                // Backfill CachedNode on the original LazyMemo wrapper in the source array.
                if (lazyMemo.CachedNode is null)
                {
                    children[i] = lazyMemo.WithCachedNode(evaluated);
                }
            }
            else
            {
                materialized[i] = UnwrapMemoNode(child);
            }
        }

        return materialized;
    }

    // =========================================================================
    // Core Diff — Recursive
    // =========================================================================

    private static void DiffInternal(Node oldNode, Node newNode, Element? parent, List<Patch> patches)
    {
        // Quick bailout: if both nodes are the exact same reference, nothing to diff.
        // This can happen when a cached node is reused across renders.
        if (ReferenceEquals(oldNode, newNode))
        {
            return;
        }

        // Lazy memo nodes: defer evaluation until keys differ.
        // This provides the true performance benefit — we don't even construct
        // the node if unchanged. Uses MemoKeyEquals to avoid boxing overhead
        // for value type keys.
        if (oldNode is LazyMemoNode oldLazy && newNode is LazyMemoNode newLazy)
        {
            if (oldLazy.MemoKeyEquals(newLazy))
            {
                // Keys match — skip evaluation AND diffing entirely.
                // Simple increment since WASM is single-threaded (no Interlocked needed).
                MemoHits++;
                return;
            }

            // Keys differ — evaluate the new lazy and diff.
            MemoMisses++;
            var oldCached = oldLazy.CachedNode ?? oldLazy.Evaluate();
            var newCached = newLazy.Evaluate();
            DiffInternal(oldCached, newCached, parent, patches);
            return;
        }

        // Regular memo nodes: skip diffing subtree if keys are equal.
        // This is similar to Elm's lazy function — major performance win for list items.
        if (oldNode is MemoNode oldMemo && newNode is MemoNode newMemo)
        {
            if (oldMemo.MemoKeyEquals(newMemo))
            {
                MemoHits++;
                return;
            }

            MemoMisses++;
            DiffInternal(oldMemo.CachedNode, newMemo.CachedNode, parent, patches);
            return;
        }

        // Unwrap any type of memo node (lazy or regular).
        var effectiveOld = UnwrapMemoNode(oldNode);
        var effectiveNew = UnwrapMemoNode(newNode);

        // If either was a memo, recurse with the unwrapped nodes.
        if (!ReferenceEquals(effectiveOld, oldNode) || !ReferenceEquals(effectiveNew, newNode))
        {
            DiffInternal(effectiveOld, effectiveNew, parent, patches);
            return;
        }

        // Text nodes only need an update when the value changes.
        if (oldNode is Text oldText && newNode is Text newText)
        {
            if (!string.Equals(oldText.Value, newText.Value, StringComparison.Ordinal) ||
                !string.Equals(oldText.Id, newText.Id, StringComparison.Ordinal))
            {
                patches.Add(new UpdateText(parent!, oldText, newText.Value, newText.Id));
            }

            return;
        }

        if (oldNode is RawHtml oldRaw && newNode is RawHtml newRaw)
        {
            if (!string.Equals(oldRaw.Html, newRaw.Html, StringComparison.Ordinal) ||
                !string.Equals(oldRaw.Id, newRaw.Id, StringComparison.Ordinal))
            {
                patches.Add(new UpdateRaw(oldRaw, newRaw.Html, newRaw.Id));
            }

            return;
        }

        // Elements may need to be replaced when the tag differs or the node type changed.
        if (oldNode is Element oldElement && newNode is Element newElement)
        {
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

            // ==========================================================
            // Void Element Diff Skip (HTML Living Standard §13.1.2)
            // ==========================================================
            // Void elements cannot have children, so skip the DiffChildren
            // call entirely. This avoids ArrayPool rents, key sequence
            // building, and function call overhead for elements like
            // <img>, <input>, <br>, <hr>, <meta>, <source>, etc.
            // ==========================================================
            if (!HtmlSpec.VoidElements.Contains(oldElement.Tag))
            {
                DiffChildren(oldElement, newElement, patches);
            }

            return;
        }

        // Fallback for node type mismatch.
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
                patches.Add(new UpdateText(parent, ot, nt.Value, nt.Id));
            }
            else if (oldNode is Text ot2 && newNode is Element ne3)
            {
                patches.Add(new ReplaceRaw(
                    new RawHtml(ot2.Id, Render.Html(ot2)),
                    new RawHtml(ne3.Id, Render.Html(ne3))));
            }
            else if (oldNode is Element oe3 && newNode is Text nt2)
            {
                patches.Add(new ReplaceRaw(
                    new RawHtml(oe3.Id, Render.Html(oe3)),
                    new RawHtml(nt2.Id, Render.Html(nt2))));
            }
        }
    }

    // =========================================================================
    // Attribute Diffing
    // =========================================================================

    private static void DiffAttributes(Element oldElement, Element newElement, List<Patch> patches)
    {
        var oldAttrs = oldElement.Attributes;
        var newAttrs = newElement.Attributes;

        // Early exit for identical attribute arrays.
        if (ReferenceEquals(oldAttrs, newAttrs))
        {
            return;
        }

        // Early exit for both empty.
        if (oldAttrs.Length == 0 && newAttrs.Length == 0)
        {
            return;
        }

        // If old is empty, just add all new attributes.
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

        // If new is empty, remove all old attributes.
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

        // =====================================================================
        // Same-Order Fast Path — Skip dictionary building when attrs match order
        // =====================================================================
        // Most renders don't change attribute order or count. When old and new
        // have the same count, try comparing them positionally first. This avoids:
        //   • Dictionary allocation and building (O(n) time + allocations)
        //   • Dictionary lookups (hash computation overhead)
        // Only fall back to dictionary approach if names don't match.
        // =====================================================================
        if (oldAttrs.Length == newAttrs.Length)
        {
            var sameOrder = true;
            for (int i = 0; i < oldAttrs.Length; i++)
            {
                var oldAttrName = oldAttrs[i].Name;
                var newAttrName = newAttrs[i].Name;
                if (!string.Equals(oldAttrName, newAttrName, StringComparison.Ordinal))
                {
                    sameOrder = false;
                    break;
                }
            }

            if (sameOrder)
            {
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

        // Fall back to dictionary-based diffing for different order/count.
        var oldMap = RentAttributeMap();
        try
        {
            if (oldMap.Count == 0 && oldAttrs.Length > 0)
            {
                oldMap.EnsureCapacity(oldAttrs.Length);
            }

            foreach (var attr in oldAttrs)
            {
                var attrName = attr.Name;
                oldMap[attrName] = attr;
            }

            foreach (var newAttr in newAttrs)
            {
                var newAttrName = newAttr.Name;
                if (oldMap.TryGetValue(newAttrName, out var oldAttr))
                {
                    oldMap.Remove(newAttrName);
                    if (!newAttr.Equals(oldAttr))
                    {
                        if (oldAttr is Handler oldHandler && newAttr is Handler newHandler)
                        {
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

            // Any remaining old attributes must be removed.
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

    // =========================================================================
    // Children Diffing
    // =========================================================================

    private static void DiffChildren(Element oldParent, Element newParent, List<Patch> patches)
    {
        var oldChildren = oldParent.Children;
        var newChildren = newParent.Children;

        // Early exit for identical child arrays.
        if (ReferenceEquals(oldChildren, newChildren))
        {
            return;
        }

        var oldLength = oldChildren.Length;
        var newLength = newChildren.Length;

        // Early exit for both empty — avoids ArrayPool rent/return overhead.
        if (oldLength == 0 && newLength == 0)
        {
            return;
        }

        // Build maps of old and new children by their keys (element Id or data-key).
        // Use ArrayPool to avoid allocations.
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
            // Clear the arrays before returning to pool (avoid memory leaks of string references).
            Array.Clear(oldKeysArray, 0, oldLength);
            Array.Clear(newKeysArray, 0, newLength);
            ArrayPool<string>.Shared.Return(oldKeysArray);
            ArrayPool<string>.Shared.Return(newKeysArray);
        }
    }

    // =========================================================================
    // Small Count Fast Path Threshold
    // =========================================================================
    // For child counts below this threshold, use O(n²) linear scan instead of
    // building dictionaries. This eliminates dictionary allocation overhead for
    // common cases (most elements have < 8 children).
    //
    // Based on profiling: Dictionary allocation + hashing overhead exceeds O(n²)
    // scan cost for small n. Threshold of 8 chosen based on benchmarks.
    // =========================================================================

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

        // =====================================================================
        // Clear Fast Path — O(1) detection before building any dictionaries
        // =====================================================================
        if (oldLength > 0 && newLength == 0)
        {
            patches.Add(new ClearChildren(oldParent, oldChildren));
            return;
        }

        // =====================================================================
        // Add-All Fast Path — O(n) when starting from empty
        // =====================================================================
        // Emit a single SetChildrenHtml patch that concatenates all children
        // into one innerHTML assignment. Eliminates N parseHtmlFragment +
        // appendChild + addEventListeners calls on the JS side.
        // =====================================================================
        if (oldLength == 0 && newLength > 0)
        {
            patches.Add(new SetChildrenHtml(newParent, MaterializeChildren(newChildren)));
            return;
        }

        // =====================================================================
        // Head/Tail Skip — Three-Phase Keyed Diff Optimization
        // =====================================================================
        // Before building key maps, skip common prefix (head) and suffix (tail).
        // Effective for: append-only (chat, logs), prepend, single item changes.
        // =====================================================================

        int headSkip = 0;
        int tailSkip = 0;
        int minLength = Math.Min(oldLength, newLength);

        // Skip matching head (common prefix).
        while (headSkip < minLength && oldKeys[headSkip] == newKeys[headSkip])
        {
            DiffInternal(oldChildren[headSkip], newChildren[headSkip], oldParent, patches);
            headSkip++;
        }

        // If all elements matched, handle length differences.
        if (headSkip == minLength)
        {
            // Remove extra old children (from end).
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

            // =================================================================
            // Append Fast Path — single insertAdjacentHTML instead of N AddChild
            // =================================================================
            // When appending multiple children (e.g., "Add 1000 rows"), emit a
            // single AppendChildrenHtml patch. This uses insertAdjacentHTML on
            // the JS side, which:
            //   1. Respects the parent's parsing context (<tr> inside <tbody>)
            //   2. Preserves existing children (unlike innerHTML)
            //   3. Is a single DOM operation instead of N appendChild calls
            // =================================================================
            if (newLength > headSkip)
            {
                var appendChildren = newChildren[headSkip..];
                patches.Add(new AppendChildrenHtml(newParent, MaterializeChildren(appendChildren)));
            }

            return;
        }

        // Skip matching tail (common suffix) — be careful not to overlap with head.
        int oldEnd = oldLength - 1;
        int newEnd = newLength - 1;
        while (oldEnd > headSkip && newEnd > headSkip &&
               oldKeys[oldEnd] == newKeys[newEnd])
        {
            DiffInternal(oldChildren[oldEnd], newChildren[newEnd], oldParent, patches);
            tailSkip++;
            oldEnd--;
            newEnd--;
        }

        // Calculate middle section bounds.
        int oldMiddleStart = headSkip;
        int oldMiddleEnd = oldLength - tailSkip; // exclusive
        int newMiddleStart = headSkip;
        int newMiddleEnd = newLength - tailSkip; // exclusive
        int oldMiddleLength = oldMiddleEnd - oldMiddleStart;
        int newMiddleLength = newMiddleEnd - newMiddleStart;

        // If middle is empty after skip, we're done.
        if (oldMiddleLength == 0 && newMiddleLength == 0)
        {
            return;
        }

        // Handle middle-only clear.
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

        // Handle middle-only add.
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

        // Create slices for the middle section only.
        var oldMiddleChildren = oldChildren.AsSpan(oldMiddleStart, oldMiddleLength);
        var newMiddleChildren = newChildren.AsSpan(newMiddleStart, newMiddleLength);
        var oldMiddleKeys = oldKeys.Slice(oldMiddleStart, oldMiddleLength);
        var newMiddleKeys = newKeys.Slice(newMiddleStart, newMiddleLength);

        // Fast path for small middle counts: use O(n²) linear scan.
        if (oldMiddleLength <= SmallChildCountThreshold && newMiddleLength <= SmallChildCountThreshold)
        {
            DiffChildrenSmallSpan(oldParent, newParent, oldMiddleChildren, newMiddleChildren,
                oldMiddleKeys, newMiddleKeys, patches);
            return;
        }

        // Check if middle keys differ at all.
        if (!oldMiddleKeys.SequenceEqual(newMiddleKeys))
        {
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

                var isReorder = oldMiddleLength == newMiddleLength &&
                                AreKeysSameSet(oldMiddleKeys, newKeyToIndex);

                if (isReorder)
                {
                    // Reorder detected: use LIS algorithm to minimize DOM moves.
                    var oldIndices = ArrayPool<int>.Shared.Rent(newMiddleLength);
                    var inLIS = ArrayPool<bool>.Shared.Rent(newMiddleLength);

                    try
                    {
                        inLIS.AsSpan(0, newMiddleLength).Clear();

                        for (int i = 0; i < newMiddleLength; i++)
                        {
                            oldIndices[i] = oldKeyToIndex[newMiddleKeys[i]];
                        }

                        ComputeLISInto(oldIndices.AsSpan(0, newMiddleLength),
                            inLIS.AsSpan(0, newMiddleLength));

                        // Diff all elements.
                        for (int i = 0; i < newMiddleLength; i++)
                        {
                            var oldIndex = oldIndices[i];
                            DiffInternal(oldMiddleChildren[oldIndex], newMiddleChildren[i],
                                oldParent, patches);
                        }

                        // Move elements NOT in LIS to their correct positions.
                        // Process in reverse so we can use insertBefore with known reference.
                        for (int i = newMiddleLength - 1; i >= 0; i--)
                        {
                            if (!inLIS[i])
                            {
                                var oldIndex = oldIndices[i];
                                var oldNode = UnwrapMemoNode(oldMiddleChildren[oldIndex]);
                                if (oldNode is Element oldChildElement)
                                {
                                    string? beforeId = null;
                                    if (i + 1 < newMiddleLength)
                                    {
                                        var nextOldIndex = oldIndices[i + 1];
                                        var nextOldNode = UnwrapMemoNode(oldMiddleChildren[nextOldIndex]);
                                        beforeId = nextOldNode.Id;
                                    }
                                    else if (tailSkip > 0)
                                    {
                                        var firstTailNode = UnwrapMemoNode(oldChildren[oldMiddleEnd]);
                                        beforeId = firstTailNode.Id;
                                    }

                                    patches.Add(new MoveChild(oldParent, oldChildElement, beforeId));
                                }
                            }
                        }
                    }
                    finally
                    {
                        ArrayPool<bool>.Shared.Return(inLIS);
                        ArrayPool<int>.Shared.Return(oldIndices);
                    }

                    return;
                }

                // Membership change: some keys added, some removed.
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

                    // =========================================================
                    // Complete Replacement Fast Path
                    // =========================================================
                    // When NO keys overlap, ALL old middle children are removed
                    // and ALL new middle children are added. Emit ClearChildren +
                    // SetChildrenHtml for 2 bulk operations instead of N+M patches.
                    // =========================================================
                    if (keysToDiff.Count == 0 && headSkip == 0 && tailSkip == 0)
                    {
                        patches.Add(new ClearChildren(oldParent, oldChildren));
                        patches.Add(new SetChildrenHtml(newParent, MaterializeChildren(newChildren)));
                        return;
                    }

                    // Remove old children that don't exist in new (iterate backwards).
                    for (int i = keysToRemove.Count - 1; i >= 0; i--)
                    {
                        var idx = keysToRemove[i];
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

                    // Diff children that exist in both trees.
                    foreach (var (oldIndex, newIndex) in keysToDiff)
                    {
                        DiffInternal(oldMiddleChildren[oldIndex], newMiddleChildren[newIndex],
                            oldParent, patches);
                    }

                    // Add new children that don't exist in old.
                    foreach (var idx in keysToAdd)
                    {
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

        // Middle keys are identical: diff in place.
        for (int i = 0; i < oldMiddleLength; i++)
        {
            DiffInternal(oldMiddleChildren[i], newMiddleChildren[i], oldParent, patches);
        }
    }

    // =========================================================================
    // Small Children Diff — O(n²) Linear Scan
    // =========================================================================

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

        if (oldLength > 0 && newLength == 0)
        {
            patches.Add(new ClearChildren(oldParent, oldChildren));
            return;
        }

        // Fast path: keys are identical.
        if (oldKeys.SequenceEqual(newKeys))
        {
            for (int i = 0; i < oldLength; i++)
            {
                DiffInternal(oldChildren[i], newChildren[i], oldParent, patches);
            }

            return;
        }

        // stackalloc for tracking matched indices — no heap allocation.
        Span<int> oldMatched = stackalloc int[oldLength];
        Span<int> newMatched = stackalloc int[newLength];
        oldMatched.Fill(-1);
        newMatched.Fill(-1);

        // O(n²) matching.
        for (int i = 0; i < oldLength; i++)
        {
            var oldKey = oldKeys[i];
            for (int j = 0; j < newLength; j++)
            {
                if (newMatched[j] == -1 &&
                    string.Equals(oldKey, newKeys[j], StringComparison.Ordinal))
                {
                    oldMatched[i] = j;
                    newMatched[j] = i;
                    break;
                }
            }
        }

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
            // Pure reorder: LIS for small lists.
            Span<int> oldIndices = stackalloc int[newLength];
            for (int i = 0; i < newLength; i++)
            {
                oldIndices[i] = newMatched[i];
            }

            Span<bool> inLIS = stackalloc bool[newLength];
            inLIS.Clear();
            ComputeLISIntoSmall(oldIndices, inLIS);

            for (int i = 0; i < newLength; i++)
            {
                var oldIndex = oldIndices[i];
                DiffInternal(oldChildren[oldIndex], newChildren[i], oldParent, patches);
            }

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

        // Membership change.
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

        for (int i = 0; i < oldLength; i++)
        {
            if (oldMatched[i] != -1)
            {
                DiffInternal(oldChildren[i], newChildren[oldMatched[i]], oldParent, patches);
            }
        }

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
    /// Fast path for diffing small child lists using O(n²) linear scan.
    /// Span-based overload to avoid ToArray() allocations when working with sliced spans.
    /// </summary>
    private static void DiffChildrenSmallSpan(
        Element oldParent,
        Element newParent,
        ReadOnlySpan<Node> oldChildren,
        ReadOnlySpan<Node> newChildren,
        ReadOnlySpan<string> oldKeys,
        ReadOnlySpan<string> newKeys,
        List<Patch> patches)
    {
        var oldLength = oldChildren.Length;
        var newLength = newChildren.Length;

        // Fast path: keys are identical.
        if (oldKeys.SequenceEqual(newKeys))
        {
            for (int i = 0; i < oldLength; i++)
            {
                DiffInternal(oldChildren[i], newChildren[i], oldParent, patches);
            }

            return;
        }

        // stackalloc for tracking matched indices.
        Span<int> oldMatched = stackalloc int[oldLength];
        Span<int> newMatched = stackalloc int[newLength];
        oldMatched.Fill(-1);
        newMatched.Fill(-1);

        for (int i = 0; i < oldLength; i++)
        {
            var oldKey = oldKeys[i];
            for (int j = 0; j < newLength; j++)
            {
                if (newMatched[j] == -1 &&
                    string.Equals(oldKey, newKeys[j], StringComparison.Ordinal))
                {
                    oldMatched[i] = j;
                    newMatched[j] = i;
                    break;
                }
            }
        }

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
            Span<int> oldIndices = stackalloc int[newLength];
            for (int i = 0; i < newLength; i++)
            {
                oldIndices[i] = newMatched[i];
            }

            Span<bool> inLIS = stackalloc bool[newLength];
            inLIS.Clear();
            ComputeLISIntoSmall(oldIndices, inLIS);

            for (int i = 0; i < newLength; i++)
            {
                var oldIndex = oldIndices[i];
                DiffInternal(oldChildren[oldIndex], newChildren[i], oldParent, patches);
            }

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

        // Membership change.
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

        for (int i = 0; i < oldLength; i++)
        {
            if (oldMatched[i] != -1)
            {
                DiffInternal(oldChildren[i], newChildren[oldMatched[i]], oldParent, patches);
            }
        }

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

    // =========================================================================
    // Longest Increasing Subsequence (LIS) — O(n log n)
    // =========================================================================
    // Used for optimal DOM reordering — elements in the LIS don't need to
    // be moved. Algorithm: patience sorting with binary search.
    // Inspired by Inferno's virtual DOM implementation.
    // =========================================================================

    /// <summary>
    /// Computes the LIS of the input array and marks positions in the LIS
    /// in the output bool span. Uses ArrayPool to avoid allocations.
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

        // result[j] = index in arr of smallest ending value for LIS of length j+1
        // p[i] = predecessor index for position i in the LIS chain
        var result = ArrayPool<int>.Shared.Rent(len);
        var p = ArrayPool<int>.Shared.Rent(len);

        try
        {
            var lisLen = 0;

            for (int i = 0; i < len; i++)
            {
                var val = arr[i];

                // Binary search to find position where val fits.
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

            // Mark LIS positions by following predecessor chain backwards.
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

    // =========================================================================
    // Key Helpers
    // =========================================================================

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
        // Fast path for common case: Element.
        if (node is Element element)
        {
            return GetElementKey(element);
        }

        // Fast path: Text and RawHtml nodes have no key.
        if (node is Text or RawHtml)
        {
            return null;
        }

        // Slow path: Memo nodes (rare in practice).
        if (node is LazyMemoNode)
        {
            return node.Id;
        }

        if (node is MemoNode memo)
        {
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
        // Check for explicit data-key attribute (backward compatibility).
        var attrs = element.Attributes;
        for (int i = 0; i < attrs.Length; i++)
        {
            var name = attrs[i].Name;
            if (name is "data-key" or "key")
            {
                return attrs[i].Value;
            }
        }

        // Use element Id as the key.
        return element.Id;
    }
}
