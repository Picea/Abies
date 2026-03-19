using Microsoft.Playwright;
using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;

namespace Picea.Abies.Conduit.Testing.E2E;

[Category("E2E")]
[ClassDataSource<ConduitAppFixture>(Shared = SharedType.Keyed, Key = "Conduit")]
[NotInParallel("Conduit")]
public sealed class UserFetchErrorsTests : IAsyncInitializer, IAsyncDisposable
{
    private readonly ConduitAppFixture _fixture;
    private IPage _page = null!;

    public UserFetchErrorsTests(ConduitAppFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync() => _page = await _fixture.CreatePageAsync();

    public async ValueTask DisposeAsync() => await _page.Context.DisposeAsync();

    [Test]
    public async Task InvalidPersistedSession_ShouldClearStorageAndShowLoggedOutState()
    {
        await _page.GotoAsync("/");
        await _page.EvaluateAsync("() => window.sessionStorage.setItem('conduit.session', 'not-a-valid-session')");
        await _page.ReloadAsync();
        await _page.WaitForWasmReady();

        await Expect(_page.Locator("nav.navbar")).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(_page.Locator("a[href='/login']")).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(_page.Locator("a[href='/register']")).ToBeVisibleAsync(new() { Timeout = 10000 });

        var sessionValue = await _page.EvaluateAsync<string?>("() => window.sessionStorage.getItem('conduit.session')");
        await Assert.That(sessionValue).IsNull();
    }

    [Test]
    public async Task ProtectedRoutes_WithNoPersistedSession_ShouldNotRenderProtectedContentAfterReload()
    {
        await _page.GotoAsync("/settings");
        await _page.WaitForWasmReady();
        await _page.ReloadAsync();
        await _page.WaitForWasmReady();

        await Expect(_page.Locator("nav.navbar")).ToBeVisibleAsync(new() { Timeout = 10000 });
        await Expect(_page.Locator(".settings-page")).ToHaveCountAsync(0, new() { Timeout = 10000 });

        await _page.GotoAsync("/editor");
        await _page.WaitForWasmReady();
        await Expect(_page.Locator(".editor-page")).ToHaveCountAsync(0, new() { Timeout = 10000 });
    }

    private static ILocatorAssertions Expect(ILocator locator) => Assertions.Expect(locator);
}
