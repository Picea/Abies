using Picea;
using Picea.Abies;
using Picea.Abies.Conduit.App;
using Microsoft.Playwright;
using Picea.Abies.Testing;

namespace Picea.Abies.Conduit.Tests;

public sealed class ConduitReplayAndVisualRegressionTests
{
    [Test]
    public async Task Register_WithValidCredentials_NavigatesToHomeWithAuthenticatedNavViaCommandFlow()
    {
        var registerUrl = new Url(["register"], new Dictionary<string, string>(), Option<string>.None);
        using var harness = ConduitIntegrationHarness.Create(initialUrl: registerUrl);

        var session = new Session("token-456", "newuser", "newuser@test.com", "bio", null);
        RegisterUser? issuedRegisterCommand = null;
        PersistSession? issuedPersistCommand = null;
        FetchFeed? issuedFeedCommand = null;
        NavigationCommand.Push? issuedNavigationCommand = null;

        harness.MockCommand<RegisterUser>(cmd =>
        {
            issuedRegisterCommand = cmd;
            return [new UserAuthenticated(session)];
        });
        harness.MockCommand<PersistSession>(cmd =>
        {
            issuedPersistCommand = cmd;
            return [];
        });
        harness.MockCommand<FetchFeed>(cmd =>
        {
            issuedFeedCommand = cmd;
            return [];
        });
        harness.MockCommand<FetchTags>(_ => []);
        harness.MockCommand<NavigationCommand.Push>(cmd =>
        {
            issuedNavigationCommand = cmd;
            return [];
        });

        harness.Dispatch(new RegisterUsernameChanged("newuser"));
        harness.Dispatch(new RegisterEmailChanged("newuser@test.com"));
        harness.Dispatch(new RegisterPasswordChanged("password123"));
        harness.DispatchAndDrain(new RegisterSubmitted());

        var home = await Assert.That(harness.Model.Page).IsTypeOf<Page.Home>();
        await Assert.That(harness.Model.Session).IsEqualTo(session);
        await Assert.That(issuedRegisterCommand).IsNotNull();
        await Assert.That(issuedRegisterCommand!.Username).IsEqualTo("newuser");
        await Assert.That(issuedRegisterCommand.Email).IsEqualTo("newuser@test.com");
        await Assert.That(issuedRegisterCommand.Password).IsEqualTo("password123");
        await Assert.That(issuedPersistCommand).IsNotNull();
        await Assert.That(issuedPersistCommand!.Session).IsEqualTo(session);
        await Assert.That(issuedFeedCommand).IsNotNull();
        await Assert.That(issuedFeedCommand!.Token).IsEqualTo(session.Token);
        await Assert.That(home.Data.ActiveTab).IsEqualTo(FeedTab.Your);
        await Assert.That(issuedNavigationCommand).IsNotNull();
        await Assert.That(issuedNavigationCommand!.Url.Path).IsEmpty();
        await Assert.That(issuedNavigationCommand.Url.Query["feed"]).IsEqualTo("following");
        await Assert.That(harness.RenderNormalizedBodyHtml()).Contains("class=\"home-page\"");
        await Assert.That(harness.RenderNormalizedBodyHtml()).Contains("newuser");
        await Assert.That(harness.RenderNormalizedBodyHtml()).Contains("Settings");
        await Assert.That(harness.RenderNormalizedBodyHtml()).Contains("New Article");
    }

    [Test]
    public async Task Login_WithInvalidCredentials_ShowsErrorsViaCommandFlow()
    {
        var loginUrl = new Url(["login"], new Dictionary<string, string>(), Option<string>.None);
        using var harness = ConduitIntegrationHarness.Create(initialUrl: loginUrl);

        LoginUser? issuedCommand = null;
        harness.MockCommand<LoginUser>(cmd =>
        {
            issuedCommand = cmd;
            return [new ApiError(["email or password is invalid"])];
        });

        harness.Dispatch(new LoginEmailChanged("nonexistent@test.com"));
        harness.Dispatch(new LoginPasswordChanged("wrongpassword"));
        harness.DispatchAndDrain(new LoginSubmitted());

        var login = await Assert.That(harness.Model.Page).IsTypeOf<Page.Login>();
        await Assert.That(issuedCommand).IsNotNull();
        await Assert.That(issuedCommand!.Email).IsEqualTo("nonexistent@test.com");
        await Assert.That(issuedCommand.Password).IsEqualTo("wrongpassword");
        await Assert.That(login.Data.Errors).Contains("email or password is invalid");
        await Assert.That(login.Data.IsSubmitting).IsFalse();
        await Assert.That(harness.RenderNormalizedBodyHtml()).Contains("class=\"error-messages\"");
        await Assert.That(harness.RenderNormalizedBodyHtml()).Contains("email or password is invalid");
    }

    [Test]
    public async Task Login_WithValidCredentials_NavigatesToHomeWithAuthenticatedNavViaCommandFlow()
    {
        var loginUrl = new Url(["login"], new Dictionary<string, string>(), Option<string>.None);
        using var harness = ConduitIntegrationHarness.Create(initialUrl: loginUrl);

        var session = new Session("token-123", "loginuser", "loginuser@test.com", "bio", null);
        LoginUser? issuedLoginCommand = null;
        PersistSession? issuedPersistCommand = null;
        FetchFeed? issuedFeedCommand = null;
        NavigationCommand.Push? issuedNavigationCommand = null;

        harness.MockCommand<LoginUser>(cmd =>
        {
            issuedLoginCommand = cmd;
            return [new UserAuthenticated(session)];
        });
        harness.MockCommand<PersistSession>(cmd =>
        {
            issuedPersistCommand = cmd;
            return [];
        });
        harness.MockCommand<FetchFeed>(cmd =>
        {
            issuedFeedCommand = cmd;
            return [];
        });
        harness.MockCommand<FetchTags>(_ => []);
        harness.MockCommand<NavigationCommand.Push>(cmd =>
        {
            issuedNavigationCommand = cmd;
            return [];
        });

        harness.Dispatch(new LoginEmailChanged("loginuser@test.com"));
        harness.Dispatch(new LoginPasswordChanged("password123"));
        harness.DispatchAndDrain(new LoginSubmitted());

        var home = await Assert.That(harness.Model.Page).IsTypeOf<Page.Home>();
        await Assert.That(harness.Model.Session).IsEqualTo(session);
        await Assert.That(issuedLoginCommand).IsNotNull();
        await Assert.That(issuedLoginCommand!.Email).IsEqualTo("loginuser@test.com");
        await Assert.That(issuedLoginCommand.Password).IsEqualTo("password123");
        await Assert.That(issuedPersistCommand).IsNotNull();
        await Assert.That(issuedPersistCommand!.Session).IsEqualTo(session);
        await Assert.That(issuedFeedCommand).IsNotNull();
        await Assert.That(issuedFeedCommand!.Token).IsEqualTo(session.Token);
        await Assert.That(home.Data.ActiveTab).IsEqualTo(FeedTab.Your);
        await Assert.That(issuedNavigationCommand).IsNotNull();
        await Assert.That(issuedNavigationCommand!.Url.Path).IsEmpty();
        await Assert.That(issuedNavigationCommand.Url.Query["feed"]).IsEqualTo("following");
        await Assert.That(harness.RenderNormalizedBodyHtml()).Contains("class=\"home-page\"");
        await Assert.That(harness.RenderNormalizedBodyHtml()).Contains("loginuser");
        await Assert.That(harness.RenderNormalizedBodyHtml()).Contains("Settings");
        await Assert.That(harness.RenderNormalizedBodyHtml()).Contains("New Article");
    }

    [Test]
    public async Task ReplaySession_LoginFlow_IsDeterministic()
    {
        var loginUrl = new Url(["login"], new Dictionary<string, string>(), Option<string>.None);
        var session = new Session("token-123", "jake", "jake@abies.dev", "bio", null);

        using var recordingHarness = ConduitIntegrationHarness.Create(initialUrl: loginUrl);
        recordingHarness.MockCommand<LoginUser>(_ => [new UserAuthenticated(session)]);
        recordingHarness.MockCommand<PersistSession>(_ => []);
        recordingHarness.MockCommand<FetchFeed>(_ => []);
        recordingHarness.MockCommand<FetchTags>(_ => []);
        recordingHarness.MockCommand<NavigationCommand.Push>(_ => []);

        recordingHarness.Dispatch(new LoginEmailChanged("jake@abies.dev"));
        recordingHarness.Dispatch(new LoginPasswordChanged("super-secret"));
        recordingHarness.DispatchAndDrain(new LoginSubmitted());

        var replayJson = recordingHarness.ExportReplaySessionJson("login-flow");

        using var replayHarness = ConduitIntegrationHarness.Create(initialUrl: loginUrl);
        replayHarness.MockCommand<LoginUser>(_ => [new UserAuthenticated(session)]);
        replayHarness.MockCommand<PersistSession>(_ => []);
        replayHarness.MockCommand<FetchFeed>(_ => []);
        replayHarness.MockCommand<FetchTags>(_ => []);
        replayHarness.MockCommand<NavigationCommand.Push>(_ => []);
        replayHarness.ReplaySessionJson(replayJson);

        await Assert.That(replayHarness.Model).IsEqualTo(recordingHarness.Model);
        await Assert.That(replayHarness.RenderNormalizedBodyHtml())
            .IsEqualTo(recordingHarness.RenderNormalizedBodyHtml());
    }

