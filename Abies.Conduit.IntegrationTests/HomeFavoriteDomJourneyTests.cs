using System.Linq;
using Abies.Conduit.IntegrationTests.Testing;
using Abies.Conduit.Page.Home;
using Abies.DOM;
using Xunit;

namespace Abies.Conduit.IntegrationTests;

public class HomeFavoriteDomJourneyTests
{
    [Fact]
    public void Home_ClickingFavorite_OnArticlePreview_SetsLoading()
    {
        // Arrange: feed with one article
        var article = new Article(
            Slug: "a",
            Title: "T",
            Description: "D",
            Body: "B",
            TagList: [],
            CreatedAt: "2020-01-01",
            UpdatedAt: "2020-01-01",
            Favorited: false,
            FavoritesCount: 0,
            Author: new Profile("bob", "", "img", Following: false));

        var model = new Model(
            Articles: [article],
            ArticlesCount: 1,
            Tags: [],
            ActiveTab: FeedTab.Global,
            ActiveTag: "",
            IsLoading: false,
            CurrentPage: 0,
            CurrentUser: null);

        // Act: click favorite button (ion-heart)
            var (m1, cmd) = MvuDomTestHarness.DispatchClick(
                model,
                Abies.Conduit.Page.Home.Page.View,
                Abies.Conduit.Page.Home.Page.Update,
            el => el.Tag == "button" && el.Attributes.Any(a => a.Name == "class" && a.Value.Contains("btn"))
                                  && el.Children.OfType<Element>().Any(c => c.Tag == "i" && c.Attributes.Any(a => a.Name == "class" && a.Value.Contains("ion-heart"))));

        // Assert: Update sets IsLoading true and returns a command batch (toggle favorite + reload).
        Assert.True(m1.IsLoading);
        Assert.NotNull(cmd);
    }
}
