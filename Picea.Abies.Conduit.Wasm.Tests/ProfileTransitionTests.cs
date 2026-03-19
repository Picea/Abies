// =============================================================================
// Profile Transition Tests — Pure State Machine Verification
// =============================================================================
// Tests for the Profile page transitions in the Conduit MVU program.
// Verifies tab switching, follow/unfollow behavior, article loading,
// and navigation between own profile and other users' profiles.
// =============================================================================

using Picea.Abies.Conduit.App;

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

    [Test]
    public async Task TabChanged_ToFavorites_UpdatesShowFavorites()
    {
        var model = CreateProfileModel();

        var (newModel, command) = ConduitProgram.Transition(model, new ProfileTabChanged(true));

        var profile = await Assert.That(newModel.Page).IsTypeOf<Page.Profile>();
        await Assert.That(profile!.Data.ShowFavorites).IsTrue();
        await Assert.That(profile.Data.CurrentPage).IsEqualTo(1);
        await Assert.That(command).IsNotEqualTo(Commands.None);
    }

    [Test]
    public async Task TabChanged_ToMyArticles_UpdatesShowFavorites()
    {
        var model = CreateProfileModel();
        var profilePage = (Page.Profile)model.Page;
        var onFavorites = model with
        {
            Page = new Page.Profile(profilePage.Data with { ShowFavorites = true })
        };

        var (newModel, _) = ConduitProgram.Transition(onFavorites, new ProfileTabChanged(false));

        var profile = await Assert.That(newModel.Page).IsTypeOf<Page.Profile>();
        await Assert.That(profile!.Data.ShowFavorites).IsFalse();
    }

    [Test]
    public async Task TabChanged_ResetsCurrentPageTo1()
    {
        var model = CreateProfileModel();
        var profilePage = (Page.Profile)model.Page;
        var onPage3 = model with
        {
            Page = new Page.Profile(profilePage.Data with { CurrentPage = 3 })
        };

        var (newModel, _) = ConduitProgram.Transition(onPage3, new ProfileTabChanged(true));

        var profile = await Assert.That(newModel.Page).IsTypeOf<Page.Profile>();
        await Assert.That(profile!.Data.CurrentPage).IsEqualTo(1);
    }

    [Test]
    public async Task TabChanged_FetchesArticlesCommand()
    {
        var model = CreateProfileModel();

        var (_, command) = ConduitProgram.Transition(model, new ProfileTabChanged(true));

        var batch = await Assert.That(command).IsTypeOf<Command.Batch>();
        await Assert.That(batch!.Commands).Any(c => c is FetchArticles);
    }

    [Test]
    public async Task FollowToggled_UpdatesProfileFollowingState()
    {
        var model = CreateProfileModel(_otherProfile with { Following = false });
        var followedProfile = _otherProfile with { Following = true };

        var (newModel, _) = ConduitProgram.Transition(model, new FollowToggled(followedProfile));

        var profile = await Assert.That(newModel.Page).IsTypeOf<Page.Profile>();
        await Assert.That(profile!.Data.Profile!.Following).IsTrue();
    }

    [Test]
    public async Task FollowToggled_Unfollow_UpdatesProfileFollowingState()
    {
        var model = CreateProfileModel(_otherProfile with { Following = true });
        var unfollowedProfile = _otherProfile with { Following = false };

        var (newModel, _) = ConduitProgram.Transition(model, new FollowToggled(unfollowedProfile));

        var profile = await Assert.That(newModel.Page).IsTypeOf<Page.Profile>();
        await Assert.That(profile!.Data.Profile!.Following).IsFalse();
    }

    [Test]
    public async Task ArticlesLoaded_UpdatesArticlesList()
    {
        var model = CreateProfileModel();
        var articles = new List<ArticlePreviewData>
        {
            new("test-article", "Test", "Desc", ["tag"],
                DateTimeOffset.Parse("2024-01-01"), DateTimeOffset.Parse("2024-01-01"),
                false, 5, new AuthorData("otheruser", "Bio", null, false))
        };

        var (newModel, _) = ConduitProgram.Transition(model, new ArticlesLoaded(articles, 1));

        var profile = await Assert.That(newModel.Page).IsTypeOf<Page.Profile>();
        await Assert.That(profile!.Data.Articles).Count().IsEqualTo(1);
        await Assert.That(profile.Data.ArticlesCount).IsEqualTo(1);
    }

    [Test]
    public async Task ProfileLoaded_UpdatesProfileData()
    {
        var model = CreateProfileModel();
        var loadedProfile = _otherProfile with { Bio = "Updated bio", Following = true };

        var (newModel, _) = ConduitProgram.Transition(model, new ProfileLoaded(loadedProfile));

        var profile = await Assert.That(newModel.Page).IsTypeOf<Page.Profile>();
        await Assert.That(profile!.Data.Profile!.Bio).IsEqualTo("Updated bio");
        await Assert.That(profile.Data.Profile.Following).IsTrue();
    }

    [Test]
    public async Task PageChanged_UpdatesCurrentPage()
    {
        var model = CreateProfileModel();

        var (newModel, command) = ConduitProgram.Transition(model, new PageChanged(2));

        var profile = await Assert.That(newModel.Page).IsTypeOf<Page.Profile>();
        await Assert.That(profile!.Data.CurrentPage).IsEqualTo(2);
        await Assert.That(command).IsNotEqualTo(Commands.None);
    }

    [Test]
    public async Task PageChanged_FetchesArticlesForNewPage()
    {
        var model = CreateProfileModel();

        var (_, command) = ConduitProgram.Transition(model, new PageChanged(3));

        await Assert.That(command).IsTypeOf<FetchArticles>();
    }
}
