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

public class CombinedArticleCommentJourneyTests
{
    [Fact]
    public async Task Article_SubmitComment_UIToApiToState()
    {
        // Arrange: fake API
        var handler = new FakeHttpMessageHandler { StrictMode = true };

        // Add comment endpoint
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
                    body = "Hello",
                    author = new { username = "alice", bio = "", image = "", following = false }
                }
            });

        // Follow-up command after CommentSubmitted
        handler.When(
            HttpMethod.Get,
            "/api/articles/a/comments",
            HttpStatusCode.OK,
            new
            {
                comments = new[]
                {
                    new
                    {
                        id = 1,
                        createdAt = "2020-01-01T00:00:00.000Z",
                        updatedAt = "2020-01-01T00:00:00.000Z",
                        body = "Hello",
                        author = new { username = "alice", bio = "", image = "", following = false }
                    }
                }
            });

    // Note: SubmitCommentCommand in Main.HandleCommand only POSTs and dispatches CommentSubmitted.

        var httpClient = new HttpClient(handler) { BaseAddress = new System.Uri("http://fake") };
        ApiClient.ConfigureHttpClient(httpClient);
        ApiClient.ConfigureBaseUrl("http://fake/api");

        // Model is already on article page and logged in
        var article = new Abies.Conduit.Page.Home.Article(
            Slug: "a",
            Title: "T",
            Description: "D",
            Body: "B",
            TagList: [],
            CreatedAt: "2020-01-01",
            UpdatedAt: "2020-01-01",
            Favorited: false,
            FavoritesCount: 0,
            Author: new Abies.Conduit.Page.Home.Profile("bob", "", "", Following: false));

        Abies.Conduit.Page.Article.Model model = new(
            Slug: new Slug("a"),
            IsLoading: false,
            Article: article,
            Comments: [],
            CommentInput: "",
            SubmittingComment: false,
            CurrentUser: new User(new UserName("alice"), new Email("alice@x"), new Token("t"), Image: "", Bio: ""));

        // Act 1: type comment
        var (m1, _) = MvuDomTestHarness.DispatchInput(
            model,
            Abies.Conduit.Page.Article.Page.View,
            Abies.Conduit.Page.Article.Page.Update,
            el => el.Tag == "textarea" && el.Attributes.Any(a => a.Name == "placeholder" && a.Value == "Write a comment..."),
            value: "Hello");

        // Act 2: click submit -> message + command
        // (we re-dispatch the exact same message through the loop runtime to avoid bias)
        var clickDom = Abies.Conduit.Page.Article.Page.View(m1);
        var (_, submitHandler) = MvuDomTestHarness.FindFirstHandler(
            clickDom,
            "click",
            el => el.Tag == "button" && el.Children.OfType<Text>().Any(t => t.Value == "Post Comment"));

        Assert.NotNull(submitHandler.Command);
        var submitMsg = (Abies.Message)submitHandler.Command!;

        var (m2, cmd) = Abies.Conduit.Page.Article.Page.Update(submitMsg, m1);

        Assert.True(m2.SubmittingComment);
        Assert.NotNull(cmd);

        // Act 3: run the interaction + all follow-ups to quiescence.
        var result = await MvuLoopRuntime.RunUntilQuiescentAsync(
            initialModel: m1,
            update: Abies.Conduit.Page.Article.Page.Update,
            initialMessage: submitMsg,
            options: new MvuLoopRuntime.Options(MaxIterations: 50, StrictUnhandledMessages: true, RequireQuiescence: true));

    // Assert: the correct HTTP flow happened
        Assert.Contains(handler.Requests, r =>
            r.Method == HttpMethod.Post && r.Uri.PathAndQuery == "/api/articles/a/comments");
        Assert.Contains(handler.Requests, r =>
            r.Method == HttpMethod.Get && r.Uri.PathAndQuery == "/api/articles/a/comments");

        // Avoid false positives: no unexpected requests
        Assert.DoesNotContain(handler.Requests, r =>
            r.Uri.PathAndQuery != "/api/articles/a/comments");

    // Assert: the final state includes the comment
        Assert.NotNull(result.Model.Comments);
        Assert.Contains(result.Model.Comments!, c => c.Id == "1" && c.Body == "Hello");

    // Guard against biased false positives: ensure we actually executed work.
        Assert.True(result.TotalCommandsExecuted > 0);
        Assert.True(result.TotalMessagesDispatched > 0);
    }
}
