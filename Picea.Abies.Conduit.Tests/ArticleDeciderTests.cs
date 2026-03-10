// =============================================================================
// Article Decider Tests — Command Validation and State Transitions
// =============================================================================
// Tests the Article Decider's Decide and Transition functions. Each test
// follows the pattern: Given(state) → When(command) → Then(events|error).
// =============================================================================

using Picea.Abies.Conduit.Domain.Article;
using Picea.Abies.Conduit.Domain.Shared;
using Picea;

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

    [Fact]
    public void Initialize_ReturnsUnpublishedState()
    {
        var (state, effect) = Article.Initialize(Unit.Value);

        Assert.False(state.Published);
        Assert.False(state.Deleted);
        Assert.IsType<ArticleEffect.None>(effect);
    }

    // =========================================================================
    // CreateArticle
    // =========================================================================

    [Fact]
    public void Decide_CreateArticle_WhenUnpublished_ProducesArticleCreatedEvent()
    {
        var state = ArticleState.Initial;
        var command = new ArticleCommand.CreateArticle(
            ArticleId, Title, Description, Body, Tags, AuthorId, Now);

        var result = Article.Decide(state, command);

        Assert.True(result.IsOk);
        var e = Assert.IsType<ArticleEvent.ArticleCreated>(Assert.Single(result.Value));
        Assert.Equal(ArticleId, e.Id);
        Assert.Equal(Title, e.Title);
        Assert.Equal(Description, e.Description);
        Assert.Equal(Body, e.Body);
        Assert.Equal(AuthorId, e.AuthorId);
        Assert.Equal(Now, e.CreatedAt);
        Assert.Equal("hello-world", e.Slug.Value);
    }

    [Fact]
    public void Decide_CreateArticle_WhenAlreadyPublished_ReturnsError()
    {
        var state = PublishedState();
        var command = new ArticleCommand.CreateArticle(
            ArticleId.New(), Title, Description, Body, Tags, AuthorId, Now);

        var result = Article.Decide(state, command);

        Assert.True(result.IsErr);
        Assert.IsType<ArticleError.AlreadyPublished>(result.Error);
    }

    [Fact]
    public void Transition_ArticleCreated_SetsAllFields()
    {
        var state = ArticleState.Initial;
        var slug = Slug.FromTitle(Title);
        var e = new ArticleEvent.ArticleCreated(
            ArticleId, slug, Title, Description, Body, Tags, AuthorId, Now);

        var (newState, _) = Article.Transition(state, e);

        Assert.Equal(ArticleId, newState.Id);
        Assert.Equal(slug, newState.Slug);
        Assert.Equal(Title, newState.Title);
        Assert.Equal(Description, newState.Description);
        Assert.Equal(Body, newState.Body);
        Assert.Equal(Tags, newState.Tags);
        Assert.Equal(AuthorId, newState.AuthorId);
        Assert.True(newState.Published);
        Assert.False(newState.Deleted);
    }

    // =========================================================================
    // Commands before creation
    // =========================================================================

    [Fact]
    public void Decide_UpdateArticle_WhenUnpublished_ReturnsNotPublished()
    {
        var state = ArticleState.Initial;
        var command = new ArticleCommand.UpdateArticle(
            Option<Title>.None, Option<Description>.None,
            Option<Body>.None, AuthorId, Now);

        var result = Article.Decide(state, command);

        Assert.True(result.IsErr);
        Assert.IsType<ArticleError.NotPublished>(result.Error);
    }

    // =========================================================================
    // UpdateArticle
    // =========================================================================

    [Fact]
    public void Decide_UpdateArticle_ByAuthor_ProducesArticleUpdatedEvent()
    {
        var state = PublishedState();
        var newTitle = new Title("Updated Title");
        var command = new ArticleCommand.UpdateArticle(
            Option.Some(newTitle), Option<Description>.None,
            Option<Body>.None, AuthorId, Now);

        var result = Article.Decide(state, command);

        Assert.True(result.IsOk);
        var e = Assert.IsType<ArticleEvent.ArticleUpdated>(Assert.Single(result.Value));
        Assert.True(e.Title.IsSome);
        Assert.Equal(newTitle, e.Title.Value);
        Assert.True(e.Description.IsNone);
        Assert.Equal("updated-title", e.Slug.Value);
    }

    [Fact]
    public void Decide_UpdateArticle_ByNonAuthor_ReturnsNotAuthor()
    {
        var state = PublishedState();
        var command = new ArticleCommand.UpdateArticle(
            Option<Title>.None, Option<Description>.None,
            Option<Body>.None, OtherUserId, Now);

        var result = Article.Decide(state, command);

        Assert.True(result.IsErr);
        var error = Assert.IsType<ArticleError.NotAuthor>(result.Error);
        Assert.Equal(OtherUserId, error.RequesterId);
    }

    [Fact]
    public void Transition_ArticleUpdated_OnlyChangesProvidedFields()
    {
        var state = PublishedState();
        var newTitle = new Title("Updated Title");
        var newSlug = Slug.FromTitle(newTitle);
        var e = new ArticleEvent.ArticleUpdated(
            Option.Some(newTitle), Option<Description>.None,
            Option<Body>.None, newSlug, Now);

        var (newState, _) = Article.Transition(state, e);

        Assert.Equal(newTitle, newState.Title);
        Assert.Equal(newSlug, newState.Slug);
        Assert.Equal(state.Description, newState.Description);
        Assert.Equal(state.Body, newState.Body);
        Assert.Equal(Now, newState.UpdatedAt);
    }

    // =========================================================================
    // DeleteArticle
    // =========================================================================

    [Fact]
    public void Decide_DeleteArticle_ByAuthor_ProducesArticleDeletedEvent()
    {
        var state = PublishedState();
        var command = new ArticleCommand.DeleteArticle(AuthorId);

        var result = Article.Decide(state, command);

        Assert.True(result.IsOk);
        Assert.IsType<ArticleEvent.ArticleDeleted>(Assert.Single(result.Value));
    }

    [Fact]
    public void Decide_DeleteArticle_ByNonAuthor_ReturnsNotAuthor()
    {
        var state = PublishedState();

        var result = Article.Decide(state, new ArticleCommand.DeleteArticle(OtherUserId));

        Assert.True(result.IsErr);
        Assert.IsType<ArticleError.NotAuthor>(result.Error);
    }

    [Fact]
    public void Transition_ArticleDeleted_SetsDeletedFlag()
    {
        var state = PublishedState();

        var (newState, _) = Article.Transition(state, new ArticleEvent.ArticleDeleted());

        Assert.True(newState.Deleted);
    }

    [Fact]
    public void IsTerminal_DeletedArticle_ReturnsTrue()
    {
        var state = PublishedState() with { Deleted = true };

        Assert.True(Article.IsTerminal(state));
    }

    [Fact]
    public void IsTerminal_LiveArticle_ReturnsFalse()
    {
        Assert.False(Article.IsTerminal(PublishedState()));
    }

    [Fact]
    public void Decide_AnyCommand_WhenDeleted_ReturnsAlreadyDeleted()
    {
        var state = PublishedState() with { Deleted = true };
        var command = new ArticleCommand.FavoriteArticle(OtherUserId);

        var result = Article.Decide(state, command);

        Assert.True(result.IsErr);
        Assert.IsType<ArticleError.AlreadyDeleted>(result.Error);
    }

    // =========================================================================
    // Comments
    // =========================================================================

    [Fact]
    public void Decide_AddComment_ProducesCommentAddedEvent()
    {
        var state = PublishedState();
        var commentId = CommentId.New();
        var body = new CommentBody("Great article!");
        var command = new ArticleCommand.AddComment(commentId, OtherUserId, body, Now);

        var result = Article.Decide(state, command);

        Assert.True(result.IsOk);
        var e = Assert.IsType<ArticleEvent.CommentAdded>(Assert.Single(result.Value));
        Assert.Equal(commentId, e.CommentId);
        Assert.Equal(OtherUserId, e.AuthorId);
        Assert.Equal(body, e.Body);
    }

    [Fact]
    public void Transition_CommentAdded_AddsToComments()
    {
        var state = PublishedState();
        var commentId = CommentId.New();
        var body = new CommentBody("Nice!");
        var e = new ArticleEvent.CommentAdded(commentId, OtherUserId, body, Now);

        var (newState, _) = Article.Transition(state, e);

        Assert.True(newState.Comments.ContainsKey(commentId));
        Assert.Equal(OtherUserId, newState.Comments[commentId].AuthorId);
        Assert.Equal(body, newState.Comments[commentId].Body);
    }

    [Fact]
    public void Decide_DeleteComment_ByCommentAuthor_ProducesEvent()
    {
        var commentId = CommentId.New();
        var state = StateWithComment(commentId, OtherUserId);

        var result = Article.Decide(state,
            new ArticleCommand.DeleteComment(commentId, OtherUserId));

        Assert.True(result.IsOk);
        var e = Assert.IsType<ArticleEvent.CommentDeleted>(Assert.Single(result.Value));
        Assert.Equal(commentId, e.CommentId);
    }

    [Fact]
    public void Decide_DeleteComment_ByNonAuthor_ReturnsNotCommentAuthor()
    {
        var commentId = CommentId.New();
        var state = StateWithComment(commentId, OtherUserId);
        var thirdUser = UserId.New();

        var result = Article.Decide(state,
            new ArticleCommand.DeleteComment(commentId, thirdUser));

        Assert.True(result.IsErr);
        Assert.IsType<ArticleError.NotCommentAuthor>(result.Error);
    }

    [Fact]
    public void Decide_DeleteComment_NotFound_ReturnsCommentNotFound()
    {
        var state = PublishedState();
        var missingId = CommentId.New();

        var result = Article.Decide(state,
            new ArticleCommand.DeleteComment(missingId, AuthorId));

        Assert.True(result.IsErr);
        Assert.IsType<ArticleError.CommentNotFound>(result.Error);
    }

    [Fact]
    public void Transition_CommentDeleted_RemovesFromComments()
    {
        var commentId = CommentId.New();
        var state = StateWithComment(commentId, OtherUserId);

        var (newState, _) = Article.Transition(state,
            new ArticleEvent.CommentDeleted(commentId));

        Assert.False(newState.Comments.ContainsKey(commentId));
    }

    // =========================================================================
    // Favorites
    // =========================================================================

    [Fact]
    public void Decide_FavoriteArticle_ProducesArticleFavoritedEvent()
    {
        var state = PublishedState();

        var result = Article.Decide(state,
            new ArticleCommand.FavoriteArticle(OtherUserId));

        Assert.True(result.IsOk);
        var e = Assert.IsType<ArticleEvent.ArticleFavorited>(Assert.Single(result.Value));
        Assert.Equal(OtherUserId, e.UserId);
    }

    [Fact]
    public void Decide_FavoriteArticle_AlreadyFavorited_ReturnsError()
    {
        var state = PublishedState() with
        {
            FavoritedBy = new HashSet<UserId> { OtherUserId }
        };

        var result = Article.Decide(state,
            new ArticleCommand.FavoriteArticle(OtherUserId));

        Assert.True(result.IsErr);
        Assert.IsType<ArticleError.AlreadyFavorited>(result.Error);
    }

    [Fact]
    public void Transition_ArticleFavorited_AddsToFavoritedBy()
    {
        var state = PublishedState();

        var (newState, _) = Article.Transition(state,
            new ArticleEvent.ArticleFavorited(OtherUserId));

        Assert.Contains(OtherUserId, newState.FavoritedBy);
    }

    [Fact]
    public void Decide_UnfavoriteArticle_ProducesArticleUnfavoritedEvent()
    {
        var state = PublishedState() with
        {
            FavoritedBy = new HashSet<UserId> { OtherUserId }
        };

        var result = Article.Decide(state,
            new ArticleCommand.UnfavoriteArticle(OtherUserId));

        Assert.True(result.IsOk);
        var e = Assert.IsType<ArticleEvent.ArticleUnfavorited>(Assert.Single(result.Value));
        Assert.Equal(OtherUserId, e.UserId);
    }

    [Fact]
    public void Decide_UnfavoriteArticle_NotFavorited_ReturnsError()
    {
        var state = PublishedState();

        var result = Article.Decide(state,
            new ArticleCommand.UnfavoriteArticle(OtherUserId));

        Assert.True(result.IsErr);
        Assert.IsType<ArticleError.NotFavorited>(result.Error);
    }

    [Fact]
    public void Transition_ArticleUnfavorited_RemovesFromFavoritedBy()
    {
        var state = PublishedState() with
        {
            FavoritedBy = new HashSet<UserId> { OtherUserId }
        };

        var (newState, _) = Article.Transition(state,
            new ArticleEvent.ArticleUnfavorited(OtherUserId));

        Assert.DoesNotContain(OtherUserId, newState.FavoritedBy);
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