    [Test]
    public async Task VisualRegression_HomeFeedSingleArticle_MatchesGoldenSnapshot()
    {
        using var harness = ConduitIntegrationHarness.Create();

        var createdAt = new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);
        var updatedAt = new DateTimeOffset(2026, 5, 1, 12, 30, 0, TimeSpan.Zero);

        var article = new ArticlePreviewData(
            Slug: "stable-release-readiness",
            Title: "Stable release readiness",
            Description: "Replay + visual regression harness in place.",
            TagList: ["release", "testing"],
            CreatedAt: createdAt,
            UpdatedAt: updatedAt,
            Favorited: false,
            FavoritesCount: 7,
            Author: new AuthorData("maurice", "Maintainer", null, false));

        harness.Dispatch(new ArticlesLoaded([article], 1));

        var html = harness.RenderNormalizedBodyHtml();
        await VisualSnapshot.AssertMatchesAsync("home-feed-single-article", html);
        await VisualSnapshot.AssertPixelMatchesAsync("home-feed-single-article", harness);
    }

    [Test]
    public async Task VisualRegression_LoginPageWithError_MatchesGoldenSnapshot()
    {
        var loginUrl = new Url(["login"], new Dictionary<string, string>(), Option<string>.None);
        using var harness = ConduitIntegrationHarness.Create(initialUrl: loginUrl);

        harness.Dispatch(new ApiError(["Email and password are required."]));

        var html = harness.RenderNormalizedBodyHtml();
        await VisualSnapshot.AssertMatchesAsync("login-page-with-error", html);
        await VisualSnapshot.AssertPixelMatchesAsync("login-page-with-error", harness);
    }

    [Test]
    public async Task Logout_FromSettings_ClearsSessionAndLoadsAnonymousHomeViaCommandFlow()
    {
        var settingsUrl = new Url(["settings"], new Dictionary<string, string>(), Option<string>.None);
        var session = new Session("token-logout", "logoutuser", "logoutuser@test.com", "bio", null);
        using var harness = ConduitIntegrationHarness.Create(session: session, initialUrl: settingsUrl);

        bool clearCommandIssued = false;
        FetchArticles? issuedFetchArticlesCommand = null;
        FetchTags? issuedFetchTagsCommand = null;

        harness.MockCommand<ClearPersistedSession>(_ =>
        {
            clearCommandIssued = true;
            return [];
        });
        harness.MockCommand<FetchArticles>(cmd =>
        {
            issuedFetchArticlesCommand = cmd;
            return [];
        });
        harness.MockCommand<FetchTags>(cmd =>
        {
            issuedFetchTagsCommand = cmd;
            return [];
        });

        harness.DispatchAndDrain(new Logout());

        var home = await Assert.That(harness.Model.Page).IsTypeOf<Page.Home>();
        await Assert.That(harness.Model.Session).IsNull();
        await Assert.That(clearCommandIssued).IsTrue();
        await Assert.That(issuedFetchArticlesCommand).IsNotNull();
        await Assert.That(issuedFetchArticlesCommand!.Token).IsNull();
        await Assert.That(home.Data.ActiveTab).IsEqualTo(FeedTab.Global);
        await Assert.That(issuedFetchTagsCommand).IsNotNull();
        await Assert.That(harness.RenderNormalizedBodyHtml()).Contains("class=\"home-page\"");
        await Assert.That(harness.RenderNormalizedBodyHtml()).DoesNotContain("Settings");
        await Assert.That(harness.RenderNormalizedBodyHtml()).Contains("Sign in");
        await Assert.That(harness.RenderNormalizedBodyHtml()).Contains("Sign up");
    }

    [Test]
    public async Task AuthenticatedHome_SwitchToYourFeed_EmitsFetchFeedAndPushNavigationViaCommandFlow()
    {
        var session = new Session("token-feed", "feeduser", "feeduser@test.com", "bio", null);
        // Start already authenticated on the home page (Global feed is the default)
        using var harness = ConduitIntegrationHarness.Create(session: session);

        // Initial home load issues FetchArticles + FetchTags; mock them so the harness doesn't throw
        harness.MockCommand<FetchArticles>(_ => []);
        harness.MockCommand<FetchTags>(_ => []);
        // Drain the initial commands from Initialize before setting up feed-tab mocks
        harness.DrainCommands();

        FetchFeed? issuedFetchFeedCommand = null;
        NavigationCommand.Push? issuedNavigationCommand = null;

        harness.MockCommand<FetchFeed>(cmd =>
        {
            issuedFetchFeedCommand = cmd;
            return [];
        });
        harness.MockCommand<NavigationCommand.Push>(cmd =>
        {
            issuedNavigationCommand = cmd;
            return [];
        });

        harness.DispatchAndDrain(new FeedTabChanged(FeedTab.Your, null));

        var home = await Assert.That(harness.Model.Page).IsTypeOf<Page.Home>();
        await Assert.That(home.Data.ActiveTab).IsEqualTo(FeedTab.Your);
        await Assert.That(home.Data.IsLoading).IsTrue();
        await Assert.That(home.Data.Articles).IsEmpty();
        await Assert.That(issuedFetchFeedCommand).IsNotNull();
        await Assert.That(issuedFetchFeedCommand!.Token).IsEqualTo(session.Token);
        await Assert.That(issuedNavigationCommand).IsNotNull();
        await Assert.That(issuedNavigationCommand!.Url.Query["feed"]).IsEqualTo("following");
        await Assert.That(harness.RenderNormalizedBodyHtml()).Contains("class=\"home-page\"");
        await Assert.That(harness.RenderNormalizedBodyHtml()).Contains("feeduser");
    }

    [Test]
    public async Task Settings_WhenLoggedIn_ShowsCurrentUserInfoPrefilled()
    {
        var settingsUrl = new Url(["settings"], new Dictionary<string, string>(), Option<string>.None);
        var session = new Session("token-settings", "settingsuser", "settings@test.com", "my bio", "https://example.com/img.png");
        using var harness = ConduitIntegrationHarness.Create(session: session, initialUrl: settingsUrl);

        var settings = await Assert.That(harness.Model.Page).IsTypeOf<Page.Settings>();
        await Assert.That(settings.Data.Username).IsEqualTo("settingsuser");
        await Assert.That(settings.Data.Email).IsEqualTo("settings@test.com");
        await Assert.That(settings.Data.Bio).IsEqualTo("my bio");
        await Assert.That(settings.Data.Image).IsEqualTo("https://example.com/img.png");

        var html = harness.RenderNormalizedBodyHtml();
        await Assert.That(html).Contains("class=\"settings-page\"");
        await Assert.That(html).Contains("Your Settings");
        await Assert.That(html).Contains("settingsuser");
        await Assert.That(html).Contains("settings@test.com");
    }

    [Test]
    public async Task Settings_UpdateBioAndImage_PersistsSessionAndShowsUpdatedValuesViaCommandFlow()
    {
        var settingsUrl = new Url(["settings"], new Dictionary<string, string>(), Option<string>.None);
        var session = new Session("token-settings-update", "settingsuser", "settings@test.com", "old bio", null);
        using var harness = ConduitIntegrationHarness.Create(session: session, initialUrl: settingsUrl);

        UpdateUser? issuedUpdateUserCommand = null;
        PersistSession? issuedPersistSessionCommand = null;

        var updatedSession = new Session(
            session.Token,
            session.Username,
            session.Email,
            "This is my updated bio from integration tests",
            "https://example.com/new-avatar.png");

        harness.MockCommand<UpdateUser>(cmd =>
        {
            issuedUpdateUserCommand = cmd;
            return [new UserUpdated(updatedSession)];
        });

        harness.MockCommand<PersistSession>(cmd =>
        {
            issuedPersistSessionCommand = cmd;
            return [];
        });

        harness.Dispatch(new SettingsBioChanged("This is my updated bio from integration tests"));
        harness.Dispatch(new SettingsImageChanged("https://example.com/new-avatar.png"));
        harness.DispatchAndDrain(new SettingsSubmitted());

        var settings = await Assert.That(harness.Model.Page).IsTypeOf<Page.Settings>();
        await Assert.That(harness.Model.Session).IsEqualTo(updatedSession);
        await Assert.That(issuedUpdateUserCommand).IsNotNull();
        await Assert.That(issuedUpdateUserCommand!.Token).IsEqualTo(session.Token);
        await Assert.That(issuedUpdateUserCommand.Username).IsEqualTo(session.Username);
        await Assert.That(issuedUpdateUserCommand.Email).IsEqualTo(session.Email);
        await Assert.That(issuedUpdateUserCommand.Bio).IsEqualTo("This is my updated bio from integration tests");
        await Assert.That(issuedUpdateUserCommand.Image).IsEqualTo("https://example.com/new-avatar.png");
        await Assert.That(issuedPersistSessionCommand).IsNotNull();
        await Assert.That(issuedPersistSessionCommand!.Session).IsEqualTo(updatedSession);
        await Assert.That(settings.Data.Bio).IsEqualTo("This is my updated bio from integration tests");
        await Assert.That(settings.Data.Image).IsEqualTo("https://example.com/new-avatar.png");
        await Assert.That(settings.Data.IsSubmitting).IsFalse();

        var html = harness.RenderNormalizedBodyHtml();
        await Assert.That(html).Contains("class=\"settings-page\"");
        await Assert.That(html).Contains("This is my updated bio from integration tests");
        await Assert.That(html).Contains("https://example.com/new-avatar.png");
    }

    [Test]
    public async Task ArticleFavoriteToggle_FavoriteThenUnfavorite_UpdatesArticleStateViaCommandFlow()
    {
        var articleUrl = new Url(["article", "favorite-toggle-article"], new Dictionary<string, string>(), Option<string>.None);
        var session = new Session("token-favorite", "favuser", "favuser@test.com", "bio", null);
        using var harness = ConduitIntegrationHarness.Create(session: session, initialUrl: articleUrl);

        var createdAt = new DateTimeOffset(2026, 5, 2, 10, 0, 0, TimeSpan.Zero);
        var baseArticle = new ArticleData(
            Slug: "favorite-toggle-article",
            Title: "Favorite toggle article",
            Description: "Description",
            Body: "Body",
            TagList: ["favorites"],
            CreatedAt: createdAt,
            UpdatedAt: createdAt,
            Favorited: false,
            FavoritesCount: 3,
            Author: new AuthorData("author-one", "Author bio", null, false));

        harness.MockCommand<FetchArticle>(_ => [new ArticleLoaded(baseArticle)]);
        harness.MockCommand<FetchComments>(_ => [new CommentsLoaded([])]);
        harness.DrainCommands();

        var phase = 1;
        var phase1FavoriteInvocations = 0;
        var phase1UnfavoriteInvocations = 0;
        var phase2FavoriteInvocations = 0;
        var phase2UnfavoriteInvocations = 0;
        FavoriteArticle? issuedFavoriteCommand = null;
        UnfavoriteArticle? issuedUnfavoriteCommand = null;

        harness.MockCommand<FavoriteArticle>(cmd =>
        {
            if (phase == 1)
            {
                phase1FavoriteInvocations++;
            }
            else
            {
                phase2FavoriteInvocations++;
            }

            issuedFavoriteCommand = cmd;
            var favoritedArticle = new ArticlePreviewData(
                Slug: baseArticle.Slug,
                Title: baseArticle.Title,
                Description: baseArticle.Description,
                TagList: baseArticle.TagList,
                CreatedAt: baseArticle.CreatedAt,
                UpdatedAt: baseArticle.UpdatedAt,
                Favorited: true,
                FavoritesCount: 4,
                Author: baseArticle.Author);
            return [new FavoriteToggled(favoritedArticle)];
        });

        harness.DispatchAndDrain(new ToggleFavorite(baseArticle.Slug, false));

        var afterFavorite = await Assert.That(harness.Model.Page).IsTypeOf<Page.Article>();
        await Assert.That(issuedFavoriteCommand).IsNotNull();
        await Assert.That(issuedFavoriteCommand!.Token).IsEqualTo(session.Token);
        await Assert.That(issuedFavoriteCommand.Slug).IsEqualTo(baseArticle.Slug);
        await Assert.That(phase1FavoriteInvocations).IsEqualTo(1);
        await Assert.That(phase1UnfavoriteInvocations).IsEqualTo(0);
        await Assert.That(phase2FavoriteInvocations).IsEqualTo(0);
        await Assert.That(phase2UnfavoriteInvocations).IsEqualTo(0);
        await Assert.That(afterFavorite.Data.Article).IsNotNull();
        await Assert.That(afterFavorite.Data.Article!.Favorited).IsTrue();
        await Assert.That(afterFavorite.Data.Article.FavoritesCount).IsEqualTo(4);
        await Assert.That(harness.RenderNormalizedBodyHtml()).Contains("Unfavorite Article (4)");

        harness.MockCommand<UnfavoriteArticle>(cmd =>
        {
            if (phase == 1)
            {
                phase1UnfavoriteInvocations++;
            }
            else
            {
                phase2UnfavoriteInvocations++;
            }

            issuedUnfavoriteCommand = cmd;
            var unfavoritedArticle = new ArticlePreviewData(
                Slug: baseArticle.Slug,
                Title: baseArticle.Title,
                Description: baseArticle.Description,
                TagList: baseArticle.TagList,
                CreatedAt: baseArticle.CreatedAt,
                UpdatedAt: baseArticle.UpdatedAt,
                Favorited: false,
                FavoritesCount: 3,
                Author: baseArticle.Author);
            return [new FavoriteToggled(unfavoritedArticle)];
        });

        phase = 2;
        issuedUnfavoriteCommand = null;

        harness.DispatchAndDrain(new ToggleFavorite(baseArticle.Slug, true));

        var afterUnfavorite = await Assert.That(harness.Model.Page).IsTypeOf<Page.Article>();
        await Assert.That(issuedUnfavoriteCommand).IsNotNull();
        await Assert.That(issuedUnfavoriteCommand!.Token).IsEqualTo(session.Token);
        await Assert.That(issuedUnfavoriteCommand.Slug).IsEqualTo(baseArticle.Slug);
        await Assert.That(phase2UnfavoriteInvocations).IsEqualTo(1);
        await Assert.That(phase2FavoriteInvocations).IsEqualTo(0);
        await Assert.That(afterUnfavorite.Data.Article).IsNotNull();
        await Assert.That(afterUnfavorite.Data.Article!.Favorited).IsFalse();
        await Assert.That(afterUnfavorite.Data.Article.FavoritesCount).IsEqualTo(3);
        await Assert.That(harness.RenderNormalizedBodyHtml()).Contains("Favorite Article (3)");
    }

    [Test]
    public async Task ArticleAuthorFollowToggle_FollowThenUnfollow_UpdatesAuthorStateViaCommandFlow()
    {
        var articleUrl = new Url(["article", "follow-toggle-article"], new Dictionary<string, string>(), Option<string>.None);
        var session = new Session("token-follow", "followuser", "followuser@test.com", "bio", null);
        using var harness = ConduitIntegrationHarness.Create(session: session, initialUrl: articleUrl);

        var createdAt = new DateTimeOffset(2026, 5, 2, 11, 0, 0, TimeSpan.Zero);
        var baseAuthor = new AuthorData("author-two", "Author bio", null, false);
        var baseArticle = new ArticleData(
            Slug: "follow-toggle-article",
            Title: "Follow toggle article",
            Description: "Description",
            Body: "Body",
            TagList: ["follow"],
            CreatedAt: createdAt,
            UpdatedAt: createdAt,
            Favorited: false,
            FavoritesCount: 1,
            Author: baseAuthor);

        harness.MockCommand<FetchArticle>(_ => [new ArticleLoaded(baseArticle)]);
        harness.MockCommand<FetchComments>(_ => [new CommentsLoaded([])]);
        harness.DrainCommands();

        var phase = 1;
        var phase1FollowInvocations = 0;
        var phase1UnfollowInvocations = 0;
        var phase2FollowInvocations = 0;
        var phase2UnfollowInvocations = 0;
        FollowUser? issuedFollowCommand = null;
        UnfollowUser? issuedUnfollowCommand = null;

        harness.MockCommand<FollowUser>(cmd =>
        {
            if (phase == 1)
            {
                phase1FollowInvocations++;
            }
            else
            {
                phase2FollowInvocations++;
            }

            issuedFollowCommand = cmd;
            return [new FollowToggled(new ProfileData(baseAuthor.Username, baseAuthor.Bio, baseAuthor.Image, true))];
        });

        harness.DispatchAndDrain(new ToggleFollow(baseAuthor.Username, false));

        var afterFollow = await Assert.That(harness.Model.Page).IsTypeOf<Page.Article>();
        await Assert.That(issuedFollowCommand).IsNotNull();
        await Assert.That(issuedFollowCommand!.Token).IsEqualTo(session.Token);
        await Assert.That(issuedFollowCommand.Username).IsEqualTo(baseAuthor.Username);
        await Assert.That(phase1FollowInvocations).IsEqualTo(1);
        await Assert.That(phase1UnfollowInvocations).IsEqualTo(0);
        await Assert.That(phase2FollowInvocations).IsEqualTo(0);
        await Assert.That(phase2UnfollowInvocations).IsEqualTo(0);
        await Assert.That(afterFollow.Data.Article).IsNotNull();
        await Assert.That(afterFollow.Data.Article!.Author.Following).IsTrue();
        await Assert.That(harness.RenderNormalizedBodyHtml()).Contains($"Unfollow {baseAuthor.Username}");

        harness.MockCommand<UnfollowUser>(cmd =>
        {
            if (phase == 1)
            {
                phase1UnfollowInvocations++;
            }
            else
            {
                phase2UnfollowInvocations++;
            }

            issuedUnfollowCommand = cmd;
            return [new FollowToggled(new ProfileData(baseAuthor.Username, baseAuthor.Bio, baseAuthor.Image, false))];
        });

        phase = 2;
        issuedUnfollowCommand = null;

        harness.DispatchAndDrain(new ToggleFollow(baseAuthor.Username, true));

        var afterUnfollow = await Assert.That(harness.Model.Page).IsTypeOf<Page.Article>();
        await Assert.That(issuedUnfollowCommand).IsNotNull();
        await Assert.That(issuedUnfollowCommand!.Token).IsEqualTo(session.Token);
        await Assert.That(issuedUnfollowCommand.Username).IsEqualTo(baseAuthor.Username);
        await Assert.That(phase2UnfollowInvocations).IsEqualTo(1);
        await Assert.That(phase2FollowInvocations).IsEqualTo(0);
        await Assert.That(afterUnfollow.Data.Article).IsNotNull();
        await Assert.That(afterUnfollow.Data.Article!.Author.Following).IsFalse();
        await Assert.That(harness.RenderNormalizedBodyHtml()).Contains($"Follow {baseAuthor.Username}");
    }

    [Test]
    public async Task ArticleCommentSubmitted_AddsCommentAtTopAndClearsInputViaCommandFlow()
    {
        var articleUrl = new Url(["article", "comment-add-article"], new Dictionary<string, string>(), Option<string>.None);
        var session = new Session("token-comment-add", "commenter", "commenter@test.com", "bio", null);
        using var harness = ConduitIntegrationHarness.Create(session: session, initialUrl: articleUrl);

        var createdAt = new DateTimeOffset(2026, 5, 2, 12, 0, 0, TimeSpan.Zero);
        var articleAuthor = new AuthorData("author-three", "Author bio", null, false);
        var article = new ArticleData(
            Slug: "comment-add-article",
            Title: "Comment add article",
            Description: "Description",
            Body: "Body",
            TagList: ["comments"],
            CreatedAt: createdAt,
            UpdatedAt: createdAt,
            Favorited: false,
            FavoritesCount: 0,
            Author: articleAuthor);

        var existingComment = new CommentData(
            Id: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            CreatedAt: createdAt,
            UpdatedAt: createdAt,
            Body: "Existing comment",
            Author: new AuthorData("existing-author", "bio", null, false));

        harness.MockCommand<FetchArticle>(_ => [new ArticleLoaded(article)]);
        harness.MockCommand<FetchComments>(_ => [new CommentsLoaded([existingComment])]);
        harness.DrainCommands();

        AddComment? issuedAddCommentCommand = null;
        var newComment = new CommentData(
            Id: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            CreatedAt: createdAt.AddMinutes(1),
            UpdatedAt: createdAt.AddMinutes(1),
            Body: "Newly added comment",
            Author: new AuthorData(session.Username, session.Bio, session.Image, false));

        harness.MockCommand<AddComment>(cmd =>
        {
            issuedAddCommentCommand = cmd;
            return [new CommentAdded(newComment)];
        });

        harness.Dispatch(new CommentBodyChanged("Newly added comment"));
        harness.DispatchAndDrain(new CommentSubmitted());

        var page = await Assert.That(harness.Model.Page).IsTypeOf<Page.Article>();
        await Assert.That(issuedAddCommentCommand).IsNotNull();
        await Assert.That(issuedAddCommentCommand!.Token).IsEqualTo(session.Token);
        await Assert.That(issuedAddCommentCommand.Slug).IsEqualTo(article.Slug);
        await Assert.That(issuedAddCommentCommand.Body).IsEqualTo("Newly added comment");
        await Assert.That(page.Data.CommentBody).IsEqualTo("");
        await Assert.That(page.Data.Comments.Count).IsEqualTo(2);
        await Assert.That(page.Data.Comments[0].Id).IsEqualTo(newComment.Id);
        await Assert.That(page.Data.Comments[1].Id).IsEqualTo(existingComment.Id);

        var html = harness.RenderNormalizedBodyHtml();
        await Assert.That(html).Contains("Newly added comment");
        await Assert.That(html).Contains("Existing comment");
    }

    [Test]
    public async Task ArticleCommentDelete_RemovesCommentFromListViaCommandFlow()
    {
        var articleUrl = new Url(["article", "comment-delete-article"], new Dictionary<string, string>(), Option<string>.None);
        var session = new Session("token-comment-delete", "comment-owner", "comment-owner@test.com", "bio", null);
        using var harness = ConduitIntegrationHarness.Create(session: session, initialUrl: articleUrl);

        var createdAt = new DateTimeOffset(2026, 5, 2, 13, 0, 0, TimeSpan.Zero);
        var article = new ArticleData(
            Slug: "comment-delete-article",
            Title: "Comment delete article",
            Description: "Description",
            Body: "Body",
            TagList: ["comments"],
            CreatedAt: createdAt,
            UpdatedAt: createdAt,
            Favorited: false,
            FavoritesCount: 0,
            Author: new AuthorData("author-four", "Author bio", null, false));

        var deletableComment = new CommentData(
            Id: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            CreatedAt: createdAt,
            UpdatedAt: createdAt,
            Body: "Comment to delete",
            Author: new AuthorData(session.Username, session.Bio, session.Image, false));
        var remainingComment = new CommentData(
            Id: Guid.Parse("44444444-4444-4444-4444-444444444444"),
            CreatedAt: createdAt.AddMinutes(1),
            UpdatedAt: createdAt.AddMinutes(1),
            Body: "Comment to keep",
            Author: new AuthorData("other-user", "bio", null, false));

        harness.MockCommand<FetchArticle>(_ => [new ArticleLoaded(article)]);
        harness.MockCommand<FetchComments>(_ => [new CommentsLoaded([deletableComment, remainingComment])]);
        harness.DrainCommands();

        DeleteCommentCommand? issuedDeleteCommentCommand = null;
        harness.MockCommand<DeleteCommentCommand>(cmd =>
        {
            issuedDeleteCommentCommand = cmd;
            return [new CommentDeleted(deletableComment.Id)];
        });

        harness.DispatchAndDrain(new DeleteComment(article.Slug, deletableComment.Id));

        var page = await Assert.That(harness.Model.Page).IsTypeOf<Page.Article>();
        await Assert.That(issuedDeleteCommentCommand).IsNotNull();
        await Assert.That(issuedDeleteCommentCommand!.Token).IsEqualTo(session.Token);
        await Assert.That(issuedDeleteCommentCommand.Slug).IsEqualTo(article.Slug);
        await Assert.That(issuedDeleteCommentCommand.CommentId).IsEqualTo(deletableComment.Id);
        await Assert.That(page.Data.Comments.Count).IsEqualTo(1);
        await Assert.That(page.Data.Comments[0].Id).IsEqualTo(remainingComment.Id);

        var html = harness.RenderNormalizedBodyHtml();
        await Assert.That(html).DoesNotContain("Comment to delete");
        await Assert.That(html).Contains("Comment to keep");
    }

    [Test]
    public async Task AnonymousArticleView_ShowsSignInPromptAndHidesCommentForm()
    {
        var articleUrl = new Url(["article", "anonymous-comment-article"], new Dictionary<string, string>(), Option<string>.None);
        using var harness = ConduitIntegrationHarness.Create(initialUrl: articleUrl);

        var createdAt = new DateTimeOffset(2026, 5, 2, 14, 0, 0, TimeSpan.Zero);
        var article = new ArticleData(
            Slug: "anonymous-comment-article",
            Title: "Anonymous comment article",
            Description: "Description",
            Body: "Body",
            TagList: ["comments"],
            CreatedAt: createdAt,
            UpdatedAt: createdAt,
            Favorited: false,
            FavoritesCount: 0,
            Author: new AuthorData("author-five", "Author bio", null, false));

        harness.MockCommand<FetchArticle>(_ => [new ArticleLoaded(article)]);
        harness.MockCommand<FetchComments>(_ => [new CommentsLoaded([])]);
        harness.DrainCommands();

        var page = await Assert.That(harness.Model.Page).IsTypeOf<Page.Article>();
        await Assert.That(page.Data.Article).IsNotNull();

        var html = harness.RenderNormalizedBodyHtml();
        await Assert.That(html).Contains("Sign in");
        await Assert.That(html).Contains("Sign up");
        await Assert.That(html).Contains("href=\"/login\"");
        await Assert.That(html).Contains("href=\"/register\"");
        await Assert.That(html).DoesNotContain("class=\"card comment-form\"");
        await Assert.That(html).DoesNotContain("<textarea");
    }

    [Test]
    public async Task ArticleActions_WhenViewingOthersArticle_ShouldNotShowEditOrDeleteInRenderedHtml()
    {
        var articleUrl = new Url(["article", "others-article-actions"], new Dictionary<string, string>(), Option<string>.None);
        var session = new Session("token-others-actions", "viewer", "viewer@test.com", "bio", null);
        using var harness = ConduitIntegrationHarness.Create(session: session, initialUrl: articleUrl);

        var createdAt = new DateTimeOffset(2026, 5, 3, 9, 0, 0, TimeSpan.Zero);
        var article = new ArticleData(
            Slug: "others-article-actions",
            Title: "Other user's article",
            Description: "Description",
            Body: "Body",
            TagList: ["actions"],
            CreatedAt: createdAt,
            UpdatedAt: createdAt,
            Favorited: false,
            FavoritesCount: 2,
            Author: new AuthorData("another-author", "Author bio", null, false));

        harness.MockCommand<FetchArticle>(_ => [new ArticleLoaded(article)]);
        harness.MockCommand<FetchComments>(_ => [new CommentsLoaded([])]);
        harness.DrainCommands();

        var page = await Assert.That(harness.Model.Page).IsTypeOf<Page.Article>();
        await Assert.That(page.Data.Article).IsNotNull();

        var html = harness.RenderNormalizedBodyHtml();
        await Assert.That(html).DoesNotContain("id=\"article-edit:banner:others-article-actions\"");
        await Assert.That(html).DoesNotContain("id=\"article-edit:body:others-article-actions\"");
        await Assert.That(html).DoesNotContain("id=\"article-delete:banner:others-article-actions\"");
        await Assert.That(html).DoesNotContain("id=\"article-delete:body:others-article-actions\"");
        await Assert.That(html).Contains("id=\"article-follow:banner:others-article-actions\"");
        await Assert.That(html).Contains("id=\"article-follow:body:others-article-actions\"");
        await Assert.That(html).Contains("id=\"article-favorite:banner:others-article-actions\"");
        await Assert.That(html).Contains("id=\"article-favorite:body:others-article-actions\"");
    }

    [Test]
    public async Task ArticleActions_WhenViewingOwnArticle_ShouldShowEditAndDeleteInRenderedHtml()
    {
        var articleUrl = new Url(["article", "own-article-actions"], new Dictionary<string, string>(), Option<string>.None);
        var session = new Session("token-own-actions", "owner", "owner@test.com", "bio", null);
        using var harness = ConduitIntegrationHarness.Create(session: session, initialUrl: articleUrl);

        var createdAt = new DateTimeOffset(2026, 5, 3, 9, 30, 0, TimeSpan.Zero);
        var article = new ArticleData(
            Slug: "own-article-actions",
            Title: "Owner article",
            Description: "Description",
            Body: "Body",
            TagList: ["actions"],
            CreatedAt: createdAt,
            UpdatedAt: createdAt,
            Favorited: false,
            FavoritesCount: 0,
            Author: new AuthorData(session.Username, "Author bio", session.Image, false));

        harness.MockCommand<FetchArticle>(_ => [new ArticleLoaded(article)]);
        harness.MockCommand<FetchComments>(_ => [new CommentsLoaded([])]);
        harness.DrainCommands();

        var page = await Assert.That(harness.Model.Page).IsTypeOf<Page.Article>();
        await Assert.That(page.Data.Article).IsNotNull();

        var html = harness.RenderNormalizedBodyHtml();
        await Assert.That(html).Contains("id=\"article-edit:banner:own-article-actions\"");
        await Assert.That(html).Contains("id=\"article-edit:body:own-article-actions\"");
        await Assert.That(html).Contains("id=\"article-delete:banner:own-article-actions\"");
        await Assert.That(html).Contains("id=\"article-delete:body:own-article-actions\"");
        await Assert.That(html).DoesNotContain("id=\"article-follow:banner:own-article-actions\"");
        await Assert.That(html).DoesNotContain("id=\"article-follow:body:own-article-actions\"");
        await Assert.That(html).DoesNotContain("id=\"article-favorite:banner:own-article-actions\"");
        await Assert.That(html).DoesNotContain("id=\"article-favorite:body:own-article-actions\"");
    }

    [Test]
    public async Task ViewArticle_WithTags_ShouldRenderTagListAndTagPills()
    {
        var articleUrl = new Url(["article", "article-with-tags"], new Dictionary<string, string>(), Option<string>.None);
        using var harness = ConduitIntegrationHarness.Create(initialUrl: articleUrl);

        var createdAt = new DateTimeOffset(2026, 5, 3, 10, 0, 0, TimeSpan.Zero);
        var article = new ArticleData(
            Slug: "article-with-tags",
            Title: "Article with tags",
            Description: "Description",
            Body: "Body",
            TagList: ["testingtag", "harness-tag"],
            CreatedAt: createdAt,
            UpdatedAt: createdAt,
            Favorited: false,
            FavoritesCount: 1,
            Author: new AuthorData("tag-author", "Author bio", null, false));

        harness.MockCommand<FetchArticle>(_ => [new ArticleLoaded(article)]);
        harness.MockCommand<FetchComments>(_ => [new CommentsLoaded([])]);
        harness.DrainCommands();

        var page = await Assert.That(harness.Model.Page).IsTypeOf<Page.Article>();
        await Assert.That(page.Data.Article).IsNotNull();
        await Assert.That(page.Data.Article!.TagList.Count).IsEqualTo(2);

        var html = harness.RenderNormalizedBodyHtml();
        await Assert.That(CountOccurrences(html, "class=\"tag-list\"")).IsGreaterThanOrEqualTo(1);
        await Assert.That(CountOccurrences(html, "class=\"tag-default tag-pill tag-outline\"")).IsEqualTo(2);
        await Assert.That(CountOccurrences(html, ">testingtag<")).IsEqualTo(1);
        await Assert.That(CountOccurrences(html, ">harness-tag<")).IsEqualTo(1);
    }

    [Test]
    public async Task ViewArticle_WithContent_ShouldRenderTitleBodyAndAuthorInArticlePage()
    {
        var articleUrl = new Url(["article", "article-with-content"], new Dictionary<string, string>(), Option<string>.None);
        using var harness = ConduitIntegrationHarness.Create(initialUrl: articleUrl);

        var createdAt = new DateTimeOffset(2026, 5, 3, 10, 15, 0, TimeSpan.Zero);
        var article = new ArticleData(
            Slug: "article-with-content",
            Title: "ViewArticle parity title",
            Description: "ViewArticle parity description",
            Body: "This is deterministic article body content for parity verification.",
            TagList: ["parity", "content"],
            CreatedAt: createdAt,
            UpdatedAt: createdAt,
            Favorited: false,
            FavoritesCount: 1,
            Author: new AuthorData("parity-author", "Author bio", null, false));

        harness.MockCommand<FetchArticle>(_ => [new ArticleLoaded(article)]);
        harness.MockCommand<FetchComments>(_ => [new CommentsLoaded([])]);
        harness.DrainCommands();

        var page = await Assert.That(harness.Model.Page).IsTypeOf<Page.Article>();
        await Assert.That(page.Data.Article).IsNotNull();
        await Assert.That(page.Data.IsLoading).IsFalse();
        await Assert.That(page.Data.Article!.Title).IsEqualTo("ViewArticle parity title");
        await Assert.That(page.Data.Article.Body).IsEqualTo("This is deterministic article body content for parity verification.");
        await Assert.That(page.Data.Article.Author.Username).IsEqualTo("parity-author");

        var html = harness.RenderNormalizedBodyHtml();
        await Assert.That(html).Contains("class=\"article-page\"");
        await Assert.That(html).Contains("ViewArticle parity title");
        await Assert.That(html).Contains("This is deterministic article body content for parity verification.");
        await Assert.That(html).Contains("parity-author");
    }

    [Test]
    public async Task DeleteArticle_AsAuthor_EmitsDeleteCommandAndArticleDeletedTransitionsToHomeViaCommandFlow()
    {
        var slug = "delete-article-command-flow";
        var articleUrl = new Url(["article", slug], new Dictionary<string, string>(), Option<string>.None);
        var session = new Session("token-delete-article", "article-owner", "article-owner@test.com", "Owner bio", null);
        using var harness = ConduitIntegrationHarness.Create(session: session, initialUrl: articleUrl);

        var createdAt = new DateTimeOffset(2026, 5, 3, 10, 45, 0, TimeSpan.Zero);
        var article = new ArticleData(
            Slug: slug,
            Title: "Delete command-flow article",
            Description: "To be deleted",
            Body: "DeleteArticle command-flow parity body",
            TagList: ["delete", "parity"],
            CreatedAt: createdAt,
            UpdatedAt: createdAt,
            Favorited: false,
            FavoritesCount: 0,
            Author: new AuthorData(session.Username, session.Bio, session.Image, false));

        harness.MockCommand<FetchArticle>(_ => [new ArticleLoaded(article)]);
        harness.MockCommand<FetchComments>(_ => [new CommentsLoaded([])]);
        harness.DrainCommands();

        DeleteArticleCommand? issuedDeleteArticleCommand = null;
        FetchArticles? issuedFetchArticlesCommand = null;
        FetchTags? issuedFetchTagsCommand = null;
        NavigationCommand.Push? issuedNavigationCommand = null;
        var deleteArticleInvocations = 0;
        var fetchArticlesInvocations = 0;
        var fetchTagsInvocations = 0;
        var navigationPushInvocations = 0;

        harness.MockCommand<DeleteArticleCommand>(cmd =>
        {
            deleteArticleInvocations++;
            issuedDeleteArticleCommand = cmd;
            return [new ArticleDeleted()];
        });
        harness.MockCommand<FetchArticles>(cmd =>
        {
            fetchArticlesInvocations++;
            issuedFetchArticlesCommand = cmd;
            return [new ArticlesLoaded([], 0)];
        });
        harness.MockCommand<FetchTags>(cmd =>
        {
            fetchTagsInvocations++;
            issuedFetchTagsCommand = cmd;
            return [new TagsLoaded(["tag-a", "tag-b"])];
        });
        harness.MockCommand<NavigationCommand.Push>(cmd =>
        {
            navigationPushInvocations++;
            issuedNavigationCommand = cmd;
            return [];
        });

        harness.DispatchAndDrain(new DeleteArticle(slug));

        var home = await Assert.That(harness.Model.Page).IsTypeOf<Page.Home>();
    await Assert.That(deleteArticleInvocations).IsEqualTo(1);
        await Assert.That(issuedDeleteArticleCommand).IsNotNull();
        await Assert.That(issuedDeleteArticleCommand!.Token).IsEqualTo(session.Token);
        await Assert.That(issuedDeleteArticleCommand.Slug).IsEqualTo(slug);
    await Assert.That(fetchArticlesInvocations).IsEqualTo(1);
        await Assert.That(issuedFetchArticlesCommand).IsNotNull();
        await Assert.That(issuedFetchArticlesCommand!.Token).IsEqualTo(session.Token);
        await Assert.That(issuedFetchArticlesCommand.Offset).IsEqualTo(0);
        await Assert.That(issuedFetchArticlesCommand.Limit).IsEqualTo(Constants.ArticlesPerPage);
        await Assert.That(issuedFetchArticlesCommand.Tag).IsNull();
        await Assert.That(issuedFetchArticlesCommand.Favorited).IsNull();
        await Assert.That(issuedFetchArticlesCommand.Author).IsNull();
    await Assert.That(fetchTagsInvocations).IsEqualTo(1);
        await Assert.That(issuedFetchTagsCommand).IsNotNull();
    await Assert.That(navigationPushInvocations).IsEqualTo(1);
        await Assert.That(issuedNavigationCommand).IsNotNull();
        await Assert.That(issuedNavigationCommand!.Url.Path).IsEmpty();
        await Assert.That(issuedNavigationCommand.Url.Query).IsEmpty();
    await Assert.That(harness.Model.Session).IsEqualTo(session);
        await Assert.That(home.Data.ActiveTab).IsEqualTo(FeedTab.Global);
        await Assert.That(home.Data.IsLoading).IsFalse();
        await Assert.That(home.Data.ArticlesCount).IsEqualTo(0);
        await Assert.That(home.Data.PopularTags.Count).IsEqualTo(2);

        var html = harness.RenderNormalizedBodyHtml();
        await Assert.That(html).Contains("class=\"home-page\"");
        await Assert.That(html).DoesNotContain("Delete command-flow article");
        await Assert.That(html).Contains("Settings");
    }

    [Test]
    public async Task OtherUsersComments_ShouldNotShowDeleteIconInRenderedHtml()
    {
        var articleUrl = new Url(["article", "foreign-comment-article"], new Dictionary<string, string>(), Option<string>.None);
        var session = new Session("token-foreign-comment", "article-owner", "article-owner@test.com", "bio", null);
        using var harness = ConduitIntegrationHarness.Create(session: session, initialUrl: articleUrl);

        var createdAt = new DateTimeOffset(2026, 5, 3, 10, 30, 0, TimeSpan.Zero);
        var article = new ArticleData(
            Slug: "foreign-comment-article",
            Title: "Foreign comment article",
            Description: "Description",
            Body: "Body",
            TagList: ["comments"],
            CreatedAt: createdAt,
            UpdatedAt: createdAt,
            Favorited: false,
            FavoritesCount: 0,
            Author: new AuthorData("article-owner", "Author bio", null, false));

        var foreignComment = new CommentData(
            Id: Guid.Parse("55555555-5555-5555-5555-555555555555"),
            CreatedAt: createdAt,
            UpdatedAt: createdAt,
            Body: "Owned by another user",
            Author: new AuthorData("another-commenter", "bio", null, false));

        harness.MockCommand<FetchArticle>(_ => [new ArticleLoaded(article)]);
        harness.MockCommand<FetchComments>(_ => [new CommentsLoaded([foreignComment])]);
        harness.DrainCommands();

        var page = await Assert.That(harness.Model.Page).IsTypeOf<Page.Article>();
        await Assert.That(page.Data.Comments.Count).IsEqualTo(1);
        await Assert.That(page.Data.Comments[0].Author.Username).IsEqualTo("another-commenter");

        var html = harness.RenderNormalizedBodyHtml();
        await Assert.That(html).Contains("Owned by another user");
        await Assert.That(html).DoesNotContain("class=\"mod-options\"");
        await Assert.That(html).DoesNotContain("onclick=\"DeleteComment");
    }

    [Test]
    public async Task CurrentUsersComment_ShouldShowDeleteAffordanceInRenderedHtml()
    {
        var articleUrl = new Url(["article", "own-comment-delete-affordance"], new Dictionary<string, string>(), Option<string>.None);
        var session = new Session("token-own-comment", "comment-owner", "comment-owner@test.com", "bio", null);
        using var harness = ConduitIntegrationHarness.Create(session: session, initialUrl: articleUrl);

        var createdAt = new DateTimeOffset(2026, 5, 3, 11, 0, 0, TimeSpan.Zero);
        var article = new ArticleData(
            Slug: "own-comment-delete-affordance",
            Title: "Own comment delete affordance",
            Description: "Description",
            Body: "Body",
            TagList: ["comments"],
            CreatedAt: createdAt,
            UpdatedAt: createdAt,
            Favorited: false,
            FavoritesCount: 0,
            Author: new AuthorData("different-article-author", "Author bio", null, false));

        var ownComment = new CommentData(
            Id: Guid.Parse("66666666-6666-6666-6666-666666666666"),
            CreatedAt: createdAt,
            UpdatedAt: createdAt,
            Body: "Owned by current user",
            Author: new AuthorData(session.Username, session.Bio, session.Image, false));

        harness.MockCommand<FetchArticle>(_ => [new ArticleLoaded(article)]);
        harness.MockCommand<FetchComments>(_ => [new CommentsLoaded([ownComment])]);
        harness.DrainCommands();

        var page = await Assert.That(harness.Model.Page).IsTypeOf<Page.Article>();
        await Assert.That(page.Data.Comments.Count).IsEqualTo(1);
        await Assert.That(page.Data.Comments[0].Author.Username).IsEqualTo(session.Username);

        var html = harness.RenderNormalizedBodyHtml();
        await Assert.That(html).Contains("Owned by current user");
        await Assert.That(html).Contains("class=\"mod-options\"");
        await Assert.That(html).Contains("class=\"ion-trash-a\"");
    }

    [Test]
    public async Task MixedOwnershipComments_ShouldRenderDeleteAffordanceOnlyForCurrentUsersComment()
    {
        var articleUrl = new Url(["article", "mixed-ownership-comments"], new Dictionary<string, string>(), Option<string>.None);
        var session = new Session("token-mixed-comment", "comment-owner", "comment-owner@test.com", "bio", null);
        using var harness = ConduitIntegrationHarness.Create(session: session, initialUrl: articleUrl);

        var createdAt = new DateTimeOffset(2026, 5, 3, 11, 30, 0, TimeSpan.Zero);
        var article = new ArticleData(
            Slug: "mixed-ownership-comments",
            Title: "Mixed ownership comments",
            Description: "Description",
            Body: "Body",
            TagList: ["comments"],
            CreatedAt: createdAt,
            UpdatedAt: createdAt,
            Favorited: false,
            FavoritesCount: 0,
            Author: new AuthorData("article-author", "Author bio", null, false));

        var ownComment = new CommentData(
            Id: Guid.Parse("77777777-7777-7777-7777-777777777777"),
            CreatedAt: createdAt,
            UpdatedAt: createdAt,
            Body: "Owned by current user",
            Author: new AuthorData(session.Username, session.Bio, session.Image, false));

        var foreignComment = new CommentData(
            Id: Guid.Parse("88888888-8888-8888-8888-888888888888"),
            CreatedAt: createdAt.AddMinutes(1),
            UpdatedAt: createdAt.AddMinutes(1),
            Body: "Owned by another user",
            Author: new AuthorData("another-commenter", "bio", null, false));

        harness.MockCommand<FetchArticle>(_ => [new ArticleLoaded(article)]);
        harness.MockCommand<FetchComments>(_ => [new CommentsLoaded([ownComment, foreignComment])]);
        harness.DrainCommands();

        var page = await Assert.That(harness.Model.Page).IsTypeOf<Page.Article>();
        await Assert.That(page.Data.Comments.Count).IsEqualTo(2);

        var html = harness.RenderNormalizedBodyHtml();
        await Assert.That(html).Contains("Owned by current user");
        await Assert.That(html).Contains("Owned by another user");
        await Assert.That(CountOccurrences(html, "class=\"mod-options\"")).IsEqualTo(1);
        await Assert.That(CountOccurrences(html, "class=\"ion-trash-a\"")).IsEqualTo(1);
    }

    [Test]
    public async Task OwnProfile_ShowsEditProfileSettingsLink()
    {
        var username = "profile-owner";
        var profileUrl = new Url(["profile", username], new Dictionary<string, string>(), Option<string>.None);
        var session = new Session("token-own-profile", username, "owner@test.com", "Profile owner bio", null);
        using var harness = ConduitIntegrationHarness.Create(session: session, initialUrl: profileUrl);

        var profileData = new ProfileData(username, "Profile owner bio", null, false);
        harness.MockCommand<FetchProfile>(_ => [new ProfileLoaded(profileData)]);
        harness.MockCommand<FetchArticles>(_ => [new ArticlesLoaded([], 0)]);
        harness.DrainCommands();

        var profile = await Assert.That(harness.Model.Page).IsTypeOf<Page.Profile>();
        await Assert.That(profile.Data.Profile).IsNotNull();

        var html = harness.RenderNormalizedBodyHtml();
        await Assert.That(html).Contains("class=\"profile-page\"");
        await Assert.That(html).Contains("class=\"btn btn-sm btn-outline-secondary action-btn\"");
        await Assert.That(html).Contains("href=\"/settings\"");
        await Assert.That(html).DoesNotContain("<button class=\"btn btn-sm btn-outline-secondary action-btn\"");
    }

    [Test]
    public async Task OtherUserProfile_HidesEditSettingsAndShowsFollowAffordance()
    {
        var profileUrl = new Url(["profile", "author-to-follow"], new Dictionary<string, string>(), Option<string>.None);
        var session = new Session("token-viewer-profile", "viewer", "viewer@test.com", "Viewer bio", null);
        using var harness = ConduitIntegrationHarness.Create(session: session, initialUrl: profileUrl);

        var profileData = new ProfileData("author-to-follow", "Author bio", null, false);
        harness.MockCommand<FetchProfile>(_ => [new ProfileLoaded(profileData)]);
        harness.MockCommand<FetchArticles>(_ => [new ArticlesLoaded([], 0)]);
        harness.DrainCommands();

        var profile = await Assert.That(harness.Model.Page).IsTypeOf<Page.Profile>();
        await Assert.That(profile.Data.Profile).IsNotNull();

        var html = harness.RenderNormalizedBodyHtml();
        await Assert.That(html).Contains("class=\"profile-page\"");
        await Assert.That(html).DoesNotContain("<a class=\"btn btn-sm btn-outline-secondary action-btn\" href=\"/settings\"");
        await Assert.That(html).Contains("class=\"btn btn-sm btn-outline-secondary action-btn\" data-event-click=\"handler\"");
        await Assert.That(html).Contains("author-to-follow");
    }

    [Test]
    public async Task EditorAddTag_RendersTagPillsBeforeSubmit()
    {
        var editorUrl = new Url(["editor"], new Dictionary<string, string>(), Option<string>.None);
        var session = new Session("token-editor-tags", "editor-user", "editor-user@test.com", "bio", null);
        using var harness = ConduitIntegrationHarness.Create(session: session, initialUrl: editorUrl);

        var createArticleInvocations = 0;
        harness.MockCommand<CreateArticle>(_ =>
        {
            createArticleInvocations++;
            return [];
        });

        harness.Dispatch(new EditorTagInputChanged("mvu"));
        harness.Dispatch(new EditorAddTag());
        harness.Dispatch(new EditorTagInputChanged("wasm"));
        harness.Dispatch(new EditorAddTag());

        var editor = await Assert.That(harness.Model.Page).IsTypeOf<Page.Editor>();
        await Assert.That(editor.Data.TagInput).IsEqualTo("");
        await Assert.That(editor.Data.TagList.Count).IsEqualTo(2);
        await Assert.That(editor.Data.TagList[0]).IsEqualTo("mvu");
        await Assert.That(editor.Data.TagList[1]).IsEqualTo("wasm");
        await Assert.That(createArticleInvocations).IsEqualTo(0);

        var html = harness.RenderNormalizedBodyHtml();
        await Assert.That(html).Contains("class=\"editor-page\"");
        await Assert.That(html).Contains("class=\"tag-list\"");
        await Assert.That(CountOccurrences(html, "class=\"tag-default tag-pill\"")).IsEqualTo(2);
        await Assert.That(html).Contains("mvu");
        await Assert.That(html).Contains("wasm");
    }

    [Test]
    public async Task EditorSubmit_CreatePath_EmitsCreateArticleAndHandlesArticleSavedNavigationFlow()
    {
        var editorUrl = new Url(["editor"], new Dictionary<string, string>(), Option<string>.None);
        var session = new Session("token-editor-submit", "editor-user", "editor-user@test.com", "bio", null);
        using var harness = ConduitIntegrationHarness.Create(session: session, initialUrl: editorUrl);

        const string createdSlug = "new-article-from-editor-flow";
        CreateArticle? issuedCreateArticleCommand = null;
        FetchArticle? issuedFetchArticleCommand = null;
        FetchComments? issuedFetchCommentsCommand = null;
        NavigationCommand.Push? issuedNavigationCommand = null;
        var createArticleInvocations = 0;
        var fetchArticleInvocations = 0;
        var fetchCommentsInvocations = 0;
        var navigationPushInvocations = 0;

        var createdAt = new DateTimeOffset(2026, 5, 5, 10, 0, 0, TimeSpan.Zero);
        var createdArticle = new ArticleData(
            Slug: createdSlug,
            Title: "Replay-safe editor submit",
            Description: "Deterministic command flow",
            Body: "Created from Conduit replay harness",
            TagList: ["mvu", "replay"],
            CreatedAt: createdAt,
            UpdatedAt: createdAt,
            Favorited: false,
            FavoritesCount: 0,
            Author: new AuthorData(session.Username, session.Bio, session.Image, false));

        harness.MockCommand<CreateArticle>(cmd =>
        {
            createArticleInvocations++;
            issuedCreateArticleCommand = cmd;
            return [new ArticleSaved(createdSlug)];
        });
        harness.MockCommand<FetchArticle>(cmd =>
        {
            fetchArticleInvocations++;
            issuedFetchArticleCommand = cmd;
            return [new ArticleLoaded(createdArticle)];
        });
        harness.MockCommand<FetchComments>(cmd =>
        {
            fetchCommentsInvocations++;
            issuedFetchCommentsCommand = cmd;
            return [new CommentsLoaded([])];
        });
        harness.MockCommand<NavigationCommand.Push>(cmd =>
        {
            navigationPushInvocations++;
            issuedNavigationCommand = cmd;
            return [];
        });

        harness.Dispatch(new EditorTitleChanged("Replay-safe editor submit"));
        harness.Dispatch(new EditorDescriptionChanged("Deterministic command flow"));
        harness.Dispatch(new EditorBodyChanged("Created from Conduit replay harness"));
        harness.Dispatch(new EditorTagInputChanged("mvu"));
        harness.Dispatch(new EditorAddTag());
        harness.Dispatch(new EditorTagInputChanged("replay"));
        harness.Dispatch(new EditorAddTag());
        harness.DispatchAndDrain(new EditorSubmitted());

        var articlePage = await Assert.That(harness.Model.Page).IsTypeOf<Page.Article>();
        await Assert.That(createArticleInvocations).IsEqualTo(1);
        await Assert.That(issuedCreateArticleCommand).IsNotNull();
        await Assert.That(issuedCreateArticleCommand!.Token).IsEqualTo(session.Token);
        await Assert.That(issuedCreateArticleCommand.Title).IsEqualTo("Replay-safe editor submit");
        await Assert.That(issuedCreateArticleCommand.Description).IsEqualTo("Deterministic command flow");
        await Assert.That(issuedCreateArticleCommand.Body).IsEqualTo("Created from Conduit replay harness");
        await Assert.That(issuedCreateArticleCommand.TagList.Count).IsEqualTo(2);
        await Assert.That(issuedCreateArticleCommand.TagList[0]).IsEqualTo("mvu");
        await Assert.That(issuedCreateArticleCommand.TagList[1]).IsEqualTo("replay");
        await Assert.That(fetchArticleInvocations).IsEqualTo(1);
        await Assert.That(issuedFetchArticleCommand).IsNotNull();
        await Assert.That(issuedFetchArticleCommand!.Slug).IsEqualTo(createdSlug);
        await Assert.That(fetchCommentsInvocations).IsEqualTo(1);
        await Assert.That(issuedFetchCommentsCommand).IsNotNull();
        await Assert.That(issuedFetchCommentsCommand!.Slug).IsEqualTo(createdSlug);
        await Assert.That(navigationPushInvocations).IsEqualTo(1);
        await Assert.That(issuedNavigationCommand).IsNotNull();
        await Assert.That(issuedNavigationCommand!.Url.Path.SequenceEqual(["article", createdSlug])).IsTrue();
        await Assert.That(articlePage.Data.Slug).IsEqualTo(createdSlug);
        await Assert.That(articlePage.Data.Article).IsNotNull();
        await Assert.That(articlePage.Data.Article!.Title).IsEqualTo("Replay-safe editor submit");

        var html = harness.RenderNormalizedBodyHtml();
        await Assert.That(html).Contains("class=\"article-page\"");
        await Assert.That(html).Contains("Replay-safe editor submit");
    }

    [Test]
    public async Task EditorTagKeyDownEnter_AddsTagAndPreventsDuplicatePills()
    {
        var editorUrl = new Url(["editor"], new Dictionary<string, string>(), Option<string>.None);
        var session = new Session("token-editor-enter", "editor-user", "editor-user@test.com", "bio", null);
        using var harness = ConduitIntegrationHarness.Create(session: session, initialUrl: editorUrl);

        harness.Dispatch(new EditorTagInputChanged("dotnet"));
        harness.Dispatch(new EditorTagKeyDown("Enter"));
        harness.Dispatch(new EditorTagInputChanged("dotnet"));
        harness.Dispatch(new EditorTagKeyDown("Enter"));

        var editor = await Assert.That(harness.Model.Page).IsTypeOf<Page.Editor>();
        await Assert.That(editor.Data.TagList.Count).IsEqualTo(1);
        await Assert.That(editor.Data.TagList[0]).IsEqualTo("dotnet");

        var html = harness.RenderNormalizedBodyHtml();
        await Assert.That(CountOccurrences(html, "class=\"tag-default tag-pill\"")).IsEqualTo(1);
        await Assert.That(CountOccurrences(html, "dotnet")).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task ProfileTabChanged_ToFavoritedArticles_EmitsFetchWithFavoritedAndPushesFavoritesUrl()
    {
        var profileUrl = new Url(["profile", "profileuser"], new Dictionary<string, string>(), Option<string>.None);
        var session = new Session("token-profile-tab", "viewer", "viewer@test.com", "bio", null);
        using var harness = ConduitIntegrationHarness.Create(session: session, initialUrl: profileUrl);

        var createdAt = new DateTimeOffset(2026, 5, 2, 15, 0, 0, TimeSpan.Zero);
        var profileData = new ProfileData("profileuser", "Profile bio", null, false);
        var authoredArticle = new ArticlePreviewData(
            Slug: "profile-article-1",
            Title: "Profile article",
            Description: "Description",
            TagList: ["profile"],
            CreatedAt: createdAt,
            UpdatedAt: createdAt,
            Favorited: false,
            FavoritesCount: 2,
            Author: new AuthorData(profileData.Username, profileData.Bio, profileData.Image, profileData.Following));

        harness.MockCommand<FetchProfile>(_ => [new ProfileLoaded(profileData)]);
        harness.MockCommand<FetchArticles>(_ => [new ArticlesLoaded([authoredArticle], 1)]);
        harness.DrainCommands();

        FetchArticles? issuedFetchArticlesCommand = null;
        NavigationCommand.Push? issuedNavigationCommand = null;

        harness.MockCommand<FetchArticles>(cmd =>
        {
            issuedFetchArticlesCommand = cmd;
            return [];
        });
        harness.MockCommand<NavigationCommand.Push>(cmd =>
        {
            issuedNavigationCommand = cmd;
            return [];
        });

        harness.DispatchAndDrain(new ProfileTabChanged(true));

        var profile = await Assert.That(harness.Model.Page).IsTypeOf<Page.Profile>();
        await Assert.That(profile.Data.ShowFavorites).IsTrue();
        await Assert.That(profile.Data.CurrentPage).IsEqualTo(1);
        await Assert.That(profile.Data.IsLoading).IsTrue();
        await Assert.That(profile.Data.Articles).IsEmpty();
        await Assert.That(issuedFetchArticlesCommand).IsNotNull();
        await Assert.That(issuedFetchArticlesCommand!.Token).IsEqualTo(session.Token);
        await Assert.That(issuedFetchArticlesCommand!.Favorited).IsEqualTo("profileuser");
        await Assert.That(issuedFetchArticlesCommand.Author).IsNull();
        await Assert.That(issuedNavigationCommand).IsNotNull();
        await Assert.That(issuedNavigationCommand!.Url.Path.SequenceEqual(["profile", "profileuser", "favorites"]))
            .IsTrue();
        await Assert.That(harness.RenderNormalizedBodyHtml()).Contains("class=\"profile-page\"");
    }

    [Test]
    public async Task ProtectedRoutes_WhenAnonymous_DoNotRenderProtectedFormsButNavbarIsVisible()
    {
        var settingsUrl = new Url(["settings"], new Dictionary<string, string>(), Option<string>.None);
        using var settingsHarness = ConduitIntegrationHarness.Create(initialUrl: settingsUrl);

        await Assert.That(settingsHarness.Model.Page).IsTypeOf<Page.Login>();
        var settingsHtml = settingsHarness.RenderNormalizedBodyHtml();
        await Assert.That(settingsHtml).DoesNotContain("class=\"settings-page\"");
        await Assert.That(settingsHtml).Contains("class=\"navbar");

        var editorUrl = new Url(["editor"], new Dictionary<string, string>(), Option<string>.None);
        using var editorHarness = ConduitIntegrationHarness.Create(initialUrl: editorUrl);

        await Assert.That(editorHarness.Model.Page).IsTypeOf<Page.Login>();
        var editorHtml = editorHarness.RenderNormalizedBodyHtml();
        await Assert.That(editorHtml).DoesNotContain("class=\"editor-page\"");
        await Assert.That(editorHtml).Contains("class=\"navbar");
    }

    [Test]
    public async Task LoginRoute_RendersAuthPageWithSignInFormAndAnonymousNavbar()
    {
        var loginUrl = new Url(["login"], new Dictionary<string, string>(), Option<string>.None);
        using var harness = ConduitIntegrationHarness.Create(initialUrl: loginUrl);

        var html = harness.RenderNormalizedBodyHtml();

        await Assert.That(harness.Model.Page).IsTypeOf<Page.Login>();
        await Assert.That(html).Contains("class=\"auth-page\"");
        await Assert.That(html).Contains("Sign in");
        await Assert.That(html).Contains("type=\"email\"");
        await Assert.That(html).Contains("type=\"password\"");
        await Assert.That(html).Contains("href=\"/register\"");
        await Assert.That(html).Contains("class=\"navbar");
        await Assert.That(html).DoesNotContain("Settings");
        await Assert.That(html).DoesNotContain("New Article");
    }

    [Test]
    public async Task RegisterRoute_RendersAuthPageWithSignUpFormAndAnonymousNavbar()
    {
        var registerUrl = new Url(["register"], new Dictionary<string, string>(), Option<string>.None);
        using var harness = ConduitIntegrationHarness.Create(initialUrl: registerUrl);

        var html = harness.RenderNormalizedBodyHtml();

        await Assert.That(harness.Model.Page).IsTypeOf<Page.Register>();
        await Assert.That(html).Contains("class=\"auth-page\"");
        await Assert.That(html).Contains("Sign up");
        await Assert.That(html).Contains("type=\"text\"");
        await Assert.That(html).Contains("type=\"email\"");
        await Assert.That(html).Contains("type=\"password\"");
        await Assert.That(html).Contains("href=\"/login\"");
        await Assert.That(html).Contains("class=\"navbar");
        await Assert.That(html).DoesNotContain("Settings");
        await Assert.That(html).DoesNotContain("New Article");
    }

    private static int CountOccurrences(string text, string needle)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(needle))
            return 0;

        var count = 0;
        var start = 0;

        while (true)
        {
            var index = text.IndexOf(needle, start, StringComparison.Ordinal);
            if (index < 0)
                return count;

            count++;
            start = index + needle.Length;
        }
    }
}

