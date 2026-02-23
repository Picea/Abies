using Abies.Commanding;
using Abies.Conduit.Capabilities;
using Abies.Conduit.Main;
using Abies.Conduit.Page.Home;
using Xunit;
using Comment = Abies.Conduit.Page.Article.Comment;
using MainMessage = Abies.Conduit.Main.Message;

namespace Abies.Conduit.IntegrationTests;

/// <summary>
/// Unit tests for individual command handlers, demonstrating the key benefit of
/// the capability-based architecture: each handler can be tested in isolation
/// with plain lambda fakes — no mocking framework or DI container required.
///
/// Capabilities return Result/Option values — errors are values, not exceptions.
/// </summary>
public class HandlerTests
{
    // ── Helpers ──

    private static User FakeUser(string name = "tester") =>
        new(new UserName(name), new Email($"{name}@test.com"), new Token("jwt-fake"), "", "");

    private static Article FakeArticle(string slug = "test-article") =>
        new(slug, "Title", "Desc", "Body", [], "2025-01-01", "2025-01-01", false, 0,
            new Profile("author", "", "", false));

    private static Comment FakeComment(string id = "1") =>
        new(id, "2025-01-01", "2025-01-01", "Nice!", new Profile("commenter", "", "", false));

    // Result/Option factory helpers for concise capability fakes
    private static Task<Result<T, ConduitError>> OkResult<T>(T value) =>
        Task.FromResult<Result<T, ConduitError>>(new Ok<T, ConduitError>(value));

    private static Task<Result<T, ConduitError>> ErrResult<T>(ConduitError error) =>
        Task.FromResult<Result<T, ConduitError>>(new Error<T, ConduitError>(error));

    private static Task<Option<T>> SomeOption<T>(T value) =>
        Task.FromResult<Option<T>>(new Some<T>(value));

    private static Task<Option<T>> NoneOption<T>() =>
        Task.FromResult<Option<T>>(new None<T>());

    /// <summary>
    /// Runs a handler for a single command and collects the returned message (if any).
    /// Handlers now return Option&lt;Message&gt; instead of dispatching via callback.
    /// </summary>
    private static async Task<List<Message>> Run(Handler handler, Command command)
    {
        var result = await handler(command);
        return result switch
        {
            Some<Message>(var msg) => [msg],
            _ => []
        };
    }

    // ═══════════════════════════════════════════════
    // Auth handlers
    // ═══════════════════════════════════════════════

    [Fact]
    public async Task LoginHandler_Success_DispatchesLoginSuccess()
    {
        var user = FakeUser();
        var handler = Handlers.LoginHandler(login: (_, _) => OkResult(user));

        var msgs = await Run(handler, new LoginCommand("test@test.com", "password"));

        var msg = Assert.Single(msgs);
        var success = Assert.IsType<Page.Login.Message.LoginSuccess>(msg);
        Assert.Equal("tester", success.User.Username.Value);
    }

    [Fact]
    public async Task LoginHandler_ValidationError_DispatchesLoginError()
    {
        var handler = Handlers.LoginHandler(
            login: (_, _) => ErrResult<User>(new ValidationError(new Dictionary<string, string[]>())));

        var msgs = await Run(handler, new LoginCommand("bad@test.com", "wrong"));

        var msg = Assert.Single(msgs);
        Assert.IsType<Page.Login.Message.LoginError>(msg);
    }

    [Fact]
    public async Task LoginHandler_Unauthorized_DispatchesUserLoggedOut()
    {
        var handler = Handlers.LoginHandler(
            login: (_, _) => ErrResult<User>(new Unauthorized()));

        var msgs = await Run(handler, new LoginCommand("x@x.com", "x"));

        var msg = Assert.Single(msgs);
        Assert.IsType<MainMessage.Event.UserLoggedOut>(msg);
    }

    [Fact]
    public async Task LoginHandler_IgnoresNonLoginCommands()
    {
        var handler = Handlers.LoginHandler(
            login: (_, _) => throw new InvalidOperationException("Should not be called"));

        var msgs = await Run(handler, new LogoutCommand());

        Assert.Empty(msgs);
    }

