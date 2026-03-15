// =============================================================================
// PostgreSQL Read Store Integration Tests
// =============================================================================
// These tests verify the full projection → query pipeline against a real
// PostgreSQL instance. They require a PostgreSQL connection string in the
// CONDUIT_POSTGRES_CONNECTION environment variable.
//
// Run with: CONDUIT_POSTGRES_CONNECTION="Host=localhost;Database=conduit_test;..."
//           dotnet test --filter "Category=Integration"
//
// Tests use a unique schema prefix per run to enable parallel execution
// and avoid cross-contamination.
// =============================================================================

using Npgsql;
using Picea.Abies.Conduit.Domain.Article;
using Picea.Abies.Conduit.Domain.Shared;
using Picea.Abies.Conduit.Domain.User;
using Picea.Abies.Conduit.ReadModel;
using TUnit.Core.Interfaces;

namespace Picea.Abies.Conduit.ReadStore.PostgreSQL.Tests;

/// <summary>
/// Integration tests for the PostgreSQL read store.
/// Requires a running PostgreSQL instance.
/// </summary>
[Property("Category", "Integration")]
public sealed class PostgresReadStoreTests : IAsyncInitializer, IAsyncDisposable
{
    private NpgsqlDataSource _dataSource = null!;
    private readonly string _connectionString;

    public PostgresReadStoreTests()
    {
        _connectionString = Environment.GetEnvironmentVariable("CONDUIT_POSTGRES_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=conduit_test;Username=postgres;Password=postgres;Include Error Detail=true";
    }

    public async Task InitializeAsync()
    {
        _dataSource = NpgsqlDataSource.Create(_connectionString);
        await Schema.EnsureCreated(_dataSource);
        await CleanTables();
    }

    public async ValueTask DisposeAsync()
    {
        await CleanTables();
        await _dataSource.DisposeAsync();
    }

    private async Task CleanTables()
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand("""
            DELETE FROM comments;
            DELETE FROM favorites;
            DELETE FROM article_tags;
            DELETE FROM articles;
            DELETE FROM follows;
            DELETE FROM users;
            """, connection);
        await command.ExecuteNonQueryAsync();
    }

    // ─── User Projection Tests ──────────────────────────────────────────

    [Test]
    public async Task UserRegistered_creates_user_in_read_store()
    {
        var userId = Guid.NewGuid();
        var registered = new UserEvent.Registered(
            new UserId(userId),
            new EmailAddress("jake@jake.jake"),
            new Username("jake"),
            new PasswordHash("hashed123"),
            new Timestamp(DateTimeOffset.UtcNow));

        await UserProjection.Apply(_dataSource, userId, registered);

        var findByEmail = QueryStore.CreateFindUserByEmail(_dataSource);
        var result = await findByEmail("jake@jake.jake");

        await Assert.That(result.IsSome).IsTrue();
        var user = result.Value;
        await Assert.That(user.Id).IsEqualTo(userId);
        await Assert.That(user.Email).IsEqualTo("jake@jake.jake");
        await Assert.That(user.Username).IsEqualTo("jake");
        await Assert.That(user.PasswordHash).IsEqualTo("hashed123");
    }

    [Test]
    public async Task UserRegistered_is_idempotent()
    {
        var userId = Guid.NewGuid();
        var registered = new UserEvent.Registered(
            new UserId(userId),
            new EmailAddress("idem@test.com"),
            new Username("idemuser"),
            new PasswordHash("hash1"),
            new Timestamp(DateTimeOffset.UtcNow));

        await UserProjection.Apply(_dataSource, userId, registered);
        await UserProjection.Apply(_dataSource, userId, registered); // Second apply

        var findById = QueryStore.CreateFindUserById(_dataSource);
        var result = await findById(userId);
        await Assert.That(result.IsSome).IsTrue();
        await Assert.That(result.Value.Username).IsEqualTo("idemuser");
    }

    [Test]
    public async Task ProfileUpdated_updates_user_fields()
    {
        var userId = Guid.NewGuid();
        var registered = new UserEvent.Registered(
            new UserId(userId),
            new EmailAddress("update@test.com"),
            new Username("original"),
            new PasswordHash("hash1"),
            new Timestamp(DateTimeOffset.UtcNow));

        await UserProjection.Apply(_dataSource, userId, registered);

        var profileUpdated = new UserEvent.ProfileUpdated(
            Email: Option<EmailAddress>.None,
            Username: Option<Username>.None,
            PasswordHash: Option<PasswordHash>.None,
            Bio: Option<Bio>.Some(new Bio("I work at StateFactory")),
            Image: Option<ImageUrl>.Some(new ImageUrl("https://i.stack.imgur.com/xHWG8.jpg")),
            UpdatedAt: new Timestamp(DateTimeOffset.UtcNow));

        await UserProjection.Apply(_dataSource, userId, profileUpdated);

        var findById = QueryStore.CreateFindUserById(_dataSource);
        var result = await findById(userId);
        await Assert.That(result.IsSome).IsTrue();
        await Assert.That(result.Value.Bio).IsEqualTo("I work at StateFactory");
        await Assert.That(result.Value.Image).IsEqualTo("https://i.stack.imgur.com/xHWG8.jpg");
    }

