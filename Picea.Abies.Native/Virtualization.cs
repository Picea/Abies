// =============================================================================
// List Windowing
// =============================================================================
// Rendering a row per item stops being viable somewhere in the low thousands:
// every row becomes a real native control that has to be created, measured and
// arranged. Windowing keeps the control count proportional to the viewport
// instead of the data — the model tracks the scroll position, and the view
// emits only the rows that are on screen.
//
// This is deliberately a pure function rather than a control. Working out which
// slice is visible, and how much empty space to leave above and below so the
// scrollbar still reflects the whole list, is the part that is easy to get
// wrong; it is also the part that is worth testing without a UI framework.
//
// What this is not: container recycling. Rows are created and destroyed as they
// scroll in and out rather than being reused, so scrolling does more work than
// a WinUI ItemsRepeater would. It bounds the control count, which is the
// difference between a 50,000-row list being impossible and being usable.
// =============================================================================

namespace Picea.Abies.Native;

/// <summary>
/// The slice of a list that should be rendered, plus the empty space that keeps
/// the scrollbar proportional to the whole list.
/// </summary>
/// <param name="StartIndex">Index of the first item to render.</param>
/// <param name="Count">How many items to render.</param>
/// <param name="TopPadding">Height to leave above the first rendered row.</param>
/// <param name="BottomPadding">Height to leave below the last rendered row.</param>
public readonly record struct ListWindow(int StartIndex, int Count, double TopPadding, double BottomPadding)
{
    /// <summary>Index just past the last rendered item.</summary>
    public int EndIndex => StartIndex + Count;

    /// <summary>An empty window, for an empty list.</summary>
    public static ListWindow Empty { get; } = new(0, 0, 0, 0);
}

/// <summary>Helpers for rendering long lists without a control per item.</summary>
public static class Virtualization
{
    /// <summary>
    /// Works out which rows are visible for a given scroll position.
    /// </summary>
    /// <param name="itemCount">Total number of items in the list.</param>
    /// <param name="rowHeight">Height of a single row, in pixels. Must be positive.</param>
    /// <param name="scrollOffset">Current vertical scroll offset, from OnScrollChanged.</param>
    /// <param name="viewportHeight">Visible height, from OnScrollChanged.</param>
    /// <param name="overscan">
    /// Extra rows rendered above and below the viewport. Without a few, fast
    /// scrolling shows blank space at the leading edge before the next render
    /// lands; too many gives back the cost windowing is there to avoid.
    /// </param>
    /// <returns>The slice to render and the padding around it.</returns>
    public static ListWindow Window(
        int itemCount,
        double rowHeight,
        double scrollOffset,
        double viewportHeight,
        int overscan = 4)
    {
        if (itemCount <= 0 || rowHeight <= 0)
        {
            return ListWindow.Empty;
        }

        // A viewport height of zero means the control has not been measured yet
        // — the very first render, before any scroll event has arrived. Render a
        // screenful's worth of rows rather than nothing, so the list is not
        // blank until the user touches it.
        var effectiveViewport = viewportHeight > 0 ? viewportHeight : rowHeight * 20;

        var clampedOffset = Math.Clamp(scrollOffset, 0, Math.Max(0, itemCount * rowHeight - effectiveViewport));

        var firstVisible = (int)Math.Floor(clampedOffset / rowHeight);
        var visibleCount = (int)Math.Ceiling(effectiveViewport / rowHeight) + 1;

        var start = Math.Max(0, firstVisible - Math.Max(0, overscan));
        var end = Math.Min(itemCount, firstVisible + visibleCount + Math.Max(0, overscan));
        var count = Math.Max(0, end - start);

        return new ListWindow(
            StartIndex: start,
            Count: count,
            TopPadding: start * rowHeight,
            BottomPadding: Math.Max(0, (itemCount - end) * rowHeight));
    }
}