    [Fact]
    public async Task RegisterHandler_Success_DispatchesRegisterSuccess()
    {
        var user = FakeUser("newbie");
        var handler = Handlers.RegisterHandler(
            register: (_, _, _) => OkResult(user));

        var msgs = await Run(handler, new RegisterCommand("newbie", "new@test.com", "pass"));

        var msg = Assert.Single(msgs);
        var success = Assert.IsType<Page.Register.Message.RegisterSuccess>(msg);
        Assert.Equal("newbie", success.User.Username.Value);
    }

    [Fact]
    public async Task RegisterHandler_ValidationError_DispatchesRegisterError()
    {
        var errors = new Dictionary<string, string[]> { { "email", ["taken"] } };
        var handler = Handlers.RegisterHandler(
            register: (_, _, _) => ErrResult<User>(new ValidationError(errors)));

        var msgs = await Run(handler, new RegisterCommand("x", "x@x.com", "x"));

        var msg = Assert.Single(msgs);
        var err = Assert.IsType<Page.Register.Message.RegisterError>(msg);
        Assert.Contains("email", err.Errors.Keys);
    }

    [Fact]
    public async Task LoadCurrentUserHandler_WithUser_DispatchesUserLoggedIn()
    {
        var user = FakeUser();
        var handler = Handlers.LoadCurrentUserHandler(loadCurrentUser: () => SomeOption(user));

        var msgs = await Run(handler, new MainMessage.Command.LoadCurrentUser());

        var msg = Assert.Single(msgs);
        var loggedIn = Assert.IsType<MainMessage.Event.UserLoggedIn>(msg);
        Assert.Equal("tester", loggedIn.User.Username.Value);
    }

    [Fact]
    public async Task LoadCurrentUserHandler_NoUser_DispatchesInitializationComplete()
    {
        var handler = Handlers.LoadCurrentUserHandler(loadCurrentUser: () => NoneOption<User>());

        var msgs = await Run(handler, new MainMessage.Command.LoadCurrentUser());

        var msg = Assert.Single(msgs);
        Assert.IsType<MainMessage.Event.InitializationComplete>(msg);
    }

    [Fact]
    public async Task LogoutHandler_DispatchesUserLoggedOut()
    {
        var logoutCalled = false;
        var handler = Handlers.LogoutHandler(logout: () =>
        {
            logoutCalled = true;
            return OkResult<Unit>(default);
        });

        var msgs = await Run(handler, new LogoutCommand());

        Assert.True(logoutCalled);
        var msg = Assert.Single(msgs);
        Assert.IsType<MainMessage.Event.UserLoggedOut>(msg);
    }

    // ═══════════════════════════════════════════════
    // Article handlers
    // ═══════════════════════════════════════════════

    [Fact]
    public async Task LoadArticlesHandler_GlobalFeed_DispatchesHomeArticlesLoaded()
    {
        var articles = new List<Article> { FakeArticle() };
        var handler = Handlers.LoadArticlesHandler(
            loadArticles: (_, _, _, _, _) => OkResult<(List<Article> Articles, int Count)>((articles, 1)));

        var msgs = await Run(handler, new LoadArticlesCommand());

        var msg = Assert.Single(msgs);
        var loaded = Assert.IsType<Page.Home.Message.ArticlesLoaded>(msg);
        Assert.Single(loaded.Articles);
        Assert.Equal(1, loaded.ArticlesCount);
    }

    [Fact]
    public async Task LoadArticlesHandler_ByAuthor_DispatchesProfileArticlesLoaded()
    {
        var articles = new List<Article> { FakeArticle() };
        var handler = Handlers.LoadArticlesHandler(
            loadArticles: (_, _, _, _, _) => OkResult<(List<Article> Articles, int Count)>((articles, 1)));

        var msgs = await Run(handler, new LoadArticlesCommand(Author: "author1"));

        var msg = Assert.Single(msgs);
        Assert.IsType<Page.Profile.Message.ArticlesLoaded>(msg);
    }

