using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Abies.Conduit.IntegrationTests.Testing;
using Abies.Conduit.Main;
using Abies.Conduit.Services;
using Abies.DOM;
using Xunit;

namespace Abies.Conduit.IntegrationTests;

/// <summary>
/// Combined journey tests that exercise the full MVU loop:
/// UI interaction → Update → Command → HandleCommand → Message → Update → ... → Quiescence
/// 
/// These tests use the real command handler with fake HTTP, providing end-to-end-like coverage
/// without browser/process overhead.
/// </summary>
public class CombinedJourneyTests
{
    private static void ConfigureFakeApi(FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new System.Uri("http://fake") };
        ApiClient.ConfigureHttpClient(httpClient);
        ApiClient.ConfigureBaseUrl("http://fake/api");
        // Configure in-memory storage to avoid browser interop calls
        Storage.Configure(new InMemoryStorageProvider());
    }

    #region Article Page - Favorite Toggle

    [Fact]
    public async Task Article_ToggleFavorite_AddsToFavorites()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler { StrictMode = true };

        // Favorite endpoint returns updated article
        handler.When(
            HttpMethod.Post,
            "/api/articles/test-slug/favorite",
            HttpStatusCode.OK,
            new
            {
                article = new
                {
                    slug = "test-slug",
                    title = "Test",
                    description = "Desc",
                    body = "Body",
                    tagList = (string[])["tag1"],
                    createdAt = "2020-01-01T00:00:00.000Z",
                    updatedAt = "2020-01-01T00:00:00.000Z",
                    favorited = true,
                    favoritesCount = 1,
                    author = new { username = "bob", bio = "", image = "", following = false }
                }
            });

        ConfigureFakeApi(handler);

        var article = new Abies.Conduit.Page.Home.Article(
            Slug: "test-slug",
            Title: "Test",
            Description: "Desc",
            Body: "Body",
            TagList: ["tag1"],
            CreatedAt: "2020-01-01",
            UpdatedAt: "2020-01-01",
            Favorited: false,
            FavoritesCount: 0,
            Author: new Abies.Conduit.Page.Home.Profile("bob", "", "", Following: false));

        var model = new Abies.Conduit.Page.Article.Model(
            Slug: new Slug("test-slug"),
            IsLoading: false,
            Article: article,
            Comments: [],
            CommentInput: "",
            SubmittingComment: false,
            CurrentUser: new User(new UserName("alice"), new Email("a@x"), new Token("t"), "", ""));

        // Find the favorite button
        var dom = Abies.Conduit.Page.Article.Page.View(model);
        var (_, favHandler) = MvuDomTestHarness.FindFirstHandler(
            dom,
            "click",
            el => el.Tag == "button" && 
                  el.Children.OfType<Text>().Any(t => t.Value.Contains("Favorite Article")));

        Assert.NotNull(favHandler.Command);
        var toggleMsg = (Abies.Message)favHandler.Command!;

        // Act
        var result = await MvuLoopRuntime.RunUntilQuiescentAsync(
            initialModel: model,
            update: Abies.Conduit.Page.Article.Page.Update,
            initialMessage: toggleMsg,
            options: new MvuLoopRuntime.Options(MaxIterations: 10));

        // Assert
        Assert.Contains(handler.Requests, r =>
            r.Method == HttpMethod.Post && r.Uri.PathAndQuery == "/api/articles/test-slug/favorite");

        Assert.NotNull(result.Model.Article);
        Assert.True(result.Model.Article!.Favorited);
        Assert.Equal(1, result.Model.Article.FavoritesCount);

        Assert.True(result.TotalCommandsExecuted > 0);
    }

    [Fact]
    public async Task Article_ToggleFavorite_RemovesFromFavorites()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler { StrictMode = true };

        // Unfavorite endpoint returns updated article
        handler.When(
            HttpMethod.Delete,
            "/api/articles/test-slug/favorite",
            HttpStatusCode.OK,
            new
            {
                article = new
                {
                    slug = "test-slug",
                    title = "Test",
                    description = "Desc",
                    body = "Body",
                    tagList = (string[])["tag1"],
                    createdAt = "2020-01-01T00:00:00.000Z",
                    updatedAt = "2020-01-01T00:00:00.000Z",
                    favorited = false,
                    favoritesCount = 0,
                    author = new { username = "bob", bio = "", image = "", following = false }
                }
            });

        ConfigureFakeApi(handler);

        var article = new Abies.Conduit.Page.Home.Article(
            Slug: "test-slug",
            Title: "Test",
            Description: "Desc",
            Body: "Body",
            TagList: ["tag1"],
            CreatedAt: "2020-01-01",
            UpdatedAt: "2020-01-01",
            Favorited: true,  // Already favorited
            FavoritesCount: 1,
            Author: new Abies.Conduit.Page.Home.Profile("bob", "", "", Following: false));

        var model = new Abies.Conduit.Page.Article.Model(
            Slug: new Slug("test-slug"),
            IsLoading: false,
            Article: article,
            Comments: [],
            CommentInput: "",
            SubmittingComment: false,
            CurrentUser: new User(new UserName("alice"), new Email("a@x"), new Token("t"), "", ""));

        // Find the unfavorite button
        var dom = Abies.Conduit.Page.Article.Page.View(model);
        var (_, favHandler) = MvuDomTestHarness.FindFirstHandler(
            dom,
            "click",
            el => el.Tag == "button" && 
                  el.Children.OfType<Text>().Any(t => t.Value.Contains("Unfavorite Article")));

        Assert.NotNull(favHandler.Command);
        var toggleMsg = (Abies.Message)favHandler.Command!;

        // Act
        var result = await MvuLoopRuntime.RunUntilQuiescentAsync(
            initialModel: model,
            update: Abies.Conduit.Page.Article.Page.Update,
            initialMessage: toggleMsg,
            options: new MvuLoopRuntime.Options(MaxIterations: 10));

        // Assert
        Assert.Contains(handler.Requests, r =>
            r.Method == HttpMethod.Delete && r.Uri.PathAndQuery == "/api/articles/test-slug/favorite");

        Assert.NotNull(result.Model.Article);
        Assert.False(result.Model.Article!.Favorited);
        Assert.Equal(0, result.Model.Article.FavoritesCount);
    }

    #endregion

    #region Article Page - Delete Comment

    [Fact]
    public async Task Article_DeleteComment_RemovesFromList()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler { StrictMode = true };

        handler.When(
            HttpMethod.Delete,
            "/api/articles/test-slug/comments/42",
            HttpStatusCode.OK,
            null);

        ConfigureFakeApi(handler);

        List<Abies.Conduit.Page.Article.Comment> existingComments =
        [
            new("42", "2020-01-01", "2020-01-01", "First comment", 
                new Abies.Conduit.Page.Home.Profile("alice", "", "", false)),
            new("99", "2020-01-02", "2020-01-02", "Second comment", 
                new Abies.Conduit.Page.Home.Profile("bob", "", "", false))
        ];

        var article = new Abies.Conduit.Page.Home.Article(
            Slug: "test-slug",
            Title: "Test",
            Description: "Desc",
            Body: "Body",
            TagList: [],
            CreatedAt: "2020-01-01",
            UpdatedAt: "2020-01-01",
            Favorited: false,
            FavoritesCount: 0,
            Author: new Abies.Conduit.Page.Home.Profile("bob", "", "", false));

        var model = new Abies.Conduit.Page.Article.Model(
            Slug: new Slug("test-slug"),
            IsLoading: false,
            Article: article,
            Comments: existingComments,
            CommentInput: "",
            SubmittingComment: false,
            CurrentUser: new User(new UserName("alice"), new Email("a@x"), new Token("t"), "", ""));

        // Create delete message directly (UI has trash icon per comment)
        var deleteMsg = new Abies.Conduit.Page.Article.Message.DeleteComment("42");

        // Act
        var result = await MvuLoopRuntime.RunUntilQuiescentAsync(
            initialModel: model,
            update: Abies.Conduit.Page.Article.Page.Update,
            initialMessage: deleteMsg,
            options: new MvuLoopRuntime.Options(MaxIterations: 10));

        // Assert
        Assert.Contains(handler.Requests, r =>
            r.Method == HttpMethod.Delete && r.Uri.PathAndQuery == "/api/articles/test-slug/comments/42");

        Assert.NotNull(result.Model.Comments);
        Assert.Single(result.Model.Comments);
        Assert.Equal("99", result.Model.Comments[0].Id);
        Assert.DoesNotContain(result.Model.Comments, c => c.Id == "42");
    }

    #endregion

    #region Article Page - Follow/Unfollow Author

    [Fact]
    public async Task Article_ToggleFollow_FollowsAuthor()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler { StrictMode = true };

        handler.When(
            HttpMethod.Post,
            "/api/profiles/bob/follow",
            HttpStatusCode.OK,
            new
            {
                profile = new { username = "bob", bio = "Bio", image = "", following = true }
            });

        ConfigureFakeApi(handler);

        var article = new Abies.Conduit.Page.Home.Article(
            Slug: "test-slug",
            Title: "Test",
            Description: "Desc",
            Body: "Body",
            TagList: [],
            CreatedAt: "2020-01-01",
            UpdatedAt: "2020-01-01",
            Favorited: false,
            FavoritesCount: 0,
            Author: new Abies.Conduit.Page.Home.Profile("bob", "Bio", "", Following: false));

        var model = new Abies.Conduit.Page.Article.Model(
            Slug: new Slug("test-slug"),
            IsLoading: false,
            Article: article,
            Comments: [],
            CommentInput: "",
            SubmittingComment: false,
            CurrentUser: new User(new UserName("alice"), new Email("a@x"), new Token("t"), "", ""));

        // Find the follow button
        var dom = Abies.Conduit.Page.Article.Page.View(model);
        var (_, followHandler) = MvuDomTestHarness.FindFirstHandler(
            dom,
            "click",
            el => el.Tag == "button" && 
                  el.Children.OfType<Text>().Any(t => t.Value.Contains("Follow bob")));

        Assert.NotNull(followHandler.Command);
        var toggleMsg = (Abies.Message)followHandler.Command!;

        // Act - Note: ToggleFollow dispatches ProfileLoaded which Article.Update doesn't handle
        // So we run with StrictUnhandledMessages=false for this case
        var result = await MvuLoopRuntime.RunUntilQuiescentAsync(
            initialModel: model,
            update: Abies.Conduit.Page.Article.Page.Update,
            initialMessage: toggleMsg,
            options: new MvuLoopRuntime.Options(MaxIterations: 10, StrictUnhandledMessages: false));

        // Assert: the follow request was made
        Assert.Contains(handler.Requests, r =>
            r.Method == HttpMethod.Post && r.Uri.PathAndQuery == "/api/profiles/bob/follow");

        // Model was optimistically updated
        Assert.True(result.Model.Article?.Author.Following);
    }

    #endregion

    #region Home Page - Load Articles with Tag Filter

    [Fact]
    public async Task Home_SelectTag_LoadsFilteredArticles()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler { StrictMode = true };

        handler.When(
            HttpMethod.Get,
            "/api/articles?tag=csharp&limit=10&offset=0",
            HttpStatusCode.OK,
            new
            {
                articles = (object[])
                [
                    new
                    {
                        slug = "csharp-article",
                        title = "C# Tips",
                        description = "Learn C#",
                        body = "...",
                        tagList = (string[])["csharp"],
                        createdAt = "2020-01-01T00:00:00.000Z",
                        updatedAt = "2020-01-01T00:00:00.000Z",
                        favorited = false,
                        favoritesCount = 5,
                        author = new { username = "dev", bio = "", image = "", following = false }
                    }
                ],
                articlesCount = 1
            });

        ConfigureFakeApi(handler);

        var model = new Abies.Conduit.Page.Home.Model(
            Articles: [],
            ArticlesCount: 0,
            Tags: ["csharp", "fsharp", "dotnet"],
            ActiveTab: Abies.Conduit.Page.Home.FeedTab.Global,
            ActiveTag: "",
            IsLoading: false,
            CurrentPage: 1,
            CurrentUser: null);

        // Create tag selection message
        var selectTagMsg = new Abies.Conduit.Page.Home.Message.TagSelected("csharp");

        // Act
        var result = await MvuLoopRuntime.RunUntilQuiescentAsync(
            initialModel: model,
            update: Abies.Conduit.Page.Home.Page.Update,
            initialMessage: selectTagMsg,
            options: new MvuLoopRuntime.Options(MaxIterations: 10));

        // Assert
        Assert.Contains(handler.Requests, r =>
            r.Method == HttpMethod.Get && r.Uri.PathAndQuery.Contains("tag=csharp"));

        Assert.Equal("csharp", result.Model.ActiveTag);
        Assert.Single(result.Model.Articles);
        Assert.Equal("csharp-article", result.Model.Articles[0].Slug);
    }

    #endregion

    #region Home Page - Pagination

    [Fact]
    public async Task Home_NavigateToPage2_LoadsNextArticles()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler { StrictMode = true };

        handler.When(
            HttpMethod.Get,
            "/api/articles?limit=10&offset=10",
            HttpStatusCode.OK,
            new
            {
                articles = (object[])
                [
                    new
                    {
                        slug = "page2-article",
                        title = "Page 2 Article",
                        description = "...",
                        body = "...",
                        tagList = (string[])[],
                        createdAt = "2020-01-01T00:00:00.000Z",
                        updatedAt = "2020-01-01T00:00:00.000Z",
                        favorited = false,
                        favoritesCount = 0,
                        author = new { username = "writer", bio = "", image = "", following = false }
                    }
                ],
                articlesCount = 15
            });

        ConfigureFakeApi(handler);

        var model = new Abies.Conduit.Page.Home.Model(
            Articles: [new Abies.Conduit.Page.Home.Article("page1", "P1", "D", "B", [], "2020", "2020", false, 0, 
                new Abies.Conduit.Page.Home.Profile("x", "", "", false))],
            ArticlesCount: 15,
            Tags: [],
            ActiveTab: Abies.Conduit.Page.Home.FeedTab.Global,
            ActiveTag: "",
            IsLoading: false,
            CurrentPage: 0,
            CurrentUser: null);

        // Create page navigation message
        var goToPage2Msg = new Abies.Conduit.Page.Home.Message.PageSelected(1); // 0-indexed

        // Act
        var result = await MvuLoopRuntime.RunUntilQuiescentAsync(
            initialModel: model,
            update: Abies.Conduit.Page.Home.Page.Update,
            initialMessage: goToPage2Msg,
            options: new MvuLoopRuntime.Options(MaxIterations: 10));

        // Assert
        Assert.Contains(handler.Requests, r =>
            r.Method == HttpMethod.Get && r.Uri.PathAndQuery.Contains("offset=10"));

        Assert.Equal(1, result.Model.CurrentPage);
        Assert.Single(result.Model.Articles);
        Assert.Equal("page2-article", result.Model.Articles[0].Slug);
    }

    #endregion

    #region Login Page

    [Fact]
    public async Task Login_SuccessfulLogin_UpdatesModelWithUser()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler { StrictMode = true };

        handler.When(
            HttpMethod.Post,
            "/api/users/login",
            HttpStatusCode.OK,
            new
            {
                user = new
                {
                    email = "alice@example.com",
                    token = "jwt-token-123",
                    username = "alice",
                    bio = "Hello",
                    image = ""
                }
            });

        ConfigureFakeApi(handler);

        var model = new Abies.Conduit.Page.Login.Model(
            Email: "alice@example.com",
            Password: "secret123",
            IsSubmitting: false,
            Errors: null,
            CurrentUser: null);

        // Submit login
        var loginMsg = new Abies.Conduit.Page.Login.Message.LoginSubmitted();

        // Act
        var result = await MvuLoopRuntime.RunUntilQuiescentAsync(
            initialModel: model,
            update: Abies.Conduit.Page.Login.Page.Update,
            initialMessage: loginMsg,
            options: new MvuLoopRuntime.Options(MaxIterations: 10));

        // Assert
        Assert.Contains(handler.Requests, r =>
            r.Method == HttpMethod.Post && r.Uri.PathAndQuery == "/api/users/login");

        Assert.False(result.Model.IsSubmitting);
        Assert.Null(result.Model.Errors);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ShowsError()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler { StrictMode = true };

        handler.When(
            HttpMethod.Post,
            "/api/users/login",
            HttpStatusCode.UnprocessableEntity,
            new
            {
                errors = new { email_or_password = (string[])["is invalid"] }
            });

        ConfigureFakeApi(handler);

        var model = new Abies.Conduit.Page.Login.Model(
            Email: "wrong@example.com",
            Password: "badpass",
            IsSubmitting: false,
            Errors: null,
            CurrentUser: null);

        var loginMsg = new Abies.Conduit.Page.Login.Message.LoginSubmitted();

        // Act
        var result = await MvuLoopRuntime.RunUntilQuiescentAsync(
            initialModel: model,
            update: Abies.Conduit.Page.Login.Page.Update,
            initialMessage: loginMsg,
            options: new MvuLoopRuntime.Options(MaxIterations: 10));

        // Assert
        Assert.Contains(handler.Requests, r =>
            r.Method == HttpMethod.Post && r.Uri.PathAndQuery == "/api/users/login");

        Assert.False(result.Model.IsSubmitting);
        Assert.NotNull(result.Model.Errors);
    }

    #endregion

    #region Editor Page - Create Article

    [Fact]
    public async Task Editor_CreateArticle_SubmitsAndSucceeds()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler { StrictMode = true };

        handler.When(
            HttpMethod.Post,
            "/api/articles",
            HttpStatusCode.OK,
            new
            {
                article = new
                {
                    slug = "my-new-article",
                    title = "My New Article",
                    description = "About something",
                    body = "Content here",
                    tagList = (string[])["csharp", "dotnet"],
                    createdAt = "2020-01-01T00:00:00.000Z",
                    updatedAt = "2020-01-01T00:00:00.000Z",
                    favorited = false,
                    favoritesCount = 0,
                    author = new { username = "alice", bio = "", image = "", following = false }
                }
            });

        ConfigureFakeApi(handler);

        var model = new Abies.Conduit.Page.Editor.Model(
            Title: "My New Article",
            Description: "About something",
            Body: "Content here",
            TagInput: "",
            TagList: ["csharp", "dotnet"],
            IsSubmitting: false,
            IsLoading: false,
            Slug: null, // New article
            Errors: null,
            CurrentUser: new User(new UserName("alice"), new Email("a@x"), new Token("t"), "", ""));

        var submitMsg = new Abies.Conduit.Page.Editor.Message.ArticleSubmitted();

        // Act
        var result = await MvuLoopRuntime.RunUntilQuiescentAsync(
            initialModel: model,
            update: Abies.Conduit.Page.Editor.Page.Update,
            initialMessage: submitMsg,
            options: new MvuLoopRuntime.Options(MaxIterations: 10));

        // Assert
        Assert.Contains(handler.Requests, r =>
            r.Method == HttpMethod.Post && r.Uri.PathAndQuery == "/api/articles");

        Assert.False(result.Model.IsSubmitting);
        Assert.Null(result.Model.Errors);
        Assert.Equal("my-new-article", result.Model.Slug);
    }

    [Fact]
    public async Task Editor_UpdateArticle_SubmitsAndSucceeds()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler { StrictMode = true };

        handler.When(
            HttpMethod.Put,
            "/api/articles/existing-slug",
            HttpStatusCode.OK,
            new
            {
                article = new
                {
                    slug = "existing-slug",
                    title = "Updated Title",
                    description = "Updated desc",
                    body = "Updated body",
                    tagList = (string[])["updated"],
                    createdAt = "2020-01-01T00:00:00.000Z",
                    updatedAt = "2020-01-02T00:00:00.000Z",
                    favorited = false,
                    favoritesCount = 0,
                    author = new { username = "alice", bio = "", image = "", following = false }
                }
            });

        ConfigureFakeApi(handler);

        var model = new Abies.Conduit.Page.Editor.Model(
            Title: "Updated Title",
            Description: "Updated desc",
            Body: "Updated body",
            TagInput: "",
            TagList: ["updated"],
            IsSubmitting: false,
            IsLoading: false,
            Slug: "existing-slug", // Existing article
            Errors: null,
            CurrentUser: new User(new UserName("alice"), new Email("a@x"), new Token("t"), "", ""));

        var submitMsg = new Abies.Conduit.Page.Editor.Message.ArticleSubmitted();

        // Act
        var result = await MvuLoopRuntime.RunUntilQuiescentAsync(
            initialModel: model,
            update: Abies.Conduit.Page.Editor.Page.Update,
            initialMessage: submitMsg,
            options: new MvuLoopRuntime.Options(MaxIterations: 10));

        // Assert
        Assert.Contains(handler.Requests, r =>
            r.Method == HttpMethod.Put && r.Uri.PathAndQuery == "/api/articles/existing-slug");

        Assert.False(result.Model.IsSubmitting);
        Assert.Null(result.Model.Errors);
    }

    [Fact]
    public async Task Editor_CreateArticle_ValidationError_ShowsErrors()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler { StrictMode = true };

        handler.When(
            HttpMethod.Post,
            "/api/articles",
            HttpStatusCode.UnprocessableEntity,
            new
            {
                errors = new { title = (string[])["can't be blank"], body = (string[])["can't be blank"] }
            });

        ConfigureFakeApi(handler);

        var model = new Abies.Conduit.Page.Editor.Model(
            Title: "",
            Description: "",
            Body: "",
            TagInput: "",
            TagList: [],
            IsSubmitting: false,
            IsLoading: false,
            Slug: null,
            Errors: null,
            CurrentUser: new User(new UserName("alice"), new Email("a@x"), new Token("t"), "", ""));

        var submitMsg = new Abies.Conduit.Page.Editor.Message.ArticleSubmitted();

        // Act
        var result = await MvuLoopRuntime.RunUntilQuiescentAsync(
            initialModel: model,
            update: Abies.Conduit.Page.Editor.Page.Update,
            initialMessage: submitMsg,
            options: new MvuLoopRuntime.Options(MaxIterations: 10));

        // Assert
        Assert.Contains(handler.Requests, r =>
            r.Method == HttpMethod.Post && r.Uri.PathAndQuery == "/api/articles");

        Assert.False(result.Model.IsSubmitting);
        Assert.NotNull(result.Model.Errors);
        Assert.True(result.Model.Errors!.ContainsKey("title") || result.Model.Errors!.ContainsKey("body"));
    }

    #endregion

    #region Register Page

    [Fact]
    public async Task Register_SuccessfulRegistration_UpdatesModelWithUser()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler { StrictMode = true };

        handler.When(
            HttpMethod.Post,
            "/api/users",
            HttpStatusCode.OK,
            new
            {
                user = new
                {
                    email = "newuser@example.com",
                    token = "jwt-token-456",
                    username = "newuser",
                    bio = "",
                    image = ""
                }
            });

        ConfigureFakeApi(handler);

        var model = new Abies.Conduit.Page.Register.Model(
            Username: "newuser",
            Email: "newuser@example.com",
            Password: "password123",
            IsSubmitting: false,
            Errors: null,
            CurrentUser: null);

        var registerMsg = new Abies.Conduit.Page.Register.Message.RegisterSubmitted();

        // Act
        var result = await MvuLoopRuntime.RunUntilQuiescentAsync(
            initialModel: model,
            update: Abies.Conduit.Page.Register.Page.Update,
            initialMessage: registerMsg,
            options: new MvuLoopRuntime.Options(MaxIterations: 10));

        // Assert
        Assert.Contains(handler.Requests, r =>
            r.Method == HttpMethod.Post && r.Uri.PathAndQuery == "/api/users");

        Assert.False(result.Model.IsSubmitting);
        Assert.Null(result.Model.Errors);
    }

    [Fact]
    public async Task Register_ValidationErrors_ShowsErrors()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler { StrictMode = true };

        handler.When(
            HttpMethod.Post,
            "/api/users",
            HttpStatusCode.UnprocessableEntity,
            new
            {
                errors = new
                {
                    email = (string[])["has already been taken"],
                    username = (string[])["has already been taken"]
                }
            });

        ConfigureFakeApi(handler);

        var model = new Abies.Conduit.Page.Register.Model(
            Username: "existinguser",
            Email: "existing@example.com",
            Password: "password123",
            IsSubmitting: false,
            Errors: null,
            CurrentUser: null);

        var registerMsg = new Abies.Conduit.Page.Register.Message.RegisterSubmitted();

        // Act
        var result = await MvuLoopRuntime.RunUntilQuiescentAsync(
            initialModel: model,
            update: Abies.Conduit.Page.Register.Page.Update,
            initialMessage: registerMsg,
            options: new MvuLoopRuntime.Options(MaxIterations: 10));

        // Assert
        Assert.Contains(handler.Requests, r =>
            r.Method == HttpMethod.Post && r.Uri.PathAndQuery == "/api/users");

        Assert.False(result.Model.IsSubmitting);
        Assert.NotNull(result.Model.Errors);
    }

    #endregion
}
