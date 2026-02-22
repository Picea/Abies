using System.Runtime.Versioning;
using Abies.DOM;
using Abies.Html;
using DOMAttribute = Abies.DOM.Attribute;

namespace Abies.Tests;

/// <summary>
/// Tests for the <see cref="Virtualize"/> helper — verifies DOM structure,
/// spacer heights, and correct item rendering.
/// </summary>
[SupportedOSPlatform("browser")]
public class VirtualizeTests
{
    private record ScrollMessage(double ScrollTop) : Message;

    private static Node RenderItem(string text, int index)
        => new Element($"item-{index}", "div",
            [new DOMAttribute($"item-{index}-class", "class", "item")],
            new Text($"text-{index}", text));

    // =========================================================================
    // Fixed-height: DOM structure
    // =========================================================================

    [Fact]
    public void Fixed_EmptyList_RendersEmptyContainer()
    {
        var node = Virtualize.Fixed<string>(
            items: [],
            itemHeight: 50,
            viewportHeight: 600,
            scrollOffset: 0,
            render: RenderItem,
            onScroll: _ => new ScrollMessage(0));

        var container = Assert.IsType<Element>(node);
        Assert.Equal("div", container.Tag);
        // Should have overflow-y:auto style
        Assert.Contains(container.Attributes, a => a.Name == "style" && a.Value.Contains("overflow-y:auto"));
    }

    [Fact]
    public void Fixed_ThreeItems_RendersAllWhenFitInViewport()
    {
        var items = new[] { "Alpha", "Beta", "Gamma" };
        var node = Virtualize.Fixed(
            items: items,
            itemHeight: 50,
            viewportHeight: 600,
            scrollOffset: 0,
            render: RenderItem,
            onScroll: _ => new ScrollMessage(0));

        var container = Assert.IsType<Element>(node);

        // Content div should be the single child
        var contentDiv = Assert.IsType<Element>(container.Children[0]);
        Assert.Equal("div", contentDiv.Tag);

        // Content should have total height style
        Assert.Contains(contentDiv.Attributes, a =>
            a.Name == "style" && a.Value.Contains("height:150px"));

        // Spacer + 3 items = 4 children
        Assert.Equal(4, contentDiv.Children.Length);

        // First child is top spacer with height:0px (at top, no scroll)
        var topSpacer = Assert.IsType<Element>(contentDiv.Children[0]);
        Assert.Contains(topSpacer.Attributes, a =>
            a.Name == "style" && a.Value.Contains("height:0px"));
    }

    [Fact]
    public void Fixed_ContainerHasScrollHandler()
    {
        var node = Virtualize.Fixed(
            items: new[] { "item" },
            itemHeight: 50,
            viewportHeight: 600,
            scrollOffset: 0,
            render: RenderItem,
            onScroll: data => new ScrollMessage(data?.ScrollTop ?? 0));

        var container = Assert.IsType<Element>(node);

        // Should have a scroll event handler attribute
        Assert.Contains(container.Attributes, a => a is Handler h && h.Name == "data-event-scroll");
    }

    [Fact]
    public void Fixed_ContainerHasViewportHeightStyle()
    {
        var node = Virtualize.Fixed(
            items: new[] { "item" },
            itemHeight: 50,
            viewportHeight: 400,
            scrollOffset: 0,
            render: RenderItem,
            onScroll: _ => new ScrollMessage(0));

        var container = Assert.IsType<Element>(node);
        Assert.Contains(container.Attributes, a =>
            a.Name == "style" && a.Value.Contains("height:400px"));
    }

    [Fact]
    public void Fixed_ContainerHasIdAttribute()
    {
        var node = Virtualize.Fixed(
            items: new[] { "item" },
            itemHeight: 50,
            viewportHeight: 600,
            scrollOffset: 0,
            render: RenderItem,
            onScroll: _ => new ScrollMessage(0),
            id: "my-list");

        var container = Assert.IsType<Element>(node);
        Assert.Equal("my-list", container.Id);
    }