internal static class VisualSnapshot
{
    private static readonly bool _updateHtmlSnapshots =
        string.Equals(Environment.GetEnvironmentVariable("ABIES_UPDATE_SNAPSHOTS"), "1", StringComparison.Ordinal);

    private static readonly bool _enablePixelSnapshots =
        string.Equals(Environment.GetEnvironmentVariable("ABIES_ENABLE_PIXEL_SNAPSHOTS"), "1", StringComparison.Ordinal);

    private static readonly bool _updatePixelSnapshots =
        string.Equals(Environment.GetEnvironmentVariable("ABIES_UPDATE_PIXEL_SNAPSHOTS"), "1", StringComparison.Ordinal);

    public static async Task AssertMatchesAsync(string snapshotName, string actual)
    {
        var snapshotPath = Path.Combine(GetProjectDirectory(), "Snapshots", $"{snapshotName}.html");
        Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);

        if (_updateHtmlSnapshots)
        {
            await File.WriteAllTextAsync(snapshotPath, actual);
            return;
        }

        if (!File.Exists(snapshotPath))
            throw new InvalidOperationException(
                $"Snapshot '{snapshotPath}' does not exist. Run tests with ABIES_UPDATE_SNAPSHOTS=1 once to generate it.");

        var expected = await File.ReadAllTextAsync(snapshotPath);
        await Assert.That(actual).IsEqualTo(expected);
    }

    public static async Task AssertPixelMatchesAsync(string snapshotName, ConduitIntegrationHarness harness)
    {
        if (!_enablePixelSnapshots)
            return;

        var snapshotPath = Path.Combine(GetProjectDirectory(), "Snapshots", $"{snapshotName}.png");
        var artifactDirectory = Path.Combine(GetProjectDirectory(), "Snapshots", "artifacts");
        Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);

        if (!File.Exists(snapshotPath) && !_updatePixelSnapshots)
        {
            throw new InvalidOperationException(
                $"Pixel snapshot '{snapshotPath}' does not exist. Run with ABIES_ENABLE_PIXEL_SNAPSHOTS=1 ABIES_UPDATE_PIXEL_SNAPSHOTS=1 once to generate it.");
        }

        await using var playwrightResources = await PlaywrightResources.CreateAsync();

        var result = await harness.CompareVisualAsync(
            playwrightResources.Page,
            snapshotPath,
            new VisualComparisonOptions(
                ViewportWidth: 1440,
                ViewportHeight: 900,
                FullPage: true,
                ArtifactDirectory: artifactDirectory,
                Tolerance: VisualComparisonTolerance.Strict));

        if (_updatePixelSnapshots)
        {
            await File.WriteAllBytesAsync(snapshotPath, result.ActualBytes);
            return;
        }

        if (!result.IsMatch)
        {
            throw new InvalidOperationException(
                $"Pixel snapshot mismatch for '{snapshotName}'. Baseline: {result.BaselinePath}; Actual artifact: {result.ActualPath}; Diff artifact: {result.DiffPath}; PixelErrorCount: {result.PixelErrorCount}; MeanError: {result.MeanError:F6}; AbsoluteError: {result.AbsoluteError:F6}.");
        }
    }

    private static string GetProjectDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var projectFile = Path.Combine(directory.FullName, "Picea.Abies.Conduit.Tests.csproj");
            if (File.Exists(projectFile))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate Picea.Abies.Conduit.Tests project directory.");
    }

    private sealed class PlaywrightResources : IAsyncDisposable
    {
        private readonly IPlaywright _playwright;
        private readonly IBrowser _browser;
        private readonly IBrowserContext _context;

        public IPage Page { get; }

        private PlaywrightResources(IPlaywright playwright, IBrowser browser, IBrowserContext context, IPage page)
        {
            _playwright = playwright;
            _browser = browser;
            _context = context;
            Page = page;
        }

        public static async Task<PlaywrightResources> CreateAsync()
        {
            var playwright = await Playwright.CreateAsync();
            var browser = await LaunchChromiumWithInstallFallback(playwright);
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            return new PlaywrightResources(playwright, browser, context, page);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
            await _browser.DisposeAsync();
            _playwright.Dispose();
        }

        private static async Task<IBrowser> LaunchChromiumWithInstallFallback(IPlaywright playwright)
        {
            try
            {
                return await LaunchChromium(playwright);
            }
            catch (PlaywrightException ex) when (ex.Message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Playwright Chromium is not installed for this test environment. Install Playwright browsers before running pixel snapshot tests.",
                    ex);
            }
        }

        private static Task<IBrowser> LaunchChromium(IPlaywright playwright) =>
            playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
    }
}