    [Test]
    public async Task Follow_and_unfollow_affects_profile_query()
    {
        var followerId = Guid.NewGuid();
        var followeeId = Guid.NewGuid();

        // Register both users
        await UserProjection.Apply(_dataSource, followerId, new UserEvent.Registered(
            new UserId(followerId), new EmailAddress("follower@test.com"),
            new Username("follower"), new PasswordHash("h1"),
            new Timestamp(DateTimeOffset.UtcNow)));

        await UserProjection.Apply(_dataSource, followeeId, new UserEvent.Registered(
            new UserId(followeeId), new EmailAddress("followee@test.com"),
            new Username("followee"), new PasswordHash("h2"),
            new Timestamp(DateTimeOffset.UtcNow)));

        var getProfile = QueryStore.CreateGetProfile(_dataSource);

        // Before follow
        var before = await getProfile("followee", Option<Guid>.Some(followerId));
        await Assert.That(before.IsSome).IsTrue();
        await Assert.That(before.Value.Following).IsFalse();

        // Follow
        await UserProjection.Apply(_dataSource, followerId, new UserEvent.Followed(new UserId(followeeId)));

        var after = await getProfile("followee", Option<Guid>.Some(followerId));
        await Assert.That(after.IsSome).IsTrue();
        await Assert.That(after.Value.Following).IsTrue();

        // Unfollow
        await UserProjection.Apply(_dataSource, followerId, new UserEvent.Unfollowed(new UserId(followeeId)));

        var afterUnfollow = await getProfile("followee", Option<Guid>.Some(followerId));
        await Assert.That(afterUnfollow.IsSome).IsTrue();
        await Assert.That(afterUnfollow.Value.Following).IsFalse();
    }

    [Test]
    public async Task FindUserByEmail_returns_none_for_unknown_email()
    {
        var findByEmail = QueryStore.CreateFindUserByEmail(_dataSource);
        var result = await findByEmail("nonexistent@test.com");
        await Assert.That(result.IsNone).IsTrue();
    }

    [Test]
    public async Task GetProfile_returns_none_for_unknown_username()
    {
        var getProfile = QueryStore.CreateGetProfile(_dataSource);
        var result = await getProfile("nobody", Option<Guid>.None);
        await Assert.That(result.IsNone).IsTrue();
    }

    // ─── Article Projection Tests ───────────────────────────────────────

    [Test]
    public async Task ArticleCreated_creates_article_in_read_store()
    {
        var userId = Guid.NewGuid();
        var articleId = Guid.NewGuid();

        await RegisterUser(userId, "author1", "author1@test.com");

        var created = new ArticleEvent.ArticleCreated(
            new ArticleId(articleId),
            new Slug("how-to-train-your-dragon"),
            new Title("How to train your dragon"),
            new Description("Ever wonder how?"),
            new Body("You have to believe"),
            new HashSet<Tag> { new("reactjs"), new("angularjs"), new("dragons") },
            new UserId(userId),
            new Timestamp(DateTimeOffset.UtcNow));

        await ArticleProjection.Apply(_dataSource, articleId, created);

        var findBySlug = QueryStore.CreateFindArticleBySlug(_dataSource);
        var result = await findBySlug("how-to-train-your-dragon", Option<Guid>.None);

        await Assert.That(result.IsSome).IsTrue();
        var article = result.Value;
        await Assert.That(article.Slug).IsEqualTo("how-to-train-your-dragon");
        await Assert.That(article.Title).IsEqualTo("How to train your dragon");
        await Assert.That(article.Description).IsEqualTo("Ever wonder how?");
        await Assert.That(article.Body).IsEqualTo("You have to believe");
        await Assert.That(article.TagList.Count).IsEqualTo(3);
        await Assert.That(article.TagList).Contains("dragons");
        await Assert.That(article.TagList).Contains("reactjs");
        await Assert.That(article.Author.Username).IsEqualTo("author1");
        await Assert.That(article.FavoritesCount).IsEqualTo(0);
        await Assert.That(article.Favorited).IsFalse();
    }

