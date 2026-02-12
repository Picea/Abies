using System.Net;
using Abies.Conduit.IntegrationTests.Testing;
using Abies.Conduit.Main;
using Abies.Conduit.Services;
using Abies.DOM;
using Xunit;
using ArticleMessage = Abies.Conduit.Page.Article.Message;
using ArticleModel = Abies.Conduit.Page.Article.Model;
using ArticlePage = Abies.Conduit.Page.Article.Page;
using Comment = Abies.Conduit.Page.Article.Comment;

namespace Abies.Conduit.IntegrationTests;

/// <summary>
/// Tests for multiple comments functionality - verifies that adding multiple comments
/// correctly displays all comments without overwriting or hiding previous ones.
/// Related to ADR-016: ID-Based DOM Diffing for Dynamic Lists
/// </summary>
public class MultiCommentJourneyTests
{
    [Fact]
    public void CommentList_MultipleComments_AllCommentsRenderedWithUniqueIds()
    {
        // Arrange: model with multiple comments
        var article = new Page.Home.Article(
            Slug: "test-article",
            Title: "Test Article",
            Description: "Description",
            Body: "Body",
            TagList: [],
            CreatedAt: "2020-01-01",
            UpdatedAt: "2020-01-01",
            Favorited: false,
            FavoritesCount: 0,
            Author: new Page.Home.Profile("author", "", "", Following: false));

        var comments = new List<Comment>
        {
            new("1", "2020-01-01T00:00:00Z", "2020-01-01T00:00:00Z", "First comment",
                new Page.Home.Profile("alice", "", "", false)),
            new("2", "2020-01-02T00:00:00Z", "2020-01-02T00:00:00Z", "Second comment",
                new Page.Home.Profile("bob", "", "", false)),
            new("3", "2020-01-03T00:00:00Z", "2020-01-03T00:00:00Z", "Third comment",
                new Page.Home.Profile("charlie", "", "", false))
        };

        var model = new ArticleModel(
            Slug: new Slug("test-article"),
            IsLoading: false,
            Article: article,
            Comments: comments,
            CommentInput: "",
            SubmittingComment: false,
            CurrentUser: new User(new UserName("viewer"), new Email("viewer@x"), new Token("t"), "", ""));

        // Act: render the view
        var view = ArticlePage.View(model);

        // Assert: find all comment cards with class="card" inside the comment section
        var commentCards = FindAllCommentCards(view);

        // Should have exactly 3 comment cards
        Assert.Equal(3, commentCards.Count);

        // Each comment card should have a unique ID (per ADR-016)
        var ids = commentCards.Select(c => c.Id).ToList();
        Assert.Equal(3, ids.Distinct().Count());

        // IDs should be based on comment IDs for stable keying (format: "comment-{id}")
        Assert.Contains("comment-1", ids);
        Assert.Contains("comment-2", ids);
        Assert.Contains("comment-3", ids);

        // All comment bodies should be present
        var allText = ExtractAllText(view);
        Assert.Contains("First comment", allText);
        Assert.Contains("Second comment", allText);
        Assert.Contains("Third comment", allText);
    }

    [Fact]
    public void CommentList_AddSecondComment_BothCommentsVisible()
    {
        // Arrange: start with one comment
        var article = new Page.Home.Article(
            Slug: "test-article",
            Title: "Test Article",
            Description: "Description",
            Body: "Body",
            TagList: [],
            CreatedAt: "2020-01-01",
            UpdatedAt: "2020-01-01",
            Favorited: false,
            FavoritesCount: 0,
            Author: new Page.Home.Profile("author", "", "", Following: false));

        var initialModel = new ArticleModel(
            Slug: new Slug("test-article"),
            IsLoading: false,
            Article: article,
            Comments: [new Comment("1", "2020-01-01T00:00:00Z", "2020-01-01T00:00:00Z", "First comment",
                new Page.Home.Profile("alice", "", "", false))],
            CommentInput: "",
            SubmittingComment: false,
            CurrentUser: new User(new UserName("viewer"), new Email("viewer@x"), new Token("t"), "", ""));

        // Act: simulate adding a second comment via CommentSubmitted message
        var newComment = new Comment("2", "2020-01-02T00:00:00Z", "2020-01-02T00:00:00Z", "Second comment",
            new Page.Home.Profile("bob", "", "", false));

        var (modelAfterSubmit, _) = ArticlePage.Update(
            new ArticleMessage.CommentSubmitted(newComment),
            initialModel);

        // Assert: model should now have both comments
        Assert.NotNull(modelAfterSubmit.Comments);
        Assert.Equal(2, modelAfterSubmit.Comments!.Count);
        Assert.Contains(modelAfterSubmit.Comments, c => c.Body == "First comment");
        Assert.Contains(modelAfterSubmit.Comments, c => c.Body == "Second comment");

        // Render the view and verify both comments are visible
        var view = ArticlePage.View(modelAfterSubmit);
        var allText = ExtractAllText(view);
        Assert.Contains("First comment", allText);
        Assert.Contains("Second comment", allText);

        // Verify unique IDs (per ADR-016)
        var commentCards = FindAllCommentCards(view);
        Assert.Equal(2, commentCards.Count);
        var ids = commentCards.Select(c => c.Id).ToList();
        Assert.Equal(2, ids.Distinct().Count());
    }

