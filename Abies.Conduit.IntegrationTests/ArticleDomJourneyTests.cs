using Abies.Conduit.IntegrationTests.Testing;
using Abies.Conduit.Page.Article;
using Abies.Conduit.Page.Home;
using Abies.DOM;
using Xunit;

namespace Abies.Conduit.IntegrationTests;

public class ArticleDomJourneyTests
{
    [Fact]
    public void Article_TypingCommentAndSubmitting_SetsSubmittingFlag()
    {
        // Arrange: logged-in user, article loaded
        var article = new Article(
            Slug: "a",
            Title: "T",
            Description: "D",
            Body: "B",
            TagList: [],
            CreatedAt: "2020-01-01",
            UpdatedAt: "2020-01-01",
            Favorited: false,
            FavoritesCount: 0,
            Author: new Profile("bob", "", "img", Following: false));

        var model = new Page.Article.Model(
            Slug: new Main.Slug("a"),
            IsLoading: false,
            Article: article,
            Comments: [],
            CommentInput: "",
            SubmittingComment: false,
            CurrentUser: new Main.User(
                new Main.UserName("alice"),
                new Main.Email("alice@x"),
                new Main.Token("token"),
                Bio: "",
                Image: ""));

        // Act: type comment
        var (m1, _) = MvuDomTestHarness.DispatchInput(
            model,
            Page.Article.Page.View,
            Page.Article.Page.Update,
            el => el.Tag == "textarea" && el.Attributes.Any(a => a.Name == "placeholder" && a.Value == "Write a comment..."),
            value: "Hello");

        // submit via button click (there is also onsubmit)
        var (m2, cmd) = MvuDomTestHarness.DispatchClick(
            m1,
            Page.Article.Page.View,
            Page.Article.Page.Update,
            el => el.Tag == "button" && el.Children.OfType<Text>().Any(t => t.Value == "Post Comment"));

        // Assert: update should move into submitting state
        Assert.True(m2.SubmittingComment);
        Assert.NotNull(cmd);
    }

    [Fact]
    public void Article_ClickingDeleteComment_RemovesItFromModel()
    {
        // Arrange
        var article = new Article(
            Slug: "a",
            Title: "T",
            Description: "D",
            Body: "B",
            TagList: [],
            CreatedAt: "2020-01-01",
            UpdatedAt: "2020-01-01",
            Favorited: false,
            FavoritesCount: 0,
            Author: new Profile("bob", "", "img", Following: false));

        var comment = new Comment(
            Id: "c1",
            CreatedAt: "2020-01-01",
            UpdatedAt: "2020-01-01",
            Body: "Hi",
            Author: new Profile("alice", "", "img", Following: false));

        var model = new Page.Article.Model(
            Slug: new Main.Slug("a"),
            IsLoading: false,
            Article: article,
            Comments: [comment],
            CommentInput: "",
            SubmittingComment: false,
            CurrentUser: new Main.User(
                new Main.UserName("alice"),
                new Main.Email("alice@x"),
                new Main.Token("token"),
                Bio: "",
                Image: ""));

        // Act: click trash icon
        var (m1, _) = MvuDomTestHarness.DispatchClick(
            model,
            Page.Article.Page.View,
            Page.Article.Page.Update,
            el => el.Tag == "i" && el.Attributes.Any(a => a.Name == "class" && a.Value.Contains("ion-trash-a")));

        // Follow-up: simulate server completion
        var (m2, _) = Page.Article.Page.Update(new Page.Article.Message.CommentDeleted("c1"), m1);

        // Assert
        Assert.NotNull(m2.Comments);
        Assert.DoesNotContain(m2.Comments!, c => c.Id == "c1");
    }
}