    [Test]
    public async Task ArticleUpdated_updates_article_fields()
    {
        var userId = Guid.NewGuid();
        var articleId = Guid.NewGuid();

        await RegisterUser(userId, "author2", "author2@test.com");
        await CreateArticle(articleId, userId, "original-title", "Original Title");

        var updated = new ArticleEvent.ArticleUpdated(
            Title: Option<Title>.Some(new Title("Updated Title")),
            Description: Option<Description>.None,
            Body: Option<Body>.Some(new Body("New body content")),
            Slug: new Slug("updated-title"),
            UpdatedAt: new Timestamp(DateTimeOffset.UtcNow));

        await ArticleProjection.Apply(_dataSource, articleId, updated);

        var findBySlug = QueryStore.CreateFindArticleBySlug(_dataSource);
        var result = await findBySlug("updated-title", Option<Guid>.None);

        await Assert.That(result.IsSome).IsTrue();
        await Assert.That(result.Value.Title).IsEqualTo("Updated Title");
        await Assert.That(result.Value.Body).IsEqualTo("New body content");
    }

    [Test]
    public async Task ArticleDeleted_soft_deletes_article()
    {
        var userId = Guid.NewGuid();
        var articleId = Guid.NewGuid();

        await RegisterUser(userId, "author3", "author3@test.com");
        await CreateArticle(articleId, userId, "to-be-deleted", "To Be Deleted");

        await ArticleProjection.Apply(_dataSource, articleId, new ArticleEvent.ArticleDeleted());

        var findBySlug = QueryStore.CreateFindArticleBySlug(_dataSource);
        var result = await findBySlug("to-be-deleted", Option<Guid>.None);

        await Assert.That(result.IsNone).IsTrue();
    }

    [Test]
    public async Task Favorite_and_unfavorite_updates_count()
    {
        var authorId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var articleId = Guid.NewGuid();

        await RegisterUser(authorId, "fav-author", "fav-author@test.com");
        await RegisterUser(userId, "fav-user", "fav-user@test.com");
        await CreateArticle(articleId, authorId, "fav-article", "Fav Article");

        // Favorite
        await ArticleProjection.Apply(_dataSource, articleId,
            new ArticleEvent.ArticleFavorited(new UserId(userId)));

        var findBySlug = QueryStore.CreateFindArticleBySlug(_dataSource);
        var afterFav = await findBySlug("fav-article", Option<Guid>.Some(userId));
        await Assert.That(afterFav.IsSome).IsTrue();
        await Assert.That(afterFav.Value.FavoritesCount).IsEqualTo(1);
        await Assert.That(afterFav.Value.Favorited).IsTrue();

        // Unfavorite
        await ArticleProjection.Apply(_dataSource, articleId,
            new ArticleEvent.ArticleUnfavorited(new UserId(userId)));

        var afterUnfav = await findBySlug("fav-article", Option<Guid>.Some(userId));
        await Assert.That(afterUnfav.IsSome).IsTrue();
        await Assert.That(afterUnfav.Value.FavoritesCount).IsEqualTo(0);
        await Assert.That(afterUnfav.Value.Favorited).IsFalse();
    }

    [Test]
    public async Task Favorite_is_idempotent()
    {
        var authorId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var articleId = Guid.NewGuid();

        await RegisterUser(authorId, "idem-author", "idem-author@test.com");
        await RegisterUser(userId, "idem-fav", "idem-fav@test.com");
        await CreateArticle(articleId, authorId, "idem-fav-article", "Idem Fav Article");

        await ArticleProjection.Apply(_dataSource, articleId,
            new ArticleEvent.ArticleFavorited(new UserId(userId)));
        await ArticleProjection.Apply(_dataSource, articleId,
            new ArticleEvent.ArticleFavorited(new UserId(userId)));

        var findBySlug = QueryStore.CreateFindArticleBySlug(_dataSource);
        var result = await findBySlug("idem-fav-article", Option<Guid>.Some(userId));
        await Assert.That(result.IsSome).IsTrue();
        await Assert.That(result.Value.FavoritesCount).IsEqualTo(1);
    }

    // ─── Comment Tests ──────────────────────────────────────────────────