    // =========================================================================
    // Fixed-height: Scrolled rendering
    // =========================================================================

    [Fact]
    public void Fixed_ScrolledDown_RendersSubsetWithSpacers()
    {
        // 100 items × 50px = 5000px total, viewport 200px
        // Scrolled 1000px → first visible is index 20
        var items = Enumerable.Range(0, 100).Select(i => $"Item {i}").ToArray();
        var node = Virtualize.Fixed(
            items: items,
            itemHeight: 50,
            viewportHeight: 200,
            scrollOffset: 1000,
            render: RenderItem,
            onScroll: _ => new ScrollMessage(0),
            overscan: 0);

        var container = Assert.IsType<Element>(node);
        var contentDiv = Assert.IsType<Element>(container.Children[0]);

        // Total height should be correct
        Assert.Contains(contentDiv.Attributes, a =>
            a.Name == "style" && a.Value.Contains("height:5000px"));

        // Top spacer should have height for 20 skipped items
        var topSpacer = Assert.IsType<Element>(contentDiv.Children[0]);
        Assert.Contains(topSpacer.Attributes, a =>
            a.Name == "style" && a.Value.Contains("height:1000px"));

        // Should NOT render all 100 items
        Assert.True(contentDiv.Children.Length < 100);
    }

    [Fact]
    public void Fixed_TenThousandItems_RendersOnly_Viewport_Items()
    {
        var items = Enumerable.Range(0, 10_000).Select(i => $"Item {i}").ToArray();
        var node = Virtualize.Fixed(
            items: items,
            itemHeight: 50,
            viewportHeight: 600,
            scrollOffset: 0,
            render: RenderItem,
            onScroll: _ => new ScrollMessage(0),
            overscan: 5);

        var container = Assert.IsType<Element>(node);
        var contentDiv = Assert.IsType<Element>(container.Children[0]);

        // Should render ~17 items (12 visible + 5 overscan below) + 1 spacer
        Assert.True(contentDiv.Children.Length <= 25);
        Assert.True(contentDiv.Children.Length >= 10);
    }

    // =========================================================================
    // Fixed-height: Custom container attributes
    // =========================================================================

    [Fact]
    public void Fixed_CustomContainerAttributes_AreMerged()
    {
        var customAttrs = new DOMAttribute[]
        {
            new("cls-id", "class", "custom-list")
        };

        var node = Virtualize.Fixed(
            items: new[] { "item" },
            itemHeight: 50,
            viewportHeight: 600,
            scrollOffset: 0,
            render: RenderItem,
            onScroll: _ => new ScrollMessage(0),
            containerAttributes: customAttrs);

        var container = Assert.IsType<Element>(node);
        Assert.Contains(container.Attributes, a => a.Name == "class" && a.Value == "custom-list");
    }

    // =========================================================================
    // Variable-height: DOM structure
    // =========================================================================

    [Fact]
    public void Variable_EmptyList_RendersEmptyContainer()
    {
        var node = Virtualize.Variable<string>(
            items: [],
            estimateHeight: _ => 50,
            viewportHeight: 600,
            scrollOffset: 0,
            render: RenderItem,
            onScroll: _ => new ScrollMessage(0));

        var container = Assert.IsType<Element>(node);
        Assert.Equal("div", container.Tag);
    }

    [Fact]
    public void Variable_MixedHeights_CorrectTotalHeight()
    {
        // Heights: 50, 100, 75 = 225 total
        var items = new[] { "short", "tall", "medium" };
        double[] heights = [50, 100, 75];

        var node = Virtualize.Variable(
            items: items,
            estimateHeight: item => heights[Array.IndexOf(items, item)],
            viewportHeight: 600,
            scrollOffset: 0,
            render: RenderItem,
            onScroll: _ => new ScrollMessage(0));

        var container = Assert.IsType<Element>(node);
        var contentDiv = Assert.IsType<Element>(container.Children[0]);

        Assert.Contains(contentDiv.Attributes, a =>
            a.Name == "style" && a.Value.Contains("height:225px"));
    }
}
