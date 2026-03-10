// =============================================================================
// Static Render E2E Tests — Conduit
// =============================================================================

using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;
using Microsoft.Playwright;

namespace Picea.Abies.Conduit.Testing.E2E;

[Trait("Category", "E2E")]
[Collection("ConduitStatic")]
public sealed class ConduitStaticRenderTests : IAsyncLifetime
{
    private readonly ConduitStaticFixture _fixture;
    private IPage _page = null!;
    private ApiSeeder _seeder = null!;

    public ConduitStaticRenderTests(ConduitStaticFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
        _seeder = new ApiSeeder(_fixture.ApiUrl);
    }

    public async Task DisposeAsync() => await _page.Context.DisposeAsync();

    [Fact]
    public async Task HomePage_ShouldRenderBannerAndNavbar()
    {
        await _page.GotoAsync("/");

        await Expect(_page.Locator(".banner h1")).ToContainTextAsync("conduit",
            new() { Timeout = 15000 });
        await Expect(_page.Locator(".banner p")).ToContainTextAsync(
            "A place to share your knowledge.");

        await Expect(_page.Locator(".navbar")).ToContainTextAsync("Sign in");
        await Expect(_page.Locator(".navbar")).ToContainTextAsync("Sign up");
    }

    [Fact]
    public async Task HomePage_ShouldRenderFeedToggle()
    {
        await _page.GotoAsync("/");
        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });

        await Expect(_page.Locator(".feed-toggle")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task LoginPage_ShouldRenderForm()
    {
        await _page.GotoAsync("/login");

        await Expect(_page.Locator("h1")).ToContainTextAsync("Sign in",
            new() { Timeout = 15000 });
        await Expect(_page.GetByPlaceholder("Email")).ToBeVisibleAsync();
        await Expect(_page.GetByPlaceholder("Password")).ToBeVisibleAsync();
        await Expect(_page.GetByRole(AriaRole.Button, new() { Name = "Sign in" }))
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task RegisterPage_ShouldRenderForm()
    {
        await _page.GotoAsync("/register");

        await Expect(_page.Locator("h1")).ToContainTextAsync("Sign up",
            new() { Timeout = 15000 });
        await Expect(_page.GetByPlaceholder("Your Name")).ToBeVisibleAsync();
        await Expect(_page.GetByPlaceholder("Email")).ToBeVisibleAsync();
        await Expect(_page.GetByPlaceholder("Password")).ToBeVisibleAsync();
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
