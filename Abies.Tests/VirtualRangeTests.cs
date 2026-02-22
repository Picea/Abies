using Abies.Virtualization;

namespace Abies.Tests;

/// <summary>
/// Tests for <see cref="VirtualRangeCalculator"/> — pure index calculation.
/// Covers fixed-height and variable-height modes, edge cases, and boundary conditions.
/// </summary>
public class VirtualRangeTests
{
    // =========================================================================
    // Fixed-height: Basic behavior
    // =========================================================================

    [Fact]
    public void FixedHeight_EmptyList_ReturnsEmptyRange()
    {
        var range = VirtualRangeCalculator.Calculate(
            itemCount: 0, itemHeight: 50, viewportHeight: 600, scrollOffset: 0);

        Assert.Equal(0, range.StartIndex);
        Assert.Equal(0, range.EndIndex);
        Assert.Equal(0, range.TotalHeight);
        Assert.Equal(0, range.OffsetY);
        Assert.Equal(0, range.VisibleCount);
    }

    [Fact]
    public void FixedHeight_SingleItem_RendersOne()
    {
        var range = VirtualRangeCalculator.Calculate(
            itemCount: 1, itemHeight: 50, viewportHeight: 600, scrollOffset: 0);

        Assert.Equal(0, range.StartIndex);
        Assert.Equal(1, range.EndIndex);
        Assert.Equal(50, range.TotalHeight);
        Assert.Equal(0, range.OffsetY);
        Assert.Equal(1, range.VisibleCount);
    }

    [Fact]
    public void FixedHeight_AtTop_RendersFirstItems()
    {
        // 100 items × 50px = 5000px total, 600px viewport shows ~12 items
        var range = VirtualRangeCalculator.Calculate(
            itemCount: 100, itemHeight: 50, viewportHeight: 600, scrollOffset: 0, overscan: 3);

        Assert.Equal(0, range.StartIndex);
        Assert.True(range.EndIndex >= 12);  // At least viewport-filling items
        Assert.True(range.EndIndex <= 18);  // Plus overscan but not too many
        Assert.Equal(5000, range.TotalHeight);
        Assert.Equal(0, range.OffsetY);
    }

    [Fact]
    public void FixedHeight_ScrolledMiddle_RendersMiddleItems()
    {
        // Scroll to 2500px → item index 50 (2500/50)
        var range = VirtualRangeCalculator.Calculate(
            itemCount: 100, itemHeight: 50, viewportHeight: 600, scrollOffset: 2500, overscan: 3);

        // First visible: 50, with 3 overscan → start at 47
        Assert.Equal(47, range.StartIndex);
        // Last visible: 50 + 12 = 62, with 3 overscan → end at 65
        Assert.True(range.EndIndex >= 62);
        Assert.True(range.EndIndex <= 68);
        Assert.Equal(5000, range.TotalHeight);
        Assert.Equal(47 * 50, range.OffsetY);
    }

    [Fact]
    public void FixedHeight_ScrolledToEnd_RendersLastItems()
    {
        // Total: 100 × 50 = 5000, viewport 600, max scroll = 4400
        var range = VirtualRangeCalculator.Calculate(
            itemCount: 100, itemHeight: 50, viewportHeight: 600, scrollOffset: 4400, overscan: 3);

        Assert.Equal(100, range.EndIndex);  // Includes last item
        Assert.True(range.StartIndex >= 80);  // Near the end
        Assert.True(range.StartIndex <= 88);
    }

    [Fact]
    public void FixedHeight_ScrolledPastEnd_ClampsToEnd()
    {
        var range = VirtualRangeCalculator.Calculate(
            itemCount: 100, itemHeight: 50, viewportHeight: 600, scrollOffset: 99999, overscan: 3);

        Assert.Equal(100, range.EndIndex);
        Assert.True(range.StartIndex < 100);
    }

    [Fact]
    public void FixedHeight_NegativeScroll_ClampsToZero()
    {
        var range = VirtualRangeCalculator.Calculate(
            itemCount: 100, itemHeight: 50, viewportHeight: 600, scrollOffset: -500, overscan: 3);

        Assert.Equal(0, range.StartIndex);
        Assert.True(range.EndIndex > 0);
    }

    [Fact]
    public void FixedHeight_ZeroItemHeight_ReturnsEmpty()
    {
        var range = VirtualRangeCalculator.Calculate(
            itemCount: 100, itemHeight: 0, viewportHeight: 600, scrollOffset: 0);

        Assert.Equal(0, range.VisibleCount);
    }

