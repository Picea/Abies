using Abies.Conduit.IntegrationTests.Testing;
using Abies.Conduit.Page.Home;
using Abies.DOM;
using Xunit;

namespace Abies.Conduit.IntegrationTests;

public class HomeMoreDomJourneyTests
{
    [Fact]
    public void Home_ClickingGlobalFeedTab_SetsActiveTabAndResetsPage()
    {
        // Arrange: start on Tag tab, current page != 0
        var model = new Model(
            Articles: [],
            ArticlesCount: 0,
            Tags: [],
            ActiveTab: FeedTab.Tag,
            ActiveTag: "dotnet",
            IsLoading: false,
            CurrentPage: 3,
            CurrentUser: null);

        // Act: click "Global Feed" tab (anchor with direct text)
        var (m1, _) = MvuDomTestHarness.DispatchClick(
            model,
            Page.Home.Page.View,
            Page.Home.Page.Update,
        MvuDomTestHarness.HasTag("a").And(MvuDomTestHarness.HasDirectText("Global Feed")));

        // Assert
        Assert.Equal(FeedTab.Global, m1.ActiveTab);
        Assert.Equal(0, m1.CurrentPage);

        // Optional DOM sanity: active class on Global Feed tab
        var dom = Page.Home.Page.View(m1);
        var activeGlobal = MvuDomTestHarness.FindFirstElement(dom,
            el => el.Tag == "a" && el.Attributes.Any(a => a.Name == "class" && a.Value.Contains("active"))
                              && el.Children.OfType<Text>().Any(t => t.Value == "Global Feed"));
        Assert.NotNull(activeGlobal);
    }

    [Fact]
    public void Home_ClickingATag_SetsTagTabAndActiveTag()
    {
        // Arrange: include a tag so the tag cloud renders
        var model = new Model(
            Articles: [],
            ArticlesCount: 0,
            Tags: ["foo"],
            ActiveTab: FeedTab.Global,
            ActiveTag: "",
            IsLoading: false,
            CurrentPage: 0,
            CurrentUser: null);

        // Act: click the tag pill
        var (m1, _) = MvuDomTestHarness.DispatchClick(
            model,
                Page.Home.Page.View,
                Page.Home.Page.Update,
            MvuDomTestHarness.HasTag("a").And(MvuDomTestHarness.HasDirectText("foo")));

        // Assert
        Assert.Equal(FeedTab.Tag, m1.ActiveTab);
        Assert.Equal("foo", m1.ActiveTag);

        // DOM: should show the active tag breadcrumb "# foo"
        var dom = Page.Home.Page.View(m1);
        var tagCrumb = MvuDomTestHarness.FindFirstElement(dom,
            MvuDomTestHarness.HasTag("a").And(MvuDomTestHarness.HasDirectText("# foo")));
        Assert.NotNull(tagCrumb);
    }
}
