// =============================================================================
// Authentication E2E Tests — InteractiveAuto Mode
// =============================================================================

using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;
using Microsoft.Playwright;

namespace Picea.Abies.Conduit.Testing.E2E;

[Trait("Category", "E2E")]
[Collection("ConduitAuto")]
public sealed class AuthenticationAutoTests : IAsyncLifetime
{
    private readonly ConduitAutoFixture _fixture;
    private IPage _page = null!;
    private ApiSeeder _seeder = null!;

    public AuthenticationAutoTests(ConduitAutoFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
        _seeder = new ApiSeeder(_fixture.ApiUrl);
    }

    public async Task DisposeAsync() => await _page.Context.DisposeAsync();

    [Fact]
    public async Task Register_WithValidCredentials_ShouldNavigateToHomeWithAuthenticatedNav()
    {
        await _page.GotoAsync("/register");
        await _page.WaitForWasmReady();
        await _page.WaitForSelectorAsync("h1:has-text('Sign up')");

        var uniqueName = $"autoreguser{Guid.NewGuid():N}"[..20];
        var email = $"{uniqueName}@test.com";

        await _page.GetByPlaceholder("Your Name").FillAsync(uniqueName);
        await _page.GetByPlaceholder("Email").FillAsync(email);
        await _page.GetByPlaceholder("Password").FillAsync("password123");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Sign up" }).ClickAsync();

        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });

        await Expect(_page.Locator(".navbar").GetByText(uniqueName))
            .ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(_page.Locator(".navbar")).ToContainTextAsync("Settings");
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldNavigateToHomeWithAuthenticatedNav()
    {
        var username = $"autologin{Guid.NewGuid():N}"[..20];
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

    [Fact]
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

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