    [Fact]
    public async Task LoadArticlesHandler_ByFavorited_DispatchesFavoritedArticlesLoaded()
    {
        var handler = Handlers.LoadArticlesHandler(
            loadArticles: (_, _, _, _, _) => OkResult<(List<Article> Articles, int Count)>(([], 0)));

        var msgs = await Run(handler, new LoadArticlesCommand(FavoritedBy: "user1"));

        var msg = Assert.Single(msgs);
        Assert.IsType<Page.Profile.Message.FavoritedArticlesLoaded>(msg);
    }

    [Fact]
    public async Task LoadTagsHandler_Success_DispatchesTagsLoaded()
    {
        var tags = new List<string> { "elm", "fp", "dotnet" };
        var handler = Handlers.LoadTagsHandler(loadTags: () => OkResult(tags));

        var msgs = await Run(handler, new LoadTagsCommand());

        var msg = Assert.Single(msgs);
        var loaded = Assert.IsType<Page.Home.Message.TagsLoaded>(msg);
        Assert.Equal(3, loaded.Tags.Count);
    }

    [Fact]
    public async Task LoadArticleHandler_Success_DispatchesArticleLoaded()
    {
        var article = FakeArticle("my-slug");
        var handler = Handlers.LoadArticleHandler(getArticle: _ => OkResult(article));

        var msgs = await Run(handler, new LoadArticleCommand("my-slug"));

        var msg = Assert.Single(msgs);
        var loaded = Assert.IsType<Page.Article.Message.ArticleLoaded>(msg);
        Assert.Equal("my-slug", loaded.Article!.Slug);
    }

    [Fact]
    public async Task CreateArticleHandler_Success_DispatchesArticleSubmitSuccess()
    {
        var article = FakeArticle("new-post");
        var handler = Handlers.CreateArticleHandler(
            createArticle: (_, _, _, _) => OkResult(article));

        var msgs = await Run(handler, new CreateArticleCommand("Title", "Desc", "Body", []));

        var msg = Assert.Single(msgs);
        var success = Assert.IsType<Page.Editor.Message.ArticleSubmitSuccess>(msg);
        Assert.Equal("new-post", success.Slug);
    }

    [Fact]
    public async Task DeleteArticleHandler_Success_DispatchesArticleDeleted()
    {
        var deletedSlug = "";
        var handler = Handlers.DeleteArticleHandler(
            deleteArticle: slug =>
            {
                deletedSlug = slug;
                return OkResult<Unit>(default);
            });

        var msgs = await Run(handler, new DeleteArticleCommand("slug-to-delete"));

        Assert.Equal("slug-to-delete", deletedSlug);
        var msg = Assert.Single(msgs);
        Assert.IsType<Page.Article.Message.ArticleDeleted>(msg);
    }

    // ═══════════════════════════════════════════════
    // Favorite / Follow toggle handlers
    // ═══════════════════════════════════════════════

    [Fact]
    public async Task ToggleFavoriteHandler_WhenNotFavorited_CallsFavorite()
    {
        var favArticle = FakeArticle() with { Favorited = true, FavoritesCount = 1 };
        var handler = Handlers.ToggleFavoriteHandler(
            favoriteArticle: _ => OkResult(favArticle),
            unfavoriteArticle: _ => throw new InvalidOperationException("Should not unfavorite"));

        var msgs = await Run(handler, new ToggleFavoriteCommand("slug", CurrentState: false));

        var msg = Assert.Single(msgs);
        var loaded = Assert.IsType<Page.Article.Message.ArticleLoaded>(msg);
        Assert.True(loaded.Article!.Favorited);
    }

    [Fact]
    public async Task ToggleFavoriteHandler_WhenFavorited_CallsUnfavorite()
    {
        var unfavArticle = FakeArticle() with { Favorited = false, FavoritesCount = 0 };
        var handler = Handlers.ToggleFavoriteHandler(
            favoriteArticle: _ => throw new InvalidOperationException("Should not favorite"),
            unfavoriteArticle: _ => OkResult(unfavArticle));

        var msgs = await Run(handler, new ToggleFavoriteCommand("slug", CurrentState: true));

        var msg = Assert.Single(msgs);
        var loaded = Assert.IsType<Page.Article.Message.ArticleLoaded>(msg);
        Assert.False(loaded.Article!.Favorited);
    }

