using Microsoft.Playwright;
using Picea.Abies.Templates.Testing.Fixtures;

namespace Picea.Abies.Templates.Testing;

/// <summary>
/// Verifies that a project scaffolded from the <c>abies-browser</c> template
/// runs correctly: serves HTML, delivers CSS, and supports full counter
/// interactivity via WASM.
/// </summary>
[Category("E2E"), Category("Template")]
[NotInParallel("BrowserTemplate")]
[ClassDataSource<BrowserTemplateFixture>(Shared = SharedType.Keyed, Key = "BrowserTemplate")]
public sealed class BrowserTemplateTests(BrowserTemplateFixture fixture)
{
    [Test]
    public async Task Renders_CounterPage()
    {
        var page = await fixture.CreatePageAsync();
        await page.GotoAsync(fixture.BaseUrl);

        // Dump initial page content for diagnostics.
        var content = await page.ContentAsync();
        Console.WriteLine($"[diag] Page content length: {content.Length}");
        Console.WriteLine($"[diag] Page URL: {page.Url}");
        Console.WriteLine($"[diag] First 500 chars: {content[..Math.Min(500, content.Length)]}");

        // Check if abies.js is reachable.
        var jsResponse = await page.APIRequest.GetAsync($"{fixture.BaseUrl}/abies.js");
        Console.WriteLine($"[diag] abies.js status: {jsResponse.Status}");

        // Check _framework/dotnet.js
        var dotnetJsResponse = await page.APIRequest.GetAsync($"{fixture.BaseUrl}/_framework/dotnet.js");
        Console.WriteLine($"[diag] dotnet.js status: {dotnetJsResponse.Status}");

        // Wait for WASM to load and render.
        await WaitForWasmInteractivity(page);

        var heading = page.Locator("h1");
        await Assertions.Expect(heading).ToBeVisibleAsync();
        await Assertions.Expect(heading).ToContainTextAsync("Abies Counter");

        var buttons = page.Locator(".counter .btn");
        await Assertions.Expect(buttons.Nth(0)).ToHaveTextAsync("-");
        await Assertions.Expect(buttons.Nth(1)).ToHaveTextAsync("+");
    }

    [Test]
    public async Task Initial_Count_Is_Zero()
    {
        var page = await fixture.CreatePageAsync();
        await page.GotoAsync(fixture.BaseUrl);
        await WaitForWasmInteractivity(page);

        var count = page.Locator(".counter-value");
        await Assertions.Expect(count).ToHaveTextAsync("0");
    }

    [Test]
    public async Task Increment_Works()
    {
        var page = await fixture.CreatePageAsync();
        await page.GotoAsync(fixture.BaseUrl);
        await WaitForWasmInteractivity(page);

        await page.GetByRole(AriaRole.Button, new() { Name = "Increase" }).ClickAsync();

        var count = page.Locator(".counter-value");
        await Assertions.Expect(count).ToHaveTextAsync("1");
    }

    [Test]
    public async Task Decrement_Works()
    {
        var page = await fixture.CreatePageAsync();
        await page.GotoAsync(fixture.BaseUrl);
        await WaitForWasmInteractivity(page);

        await page.GetByRole(AriaRole.Button, new() { Name = "Decrease" }).ClickAsync();

        var count = page.Locator(".counter-value");
        await Assertions.Expect(count).ToHaveTextAsync("-1");
    }

    [Test]
    public async Task Reset_Works()
    {
        var page = await fixture.CreatePageAsync();
        await page.GotoAsync(fixture.BaseUrl);
        await WaitForWasmInteractivity(page);

        var incrementBtn = page.GetByRole(AriaRole.Button, new() { Name = "Increase" });
        await incrementBtn.ClickAsync();
        await incrementBtn.ClickAsync();
        await Assertions.Expect(page.Locator(".counter-value")).ToHaveTextAsync("2");

        await page.GetByRole(AriaRole.Button, new() { Name = "Reset" }).ClickAsync();
        await Assertions.Expect(page.Locator(".counter-value")).ToHaveTextAsync("0");
    }

    [Test]
    public async Task Multiple_Clicks_Accumulate()
    {
        var page = await fixture.CreatePageAsync();
        await page.GotoAsync(fixture.BaseUrl);
        await WaitForWasmInteractivity(page);

        var incrementBtn = page.GetByRole(AriaRole.Button, new() { Name = "Increase" });
        for (var i = 0; i < 5; i++)
            await incrementBtn.ClickAsync();

        await Assertions.Expect(page.Locator(".counter-value")).ToHaveTextAsync("5");
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Waits until the WASM runtime has loaded and the MVU runtime is processing
    /// messages. Polls by clicking increment until the count changes, then resets.
    /// </summary>
    private static async Task WaitForWasmInteractivity(IPage page)
    {
        // 120 s gives adequate headroom on loaded CI runners where WASM startup
        // (4 MB bundle download + .NET runtime init) can easily take 60+ seconds.
        var timeout = TimeSpan.FromSeconds(120);
        var deadline = DateTime.UtcNow + timeout;

        var count = page.Locator(".counter-value");
        var incrementBtn = page.GetByRole(AriaRole.Button, new() { Name = "Increase" });

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await incrementBtn.ClickAsync(
                    new LocatorClickOptions { Timeout = 2_000 });
                var text = await count.TextContentAsync(
                    new LocatorTextContentOptions { Timeout = 2_000 });

                if (text is not null and not "0")
                {
                    // Interactive! Reset and return.
                    var resetBtn = page.GetByRole(AriaRole.Button, new() { Name = "Reset" });
                    await resetBtn.ClickAsync(
                        new LocatorClickOptions { Timeout = 2_000 });
                    await Assertions.Expect(count)
                        .ToHaveTextAsync("0", new() { Timeout = 5_000 });
                    return;
                }
            }
            catch (Exception)
            {
                // Not interactive yet. Catch-all is intentional: Playwright's
                // ClickAsync uses Task.WaitAsync internally which throws
                // System.TimeoutException — a type that does NOT inherit from
                // PlaywrightException — so a narrower catch silently escapes
                // the retry loop.
            }

            await Task.Delay(500);
        }

        throw new TimeoutException(
            $"Browser template did not become interactive within {timeout.TotalSeconds}s.");
    }
}
