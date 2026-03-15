// =============================================================================
// Article Decider Tests — Command Validation and State Transitions
// =============================================================================
// Tests the Article Decider's Decide and Transition functions. Each test
// follows the pattern: Given(state) → When(command) → Then(events|error).
// =============================================================================

using Picea.Abies.Conduit.Domain.Article;
using Picea.Abies.Conduit.Domain.Shared;

namespace Picea.Abies.Conduit.Tests;

public class ArticleDeciderTests
{
    private static readonly UserId AuthorId = UserId.New();
    private static readonly UserId OtherUserId = UserId.New();
    private static readonly Timestamp Now = Timestamp.Now();
    private static readonly ArticleId ArticleId = ArticleId.New();

    private static readonly Title Title = new("Hello World");
    private static readonly Description Description = new("A test article.");
    private static readonly Body Body = new("# Hello\n\nThis is the body.");
    private static readonly IReadOnlySet<Tag> Tags =
        new HashSet<Tag> { new("csharp"), new("dotnet") };

    // =========================================================================
    // Initialize
    // =========================================================================

    [Test]
    public async Task Initialize_ReturnsUnpublishedState()
    {
        var (state, effect) = Article.Initialize(Unit.Value);

        await Assert.That(state.Published).IsFalse();
        await Assert.That(state.Deleted).IsFalse();
        await Assert.That(effect).IsTypeOf<ArticleEffect.None>();
    }

    // =========================================================================
    // CreateArticle
    // =========================================================================

