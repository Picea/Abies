using Abies.Conduit.IntegrationTests.Testing;
using Abies.Conduit.Main;
using Abies.Conduit.Services;
using Abies.DOM;
using Xunit;

namespace Abies.Conduit.IntegrationTests;

/// <summary>
/// Multi-user journey tests that exercise scenarios involving multiple users interacting.
/// Uses the StatefulFakeApi to maintain consistent state across user switches.
/// 
/// Scenarios covered:
/// - User B favorites User A's article
/// - User A sees updated favorite count
/// - User B views their favorited articles
/// - Multiple users following each other
/// </summary>
public class MultiUserJourneyTests
{
    private static HttpClient ConfigureApiWithStatefulFake(StatefulFakeApi fakeApi)
    {
        var httpClient = new HttpClient(fakeApi) { BaseAddress = new Uri("http://fake") };
        ApiClient.ConfigureHttpClient(httpClient);
        ApiClient.ConfigureBaseUrl("http://fake/api");
        Storage.Configure(new InMemoryStorageProvider());
        return httpClient;
    }

    private static void SetCurrentUser(FakeUser user)
    {
        ApiClient.SetAuthToken(user.Token);
    }

    #region Cross-User Favoriting

    [Fact]
    public async Task UserB_FavoritesUserA_Article_IncrementsFavoritesCount()
    {
        // Arrange: Set up two users and an article by User A
        var fakeApi = new StatefulFakeApi();
        var userA = fakeApi.AddUser("alice", "alice@example.com", "password", bio: "I write articles");
        var userB = fakeApi.AddUser("bob", "bob@example.com", "password", bio: "I read articles");
        var article = fakeApi.AddArticle("alice-article", "Alice's Great Article", "Description", "Body content", "alice", ["csharp"]);

        ConfigureApiWithStatefulFake(fakeApi);

        // Act: User B favorites User A's article
        SetCurrentUser(userB);

        var model = new Page.Article.Model(
            Slug: new Slug("alice-article"),
            IsLoading: false,
            Article: new Page.Home.Article(
                Slug: "alice-article",
                Title: "Alice's Great Article",
                Description: "Description",
                Body: "Body content",
                TagList: ["csharp"],
                CreatedAt: article.CreatedAt,
                UpdatedAt: article.UpdatedAt,
                Favorited: false,
                FavoritesCount: 0,
                Author: new Page.Home.Profile("alice", "I write articles", "", Following: false)),
            Comments: [],
            CommentInput: "",
            SubmittingComment: false,
            CurrentUser: new User(new UserName("bob"), new Email("bob@example.com"), new Token(userB.Token), "", "I read articles"));

        // Find and click favorite button
        var dom = Page.Article.Page.View(model);
        var (_, favHandler) = MvuDomTestHarness.FindFirstHandler(
            dom, "click",
            el => el.Tag == "button" && el.Children.OfType<Text>().Any(t => t.Value.Contains("Favorite Article")));

        Assert.NotNull(favHandler.Command);
        var toggleMsg = favHandler.Command!;

        var result = await MvuLoopRuntime.RunUntilQuiescentAsync(
            initialModel: model,
            update: Page.Article.Page.Update,
            initialMessage: toggleMsg,
            options: new MvuLoopRuntime.Options(MaxIterations: 10));

        // Assert: Article now shows as favorited with count 1
        Assert.True(result.Model.Article!.Favorited);
        Assert.Equal(1, result.Model.Article.FavoritesCount);

        // Verify the API received the favorite request
        Assert.Contains(fakeApi.Requests, r =>
            r.Method == HttpMethod.Post &&
            r.Uri.PathAndQuery == "/api/articles/alice-article/favorite" &&
            r.AuthUser == "bob");

        // Verify state is consistent in the fake API
        Assert.True(fakeApi.IsFavorited("bob", "alice-article"));
        Assert.Equal(1, fakeApi.GetFavoritesCount("alice-article"));
    }

    [Fact]
    public async Task UserA_SeesUpdatedFavoriteCount_AfterUserB_Favorites()
    {
        // Arrange: User B has already favorited User A's article
        var fakeApi = new StatefulFakeApi();
        var userA = fakeApi.AddUser("alice", "alice@example.com", "password");
        var userB = fakeApi.AddUser("bob", "bob@example.com", "password");
        var article = fakeApi.AddArticle("alice-article", "Alice's Article", "Desc", "Body", "alice");

        // User B favorited the article
        fakeApi.SetFavorite("bob", "alice-article", true);

        ConfigureApiWithStatefulFake(fakeApi);

        // Act: User A views their own article
        SetCurrentUser(userA);

        // Fetch the article (simulated by getting from API)
        var response = await ApiClient.GetArticleAsync("alice-article");

        // Assert: User A sees favoritesCount = 1, but Favorited = false (A hasn't favorited their own article)
        Assert.Equal(1, response.Article.FavoritesCount);
        Assert.False(response.Article.Favorited); // Alice hasn't favorited her own article
    }

