using Microsoft.Playwright;
using Xunit;
using System.Text.RegularExpressions;

namespace Abies.Conduit.E2E;

[Collection("Conduit collection")]
public class CommentTests
{
    private readonly ConduitFixture _fixture;
    public CommentTests(ConduitFixture fixture) => _fixture = fixture;

    private static async Task WaitForLoggedInAsync(IPage page)
    {
        await page.WaitForFunctionAsync("() => window.abiesReady === true");
        await page.WaitForFunctionAsync("() => !!localStorage.getItem('jwt')", null, new() { Timeout = 60000 });
        await page.WaitForSelectorAsync("text=Settings", new() { Timeout = 60000 });
    }

    [Fact]
    public async Task CanAddAndDeleteComment()
    {
        var browser = _fixture.Browser;
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

    // Pipe browser console to test output for debugging
    page.Console += (_, msg) => System.Console.WriteLine($"[browser] {msg.Type}: {msg.Text}");

        var email = $"e2e{System.Guid.NewGuid():N}@example.com";

        // register and login
    await page.GotoAsync(_fixture.AppBaseUrl + "/register");
        await page.WaitForFunctionAsync("() => window.abiesReady === true");
    var username = $"comment{System.Guid.NewGuid():N}".Substring(0, 16);
    await page.GetByPlaceholder("Username").FillAsync(username);
    await page.GetByPlaceholder("Username").DispatchEventAsync("input");
    await page.GetByPlaceholder("Username").DispatchEventAsync("change");
    await page.GetByPlaceholder("Email").FillAsync(email);
    await page.GetByPlaceholder("Email").DispatchEventAsync("input");
    await page.GetByPlaceholder("Email").DispatchEventAsync("change");
    await page.GetByPlaceholder("Password").FillAsync("Password1!");
    await page.GetByPlaceholder("Password").DispatchEventAsync("input");
    await page.GetByPlaceholder("Password").DispatchEventAsync("change");
    await page.WaitForSelectorAsync("button:has-text('Sign up'):not([disabled])");
    await page.Locator("button:has-text('Sign up')").ClickAsync();
        await WaitForLoggedInAsync(page);

        // create article
    await page.ClickAsync("text=New Article");
    await page.WaitForSelectorAsync("input[placeholder='Article Title']");
    await page.GetByPlaceholder("Article Title").FillAsync("Comment Article");
    await page.GetByPlaceholder("Article Title").DispatchEventAsync("input");
    await page.GetByPlaceholder("Article Title").DispatchEventAsync("change");
    await page.GetByPlaceholder("What's this article about?").FillAsync("Comments");
    await page.GetByPlaceholder("What's this article about?").DispatchEventAsync("input");
    await page.GetByPlaceholder("What's this article about?").DispatchEventAsync("change");
    await page.GetByPlaceholder("Write your article (in markdown)").FillAsync("This is an E2E test article body with enough content.");
    await page.GetByPlaceholder("Write your article (in markdown)").DispatchEventAsync("input");
    await page.GetByPlaceholder("Write your article (in markdown)").DispatchEventAsync("change");
    // Add a tag to ensure full payload; tag is added on Enter
    await page.GetByPlaceholder("Enter tags").FillAsync("e2e");
    await page.GetByPlaceholder("Enter tags").DispatchEventAsync("input");
    await page.GetByPlaceholder("Enter tags").DispatchEventAsync("change");
    await page.GetByPlaceholder("Enter tags").PressAsync("Enter");
    await page.WaitForSelectorAsync(".tag-pill:has-text('e2e')");
    await page.WaitForSelectorAsync("button:has-text('Publish Article'):not([disabled])");
    await page.Locator("button:has-text('Publish Article')").ClickAsync();
    await page.WaitForSelectorAsync(".article-page .banner, .article-page .article-actions");
    await page.WaitForURLAsync(new Regex(@".*/article/[^/]+$"));

    // add comment
    var unique = $"First! {System.Guid.NewGuid():N}";
    await page.FillAsync("textarea[placeholder='Write a comment...']", unique);
    await page.Locator("textarea[placeholder='Write a comment...']").DispatchEventAsync("input");
    await page.Locator("textarea[placeholder='Write a comment...']").DispatchEventAsync("change");
    await page.WaitForSelectorAsync("button:has-text('Post Comment'):not([disabled])");
    await page.ClickAsync("button:has-text('Post Comment')");
    await page.WaitForSelectorAsync($".card .card-text:has-text('{unique}')");

        // delete comment
    var deleteResp = page.WaitForResponseAsync(r => r.Url.Contains("/api/articles/") && r.Url.Contains("/comments/") && r.Request.Method == "DELETE");
    await page.ClickAsync(".mod-options i.ion-trash-a");
    await deleteResp;
    await page.WaitForSelectorAsync($".card .card-text:has-text('{unique}')", new() { State = WaitForSelectorState.Detached, Timeout = 30000 });
    }
}