    [Test]
    public async Task Decide_CreateArticle_WhenUnpublished_ProducesArticleCreatedEvent()
    {
        var state = ArticleState.Initial;
        var command = new ArticleCommand.CreateArticle(
            ArticleId, Title, Description, Body, Tags, AuthorId, Now);

        var result = Article.Decide(state, command);

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).Count().IsEqualTo(1);
        var e = await Assert.That(result.Value[0]).IsTypeOf<ArticleEvent.ArticleCreated>();
        await Assert.That(e.Id).IsEqualTo(ArticleId);
        await Assert.That(e.Title).IsEqualTo(Title);
        await Assert.That(e.Description).IsEqualTo(Description);
        await Assert.That(e.Body).IsEqualTo(Body);
        await Assert.That(e.AuthorId).IsEqualTo(AuthorId);
        await Assert.That(e.CreatedAt).IsEqualTo(Now);
        await Assert.That(e.Slug.Value).IsEqualTo("hello-world");
    }

    [Test]
    public async Task Decide_CreateArticle_WhenAlreadyPublished_ReturnsError()
    {
        var state = PublishedState();
        var command = new ArticleCommand.CreateArticle(
            ArticleId.New(), Title, Description, Body, Tags, AuthorId, Now);

        var result = Article.Decide(state, command);

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsTypeOf<ArticleError.AlreadyPublished>();
    }

    [Test]
    public async Task Transition_ArticleCreated_SetsAllFields()
    {
        var state = ArticleState.Initial;
        var slug = Slug.FromTitle(Title);
        var e = new ArticleEvent.ArticleCreated(
            ArticleId, slug, Title, Description, Body, Tags, AuthorId, Now);

        var (newState, _) = Article.Transition(state, e);

        await Assert.That(newState.Id).IsEqualTo(ArticleId);
        await Assert.That(newState.Slug).IsEqualTo(slug);
        await Assert.That(newState.Title).IsEqualTo(Title);
        await Assert.That(newState.Description).IsEqualTo(Description);
        await Assert.That(newState.Body).IsEqualTo(Body);
        await Assert.That(newState.Tags).IsEqualTo(Tags);
        await Assert.That(newState.AuthorId).IsEqualTo(AuthorId);
        await Assert.That(newState.Published).IsTrue();
        await Assert.That(newState.Deleted).IsFalse();
    }

    // =========================================================================
    // Commands before creation
    // =========================================================================

    [Test]
    public async Task Decide_UpdateArticle_WhenUnpublished_ReturnsNotPublished()
    {
        var state = ArticleState.Initial;
        var command = new ArticleCommand.UpdateArticle(
            Option<Title>.None, Option<Description>.None,
            Option<Body>.None, AuthorId, Now);

        var result = Article.Decide(state, command);

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsTypeOf<ArticleError.NotPublished>();
    }

    // =========================================================================
    // UpdateArticle
    // =========================================================================

    [Test]
    public async Task Decide_UpdateArticle_ByAuthor_ProducesArticleUpdatedEvent()
    {
        var state = PublishedState();
        var newTitle = new Title("Updated Title");
        var command = new ArticleCommand.UpdateArticle(
            Option.Some(newTitle), Option<Description>.None,
            Option<Body>.None, AuthorId, Now);

        var result = Article.Decide(state, command);

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).Count().IsEqualTo(1);
        var e = await Assert.That(result.Value[0]).IsTypeOf<ArticleEvent.ArticleUpdated>();
        await Assert.That(e.Title.IsSome).IsTrue();
        await Assert.That(e.Title.Value).IsEqualTo(newTitle);
        await Assert.That(e.Description.IsNone).IsTrue();
        await Assert.That(e.Slug.Value).IsEqualTo("updated-title");
    }

    [Test]
    public async Task Decide_UpdateArticle_ByNonAuthor_ReturnsNotAuthor()
    {
        var state = PublishedState();
        var command = new ArticleCommand.UpdateArticle(
            Option<Title>.None, Option<Description>.None,
            Option<Body>.None, OtherUserId, Now);

        var result = Article.Decide(state, command);

        await Assert.That(result.IsErr).IsTrue();
        var error = await Assert.That(result.Error).IsTypeOf<ArticleError.NotAuthor>();
        await Assert.That(error.RequesterId).IsEqualTo(OtherUserId);
    }

    [Test]
    public async Task Transition_ArticleUpdated_OnlyChangesProvidedFields()
    {
        var state = PublishedState();
        var newTitle = new Title("Updated Title");
        var newSlug = Slug.FromTitle(newTitle);
        var e = new ArticleEvent.ArticleUpdated(
            Option.Some(newTitle), Option<Description>.None,
            Option<Body>.None, newSlug, Now);

        var (newState, _) = Article.Transition(state, e);

        await Assert.That(newState.Title).IsEqualTo(newTitle);
        await Assert.That(newState.Slug).IsEqualTo(newSlug);
        await Assert.That(newState.Description).IsEqualTo(state.Description);
        await Assert.That(newState.Body).IsEqualTo(state.Body);
        await Assert.That(newState.UpdatedAt).IsEqualTo(Now);
    }

    // =========================================================================
    // DeleteArticle
    // =========================================================================

    [Test]
    public async Task Decide_DeleteArticle_ByAuthor_ProducesArticleDeletedEvent()
    {
        var state = PublishedState();
        var command = new ArticleCommand.DeleteArticle(AuthorId);

        var result = Article.Decide(state, command);

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).Count().IsEqualTo(1);
        await Assert.That(result.Value[0]).IsTypeOf<ArticleEvent.ArticleDeleted>();
    }

    [Test]
    public async Task Decide_DeleteArticle_ByNonAuthor_ReturnsNotAuthor()
    {
        var state = PublishedState();

        var result = Article.Decide(state, new ArticleCommand.DeleteArticle(OtherUserId));

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsTypeOf<ArticleError.NotAuthor>();
    }

    [Test]
    public async Task Transition_ArticleDeleted_SetsDeletedFlag()
    {
        var state = PublishedState();

        var (newState, _) = Article.Transition(state, new ArticleEvent.ArticleDeleted());

        await Assert.That(newState.Deleted).IsTrue();
    }

    [Test]
    public async Task IsTerminal_DeletedArticle_ReturnsTrue()
    {
        var state = PublishedState() with { Deleted = true };

        await Assert.That(Article.IsTerminal(state)).IsTrue();
    }

    [Test]
    public async Task IsTerminal_LiveArticle_ReturnsFalse()
    {
        await Assert.That(Article.IsTerminal(PublishedState())).IsFalse();
    }

    [Test]
    public async Task Decide_AnyCommand_WhenDeleted_ReturnsAlreadyDeleted()
    {
        var state = PublishedState() with { Deleted = true };
        var command = new ArticleCommand.FavoriteArticle(OtherUserId);

        var result = Article.Decide(state, command);

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsTypeOf<ArticleError.AlreadyDeleted>();
    }

    // =========================================================================
    // Comments
    // =========================================================================

    [Test]
    public async Task Decide_AddComment_ProducesCommentAddedEvent()
    {
        var state = PublishedState();
        var commentId = CommentId.New();
        var body = new CommentBody("Great article!");
        var command = new ArticleCommand.AddComment(commentId, OtherUserId, body, Now);

        var result = Article.Decide(state, command);

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).Count().IsEqualTo(1);
        var e = await Assert.That(result.Value[0]).IsTypeOf<ArticleEvent.CommentAdded>();
        await Assert.That(e.CommentId).IsEqualTo(commentId);
        await Assert.That(e.AuthorId).IsEqualTo(OtherUserId);
        await Assert.That(e.Body).IsEqualTo(body);
    }

    [Test]
    public async Task Transition_CommentAdded_AddsToComments()
    {
        var state = PublishedState();
        var commentId = CommentId.New();
        var body = new CommentBody("Nice!");
        var e = new ArticleEvent.CommentAdded(commentId, OtherUserId, body, Now);

        var (newState, _) = Article.Transition(state, e);

        await Assert.That(newState.Comments.ContainsKey(commentId)).IsTrue();
        await Assert.That(newState.Comments[commentId].AuthorId).IsEqualTo(OtherUserId);
        await Assert.That(newState.Comments[commentId].Body).IsEqualTo(body);
    }

    [Test]
    public async Task Decide_DeleteComment_ByCommentAuthor_ProducesEvent()
    {
        var commentId = CommentId.New();
        var state = StateWithComment(commentId, OtherUserId);

        var result = Article.Decide(state,
            new ArticleCommand.DeleteComment(commentId, OtherUserId));

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).Count().IsEqualTo(1);
        var e = await Assert.That(result.Value[0]).IsTypeOf<ArticleEvent.CommentDeleted>();
        await Assert.That(e.CommentId).IsEqualTo(commentId);
    }

    [Test]
    public async Task Decide_DeleteComment_ByNonAuthor_ReturnsNotCommentAuthor()
    {
        var commentId = CommentId.New();
        var state = StateWithComment(commentId, OtherUserId);
        var thirdUser = UserId.New();

        var result = Article.Decide(state,
            new ArticleCommand.DeleteComment(commentId, thirdUser));

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsTypeOf<ArticleError.NotCommentAuthor>();
    }

    [Test]
    public async Task Decide_DeleteComment_NotFound_ReturnsCommentNotFound()
    {
        var state = PublishedState();
        var missingId = CommentId.New();

        var result = Article.Decide(state,
            new ArticleCommand.DeleteComment(missingId, AuthorId));

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsTypeOf<ArticleError.CommentNotFound>();
    }

    [Test]
    public async Task Transition_CommentDeleted_RemovesFromComments()
    {
        var commentId = CommentId.New();
        var state = StateWithComment(commentId, OtherUserId);

        var (newState, _) = Article.Transition(state,
            new ArticleEvent.CommentDeleted(commentId));

        await Assert.That(newState.Comments.ContainsKey(commentId)).IsFalse();
    }

    // =========================================================================
    // Favorites
    // =========================================================================

    [Test]
    public async Task Decide_FavoriteArticle_ProducesArticleFavoritedEvent()
    {
        var state = PublishedState();

        var result = Article.Decide(state,
            new ArticleCommand.FavoriteArticle(OtherUserId));

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).Count().IsEqualTo(1);
        var e = await Assert.That(result.Value[0]).IsTypeOf<ArticleEvent.ArticleFavorited>();
        await Assert.That(e.UserId).IsEqualTo(OtherUserId);
    }

    [Test]
    public async Task Decide_FavoriteArticle_AlreadyFavorited_ReturnsError()
    {
        var state = PublishedState() with
        {
            FavoritedBy = new HashSet<UserId> { OtherUserId }
        };

        var result = Article.Decide(state,
            new ArticleCommand.FavoriteArticle(OtherUserId));

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsTypeOf<ArticleError.AlreadyFavorited>();
    }

    [Test]
    public async Task Transition_ArticleFavorited_AddsToFavoritedBy()
    {
        var state = PublishedState();

        var (newState, _) = Article.Transition(state,
            new ArticleEvent.ArticleFavorited(OtherUserId));

        await Assert.That(newState.FavoritedBy).Contains(OtherUserId);
    }

    [Test]
    public async Task Decide_UnfavoriteArticle_ProducesArticleUnfavoritedEvent()
    {
        var state = PublishedState() with
        {
            FavoritedBy = new HashSet<UserId> { OtherUserId }
        };

        var result = Article.Decide(state,
            new ArticleCommand.UnfavoriteArticle(OtherUserId));

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).Count().IsEqualTo(1);
        var e = await Assert.That(result.Value[0]).IsTypeOf<ArticleEvent.ArticleUnfavorited>();
        await Assert.That(e.UserId).IsEqualTo(OtherUserId);
    }

    [Test]
    public async Task Decide_UnfavoriteArticle_NotFavorited_ReturnsError()
    {
        var state = PublishedState();

        var result = Article.Decide(state,
            new ArticleCommand.UnfavoriteArticle(OtherUserId));

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsTypeOf<ArticleError.NotFavorited>();
    }

    [Test]
    public async Task Transition_ArticleUnfavorited_RemovesFromFavoritedBy()
    {
        var state = PublishedState() with
        {
            FavoritedBy = new HashSet<UserId> { OtherUserId }
        };

        var (newState, _) = Article.Transition(state,
            new ArticleEvent.ArticleUnfavorited(OtherUserId));

        await Assert.That(newState.FavoritedBy).DoesNotContain(OtherUserId);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Creates a published article state for testing post-creation commands.
    /// </summary>
    private static ArticleState PublishedState()
    {
        var (initial, _) = Article.Initialize(Unit.Value);
        var created = new ArticleEvent.ArticleCreated(
            ArticleId, Slug.FromTitle(Title), Title, Description, Body,
            Tags, AuthorId, Now);
        var (state, _) = Article.Transition(initial, created);
        return state;
    }

    /// <summary>
    /// Creates a published article state with one comment.
    /// </summary>
    private static ArticleState StateWithComment(CommentId commentId, UserId commentAuthorId)
    {
        var state = PublishedState();
        var added = new ArticleEvent.CommentAdded(
            commentId, commentAuthorId, new CommentBody("Test comment."), Now);
        var (newState, _) = Article.Transition(state, added);
        return newState;
    }
}
