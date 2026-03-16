// =============================================================================
// Health E2E Tests — App shell and API availability smoke coverage
// =============================================================================

using Microsoft.Playwright;
using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;

namespace Picea.Abies.Conduit.Testing.E2E;

[Category("E2E")]
[ClassDataSource<ConduitAppFixture>(Shared = SharedType.Keyed, Key = "Conduit")]
[NotInParallel("Conduit")]
public sealed class HealthTests : IAsyncInitializer, IAsyncDisposable
{
    private readonly ConduitAppFixture _fixture;
    private IPage _page = null!;

    public HealthTests(ConduitAppFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _page = await _fixture.CreatePageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _page.Context.DisposeAsync();
    }

    [Test]
    public async Task AppRoot_ShouldLoadNavbarAndBranding()
    {
        await _page.GotoAsync("/");
        await _page.WaitForWasmReady();

        await Expect(_page.Locator("a.navbar-brand")).ToContainTextAsync("conduit", new() { Timeout = 10000 });
        await Expect(_page.Locator("nav.navbar")).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(_page.Locator(".banner h1")).ToContainTextAsync("conduit", new() { Timeout = 10000 });
    }

    [Test]
    public async Task ApiTags_ShouldBeReachable()
    {
        using var http = new HttpClient();
        var response = await http.GetAsync($"{_fixture.ApiUrl}/api/tags");

        await Assert.That((int)response.StatusCode).IsEqualTo(200);
        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(body.Contains("tags", StringComparison.OrdinalIgnoreCase)).IsTrue();
    }

    [Test]
    public async Task LoginPage_ShouldLoadCoreFields()
    {
        await _page.GotoAsync("/login");
        await _page.WaitForWasmReady();

        await Expect(_page.Locator("h1")).ToContainTextAsync("Sign in", new() { Timeout = 10000 });
        await Expect(_page.GetByPlaceholder("Email")).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(_page.GetByPlaceholder("Password")).ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    [Test]
    public async Task RegisterPage_ShouldLoadCoreFields()
    {
        await _page.GotoAsync("/register");
        await _page.WaitForWasmReady();

        await Expect(_page.Locator("h1")).ToContainTextAsync("Sign up", new() { Timeout = 10000 });
        await Expect(_page.GetByPlaceholder("Your Name")).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(_page.GetByPlaceholder("Email")).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(_page.GetByPlaceholder("Password")).ToBeVisibleAsync(new() { Timeout = 10000 });
    }

    private static ILocatorAssertions Expect(ILocator locator) =>
        Assertions.Expect(locator);
}