    [Fact]
    public void FixedHeight_ZeroViewportHeight_ReturnsEmpty()
    {
        var range = VirtualRangeCalculator.Calculate(
            itemCount: 100, itemHeight: 50, viewportHeight: 0, scrollOffset: 0);

        Assert.Equal(0, range.VisibleCount);
    }

    [Fact]
    public void FixedHeight_NegativeItemCount_ReturnsEmpty()
    {
        var range = VirtualRangeCalculator.Calculate(
            itemCount: -5, itemHeight: 50, viewportHeight: 600, scrollOffset: 0);

        Assert.Equal(0, range.VisibleCount);
    }

    // =========================================================================
    // Fixed-height: Overscan behavior
    // =========================================================================

    [Fact]
    public void FixedHeight_ZeroOverscan_RendersOnlyVisible()
    {
        var range = VirtualRangeCalculator.Calculate(
            itemCount: 100, itemHeight: 50, viewportHeight: 600, scrollOffset: 0, overscan: 0);

        // 600 / 50 = 12 items visible
        Assert.Equal(0, range.StartIndex);
        Assert.Equal(12, range.EndIndex);
        Assert.Equal(12, range.VisibleCount);
    }

    [Fact]
    public void FixedHeight_LargeOverscan_ClampedToListBounds()
    {
        var range = VirtualRangeCalculator.Calculate(
            itemCount: 10, itemHeight: 50, viewportHeight: 600, scrollOffset: 0, overscan: 100);

        // Overscan of 100 but only 10 items, so start=0, end=10
        Assert.Equal(0, range.StartIndex);
        Assert.Equal(10, range.EndIndex);
        Assert.Equal(10, range.VisibleCount);
    }

    // =========================================================================
    // Fixed-height: O(1) performance guarantee
    // =========================================================================

    [Fact]
    public void FixedHeight_TenThousandItems_SameRangeAsSmallList()
    {
        var smallRange = VirtualRangeCalculator.Calculate(
            itemCount: 50, itemHeight: 50, viewportHeight: 600, scrollOffset: 0, overscan: 3);

        var largeRange = VirtualRangeCalculator.Calculate(
            itemCount: 10_000, itemHeight: 50, viewportHeight: 600, scrollOffset: 0, overscan: 3);

        // Same viewport, same item height → same visible count
        Assert.Equal(smallRange.VisibleCount, largeRange.VisibleCount);
        Assert.Equal(smallRange.StartIndex, largeRange.StartIndex);
        Assert.Equal(smallRange.EndIndex, largeRange.EndIndex);
    }

    [Fact]
    public void FixedHeight_TenThousandItems_CorrectTotalHeight()
    {
        var range = VirtualRangeCalculator.Calculate(
            itemCount: 10_000, itemHeight: 50, viewportHeight: 600, scrollOffset: 0);

        Assert.Equal(500_000, range.TotalHeight);
    }

    // =========================================================================
    // Fixed-height: TotalHeight and OffsetY invariants
    // =========================================================================

    [Fact]
    public void FixedHeight_TotalHeight_EqualsItemCountTimesHeight()
    {
        var range = VirtualRangeCalculator.Calculate(
            itemCount: 42, itemHeight: 73, viewportHeight: 500, scrollOffset: 0);

        Assert.Equal(42.0 * 73, range.TotalHeight);
    }

    [Fact]
    public void FixedHeight_OffsetY_EqualsStartIndexTimesHeight()
    {
        var range = VirtualRangeCalculator.Calculate(
            itemCount: 100, itemHeight: 50, viewportHeight: 600, scrollOffset: 1000, overscan: 3);

        Assert.Equal(range.StartIndex * 50, range.OffsetY);
    }

    [Fact]
    public void FixedHeight_VisibleCount_EqualsEndMinusStart()
    {
        var range = VirtualRangeCalculator.Calculate(
            itemCount: 100, itemHeight: 50, viewportHeight: 600, scrollOffset: 1500, overscan: 5);

        Assert.Equal(range.EndIndex - range.StartIndex, range.VisibleCount);
    }

    // =========================================================================
    // Fixed-height: All items fit in viewport
    // =========================================================================

    [Fact]
    public void FixedHeight_AllFitInViewport_RendersAll()
    {
        // 5 items × 50px = 250px, viewport = 600px → all fit
        var range = VirtualRangeCalculator.Calculate(
            itemCount: 5, itemHeight: 50, viewportHeight: 600, scrollOffset: 0, overscan: 3);

        Assert.Equal(0, range.StartIndex);
        Assert.Equal(5, range.EndIndex);
        Assert.Equal(5, range.VisibleCount);
    }

