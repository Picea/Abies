// =============================================================================
// Error Handling E2E Tests — Form failures, protected routes, degraded shells
// =============================================================================

using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;

namespace Picea.Abies.Conduit.Testing.E2E;

[Category("E2E")]
[ClassDataSource<ConduitAppFixture>(Shared = SharedType.Keyed, Key = "Conduit")]
[NotInParallel("Conduit")]
public sealed class ErrorHandlingTests : IAsyncInitializer, IAsyncDisposable
{
    private readonly ConduitAppFixture _fixture;
    private IPage _page = null!;
    private ApiSeeder _seeder = null!;

    public ErrorHandlingTests(ConduitAppFixture fixture)
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
    public async Task Login_WhenApiReturnsValidationErrors_ShouldShowErrorAndStayOnLogin()
    {
        await _page.RouteAsync("**/api/users/login", route =>
            route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 401,
                ContentType = "application/json",
                Body = """
                {"errors":{"body":["email or password is invalid"]}}
                """
            }));

        await _page.GotoAsync("/login");
        await _page.WaitForWasmReady();
        await _page.GetByPlaceholder("Email").FillAsync("nobody@example.com");
        await _page.GetByPlaceholder("Password").FillAsync("wrongpassword");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        await Expect(_page).ToHaveURLAsync(new Regex("/login$"), new() { Timeout = 10000 });
        await Expect(_page.Locator(".error-messages")).ToContainTextAsync("email or password is invalid");
        await Expect(_page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }))
            .ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Test]
    public async Task Register_WhenNetworkFails_ShouldShowErrorAndStayOnRegister()
    {
        await _page.RouteAsync("**/api/users", route => route.AbortAsync("internetdisconnected"));

        await _page.GotoAsync("/register");
        await _page.WaitForWasmReady();
        await _page.GetByPlaceholder("Your Name").FillAsync($"user{Guid.NewGuid():N}"[..20]);
        await _page.GetByPlaceholder("Email").FillAsync($"{Guid.NewGuid():N}@test.com");
        await _page.GetByPlaceholder("Password").FillAsync("password123");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Sign up" }).ClickAsync();

        await Expect(_page).ToHaveURLAsync(new Regex("/register$"), new() { Timeout = 10000 });
        await Expect(_page.Locator(".error-messages")).ToContainTextAsync("Network error");
        await Expect(_page.GetByRole(AriaRole.Button, new() { Name = "Sign up" }))
            .ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Test]
    public async Task SettingsUpdate_WhenApiReturnsUnauthorized_ShouldShowErrorAndKeepFormUsable()
    {
        var username = $"setterr{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        await _seeder.RegisterUserAsync(username, email, "password123");
        await LoginViaUi(email, "password123");

        await _page.RouteAsync("**/api/user", async route =>
        {
            if (route.Request.Method == "PUT")
            {
                await route.FulfillAsync(new RouteFulfillOptions
                {
                    Status = 401,
                    ContentType = "application/json",
                    Body = """
                    {"errors":{"body":["Session expired"]}}
                    """
                });
                return;
            }

            await route.ContinueAsync();
        });

        await _page.Locator(".navbar").GetByText("Settings").ClickAsync();
        await _page.WaitForSelectorAsync(".settings-page", new() { Timeout = 10000 });
        await _page.GetByPlaceholder("Short bio about you").FillAsync("Updated but unauthorized");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Update Settings" }).ClickAsync();

        await Expect(_page.Locator(".error-messages")).ToContainTextAsync("Session expired", new() { Timeout = 10000 });
        await Expect(_page.GetByRole(AriaRole.Button, new() { Name = "Update Settings" }))
            .ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(_page.GetByPlaceholder("Email")).ToHaveValueAsync(email);
    }

    [Test]
    public async Task ProtectedRoutes_WhenAnonymous_ShouldNotRenderProtectedForms()
    {
        await _page.GotoAsync("/settings");
        await _page.WaitForWasmReady();

        await Expect(_page).ToHaveURLAsync(new Regex("/settings$"), new() { Timeout = 10000 });
        await Expect(_page.Locator("nav.navbar")).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(_page.Locator(".settings-page")).ToHaveCountAsync(0);

        await _page.GotoAsync("/editor");
        await _page.WaitForWasmReady();

        await Expect(_page).ToHaveURLAsync(new Regex("/editor$"), new() { Timeout = 10000 });
        await Expect(_page.Locator("nav.navbar")).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(_page.Locator(".editor-page")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task GlobalFeed_WhenArticlesRequestFails_ShouldKeepShellVisible()
    {
        await _page.RouteAsync("**/api/articles?*", route =>
            route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 500,
                ContentType = "application/json",
                Body = """
                {"errors":{"body":["temporary backend failure"]}}
                """
            }));

        await _page.GotoAsync("/");
        await _page.WaitForWasmReady();

        await Expect(_page.Locator("nav.navbar")).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(_page.Locator(".banner")).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(_page.Locator(".feed-toggle .nav-link.active"))
            .ToContainTextAsync("Global Feed", new() { Timeout = 10000 });
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

    private static IPageAssertions Expect(IPage page) =>
        Assertions.Expect(page);
}
