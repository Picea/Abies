// =============================================================================
// Virtualize — Windowed Rendering for Large Collections
// =============================================================================
// Renders only visible items (plus configurable overscan) from a large list,
// using spacer elements to maintain correct scroll bar height.
//
// Two modes:
// - Fixed-height: O(1) calculation, simplest to use
// - Variable-height: O(n) calculation with per-item height estimation
//
// The virtualize() function produces a standard DOM subtree:
//
//   div.abies-virtualize-container  (overflow-y: auto, fixed height)
//     div.abies-virtualize-content  (total scrollable height via CSS)
//       div.abies-virtualize-spacer (top padding for skipped items)
//       [rendered items]            (only visible + overscan items)
//
// Scroll position is tracked via the onscroll event handler on the container.
// Items are memoized using lazy() for efficient diffing.
//
// Architecture Decision Records:
// - ADR-003: Virtual DOM (docs/adr/ADR-003-virtual-dom.md)
// =============================================================================

using Abies.DOM;
using Abies.Virtualization;
using Praefixum;
using Attribute = Abies.DOM.Attribute;

namespace Abies.Html;

/// <summary>
/// Provides virtualized list rendering for large collections.
/// Only items visible in the viewport (plus overscan) are rendered to the DOM.
/// </summary>
/// <remarks>
/// <para>
/// Virtualization dramatically improves performance for large lists by reducing
/// the number of DOM nodes from N (full list) to ~20-50 (visible items).
/// This benefits initial render time, diffing time, and memory usage.
/// </para>
/// <para>
/// The scroll position must be tracked in the MVU model via the <c>onScroll</c>
/// callback, which fires whenever the user scrolls the container.
/// </para>
/// </remarks>
public static class Virtualize
{
    /// <summary>
    /// Renders a virtualized list with fixed-height items.
    /// </summary>
    /// <typeparam name="T">The type of items in the list.</typeparam>
    /// <param name="items">The complete list of items.</param>
    /// <param name="itemHeight">Height of each item in pixels. Must be positive.</param>
    /// <param name="viewportHeight">Height of the visible viewport in pixels.</param>
    /// <param name="scrollOffset">Current scroll position (scrollTop) from the model.</param>
    /// <param name="render">Function that renders a single item, given the item and its index.</param>
    /// <param name="onScroll">Callback that maps scroll event data to a Message for the MVU loop.</param>
    /// <param name="overscan">Number of extra items rendered above and below the viewport. Default 3.</param>
    /// <param name="containerAttributes">Optional additional attributes for the outer container div.</param>
    /// <param name="id">Auto-generated unique ID for the container element.</param>
    /// <returns>A virtual DOM node representing the virtualized list.</returns>
    /// <example>
    /// <code>
    /// virtualize(
    ///     items: model.Articles,
    ///     itemHeight: 80,
    ///     viewportHeight: 600,
    ///     scrollOffset: model.ScrollOffset,
    ///     render: (article, index) =>
    ///         div([class_("article-preview")], [
    ///             h2([], [text(article.Title)]),
    ///             p([], [text(article.Description)])
    ///         ], id: $"article-{article.Slug}"),
    ///     onScroll: data => new Message.ScrollChanged(data.ScrollTop),
    ///     overscan: 5
    /// )
    /// </code>
    /// </example>
    public static Node Fixed<T>(
        IReadOnlyList<T> items,
        double itemHeight,
        double viewportHeight,
        double scrollOffset,
        Func<T, int, Node> render,
        Func<ScrollEventData?, Message> onScroll,
        int overscan = 3,
        Attribute[]? containerAttributes = null,
        [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
    {
        var range = VirtualRangeCalculator.Calculate(
            items.Count, itemHeight, viewportHeight, scrollOffset, overscan);

        return BuildContainer(
            items, range, render, onScroll, viewportHeight, containerAttributes, id);
    }

    /// <summary>
    /// Renders a virtualized list with variable-height items.
    /// </summary>
    /// <typeparam name="T">The type of items in the list.</typeparam>
    /// <param name="items">The complete list of items.</param>
    /// <param name="estimateHeight">Function that estimates height for a given item.</param>
    /// <param name="viewportHeight">Height of the visible viewport in pixels.</param>
    /// <param name="scrollOffset">Current scroll position (scrollTop) from the model.</param>
    /// <param name="render">Function that renders a single item, given the item and its index.</param>
    /// <param name="onScroll">Callback that maps scroll event data to a Message for the MVU loop.</param>
    /// <param name="overscan">Number of extra items rendered above and below the viewport. Default 3.</param>
    /// <param name="containerAttributes">Optional additional attributes for the outer container div.</param>
    /// <param name="id">Auto-generated unique ID for the container element.</param>
    /// <returns>A virtual DOM node representing the virtualized list.</returns>
    /// <example>
    /// <code>
    /// Virtualize.Variable(
    ///     items: model.Comments,
    ///     estimateHeight: comment => comment.Body.Length > 200 ? 120.0 : 60.0,
    ///     viewportHeight: 400,
    ///     scrollOffset: model.ScrollOffset,
    ///     render: (comment, index) => CommentView(comment),
    ///     onScroll: data => new Message.ScrollChanged(data.ScrollTop)
    /// )
    /// </code>
    /// </example>
    public static Node Variable<T>(
        IReadOnlyList<T> items,
        Func<T, double> estimateHeight,
        double viewportHeight,
        double scrollOffset,
        Func<T, int, Node> render,
        Func<ScrollEventData?, Message> onScroll,
        int overscan = 3,
        Attribute[]? containerAttributes = null,
        [UniqueId(UniqueIdFormat.HtmlId)] string? id = null)
    {
        var range = VirtualRangeCalculator.Calculate(
            items.Count,
            index => estimateHeight(items[index]),
            viewportHeight,
            scrollOffset,
            overscan);

        return BuildContainer(
            items, range, render, onScroll, viewportHeight, containerAttributes, id);
    }

    /// <summary>
    /// Builds the container DOM structure for a virtualized list.
    /// </summary>
    private static Node BuildContainer<T>(
        IReadOnlyList<T> items,
        VirtualRange range,
        Func<T, int, Node> render,
        Func<ScrollEventData?, Message> onScroll,
        double viewportHeight,
        Attribute[]? containerAttributes,
        string? id)
    {
        var containerId = id ?? "abies-vlist";

        // Build visible item nodes
        var visibleItems = new Node[range.VisibleCount];
        for (var i = 0; i < range.VisibleCount; i++)
        {
            var itemIndex = range.StartIndex + i;
            var item = items[itemIndex];
            visibleItems[i] = render(item, itemIndex);
        }

        // Calculate bottom spacer height
        var bottomSpacerHeight = range.TotalHeight - range.OffsetY
            - (range.VisibleCount > 0
                ? range.TotalHeight / items.Count * range.VisibleCount
                : 0);
        if (bottomSpacerHeight < 0) bottomSpacerHeight = 0;

        // Content div with total height for proper scrollbar
        var contentChildren = new Node[visibleItems.Length + 1];

        // Top spacer (pushes visible items to correct scroll position)
        contentChildren[0] = new Element(
            $"{containerId}__spacer-top",
            "div",
            [
                new Attribute($"{containerId}__spacer-top-style", "style",
                    $"height:{range.OffsetY:F0}px"),
                new Attribute($"{containerId}__spacer-top-aria", "aria-hidden", "true"),
                new Attribute($"{containerId}__spacer-top-id", "id", $"{containerId}__spacer-top")
            ]);

        // Visible items
        Array.Copy(visibleItems, 0, contentChildren, 1, visibleItems.Length);

        var contentDiv = new Element(
            $"{containerId}__content",
            "div",
            [
                new Attribute($"{containerId}__content-style", "style",
                    $"height:{range.TotalHeight:F0}px;position:relative"),
                new Attribute($"{containerId}__content-id", "id", $"{containerId}__content")
            ],
            contentChildren);

        // Container div with overflow scroll and fixed viewport height
        var scrollHandler = Events.onscroll(onScroll);

        var baseAttributes = new Attribute[]
        {
            new($"{containerId}-style", "style",
                $"overflow-y:auto;height:{viewportHeight:F0}px;will-change:transform"),
            new($"{containerId}-id", "id", containerId),
            scrollHandler
        };

        // Merge with any user-provided container attributes
        Attribute[] allAttributes;
        if (containerAttributes is { Length: > 0 })
        {
            allAttributes = new Attribute[baseAttributes.Length + containerAttributes.Length];
            Array.Copy(baseAttributes, allAttributes, baseAttributes.Length);
            Array.Copy(containerAttributes, 0, allAttributes, baseAttributes.Length, containerAttributes.Length);
        }
        else
        {
            allAttributes = baseAttributes;
        }

        return new Element(containerId, "div", allAttributes, contentDiv);
    }
}
