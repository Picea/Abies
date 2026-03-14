// =============================================================================
// InteractivityHelpers — Polling Logic for Template E2E Tests
// =============================================================================
// Detects when a template-generated app becomes interactive by clicking
// the increment button and watching for the count to change from "0".
//
// Two variants:
//   - WaitForServerInteractivity: WebSocket-based, fast (~1-5s)
//   - WaitForWasmInteractivity: WASM boot required, slower (~10-30s)
//
// After interactivity is confirmed, the counter is reset to "0" (if the
// template supports Reset) so the test starts from a clean state.
// =============================================================================

using Microsoft.Playwright;

namespace Picea.Abies.Templates.Testing.E2E.Helpers;

/// <summary>
/// Shared helpers for detecting when a template-generated app becomes interactive.
/// </summary>
public static class InteractivityHelpers
{
    /// <summary>
    /// Waits for the server template's WebSocket connection to establish and
    /// the MVU runtime to start processing events.
    /// </summary>
    public static Task WaitForServerInteractivity(IPage page) =>
        WaitForInteractivity(page, timeoutSeconds: 90, hasReset: true,
            "Server-side MVU runtime did not become interactive within 90 seconds.");

    /// <summary>
    /// Waits for the browser (WASM) template's runtime to boot and become interactive.
    /// WASM apps need to download the .NET runtime, so this is slower.
    /// </summary>
    public static Task WaitForWasmInteractivity(IPage page) =>
        WaitForInteractivity(page, timeoutSeconds: 120, hasReset: false,
            "WASM runtime did not boot within 120 seconds. " +
            "The counter did not become interactive.");

    /// <summary>
    /// Core polling loop: clicks the increment button repeatedly until the
    /// count changes from "0", indicating the MVU runtime is processing events.
    /// If no progress after 30 seconds, reloads the page to recover from
    /// stale WebSocket connections (server-side) or failed WASM boot.
    /// </summary>
    /// <param name="page">The Playwright page.</param>
    /// <param name="timeoutSeconds">Maximum wait time.</param>
    /// <param name="hasReset">Whether the template includes a Reset button.</param>
    /// <param name="timeoutMessage">Error message on timeout.</param>
    private static async Task WaitForInteractivity(
        IPage page, int timeoutSeconds, bool hasReset, string timeoutMessage)
    {
        // Wait for the counter to be visible first
        await Assertions.Expect(page.Locator(".count"))
            .ToBeVisibleAsync(new() { Timeout = 30_000 });

        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        var nextReloadAt = DateTime.UtcNow.AddSeconds(30);

        while (DateTime.UtcNow < deadline)
        {
            // If we've been polling for 30s with no success, reload the page.
            // This recovers from stale WebSocket connections or failed WASM boot.
            if (DateTime.UtcNow >= nextReloadAt)
            {
                Console.WriteLine("[InteractivityHelper] No interactivity after 30s — reloading page...");
                await page.ReloadAsync(new() { WaitUntil = WaitUntilState.Load });

                await Assertions.Expect(page.Locator(".count"))
                    .ToBeVisibleAsync(new() { Timeout = 15_000 });

                nextReloadAt = DateTime.UtcNow.AddSeconds(30);
            }

            try
            {
                await page.GetByRole(AriaRole.Button, new() { Name = "+" }).ClickAsync();
                var countText = await page.Locator(".count").TextContentAsync();

                if (countText is not null && countText != "0")
                {
                    // Runtime is interactive!
                    if (hasReset)
                    {
                        // Server template has Reset — use it to return to "0"
                        await page.GetByRole(AriaRole.Button, new() { Name = "Reset" })
                            .ClickAsync();
                        await Assertions.Expect(page.Locator(".count"))
                            .ToHaveTextAsync("0", new() { Timeout = 5_000 });
                    }
                    else
                    {
                        // Browser template has no Reset — click decrement to return to "0"
                        await page.GetByRole(AriaRole.Button, new() { Name = "\u2212" })
                            .ClickAsync();
                        await Assertions.Expect(page.Locator(".count"))
                            .ToHaveTextAsync("0", new() { Timeout = 5_000 });
                    }

                    return;
                }
            }
            catch (PlaywrightException)
            {
                // Button might not be interactive yet
            }

            await Task.Delay(500);
        }

        throw new TimeoutException(timeoutMessage);
    }
}
