// =============================================================================
// Authentication E2E Tests — Register, Login, Logout
// =============================================================================

using Microsoft.Playwright;
using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;

namespace Picea.Abies.Conduit.Testing.E2E;

[Category("E2E")]
[ClassDataSource<ConduitAppFixture>(Shared = SharedType.Keyed, Key = "Conduit")]
[NotInParallel("Conduit")]
public sealed class AuthenticationTests : IAsyncInitializer, IAsyncDisposable
{
    private readonly ConduitAppFixture _fixture;
    private IPage _page = null!;
    private ApiSeeder _seeder = null!;

    public AuthenticationTests(ConduitAppFixture fixture)
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
    public async Task Register_WithValidCredentials_ShouldNavigateToHomeWithAuthenticatedNav()
    {
        await _page.GotoAsync("/register");
        await _page.WaitForWasmReady();
        await _page.WaitForSelectorAsync("h1:has-text('Sign up')");

        var uniqueName = $"e2euser{Guid.NewGuid():N}"[..20];
        var email = $"{uniqueName}@test.com";

        await _page.GetByPlaceholder("Your Name").FillAsync(uniqueName);
        await _page.GetByPlaceholder("Email").FillAsync(email);
        await _page.GetByPlaceholder("Password").FillAsync("password123");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Sign up" }).ClickAsync();

        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });

        await Expect(_page.Locator(".navbar").GetByText(uniqueName))
            .ToBeVisibleAsync(new() { Timeout = 10000 });

        await Expect(_page.Locator(".navbar")).ToContainTextAsync("Settings");
        await Expect(_page.Locator(".navbar")).ToContainTextAsync("New Article");
    }

    [Test]
    public async Task Login_WithValidCredentials_ShouldNavigateToHomeWithAuthenticatedNav()
    {
        var username = $"loginuser{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        const string password = "password123";
        await _seeder.RegisterUserAsync(username, email, password);

        await _page.GotoAsync("/login");
        await _page.WaitForWasmReady();
        await _page.WaitForSelectorAsync("h1:has-text('Sign in')");

        await _page.GetByPlaceholder("Email").FillAsync(email);
        await _page.GetByPlaceholder("Password").FillAsync(password);
        await _page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });
        await Expect(_page.Locator(".navbar")).ToContainTextAsync(username);
    }

    [Test]
    public async Task Login_WithInvalidCredentials_ShouldShowErrors()
    {
        await _page.GotoAsync("/login");
        await _page.WaitForWasmReady();
        await _page.WaitForSelectorAsync("h1:has-text('Sign in')");

        await _page.GetByPlaceholder("Email").FillAsync("nonexistent@test.com");
        await _page.GetByPlaceholder("Password").FillAsync("wrongpassword");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        await Expect(_page.Locator(".error-messages")).ToBeVisibleAsync(
            new() { Timeout = 10000 });
    }

    [Test]
    public async Task Logout_FromSettings_ShouldClearSessionAndNavigateToHome()
    {
        var username = $"logoutuser{Guid.NewGuid():N}"[..20];
        var email = $"{username}@test.com";
        await _seeder.RegisterUserAsync(username, email, "password123");

        await _page.GotoAsync("/login");
        await _page.WaitForWasmReady();
        await _page.WaitForSelectorAsync("h1:has-text('Sign in')");
        await _page.GetByPlaceholder("Email").FillAsync(email);
        await _page.GetByPlaceholder("Password").FillAsync("password123");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });

        await _page.Locator(".navbar").GetByText("Settings").ClickAsync();
        await _page.WaitForSelectorAsync(".settings-page", new() { Timeout = 10000 });

        await _page.GetByRole(AriaRole.Button, new() { Name = "Or click here to logout." })
            .ClickAsync();

        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 10000 });
        await Expect(_page.Locator(".navbar")).ToContainTextAsync("Sign in");
        await Expect(_page.Locator(".navbar")).ToContainTextAsync("Sign up");
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);

    private static IPageAssertions Expect(IPage page) =>
        Assertions.Expect(page);
}