    [Fact]
    public async Task MultipleUsers_Favorite_SameArticle_CountsCorrectly()
    {
        // Arrange: Three users, one article
        var fakeApi = new StatefulFakeApi();
        var userA = fakeApi.AddUser("alice", "alice@example.com", "password");
        var userB = fakeApi.AddUser("bob", "bob@example.com", "password");
        var userC = fakeApi.AddUser("charlie", "charlie@example.com", "password");
        fakeApi.AddArticle("popular-article", "Popular Article", "Desc", "Body", "alice");

        // Users B and C favorite the article
        fakeApi.SetFavorite("bob", "popular-article", true);
        fakeApi.SetFavorite("charlie", "popular-article", true);

        ConfigureApiWithStatefulFake(fakeApi);

        // Act: User A views the article
        SetCurrentUser(userA);
        var response = await ApiClient.GetArticleAsync("popular-article");

        // Assert
        Assert.Equal(2, response.Article.FavoritesCount);
        Assert.False(response.Article.Favorited); // Alice hasn't favorited

        // Act: User B views the article
        SetCurrentUser(userB);
        response = await ApiClient.GetArticleAsync("popular-article");

        // Assert
        Assert.Equal(2, response.Article.FavoritesCount);
        Assert.True(response.Article.Favorited); // Bob has favorited
    }

    [Fact]
    public async Task UserB_ViewsFavoritedArticles_SeesUserA_Article()
    {
        // Arrange
        var fakeApi = new StatefulFakeApi();
        var userA = fakeApi.AddUser("alice", "alice@example.com", "password");
        var userB = fakeApi.AddUser("bob", "bob@example.com", "password");
        fakeApi.AddArticle("alice-article-1", "Article 1", "Desc", "Body", "alice");
        fakeApi.AddArticle("alice-article-2", "Article 2", "Desc", "Body", "alice");
        fakeApi.AddArticle("bob-article", "Bob's Article", "Desc", "Body", "bob");

        // Bob favorites only alice-article-1
        fakeApi.SetFavorite("bob", "alice-article-1", true);

        ConfigureApiWithStatefulFake(fakeApi);
        SetCurrentUser(userB);

        // Act: Bob queries for his favorited articles
        var response = await ApiClient.GetArticlesAsync(favoritedBy: "bob");

        // Assert: Only alice-article-1 appears
        Assert.Single(response.Articles);
        Assert.Equal("alice-article-1", response.Articles[0].Slug);
        Assert.True(response.Articles[0].Favorited);
    }

    #endregion

    #region Cross-User Following

    [Fact]
    public async Task UserB_FollowsUserA_ThenViewsProfile()
    {
        // Arrange
        var fakeApi = new StatefulFakeApi();
        var userA = fakeApi.AddUser("alice", "alice@example.com", "password", bio: "Writer");
        var userB = fakeApi.AddUser("bob", "bob@example.com", "password");

        ConfigureApiWithStatefulFake(fakeApi);
        SetCurrentUser(userB);

        // Act: Bob follows Alice
        var followResponse = await ApiClient.FollowUserAsync("alice");

        // Assert
        Assert.True(followResponse.Profile.Following);
        Assert.Equal("alice", followResponse.Profile.Username);

        // Verify state
        Assert.True(fakeApi.IsFollowing("bob", "alice"));
    }

    [Fact]
    public async Task UserA_ArticleShowsFollowing_WhenViewedByFollower()
    {
        // Arrange
        var fakeApi = new StatefulFakeApi();
        var userA = fakeApi.AddUser("alice", "alice@example.com", "password");
        var userB = fakeApi.AddUser("bob", "bob@example.com", "password");
        fakeApi.AddArticle("alice-article", "Article", "Desc", "Body", "alice");
        fakeApi.SetFollow("bob", "alice", true);

        ConfigureApiWithStatefulFake(fakeApi);
        SetCurrentUser(userB);

        // Act: Bob views Alice's article
        var response = await ApiClient.GetArticleAsync("alice-article");

        // Assert: Author shows as followed
        Assert.True(response.Article.Author.Following);
    }

    [Fact]
    public async Task MutualFollowing_BothUsersFollowEachOther()
    {
        // Arrange
        var fakeApi = new StatefulFakeApi();
        var userA = fakeApi.AddUser("alice", "alice@example.com", "password");
        var userB = fakeApi.AddUser("bob", "bob@example.com", "password");
        fakeApi.SetFollow("alice", "bob", true);
        fakeApi.SetFollow("bob", "alice", true);

        ConfigureApiWithStatefulFake(fakeApi);

        // Act & Assert: Alice views Bob's profile
        SetCurrentUser(userA);
        var bobProfile = await ApiClient.GetProfileAsync("bob");
        Assert.True(bobProfile.Profile.Following);

        // Act & Assert: Bob views Alice's profile
        SetCurrentUser(userB);
        var aliceProfile = await ApiClient.GetProfileAsync("alice");
        Assert.True(aliceProfile.Profile.Following);
    }

