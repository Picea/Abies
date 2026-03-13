using Microsoft.Playwright;
using Picea.Abies.Templates.Testing.Fixtures;

namespace Picea.Abies.Templates.Testing;

/// <summary>
/// Verifies that a project scaffolded from the <c>abies-server</c> template
/// runs correctly: serves HTML, delivers CSS, and supports full counter
/// interactivity over WebSocket.
/// </summary>
[Collection("ServerTemplate")]
[Trait("Category", "E2E")]
[Trait("Category", "Template")]
public sealed class ServerTemplateTests(ServerTemplateFixture fixture)
{
    [Fact]
    public async Task Renders_CounterPage()
    {
        var page = await fixture.CreatePageAsync();
        await page.GotoAsync(fixture.BaseUrl);

        var heading = page.Locator("h1");
        await Assertions.Expect(heading).ToBeVisibleAsync();
        await Assertions.Expect(heading).ToHaveTextAsync("Counter");
    }

    [Fact]
    public async Task Serves_Stylesheet()
    {
        using var http = new HttpClient();
        var response = await http.GetAsync($"{fixture.BaseUrl}/site.css");

        Assert.True(response.IsSuccessStatusCode,
            $"Expected 200 for /site.css but got {(int)response.StatusCode}");
        Assert.Contains("text/css",
            response.Content.Headers.ContentType?.MediaType ?? "");
    }

    [Fact]
    public async Task Initial_Count_Is_Zero()
    {
        var page = await fixture.CreatePageAsync();
        await page.GotoAsync(fixture.BaseUrl);
        await WaitForServerInteractivity(page);

        var count = page.Locator(".count");
        await Assertions.Expect(count).ToHaveTextAsync("0");
    }

    [Fact]
    public async Task Increment_Works()
    {
        var page = await fixture.CreatePageAsync();
        await page.GotoAsync(fixture.BaseUrl);
        await WaitForServerInteractivity(page);

        // The server template uses "+" text for the increment button.
        await page.Locator("button.btn", new() { HasTextString = "+" }).ClickAsync();

        var count = page.Locator(".count");
        await Assertions.Expect(count).ToHaveTextAsync("1");
    }

    [Fact]
    public async Task Decrement_Works()
    {
        var page = await fixture.CreatePageAsync();
        await page.GotoAsync(fixture.BaseUrl);
        await WaitForServerInteractivity(page);

        // The server template uses "−" (U+2212) for the button label,
        // but the counter value is rendered as a plain integer (e.g. "-1").
        await page.Locator("button.btn").First.ClickAsync();

        var count = page.Locator(".count");
        await Assertions.Expect(count).ToHaveTextAsync("-1");
    }

    [Fact]
    public async Task Reset_Works()
    {
        var page = await fixture.CreatePageAsync();
        await page.GotoAsync(fixture.BaseUrl);
        await WaitForServerInteractivity(page);

        // Increment a few times first.
        var incrementBtn = page.Locator("button.btn", new() { HasTextString = "+" });
        await incrementBtn.ClickAsync();
        await incrementBtn.ClickAsync();
        await Assertions.Expect(page.Locator(".count")).ToHaveTextAsync("2");

        // Reset.
        await page.Locator(".btn-reset").ClickAsync();
        await Assertions.Expect(page.Locator(".count")).ToHaveTextAsync("0");
    }

    [Fact]
    public async Task Multiple_Clicks_Accumulate()
    {
        var page = await fixture.CreatePageAsync();
        await page.GotoAsync(fixture.BaseUrl);
        await WaitForServerInteractivity(page);

        var incrementBtn = page.Locator("button.btn", new() { HasTextString = "+" });
        for (var i = 0; i < 5; i++)
            await incrementBtn.ClickAsync();

        await Assertions.Expect(page.Locator(".count")).ToHaveTextAsync("5");
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Waits until the WebSocket connection is established and the server-side
    /// MVU runtime is processing messages. Polls by clicking increment until
    /// the count changes, then resets.
    /// </summary>
    private static async Task WaitForServerInteractivity(IPage page)
    {
        var timeout = TimeSpan.FromSeconds(45);
        var deadline = DateTime.UtcNow + timeout;

        var count = page.Locator(".count");
        var incrementBtn = page.Locator("button.btn", new() { HasTextString = "+" });

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
                    var resetBtn = page.Locator(".btn-reset");
                    await resetBtn.ClickAsync(
                        new LocatorClickOptions { Timeout = 2_000 });
                    await Assertions.Expect(count)
                        .ToHaveTextAsync("0", new() { Timeout = 2_000 });
                    return;
                }
            }
            catch (TimeoutException)
            {
                // Not ready yet — retry.
            }

            await Task.Delay(200);
        }

        throw new TimeoutException(
            $"Server template did not become interactive within {timeout.TotalSeconds}s.");
    }
}