    [Test]
    public async Task CommentAdded_creates_comment_in_read_store()
    {
        var authorId = Guid.NewGuid();
        var commenterId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        var commentId = Guid.NewGuid();

        await RegisterUser(authorId, "comment-author", "comment-author@test.com");
        await RegisterUser(commenterId, "commenter", "commenter@test.com");
        await CreateArticle(articleId, authorId, "commented-article", "Commented Article");

        var commentAdded = new ArticleEvent.CommentAdded(
            new CommentId(commentId),
            new UserId(commenterId),
            new CommentBody("His name was my name too."),
            new Timestamp(DateTimeOffset.UtcNow));

        await ArticleProjection.Apply(_dataSource, articleId, commentAdded);

        var getComments = QueryStore.CreateGetComments(_dataSource);
        var comments = await getComments("commented-article", Option<Guid>.None);

        await Assert.That(comments).Count().IsEqualTo(1);
        await Assert.That(comments[0].Id).IsEqualTo(commentId);
        await Assert.That(comments[0].Body).IsEqualTo("His name was my name too.");
        await Assert.That(comments[0].Author.Username).IsEqualTo("commenter");
    }

    [Test]
    public async Task CommentDeleted_soft_deletes_comment()
    {
        var authorId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        var commentId = Guid.NewGuid();

        await RegisterUser(authorId, "del-comment-author", "del-comment@test.com");
        await CreateArticle(articleId, authorId, "del-comment-article", "Del Comment Article");

        await ArticleProjection.Apply(_dataSource, articleId,
            new ArticleEvent.CommentAdded(
                new CommentId(commentId), new UserId(authorId),
                new CommentBody("To be deleted"), new Timestamp(DateTimeOffset.UtcNow)));

        await ArticleProjection.Apply(_dataSource, articleId,
            new ArticleEvent.CommentDeleted(new CommentId(commentId)));

        var getComments = QueryStore.CreateGetComments(_dataSource);
        var comments = await getComments("del-comment-article", Option<Guid>.None);
        await Assert.That(comments).IsEmpty();
    }

    // ─── List / Feed / Tags Tests ───────────────────────────────────────

    [Test]
    public async Task ListArticles_with_tag_filter()
    {
        var userId = Guid.NewGuid();
        await RegisterUser(userId, "list-author", "list-author@test.com");

        var articleId1 = Guid.NewGuid();
        var articleId2 = Guid.NewGuid();

        await ArticleProjection.Apply(_dataSource, articleId1,
            new ArticleEvent.ArticleCreated(
                new ArticleId(articleId1), new Slug("article-one"), new Title("Article One"),
                new Description("Desc 1"), new Body("Body 1"),
                new HashSet<Tag> { new("csharp"), new("dotnet") },
                new UserId(userId), new Timestamp(DateTimeOffset.UtcNow)));

        await ArticleProjection.Apply(_dataSource, articleId2,
            new ArticleEvent.ArticleCreated(
                new ArticleId(articleId2), new Slug("article-two"), new Title("Article Two"),
                new Description("Desc 2"), new Body("Body 2"),
                new HashSet<Tag> { new("python") },
                new UserId(userId), new Timestamp(DateTimeOffset.UtcNow)));

        var listArticles = QueryStore.CreateListArticles(_dataSource);
        var result = await listArticles(new ArticleFilter(
            Tag: Option<string>.Some("csharp"),
            Author: Option<string>.None,
            FavoritedBy: Option<string>.None));

        await Assert.That(result.ArticlesCount).IsEqualTo(1);
        await Assert.That(result.Articles).Count().IsEqualTo(1);
        await Assert.That(result.Articles[0].Slug).IsEqualTo("article-one");
    }

    [Test]
    public async Task GetFeed_returns_articles_by_followed_authors()
    {
        var followerId = Guid.NewGuid();
        var followedAuthorId = Guid.NewGuid();
        var unfollowedAuthorId = Guid.NewGuid();

        await RegisterUser(followerId, "feed-reader", "feed-reader@test.com");
        await RegisterUser(followedAuthorId, "feed-followed", "feed-followed@test.com");
        await RegisterUser(unfollowedAuthorId, "feed-unfollowed", "feed-unfollowed@test.com");

        // Follow one author
        await UserProjection.Apply(_dataSource, followerId,
            new UserEvent.Followed(new UserId(followedAuthorId)));

        // Create articles by both authors
        var articleByFollowed = Guid.NewGuid();
        var articleByUnfollowed = Guid.NewGuid();

        await ArticleProjection.Apply(_dataSource, articleByFollowed,
            new ArticleEvent.ArticleCreated(
                new ArticleId(articleByFollowed), new Slug("followed-article"),
                new Title("By Followed"), new Description("D"), new Body("B"),
                new HashSet<Tag>(), new UserId(followedAuthorId),
                new Timestamp(DateTimeOffset.UtcNow)));

        await ArticleProjection.Apply(_dataSource, articleByUnfollowed,
            new ArticleEvent.ArticleCreated(
                new ArticleId(articleByUnfollowed), new Slug("unfollowed-article"),
                new Title("By Unfollowed"), new Description("D"), new Body("B"),
                new HashSet<Tag>(), new UserId(unfollowedAuthorId),
                new Timestamp(DateTimeOffset.UtcNow)));

        var getFeed = QueryStore.CreateGetFeed(_dataSource);
        var feed = await getFeed(followerId, 20, 0);

        await Assert.That(feed.ArticlesCount).IsEqualTo(1);
        await Assert.That(feed.Articles).Count().IsEqualTo(1);
        await Assert.That(feed.Articles[0].Slug).IsEqualTo("followed-article");
        await Assert.That(feed.Articles[0].Author.Following).IsTrue();
    }