    [Fact]
    public async Task ToggleFollowHandler_WhenNotFollowing_CallsFollow()
    {
        var profile = new Profile("user", "", "", true);
        var handler = Handlers.ToggleFollowHandler(
            followUser: _ => OkResult(profile),
            unfollowUser: _ => throw new InvalidOperationException("Should not unfollow"));

        var msgs = await Run(handler, new ToggleFollowCommand("user", CurrentState: false));

        var msg = Assert.Single(msgs);
        var loaded = Assert.IsType<Page.Profile.Message.ProfileLoaded>(msg);
        Assert.True(loaded.Profile.Following);
    }

    [Fact]
    public async Task ToggleFollowHandler_WhenFollowing_CallsUnfollow()
    {
        var profile = new Profile("user", "", "", false);
        var handler = Handlers.ToggleFollowHandler(
            followUser: _ => throw new InvalidOperationException("Should not follow"),
            unfollowUser: _ => OkResult(profile));

        var msgs = await Run(handler, new ToggleFollowCommand("user", CurrentState: true));

        var msg = Assert.Single(msgs);
        var loaded = Assert.IsType<Page.Profile.Message.ProfileLoaded>(msg);
        Assert.False(loaded.Profile.Following);
    }

    // ═══════════════════════════════════════════════
    // Comment handlers
    // ═══════════════════════════════════════════════

    [Fact]
    public async Task LoadCommentsHandler_Success_DispatchesCommentsLoaded()
    {
        var comments = new List<Comment> { FakeComment() };
        var handler = Handlers.LoadCommentsHandler(loadComments: _ => OkResult(comments));

        var msgs = await Run(handler, new LoadCommentsCommand("slug"));

        var msg = Assert.Single(msgs);
        var loaded = Assert.IsType<Page.Article.Message.CommentsLoaded>(msg);
        Assert.Single(loaded.Comments);
    }

    [Fact]
    public async Task SubmitCommentHandler_Success_DispatchesCommentSubmitted()
    {
        var comment = FakeComment("42");
        var handler = Handlers.SubmitCommentHandler(addComment: (_, _) => OkResult(comment));

        var msgs = await Run(handler, new SubmitCommentCommand("slug", "Great post!"));

        var msg = Assert.Single(msgs);
        var submitted = Assert.IsType<Page.Article.Message.CommentSubmitted>(msg);
        Assert.Equal("42", submitted.Comment.Id);
    }

    [Fact]
    public async Task DeleteCommentHandler_Success_DispatchesCommentDeleted()
    {
        var handler = Handlers.DeleteCommentHandler(
            deleteComment: (_, _) => OkResult<Unit>(default));

        var msgs = await Run(handler, new DeleteCommentCommand("slug", "7"));

        var msg = Assert.Single(msgs);
        var deleted = Assert.IsType<Page.Article.Message.CommentDeleted>(msg);
        Assert.Equal("7", deleted.Id);
    }

    // ═══════════════════════════════════════════════
    // Profile handlers
    // ═══════════════════════════════════════════════

    [Fact]
    public async Task LoadProfileHandler_Success_DispatchesProfileLoaded()
    {
        var profile = new Profile("celeb", "Famous person", "pic.jpg", false);
        var handler = Handlers.LoadProfileHandler(loadProfile: _ => OkResult(profile));

        var msgs = await Run(handler, new LoadProfileCommand("celeb"));

        var msg = Assert.Single(msgs);
        var loaded = Assert.IsType<Page.Profile.Message.ProfileLoaded>(msg);
        Assert.Equal("celeb", loaded.Profile.Username);
    }

    // ═══════════════════════════════════════════════
    // Pipeline infrastructure
    // ═══════════════════════════════════════════════

    [Fact]
    public async Task Pipeline_Empty_DoesNothing()
    {
        var msgs = await Run(Pipeline.Empty, new LoginCommand("a", "b"));
        Assert.Empty(msgs);
    }

