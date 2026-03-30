// =============================================================================
// PageExtensions — Playwright Page Helpers for SPA Navigation
// =============================================================================

using Microsoft.Playwright;

namespace Picea.Abies.Conduit.Testing.E2E.Helpers;

/// <summary>
/// Extension methods for Playwright <see cref="IPage"/> to support SPA navigation.
/// </summary>
public static class PageExtensions
{
    /// <summary>
    /// Navigates within the SPA by calling history.pushState and dispatching
    /// a popstate event.
    /// </summary>
    public static async Task NavigateInApp(this IPage page, string path)
    {
        await page.EvaluateAsync(
            "path => { history.pushState(null, '', path); window.dispatchEvent(new PopStateEvent('popstate')); }",
            path);
    }

    /// <summary>
    /// Fills a form field and waits for the server-side DOM patch to settle.
    /// </summary>
    public static async Task FillAndWaitForPatch(this ILocator locator, string value, int timeoutMs = 5000)
    {
        await locator.FillAsync(value);
        await Assertions.Expect(locator).ToHaveValueAsync(value, new() { Timeout = timeoutMs });
    }

    /// <summary>
    /// Waits for the WASM runtime to finish taking over from the server-rendered page.
    /// </summary>
    public static async Task WaitForWasmReady(this IPage page, int timeoutMs = 30000)
    {
        try
        {
            await page.WaitForSelectorAsync("[data-abies-mode='wasm']",
                new() { State = WaitForSelectorState.Attached, Timeout = timeoutMs });
        }
        catch (TimeoutException)
        {
            await page.WaitForSelectorAsync(
                ".home-page, .auth-page, .article-page, .editor-page, .settings-page, .profile-page",
                new() { State = WaitForSelectorState.Attached, Timeout = timeoutMs });
        }
    }

    /// <summary>
    /// Waits until one of the known Conduit route shells is attached.
    /// </summary>
    public static Task WaitForConduitShellReady(this IPage page, int timeoutMs = 30000) =>
        page.WaitForSelectorAsync(
            ".home-page, .auth-page, .article-page, .editor-page, .settings-page, .profile-page",
            new() { State = WaitForSelectorState.Attached, Timeout = timeoutMs });

    /// <summary>
    /// Waits for authenticated UI state after login.
    /// </summary>
    public static Task WaitForAuthenticatedShell(this IPage page, int timeoutMs = 20000) =>
        page.WaitForSelectorAsync(
            "nav.navbar",
            new() { State = WaitForSelectorState.Attached, Timeout = timeoutMs });
}