    #endregion

    #region Cross-User Comments

    [Fact]
    public async Task UserB_CommentsOnUserA_Article()
    {
        // Arrange
        var fakeApi = new StatefulFakeApi();
        fakeApi.AddUser("alice", "alice@example.com", "password");
        var userB = fakeApi.AddUser("bob", "bob@example.com", "password");
        fakeApi.AddArticle("alice-article", "Article", "Desc", "Body", "alice");

        ConfigureApiWithStatefulFake(fakeApi);
        SetCurrentUser(userB);

        // Act: Bob adds a comment
        var response = await ApiClient.AddCommentAsync("alice-article", "Great article, Alice!");

        // Assert
        Assert.Equal("Great article, Alice!", response.Comment.Body);
        Assert.Equal("bob", response.Comment.Author.Username);
    }

    [Fact]
    public async Task UserA_SeesUserB_Comment_OnTheirArticle()
    {
        // Arrange
        var fakeApi = new StatefulFakeApi();
        var userA = fakeApi.AddUser("alice", "alice@example.com", "password");
        fakeApi.AddUser("bob", "bob@example.com", "password");
        fakeApi.AddArticle("alice-article", "Article", "Desc", "Body", "alice");
        fakeApi.AddComment("alice-article", "bob", "Nice work!");

        ConfigureApiWithStatefulFake(fakeApi);
        SetCurrentUser(userA);

        // Act: Alice views comments on her article
        var response = await ApiClient.GetCommentsAsync("alice-article");

        // Assert
        Assert.Single(response.Comments);
        Assert.Equal("Nice work!", response.Comments[0].Body);
        Assert.Equal("bob", response.Comments[0].Author.Username);
    }

    #endregion

    #region Combined Multi-User Journey

    [Fact]
    public async Task FullMultiUserJourney_FavoriteFollowComment()
    {
        // This test simulates a realistic multi-user interaction:
        // 1. Alice creates an article
        // 2. Bob follows Alice
        // 3. Bob favorites Alice's article
        // 4. Bob comments on Alice's article
        // 5. Charlie also favorites Alice's article
        // 6. Alice views her article and sees 2 favorites, 1 comment

        var fakeApi = new StatefulFakeApi();
        var alice = fakeApi.AddUser("alice", "alice@example.com", "password");
        var bob = fakeApi.AddUser("bob", "bob@example.com", "password");
        var charlie = fakeApi.AddUser("charlie", "charlie@example.com", "password");

        ConfigureApiWithStatefulFake(fakeApi);

        // Step 1: Alice creates an article
        SetCurrentUser(alice);
        var createResponse = await ApiClient.CreateArticleAsync(
            "My Great Post", "A description", "The full body", ["csharp", "dotnet"]);
        var slug = createResponse.Article.Slug;

        Assert.Equal("my-great-post", slug);
        Assert.Equal("alice", createResponse.Article.Author.Username);

        // Step 2: Bob follows Alice
        SetCurrentUser(bob);
        var followResponse = await ApiClient.FollowUserAsync("alice");
        Assert.True(followResponse.Profile.Following);

        // Step 3: Bob favorites Alice's article
        var favoriteResponse = await ApiClient.FavoriteArticleAsync(slug);
        Assert.True(favoriteResponse.Article.Favorited);
        Assert.Equal(1, favoriteResponse.Article.FavoritesCount);

        // Step 4: Bob comments on Alice's article
        var commentResponse = await ApiClient.AddCommentAsync(slug, "Awesome post!");
        Assert.Equal("bob", commentResponse.Comment.Author.Username);

        // Step 5: Charlie also favorites
        SetCurrentUser(charlie);
        var charliesFavorite = await ApiClient.FavoriteArticleAsync(slug);
        Assert.True(charliesFavorite.Article.Favorited);
        Assert.Equal(2, charliesFavorite.Article.FavoritesCount);

        // Step 6: Alice views her article
        SetCurrentUser(alice);
        var alicesView = await ApiClient.GetArticleAsync(slug);
        Assert.Equal(2, alicesView.Article.FavoritesCount);
        Assert.False(alicesView.Article.Favorited); // Alice hasn't favorited her own article

        var comments = await ApiClient.GetCommentsAsync(slug);
        Assert.Single(comments.Comments);
        Assert.Equal("Awesome post!", comments.Comments[0].Body);

        // Verify final state
        Assert.True(fakeApi.IsFavorited("bob", slug));
        Assert.True(fakeApi.IsFavorited("charlie", slug));
        Assert.False(fakeApi.IsFavorited("alice", slug));
        Assert.True(fakeApi.IsFollowing("bob", "alice"));
        Assert.Equal(2, fakeApi.GetFavoritesCount(slug));
    }

    #endregion
}
