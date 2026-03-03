// =============================================================================
// Virtual Range Calculation
// =============================================================================
// Pure functions for calculating which items are visible in a virtualized list.
// These functions are stateless and deterministic, making them easy to test
// and reason about.
//
// Two modes are supported:
// - Fixed-height: O(1) index calculation when all items have the same height
// - Variable-height: O(n) calculation using per-item height estimation
//
// Architecture Decision Records:
// - ADR-003: Virtual DOM (docs/adr/ADR-003-virtual-dom.md)
// =============================================================================

using System.Runtime.CompilerServices;

namespace Abies.Virtualization;

/// <summary>
/// Describes the visible range of items in a virtualized list.
/// </summary>
/// <param name="StartIndex">First visible item index (inclusive, including overscan).</param>
/// <param name="EndIndex">Last visible item index (exclusive, including overscan).</param>
/// <param name="TotalHeight">Total scrollable height in pixels (for spacer sizing).</param>
/// <param name="OffsetY">Top offset in pixels for the first rendered item.</param>
/// <param name="VisibleCount">Number of items actually rendered (EndIndex - StartIndex).</param>
public readonly record struct VirtualRange(
    int StartIndex,
    int EndIndex,
    double TotalHeight,
    double OffsetY,
    int VisibleCount);

/// <summary>
/// Pure functions for calculating which items are visible in a virtualized list.
/// </summary>
public static class VirtualRangeCalculator
{
    /// <summary>
    /// Calculates the visible range for fixed-height items.
    /// O(1) time complexity — uses simple division to find visible indices.
    /// </summary>
    /// <param name="itemCount">Total number of items in the list.</param>
    /// <param name="itemHeight">Height of each item in pixels. Must be positive.</param>
    /// <param name="viewportHeight">Height of the visible area in pixels.</param>
    /// <param name="scrollOffset">Current scroll position (scrollTop) in pixels.</param>
    /// <param name="overscan">Number of extra items to render above and below the viewport.</param>
    /// <returns>A <see cref="VirtualRange"/> describing which items to render.</returns>
    /// <example>
    /// <code>
    /// // 1000 items, each 50px tall, 600px viewport, scrolled 250px, 5 item overscan
    /// var range = VirtualRangeCalculator.Calculate(1000, 50, 600, 250, 5);
    /// // range.StartIndex == 0  (max(0, 5 - 5))
    /// // range.EndIndex == 22   (min(1000, 17 + 5))
    /// // range.TotalHeight == 50000
    /// // range.OffsetY == 0
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VirtualRange Calculate(
        int itemCount,
        double itemHeight,
        double viewportHeight,
        double scrollOffset,
        int overscan = 3)
    {
        if (itemCount <= 0 || itemHeight <= 0 || viewportHeight <= 0)
        {
            return new VirtualRange(0, 0, 0, 0, 0);
        }

        // Clamp scroll offset to valid range
        var totalHeight = itemCount * itemHeight;
        var clampedScroll = Math.Max(0, Math.Min(scrollOffset, totalHeight - viewportHeight));

        // O(1) index calculation
        var firstVisible = (int)(clampedScroll / itemHeight);
        var visibleCount = (int)Math.Ceiling(viewportHeight / itemHeight);
        var lastVisible = firstVisible + visibleCount; // exclusive

        // Apply overscan
        var startIndex = Math.Max(0, firstVisible - overscan);
        var endIndex = Math.Min(itemCount, lastVisible + overscan);

        var offsetY = startIndex * itemHeight;

        return new VirtualRange(
            StartIndex: startIndex,
            EndIndex: endIndex,
            TotalHeight: totalHeight,
            OffsetY: offsetY,
            VisibleCount: endIndex - startIndex);
    }

    /// <summary>
    /// Calculates the visible range for variable-height items.
    /// O(n) time complexity in the worst case, but typically scans only visible items.
    /// </summary>
    /// <param name="itemCount">Total number of items in the list.</param>
    /// <param name="estimateHeight">Function that estimates the height of an item at a given index.</param>
    /// <param name="viewportHeight">Height of the visible area in pixels.</param>
    /// <param name="scrollOffset">Current scroll position (scrollTop) in pixels.</param>
    /// <param name="overscan">Number of extra items to render above and below the viewport.</param>
    /// <returns>A <see cref="VirtualRange"/> describing which items to render.</returns>
    public static VirtualRange Calculate(
        int itemCount,
        Func<int, double> estimateHeight,
        double viewportHeight,
        double scrollOffset,
        int overscan = 3)
    {
        if (itemCount <= 0 || viewportHeight <= 0 || estimateHeight is null)
        {
            return new VirtualRange(0, 0, 0, 0, 0);
        }

        var clampedScroll = Math.Max(0, scrollOffset);

        // Single pass: compute total height, find first visible and last visible items.
        // An item is "first visible" when its bottom edge exceeds the scroll offset.
        var totalHeight = 0.0;
        var firstVisible = -1;
        var lastVisible = -1;  // exclusive

        var runningTop = 0.0;
        var viewportEnd = clampedScroll + viewportHeight;

        for (var i = 0; i < itemCount; i++)
        {
            var h = Math.Max(0, estimateHeight(i));
            var bottom = runningTop + h;

            if (firstVisible < 0 && bottom > clampedScroll)
            {
                firstVisible = i;
            }

            if (lastVisible < 0 && bottom >= viewportEnd)
            {
                lastVisible = i + 1; // exclusive
            }

            runningTop = bottom;
        }

        totalHeight = runningTop;

        // Handle edge cases
        if (firstVisible < 0)
        {
            // Scrolled past all items — show last items
            firstVisible = Math.Max(0, itemCount - 1);
        }

        if (lastVisible < 0)
        {
            // Viewport extends past all items
            lastVisible = itemCount;
        }

        // Apply overscan
        var startIndex = Math.Max(0, firstVisible - overscan);
        var endIndex = Math.Min(itemCount, lastVisible + overscan);

        // Calculate offset for the start index
        var offsetY = 0.0;
        for (var i = 0; i < startIndex; i++)
        {
            offsetY += Math.Max(0, estimateHeight(i));
        }

        return new VirtualRange(
            StartIndex: startIndex,
            EndIndex: endIndex,
            TotalHeight: totalHeight,
            OffsetY: offsetY,
            VisibleCount: endIndex - startIndex);
    }
}
