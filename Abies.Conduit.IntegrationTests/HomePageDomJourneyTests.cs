using Abies.Conduit.IntegrationTests.Testing;
using Abies.Conduit.Page.Home;
using Abies.DOM;
using Xunit;
using HomePage = Abies.Conduit.Page.Home.Page;

namespace Abies.Conduit.IntegrationTests;

public class HomePageDomJourneyTests
{
    [Fact]
    public void Pagination_ClickingPage2_SetsActivePageAndAriaCurrent()
    {
        // Arrange: a model that will render 2 pages.
        // ArticlesCount=20 => 2 pages. CurrentPage=0 initially.
        var model = new Model(
            Articles: [],
            ArticlesCount: 20,
            Tags: [],
            ActiveTab: FeedTab.Global,
            ActiveTag: "",
            IsLoading: false,
            CurrentPage: 0,
            CurrentUser: null);

        // Act: click the pagination button whose text is "2".
        var (nextModel, _) = MvuDomTestHarness.DispatchClick(
            model,
            HomePage.View,
            HomePage.Update,
            elementPredicate: el => el.Tag == "button" && el.Children.OfType<Text>().Any(t => t.Value == "2"));

        var nextDom = HomePage.View(nextModel);

        // Assert: model updated AND DOM reflects it.
        Assert.Equal(1, nextModel.CurrentPage);

        // Find the active page button and check it has aria-current="page".
        var activeButton = MvuDomTestHarness.FindFirstElement(nextDom,
            el => el.Tag == "button" && el.Attributes.Any(a => a.Name == "aria-current" && a.Value == "page"));

        Assert.Contains(activeButton.Children.OfType<Text>(), t => t.Value == "2");
    }
}