    [Fact]
    public async Task AddMultipleComments_FullJourney_AllCommentsDisplayed()
    {
        // Arrange: fake API
        var handler = new FakeHttpMessageHandler { StrictMode = true };

        // First comment submission
        handler.When(
            HttpMethod.Post,
            "/api/articles/a/comments",
            HttpStatusCode.OK,
            new
            {
                comment = new
                {
                    id = 1,
                    createdAt = "2020-01-01T00:00:00.000Z",
                    updatedAt = "2020-01-01T00:00:00.000Z",
                    body = "First comment",
                    author = new { username = "alice", bio = "", image = "", following = false }
                }
            });

        // Get comments after first submission - returns only first comment
        handler.When(
            HttpMethod.Get,
            "/api/articles/a/comments",
            HttpStatusCode.OK,
            new
            {
                comments = new object[]
                {
                    new
                    {
                        id = 1,
                        createdAt = "2020-01-01T00:00:00.000Z",
                        updatedAt = "2020-01-01T00:00:00.000Z",
                        body = "First comment",
                        author = new { username = "alice", bio = "", image = "", following = false }
                    }
                }
            });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://fake") };
        ApiClient.ConfigureHttpClient(httpClient);
        ApiClient.ConfigureBaseUrl("http://fake/api");

        var article = new Page.Home.Article(
            Slug: "a",
            Title: "T",
            Description: "D",
            Body: "B",
            TagList: [],
            CreatedAt: "2020-01-01",
            UpdatedAt: "2020-01-01",
            Favorited: false,
            FavoritesCount: 0,
            Author: new Page.Home.Profile("bob", "", "", Following: false));

        var model = new ArticleModel(
            Slug: new Slug("a"),
            IsLoading: false,
            Article: article,
            Comments: [],
            CommentInput: "First comment",
            SubmittingComment: false,
            CurrentUser: new User(new UserName("alice"), new Email("alice@x"), new Token("t"), "", ""));

        // Act: submit the first comment
        var result = await MvuLoopRuntime.RunUntilQuiescentAsync(
            initialModel: model,
            update: ArticlePage.Update,
            initialMessage: new ArticleMessage.SubmitComment(),
            options: new MvuLoopRuntime.Options(MaxIterations: 50, StrictUnhandledMessages: true, RequireQuiescence: true));

        // Assert: the final state includes the comment
        Assert.NotNull(result.Model.Comments);
        Assert.Contains(result.Model.Comments!, c => c.Id == "1" && c.Body == "First comment");

        // Now verify the DOM has the comment with unique ID
        var view = ArticlePage.View(result.Model);
        var commentCards = FindAllCommentCards(view);
        Assert.Single(commentCards);

        // The comment card should have a stable ID based on comment ID (format: "comment-{id}")
        var card = commentCards[0];
        Assert.Equal("comment-1", card.Id);
    }

    private static List<Element> FindAllCommentCards(Node node)
    {
        var results = new List<Element>();
        FindCommentCardsRecursive(node, results);
        return results;
    }

    private static void FindCommentCardsRecursive(Node node, List<Element> results)
    {
        if (node is Element element)
        {
            // Check if this is a comment card by looking for ID starting with "comment-"
            if (element.Tag == "div" && element.Id.StartsWith("comment-"))
            {
                results.Add(element);
            }

            foreach (var child in element.Children)
            {
                FindCommentCardsRecursive(child, results);
            }
        }
    }

    private static string ExtractAllText(Node node)
    {
        return node switch
        {
            Text text => text.Value,
            Element element => string.Join(" ", element.Children.Select(ExtractAllText)),
            _ => ""
        };
    }
}