    [Test]
    public async Task GetTags_returns_all_unique_tags()
    {
        var userId = Guid.NewGuid();
        await RegisterUser(userId, "tags-author", "tags-author@test.com");

        var a1 = Guid.NewGuid();
        var a2 = Guid.NewGuid();

        await ArticleProjection.Apply(_dataSource, a1,
            new ArticleEvent.ArticleCreated(
                new ArticleId(a1), new Slug("tags-article-1"), new Title("T1"),
                new Description("D1"), new Body("B1"),
                new HashSet<Tag> { new("csharp"), new("dotnet") },
                new UserId(userId), new Timestamp(DateTimeOffset.UtcNow)));

        await ArticleProjection.Apply(_dataSource, a2,
            new ArticleEvent.ArticleCreated(
                new ArticleId(a2), new Slug("tags-article-2"), new Title("T2"),
                new Description("D2"), new Body("B2"),
                new HashSet<Tag> { new("csharp"), new("python") },
                new UserId(userId), new Timestamp(DateTimeOffset.UtcNow)));

        var getTags = QueryStore.CreateGetTags(_dataSource);
        var tags = await getTags();

        await Assert.That(tags).Contains("csharp");
        await Assert.That(tags).Contains("dotnet");
        await Assert.That(tags).Contains("python");
        await Assert.That(tags.Count).IsEqualTo(3); // csharp appears twice but should be distinct
    }

    [Test]
    public async Task ProfileUpdated_cascades_to_articles_and_comments()
    {
        var userId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        var commentId = Guid.NewGuid();

        await RegisterUser(userId, "cascade-author", "cascade@test.com");
        await CreateArticle(articleId, userId, "cascade-article", "Cascade Article");

        await ArticleProjection.Apply(_dataSource, articleId,
            new ArticleEvent.CommentAdded(
                new CommentId(commentId), new UserId(userId),
                new CommentBody("A comment"), new Timestamp(DateTimeOffset.UtcNow)));

        // Update the user's profile
        await UserProjection.Apply(_dataSource, userId, new UserEvent.ProfileUpdated(
            Email: Option<EmailAddress>.None,
            Username: Option<Username>.Some(new Username("new-cascade-author")),
            PasswordHash: Option<PasswordHash>.None,
            Bio: Option<Bio>.Some(new Bio("Updated bio")),
            Image: Option<ImageUrl>.None,
            UpdatedAt: new Timestamp(DateTimeOffset.UtcNow)));

        // Verify article author data was updated
        var findBySlug = QueryStore.CreateFindArticleBySlug(_dataSource);
        var article = await findBySlug("cascade-article", Option<Guid>.None);
        await Assert.That(article.IsSome).IsTrue();
        await Assert.That(article.Value.Author.Username).IsEqualTo("new-cascade-author");
        await Assert.That(article.Value.Author.Bio).IsEqualTo("Updated bio");

        // Verify comment author data was updated
        var getComments = QueryStore.CreateGetComments(_dataSource);
        var comments = await getComments("cascade-article", Option<Guid>.None);
        await Assert.That(comments).Count().IsEqualTo(1);
        await Assert.That(comments[0].Author.Username).IsEqualTo("new-cascade-author");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────

    private async Task RegisterUser(Guid userId, string username, string email)
    {
        await UserProjection.Apply(_dataSource, userId, new UserEvent.Registered(
            new UserId(userId),
            new EmailAddress(email),
            new Username(username),
            new PasswordHash("hash"),
            new Timestamp(DateTimeOffset.UtcNow)));
    }

    private async Task CreateArticle(Guid articleId, Guid authorId, string slug, string title)
    {
        await ArticleProjection.Apply(_dataSource, articleId, new ArticleEvent.ArticleCreated(
            new ArticleId(articleId),
            new Slug(slug),
            new Title(title),
            new Description("Description"),
            new Body("Body content"),
            new HashSet<Tag>(),
            new UserId(authorId),
            new Timestamp(DateTimeOffset.UtcNow)));
    }
}