    [Fact]
    public async Task Pipeline_Compose_RoutesToCorrectHandler()
    {
        var user = FakeUser();
        var tags = new List<string> { "tag1" };

        var handler = Pipeline.Compose(
            Handlers.LoginHandler(login: (_, _) => OkResult(user)),
            Handlers.LoadTagsHandler(loadTags: () => OkResult(tags))
        );

        // Login command should route to login handler
        var loginMsgs = await Run(handler, new LoginCommand("a@b.com", "p"));
        Assert.Single(loginMsgs);
        Assert.IsType<Page.Login.Message.LoginSuccess>(loginMsgs[0]);

        // Tags command should route to tags handler
        var tagsMsgs = await Run(handler, new LoadTagsCommand());
        Assert.Single(tagsMsgs);
        Assert.IsType<Page.Home.Message.TagsLoaded>(tagsMsgs[0]);
    }

    [Fact]
    public async Task Pipeline_Compose_HandlesNoneCommand()
    {
        var handler = Pipeline.Compose(
            Handlers.LoginHandler(login: (_, _) => throw new InvalidOperationException("Should not be called"))
        );

        var msgs = await Run(handler, Commands.None);
        Assert.Empty(msgs);
    }

    [Fact]
    public async Task Pipeline_For_IgnoresNonMatchingCommands()
    {
        var handler = Pipeline.For<LoginCommand>(async cmd =>
        {
            return new Some<Message>(new Page.Login.Message.LoginSuccess(FakeUser()));
        });

        // Should ignore a non-LoginCommand
        var msgs = await Run(handler, new LogoutCommand());
        Assert.Empty(msgs);
    }

    // ═══════════════════════════════════════════════
    // ComposeAll
    // ═══════════════════════════════════════════════

    [Fact]
    public async Task ComposeAll_RoutesAllCommandTypes()
    {
        // Wire all capabilities with minimal fakes returning Result/Option values
        var user = FakeUser();
        var article = FakeArticle();
        var comment = FakeComment();
        var profile = new Profile("u", "", "", false);
        var tags = new List<string> { "tag" };

        var handler = Handlers.ComposeAll(
            login: (_, _) => OkResult(user),
            register: (_, _, _) => OkResult(user),
            updateUser: (_, _, _, _, _) => OkResult(user),
            loadCurrentUser: () => SomeOption(user),
            logout: () => OkResult<Unit>(default),
            loadArticles: (_, _, _, _, _) => OkResult<(List<Article> Articles, int Count)>(([article], 1)),
            loadFeed: (_, _) => OkResult<(List<Article> Articles, int Count)>(([article], 1)),
            getArticle: _ => OkResult(article),
            createArticle: (_, _, _, _) => OkResult(article),
            updateArticle: (_, _, _, _) => OkResult(article),
            deleteArticle: _ => OkResult<Unit>(default),
            favoriteArticle: _ => OkResult(article),
            unfavoriteArticle: _ => OkResult(article),
            loadComments: _ => OkResult(new List<Comment> { comment }),
            addComment: (_, _) => OkResult(comment),
            deleteComment: (_, _) => OkResult<Unit>(default),
            loadTags: () => OkResult(tags),
            loadProfile: _ => OkResult(profile),
            followUser: _ => OkResult(profile),
            unfollowUser: _ => OkResult(profile)
        );

        // Verify a few representative commands route correctly
        var loginMsgs = await Run(handler, new LoginCommand("a@b.com", "p"));
        Assert.IsType<Page.Login.Message.LoginSuccess>(Assert.Single(loginMsgs));

        var tagsMsgs = await Run(handler, new LoadTagsCommand());
        Assert.IsType<Page.Home.Message.TagsLoaded>(Assert.Single(tagsMsgs));

        var articleMsgs = await Run(handler, new LoadArticleCommand("slug"));
        Assert.IsType<Page.Article.Message.ArticleLoaded>(Assert.Single(articleMsgs));

        var logoutMsgs = await Run(handler, new LogoutCommand());
        Assert.IsType<MainMessage.Event.UserLoggedOut>(Assert.Single(logoutMsgs));
    }
}
