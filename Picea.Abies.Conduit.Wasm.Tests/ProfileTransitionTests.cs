// =============================================================================
// Profile Transition Tests — Pure State Machine Verification
// =============================================================================
// Tests for the Profile page transitions in the Conduit MVU program.
// Verifies tab switching, follow/unfollow behavior, article loading,
// and navigation between own profile and other users' profiles.
// =============================================================================

using Picea.Abies.Conduit.App;
using FluentAssertions;

namespace Picea.Abies.Conduit.Wasm.Tests;

public class ProfileTransitionTests
{
    private static readonly Session _testSession = new(
        Token: "test-token",
        Username: "testuser",
        Email: "test@example.com",
        Bio: "Test bio",
        Image: null);

    private static readonly ProfileData _otherProfile = new(
        Username: "otheruser",
        Bio: "Other bio",
        Image: "https://example.com/other.jpg",
        Following: false);

    private static readonly ProfileData _ownProfile = new(
        Username: "testuser",
        Bio: "Test bio",
        Image: null,
        Following: false);

    /// <summary>Creates a model with the Profile page for a given profile.</summary>
    private static Model CreateProfileModel(ProfileData? profile = null) =>
        new(
            Page: new Page.Profile(new ProfileModel(
                Username: (profile ?? _otherProfile).Username,
                Profile: profile ?? _otherProfile,
                Articles: [],
                ArticlesCount: 0,
                CurrentPage: 1,
                ShowFavorites: false,
                IsLoading: false)),
            Session: _testSession,
            ApiUrl: "http://localhost:5000");

    [Fact]
    public void TabChanged_ToFavorites_UpdatesShowFavorites()
    {
        var model = CreateProfileModel();

        var (newModel, command) = ConduitProgram.Transition(model, new ProfileTabChanged(true));

        var profile = newModel.Page.Should().BeOfType<Page.Profile>().Subject;
        profile.Data.ShowFavorites.Should().BeTrue();
        profile.Data.CurrentPage.Should().Be(1);
        command.Should().NotBe(Commands.None);
    }

    [Fact]
    public void TabChanged_ToMyArticles_UpdatesShowFavorites()
    {
        var model = CreateProfileModel();
        var profilePage = (Page.Profile)model.Page;
        var onFavorites = model with
        {
            Page = new Page.Profile(profilePage.Data with { ShowFavorites = true })
        };

        var (newModel, _) = ConduitProgram.Transition(onFavorites, new ProfileTabChanged(false));

        var profile = newModel.Page.Should().BeOfType<Page.Profile>().Subject;
        profile.Data.ShowFavorites.Should().BeFalse();
    }

    [Fact]
    public void TabChanged_ResetsCurrentPageTo1()
    {
        var model = CreateProfileModel();
        var profilePage = (Page.Profile)model.Page;
        var onPage3 = model with
        {
            Page = new Page.Profile(profilePage.Data with { CurrentPage = 3 })
        };

        var (newModel, _) = ConduitProgram.Transition(onPage3, new ProfileTabChanged(true));

        var profile = newModel.Page.Should().BeOfType<Page.Profile>().Subject;
        profile.Data.CurrentPage.Should().Be(1);
    }

    [Fact]
    public void TabChanged_FetchesArticlesCommand()
    {
        var model = CreateProfileModel();

        var (_, command) = ConduitProgram.Transition(model, new ProfileTabChanged(true));

        command.Should().BeOfType<FetchArticles>();
    }

    [Fact]
    public void FollowToggled_UpdatesProfileFollowingState()
    {
        var model = CreateProfileModel(_otherProfile with { Following = false });
        var followedProfile = _otherProfile with { Following = true };

        var (newModel, _) = ConduitProgram.Transition(model, new FollowToggled(followedProfile));

        var profile = newModel.Page.Should().BeOfType<Page.Profile>().Subject;
        profile.Data.Profile!.Following.Should().BeTrue();
    }

    [Fact]
    public void FollowToggled_Unfollow_UpdatesProfileFollowingState()
    {
        var model = CreateProfileModel(_otherProfile with { Following = true });
        var unfollowedProfile = _otherProfile with { Following = false };

        var (newModel, _) = ConduitProgram.Transition(model, new FollowToggled(unfollowedProfile));

        var profile = newModel.Page.Should().BeOfType<Page.Profile>().Subject;
        profile.Data.Profile!.Following.Should().BeFalse();
    }

    [Fact]
    public void ArticlesLoaded_UpdatesArticlesList()
    {
        var model = CreateProfileModel();
        var articles = new List<ArticlePreviewData>
        {
            new("test-article", "Test", "Desc", ["tag"],
                DateTimeOffset.Parse("2024-01-01"), DateTimeOffset.Parse("2024-01-01"),
                false, 5, new AuthorData("otheruser", "Bio", null, false))
        };

        var (newModel, _) = ConduitProgram.Transition(model, new ArticlesLoaded(articles, 1));

        var profile = newModel.Page.Should().BeOfType<Page.Profile>().Subject;
        profile.Data.Articles.Should().HaveCount(1);
        profile.Data.ArticlesCount.Should().Be(1);
    }

    [Fact]
    public void ProfileLoaded_UpdatesProfileData()
    {
        var model = CreateProfileModel();
        var loadedProfile = _otherProfile with { Bio = "Updated bio", Following = true };

        var (newModel, _) = ConduitProgram.Transition(model, new ProfileLoaded(loadedProfile));

        var profile = newModel.Page.Should().BeOfType<Page.Profile>().Subject;
        profile.Data.Profile!.Bio.Should().Be("Updated bio");
        profile.Data.Profile.Following.Should().BeTrue();
    }

    [Fact]
    public void PageChanged_UpdatesCurrentPage()
    {
        var model = CreateProfileModel();

        var (newModel, command) = ConduitProgram.Transition(model, new PageChanged(2));

        var profile = newModel.Page.Should().BeOfType<Page.Profile>().Subject;
        profile.Data.CurrentPage.Should().Be(2);
        command.Should().NotBe(Commands.None);
    }

    [Fact]
    public void PageChanged_FetchesArticlesForNewPage()
    {
        var model = CreateProfileModel();

        var (_, command) = ConduitProgram.Transition(model, new PageChanged(3));

        command.Should().BeOfType<FetchArticles>();
    }
}
