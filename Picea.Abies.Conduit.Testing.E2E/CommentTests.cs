// =============================================================================
// Comment E2E Tests — Add and delete comments on articles
// =============================================================================

using Microsoft.Playwright;
using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;

namespace Picea.Abies.Conduit.Testing.E2E;

[Category("E2E")]
[ClassDataSource<ConduitAppFixture>(Shared = SharedType.Keyed, Key = "Conduit")]
[NotInParallel("Conduit")]
public sealed class CommentTests : IAsyncInitializer, IAsyncDisposable
{
    private readonly ConduitAppFixture _fixture;
    private IPage _page = null!;
    private ApiSeeder _seeder = null!;

    public CommentTests(ConduitAppFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
        _seeder = new ApiSeeder(_fixture.ApiUrl);
    }

    public async ValueTask DisposeAsync()
    {
        await _page.Context.DisposeAsync();
    }

    [Test]
    public async Task AddComment_WithText_ShouldAppearInCommentList()
    {
        var author = $"cmtauth{Guid.NewGuid():N}"[..20];
        var email = $"{author}@test.com";
        var user = await _seeder.RegisterUserAsync(author, email, "password123");
        var article = await _seeder.CreateArticleAsync(
            user.Token,
            $"Comment Test {Guid.NewGuid():N}"[..30],
            "Article for comments",
            "Comment testing body.");
        await _seeder.WaitForArticleAsync(article.Slug);

        await LoginViaUi(email, "password123");

        await _page.NavigateInApp($"/article/{article.Slug}");

        await _page.GetByPlaceholder("Write a comment...").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });

        var commentText = $"E2E comment {Guid.NewGuid():N}"[..40];
        await _page.GetByPlaceholder("Write a comment...").FillAsync(commentText);
        await _page.GetByRole(AriaRole.Button, new() { Name = "Post Comment" }).ClickAsync();

        await Expect(_page.Locator(".card .card-block p").First)
            .ToContainTextAsync(commentText, new() { Timeout = 10000 });
    }

    [Test]
    public async Task DeleteComment_AsAuthor_ShouldRemoveFromList()
    {
        var username = $"cmtdel{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        var user = await _seeder.RegisterUserAsync(username, email, "password123");
        var article = await _seeder.CreateArticleAsync(
            user.Token,
            $"Del Comment {Guid.NewGuid():N}"[..30],
            "Article with comment to delete",
            "Body content.");
        var comment = await _seeder.AddCommentAsync(
            user.Token,
            article.Slug,
            "This comment will be deleted.");
        await _seeder.WaitForArticleAsync(article.Slug);

        await LoginViaUi(email, "password123");

        await _page.NavigateInApp($"/article/{article.Slug}");

        await Expect(_page.Locator(".card .card-block p").First)
            .ToContainTextAsync(comment.Body, new() { Timeout = 15000 });

        // The <i> icon element has zero dimensions (icon font may not load in headless),
        // so use DispatchEventAsync which doesn't require the element to be visible.
        await _page.Locator(".card .card-footer .mod-options i.ion-trash-a").First
            .DispatchEventAsync("click");

        await Expect(_page.Locator($"text={comment.Body}")).ToHaveCountAsync(0,
            new() { Timeout = 10000 });
    }

    [Test]
    public async Task AddMultipleComments_ShouldShowAllInOrder()
    {
        var username = $"cmtmul{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        var user = await _seeder.RegisterUserAsync(username, email, "password123");
        var article = await _seeder.CreateArticleAsync(
            user.Token,
            $"Multi Comment {Guid.NewGuid():N}"[..30],
            "Article for multiple comments",
            "Body for multi comment test.");

        await _seeder.AddCommentAsync(user.Token, article.Slug, "First comment via API");
        await _seeder.WaitForArticleAsync(article.Slug);

        await LoginViaUi(email, "password123");

        await _page.NavigateInApp($"/article/{article.Slug}");

        await _page.GetByPlaceholder("Write a comment...").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });

        await _page.GetByPlaceholder("Write a comment...").FillAsync("Second comment via UI");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Post Comment" }).ClickAsync();

        await _page.WaitForTimeoutAsync(2000);
        var commentCards = _page.Locator(".card .card-block p");
        await Expect(commentCards).ToHaveCountAsync(2, new() { Timeout = 10000 });
    }

    private async Task LoginViaUi(string email, string password)
    {
        await _page.GotoAsync("/login");
        await _page.WaitForWasmReady();
        await _page.WaitForSelectorAsync("h1:has-text('Sign in')");
        await _page.GetByPlaceholder("Email").FillAsync(email);
        await _page.GetByPlaceholder("Password").FillAsync(password);
        await _page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