    [Fact]
    public void FixedHeight_ExactlyFitViewport_RendersAll()
    {
        // 12 items × 50px = 600px, viewport = 600px → exact fit
        var range = VirtualRangeCalculator.Calculate(
            itemCount: 12, itemHeight: 50, viewportHeight: 600, scrollOffset: 0, overscan: 0);

        Assert.Equal(0, range.StartIndex);
        Assert.Equal(12, range.EndIndex);
        Assert.Equal(12, range.VisibleCount);
    }

    // =========================================================================
    // Variable-height: Basic behavior
    // =========================================================================

    [Fact]
    public void VariableHeight_EmptyList_ReturnsEmptyRange()
    {
        var range = VirtualRangeCalculator.Calculate(
            itemCount: 0,
            estimateHeight: _ => 50,
            viewportHeight: 600,
            scrollOffset: 0);

        Assert.Equal(0, range.VisibleCount);
    }

    [Fact]
    public void VariableHeight_SingleItem_RendersOne()
    {
        var range = VirtualRangeCalculator.Calculate(
            itemCount: 1,
            estimateHeight: _ => 100,
            viewportHeight: 600,
            scrollOffset: 0);

        Assert.Equal(0, range.StartIndex);
        Assert.Equal(1, range.EndIndex);
        Assert.Equal(100, range.TotalHeight);
    }

    [Fact]
    public void VariableHeight_MixedHeights_CorrectTotalHeight()
    {
        // Items: 50, 100, 75, 50, 100 = 375 total
        double[] heights = [50, 100, 75, 50, 100];
        var range = VirtualRangeCalculator.Calculate(
            itemCount: 5,
            estimateHeight: i => heights[i],
            viewportHeight: 600,
            scrollOffset: 0);

        Assert.Equal(375, range.TotalHeight);
    }

    [Fact]
    public void VariableHeight_AllFitInViewport_RendersAll()
    {
        double[] heights = [50, 100, 75, 50, 100]; // 375 total
        var range = VirtualRangeCalculator.Calculate(
            itemCount: 5,
            estimateHeight: i => heights[i],
            viewportHeight: 600,
            scrollOffset: 0,
            overscan: 3);

        Assert.Equal(0, range.StartIndex);
        Assert.Equal(5, range.EndIndex);
    }

    [Fact]
    public void VariableHeight_NullEstimator_ReturnsEmpty()
    {
        var range = VirtualRangeCalculator.Calculate(
            itemCount: 10,
            estimateHeight: null!,
            viewportHeight: 600,
            scrollOffset: 0);

        Assert.Equal(0, range.VisibleCount);
    }

    // =========================================================================
    // Variable-height: Scrolled position
    // =========================================================================

    [Fact]
    public void VariableHeight_ScrolledPastFirstItems_SkipsThem()
    {
        // 10 items, each 100px = 1000px total
        // Scrolled 300px → first visible is index 3
        var range = VirtualRangeCalculator.Calculate(
            itemCount: 10,
            estimateHeight: _ => 100,
            viewportHeight: 400,
            scrollOffset: 300,
            overscan: 1);

        Assert.True(range.StartIndex <= 3);  // With overscan, may start at 2
        Assert.True(range.EndIndex >= 7);    // At least index 3..7 visible
    }

    [Fact]
    public void VariableHeight_OffsetY_EqualsSkippedItemHeights()
    {
        double[] heights = [50, 100, 75, 50, 100, 80, 60, 90, 70, 110];
        var range = VirtualRangeCalculator.Calculate(
            itemCount: 10,
            estimateHeight: i => heights[i],
            viewportHeight: 200,
            scrollOffset: 300,
            overscan: 0);

        // OffsetY should equal sum of heights for items before startIndex
        var expectedOffset = 0.0;
        for (var i = 0; i < range.StartIndex; i++)
        {
            expectedOffset += heights[i];
        }
        Assert.Equal(expectedOffset, range.OffsetY);
    }

    // =========================================================================
    // Variable-height: Negative height items
    // =========================================================================

    [Fact]
    public void VariableHeight_NegativeHeightEstimate_TreatedAsZero()
    {
        var range = VirtualRangeCalculator.Calculate(
            itemCount: 5,
            estimateHeight: _ => -100,
            viewportHeight: 600,
            scrollOffset: 0);

        Assert.Equal(0, range.TotalHeight);
    }
}
