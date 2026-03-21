// =============================================================================
// InteractivityHelpers — Shared Polling Logic for E2E Tests
// =============================================================================
// Provides wait-for-interactivity methods used across render modes:
//
//   - WaitForWasmInteractivity: Polls until WASM boots and the count changes
//   - WaitForServerInteractivity: Polls until WebSocket connects and events work
//
// Both methods detect interactivity the same way: click the increment button
// and watch for the count to change from "0". The difference is in timing —
// WASM needs to download and JIT (~5-15s), while InteractiveServer connects
// via WebSocket (~1-3s).
// =============================================================================

using Microsoft.Playwright;

namespace Picea.Abies.Counter.Testing.E2E.Helpers;

/// <summary>
/// Shared helpers for detecting when the Counter app becomes interactive
/// across different render modes.
/// </summary>
public static class InteractivityHelpers
{
    /// <summary>
    /// Waits for the WASM runtime to boot and the app to become interactive.
    /// The server prerender shows count "0" immediately. WASM boot replaces
    /// the DOM via OP_ADD_ROOT and re-renders. We detect interactivity by
    /// clicking the increment button and waiting for the count to change.
    /// </summary>
    /// <remarks>
    /// WASM boot includes downloading ~1MB of .NET runtime files, JIT compilation,
    /// and Abies runtime initialization. This can take 5-15 seconds depending on
    /// the machine. We use a generous timeout to avoid flakiness in CI.
    /// </remarks>
    public static Task WaitForWasmInteractivity(IPage page) =>
        WaitForInteractivity(page, timeoutSeconds: 60,
            "WASM runtime did not boot within 60 seconds. " +
            "The counter did not become interactive.");

    /// <summary>
    /// Waits for the WebSocket connection to establish and the server-side
    /// MVU runtime to start processing events. Much faster than WASM boot
    /// since no download is needed — just a WebSocket handshake.
    /// </summary>
    /// <remarks>
    /// The 45-second timeout accounts for parallel fixture startup: when all
    /// 4 render-mode fixtures initialize simultaneously (each with its own
    /// Kestrel server + Chromium instance), resource contention can delay
    /// the WebSocket handshake beyond the ~1s it takes in isolation.
    /// </remarks>
    public static Task WaitForServerInteractivity(IPage page) =>
        WaitForInteractivity(page, timeoutSeconds: 45,
            "Server-side MVU runtime did not become interactive within 45 seconds. " +
            "The WebSocket connection may have failed.");

    /// <summary>
    /// Core polling loop: clicks the increment button repeatedly until the
    /// count changes from "0", indicating the MVU runtime is processing events.
    /// Then resets the counter to "0" for the actual test.
    /// </summary>
    private static async Task WaitForInteractivity(
        IPage page, int timeoutSeconds, string timeoutMessage)
    {
        await Assertions.Expect(page.Locator(".count"))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });

        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await page.GetByRole(AriaRole.Button, new() { Name = "+" }).ClickAsync();
                var countText = await page.Locator(".count").TextContentAsync();

                if (countText is not null and not "0")
                {
                    // Runtime is interactive! Reset the count for the actual test.
                    await page.GetByRole(AriaRole.Button, new() { Name = "Reset" }).ClickAsync();
                    await Assertions.Expect(page.Locator(".count"))
                        .ToHaveTextAsync("0", new() { Timeout = 5_000 });
                    return;
                }
            }
            catch (PlaywrightException)
            {
                // Button might not be visible yet during DOM replacement
            }

            await Task.Delay(500);
        }

        throw new TimeoutException(timeoutMessage);
    }
}
