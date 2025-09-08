using Microsoft.Playwright;
using Xunit;

namespace Abies.Conduit.E2E;

[CollectionDefinition("Conduit collection")]
public class ConduitCollection : ICollectionFixture<ConduitFixture> { }

[Collection("Conduit collection")]
public class AuthenticationTests
{
    private readonly ConduitFixture _fixture;
    public AuthenticationTests(ConduitFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CanRegisterAndLogin()
    {
        var browser = _fixture.Browser;
        var context = await browser.NewContextAsync();
        await context.Tracing.StartAsync(new()
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });
        var page = await context.NewPageAsync();
        page.Console += (_, msg) => System.Console.WriteLine($"[console:{msg.Type}] {msg.Text}");
        page.PageError += (_, err) => System.Console.WriteLine($"[pageerror] {err}");
        page.RequestFailed += (_, req) => System.Console.WriteLine($"[requestfailed] {req.Method} {req.Url} - {req.Failure}");
        page.Response += async (_, resp) =>
        {
            if (!resp.Ok)
            {
                string body = string.Empty;
                try { body = await resp.TextAsync(); } catch { }
                System.Console.WriteLine($"[response] {resp.Request.Method} {resp.Url} -> {(int)resp.Status} {resp.StatusText} :: {body}");
            }
        };

        string tracePath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "auth-trace.zip");
        try
        {
            // Open home, then navigate to Register
            await page.GotoAsync(_fixture.AppBaseUrl);
            System.Console.WriteLine("[e2e] waiting for abiesReady after home");
            await page.WaitForFunctionAsync("() => window.abiesReady === true");

            await page.GotoAsync(_fixture.AppBaseUrl + "/register");
            System.Console.WriteLine("[e2e] waiting for abiesReady on /register");
            await page.WaitForFunctionAsync("() => window.abiesReady === true");
            System.Console.WriteLine($"[e2e] current url: {page.Url}");

            System.Console.WriteLine("[e2e] waiting for register form fields");
            await page.WaitForSelectorAsync("input[placeholder=Username]");
            await page.WaitForSelectorAsync("input[placeholder=Email]");
            await page.WaitForSelectorAsync("input[placeholder=Password]");

            System.Console.WriteLine("[e2e] filling register form");
            await page.GetByPlaceholder("Username").FillAsync("e2euser" + System.Guid.NewGuid().ToString("N").Substring(0, 6));
            await page.GetByPlaceholder("Username").DispatchEventAsync("input");
            await page.GetByPlaceholder("Username").DispatchEventAsync("change");
            var email = $"e2e{System.Guid.NewGuid():N}@example.com";
            await page.GetByPlaceholder("Email").FillAsync(email);
            await page.GetByPlaceholder("Email").DispatchEventAsync("input");
            await page.GetByPlaceholder("Email").DispatchEventAsync("change");
            await page.GetByPlaceholder("Password").FillAsync("Password1!");
            await page.GetByPlaceholder("Password").DispatchEventAsync("input");
            await page.GetByPlaceholder("Password").DispatchEventAsync("change");
            System.Console.WriteLine("[e2e] waiting for Sign up button enabled");
            await page.WaitForFunctionAsync(@"() => { const btn = Array.from(document.querySelectorAll('button')).find(b => b.textContent && b.textContent.trim() === 'Sign up'); return !!btn && !btn.disabled; }");
            await page.Locator("button:has-text('Sign up')").ClickAsync();
            System.Console.WriteLine("[e2e] waiting for Your Feed after register");
            await page.WaitForSelectorAsync("text=Your Feed");
            await page.ClickAsync("text=Settings");
            await page.ClickAsync("text=Or click here to logout");

            await page.GotoAsync(_fixture.AppBaseUrl + "/login");
            System.Console.WriteLine("[e2e] waiting for abiesReady on /login");
            await page.WaitForFunctionAsync("() => window.abiesReady === true");
            System.Console.WriteLine("[e2e] waiting for login form fields");
            await page.WaitForSelectorAsync("input[placeholder=Email]");
            await page.WaitForSelectorAsync("input[placeholder=Password]");
            await page.GetByPlaceholder("Email").FillAsync(email);
            await page.GetByPlaceholder("Email").DispatchEventAsync("input");
            await page.GetByPlaceholder("Email").DispatchEventAsync("change");
            await page.GetByPlaceholder("Password").FillAsync("Password1!");
            await page.GetByPlaceholder("Password").DispatchEventAsync("input");
            await page.GetByPlaceholder("Password").DispatchEventAsync("change");
            System.Console.WriteLine("[e2e] waiting for Sign in button enabled");
            await page.WaitForFunctionAsync(@"() => { const btn = Array.from(document.querySelectorAll('button')).find(b => b.textContent && b.textContent.trim() === 'Sign in'); return !!btn && !btn.disabled; }");
            await page.Locator("button:has-text('Sign in')").ClickAsync();
            System.Console.WriteLine("[e2e] waiting for Your Feed after login");
            await page.WaitForSelectorAsync("text=Your Feed");
        }
        finally
        {
            try { await context.Tracing.StopAsync(new() { Path = tracePath }); } catch { }
        }
    }
}
