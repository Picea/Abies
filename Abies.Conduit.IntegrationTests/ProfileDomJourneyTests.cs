using Abies.Conduit.IntegrationTests.Testing;
using Abies.Conduit.Page.Profile;
using Abies.DOM;
using Xunit;

namespace Abies.Conduit.IntegrationTests;

public class ProfileDomJourneyTests
{
    [Fact]
    public void Profile_ClickingFollowButton_TogglesFollowing_AndButtonText()
    {
        // Arrange
        Model model = new(
                UserName: new Main.UserName("bob"),
                IsLoading: false,
                Profile: new Page.Home.Profile("bob", "", "img", Following: false),
                Articles: [],
                ArticlesCount: 0,
                ActiveTab: ProfileTab.MyArticles,
                CurrentPage: 0,
                CurrentUser: null);

        // Act: click the follow/unfollow button
        var (m1, _) = MvuDomTestHarness.DispatchClick(
                model,
                Page.Profile.Page.View,
                Page.Profile.Page.Update,
                el => el.Tag == "button" && el.Children.OfType<Text>().Any(t => t.Value.Contains("Follow bob")));

        // Assert model
        Assert.NotNull(m1.Profile);
        Assert.True(m1.Profile!.Following);

        // Assert DOM
        var dom = Page.Profile.Page.View(m1);
        var unfollowBtn = MvuDomTestHarness.FindFirstElement(dom,
            el => el.Tag == "button" && el.Children.OfType<Text>().Any(t => t.Value.Contains("Unfollow bob")));
        Assert.NotNull(unfollowBtn);
    }
}
