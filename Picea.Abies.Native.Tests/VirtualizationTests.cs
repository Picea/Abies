// =============================================================================
// List Windowing Tests
// =============================================================================
// The window arithmetic decides both what the user sees and whether the
// scrollbar tells the truth, and it is pure — so it is worth pinning hard,
// including the boundaries where off-by-one errors show up as blank rows.
// =============================================================================

using Picea.Abies.DOM;
using Picea.Abies.Native.Rendering;

namespace Picea.Abies.Native.Tests;

public class VirtualizationTests
{
    private const double RowHeight = 40;

    [Test]
    public async Task AtTheTop_RendersFromTheFirstItem()
    {
        var window = Virtualization.Window(itemCount: 10_000, RowHeight, scrollOffset: 0, viewportHeight: 400);

        await Assert.That(window.StartIndex).IsEqualTo(0);
        await Assert.That(window.TopPadding).IsEqualTo(0);
        // A screenful is 10 rows; the window adds one partial row plus overscan.
        await Assert.That(window.Count).IsLessThan(25);
        await Assert.That(window.Count).IsGreaterThan(10);
    }

    /// <summary>Padding above plus rows plus padding below must equal the full list height,
    /// or the scrollbar misrepresents how much there is to scroll.</summary>
    [Test]
    public async Task PaddingAndRows_AlwaysSpanTheWholeList()
    {
        foreach (var offset in new double[] { 0, 137, 4000, 100_000, 399_600 })
        {
            var window = Virtualization.Window(10_000, RowHeight, offset, viewportHeight: 400);
            var spanned = window.TopPadding + (window.Count * RowHeight) + window.BottomPadding;

            await Assert.That(spanned).IsEqualTo(10_000 * RowHeight)
                .Because($"offset {offset} should still span the full list height");
        }
    }

    [Test]
    public async Task ScrolledIntoTheMiddle_WindowFollowsTheOffset()
    {
        // 100 rows down.
        var window = Virtualization.Window(10_000, RowHeight, scrollOffset: 100 * RowHeight, viewportHeight: 400);

        await Assert.That(window.StartIndex).IsLessThanOrEqualTo(100);
        await Assert.That(window.EndIndex).IsGreaterThan(110);
        await Assert.That(window.TopPadding).IsEqualTo(window.StartIndex * RowHeight);
    }

    [Test]
    public async Task AtTheBottom_DoesNotRunPastTheEnd()
    {
        var itemCount = 500;
        var window = Virtualization.Window(itemCount, RowHeight, scrollOffset: itemCount * RowHeight, viewportHeight: 400);

        await Assert.That(window.EndIndex).IsEqualTo(itemCount);
        await Assert.That(window.BottomPadding).IsEqualTo(0);
        await Assert.That(window.StartIndex).IsGreaterThan(0);
    }

    /// <summary>A scroll offset past the end must clamp, not produce a negative window.</summary>
    [Test]
    public async Task OffsetBeyondTheEnd_Clamps()
    {
        var window = Virtualization.Window(100, RowHeight, scrollOffset: 999_999, viewportHeight: 400);

        await Assert.That(window.StartIndex).IsGreaterThanOrEqualTo(0);
        await Assert.That(window.EndIndex).IsEqualTo(100);
        await Assert.That(window.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task ViewportTallerThanTheList_RendersEverything()
    {
        var window = Virtualization.Window(itemCount: 5, RowHeight, scrollOffset: 0, viewportHeight: 2000);

        await Assert.That(window.StartIndex).IsEqualTo(0);
        await Assert.That(window.Count).IsEqualTo(5);
        await Assert.That(window.TopPadding).IsEqualTo(0);
        await Assert.That(window.BottomPadding).IsEqualTo(0);
    }

    [Test]
    public async Task EmptyList_ProducesAnEmptyWindow()
    {
        await Assert.That(Virtualization.Window(0, RowHeight, 0, 400)).IsEqualTo(ListWindow.Empty);
    }

    /// <summary>Guards against a divide-by-zero from an unmeasured row height.</summary>
    [Test]
    public async Task NonPositiveRowHeight_ProducesAnEmptyWindow()
    {
        await Assert.That(Virtualization.Window(100, 0, 0, 400)).IsEqualTo(ListWindow.Empty);
        await Assert.That(Virtualization.Window(100, -5, 0, 400)).IsEqualTo(ListWindow.Empty);
    }

    /// <summary>
    /// Before the first scroll event the viewport is still zero. Rendering
    /// nothing would leave the list blank until the user touched it.
    /// </summary>
    [Test]
    public async Task UnmeasuredViewport_StillRendersAScreenful()
    {
        var window = Virtualization.Window(10_000, RowHeight, scrollOffset: 0, viewportHeight: 0);

        await Assert.That(window.Count).IsGreaterThan(0);
        await Assert.That(window.StartIndex).IsEqualTo(0);
    }

    [Test]
    public async Task Overscan_WidensTheWindowOnBothSides()
    {
        var none = Virtualization.Window(10_000, RowHeight, 100 * RowHeight, 400, overscan: 0);
        var wide = Virtualization.Window(10_000, RowHeight, 100 * RowHeight, 400, overscan: 10);

        await Assert.That(wide.StartIndex).IsLessThan(none.StartIndex);
        await Assert.That(wide.EndIndex).IsGreaterThan(none.EndIndex);
    }

    [Test]
    public async Task NegativeOverscan_IsTreatedAsZero()
    {
        var negative = Virtualization.Window(10_000, RowHeight, 100 * RowHeight, 400, overscan: -5);
        var zero = Virtualization.Window(10_000, RowHeight, 100 * RowHeight, 400, overscan: 0);

        await Assert.That(negative).IsEqualTo(zero);
    }

    /// <summary>
    /// Scrolling by one row should recycle the overlap: only the row entering
    /// the window is created. If this regresses, windowing degrades into
    /// rebuilding the whole visible list on every scroll event.
    /// </summary>
    [Test]
    public async Task ScrollingOneRow_OnlyCreatesTheEnteringRow()
    {
        var backend = new FakeBackend();
        var interpreter = new PatchInterpreter<FakeControl>(backend);

        static Element Rows(ListWindow w) =>
            (Element)Elements.StackPanel([Properties.Id("list")],
            [
                .. Enumerable.Range(w.StartIndex, w.Count).Select(i =>
                    Elements.TextBlock([Properties.Id($"row-{i}"), Properties.Key($"k{i}")], $"item {i}")),
            ]);

        var before = Rows(Virtualization.Window(10_000, RowHeight, 0, 400, overscan: 0));
        var after = Rows(Virtualization.Window(10_000, RowHeight, RowHeight, 400, overscan: 0));

        interpreter.Apply(Operations.Diff(null, before));
        var createdAfterFirstRender = backend.AllCreated.Count;

        interpreter.Apply(Operations.Diff(before, after));
        var createdByScrolling = backend.AllCreated.Count - createdAfterFirstRender;

        // One row leaves the top, one enters the bottom; everything between is reused.
        await Assert.That(createdByScrolling).IsEqualTo(1)
            .Because("scrolling one row should build one row, not rebuild the window");
    }

    /// <summary>The point of the exercise: control count tracks the viewport, not the data.</summary>
    [Test]
    public async Task WindowSize_IsBoundedByViewportNotItemCount()
    {
        var small = Virtualization.Window(100, RowHeight, 0, 400);
        var huge = Virtualization.Window(1_000_000, RowHeight, 0, 400);

        await Assert.That(huge.Count).IsEqualTo(small.Count);
        await Assert.That(huge.Count).IsLessThan(30);
    }
}
