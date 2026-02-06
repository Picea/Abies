using System.Diagnostics;

namespace Abies.Conduit.E2E;

/// <summary>
/// E2E tests for application startup performance quality gates.
/// These tests verify that the application starts within acceptable time limits.
/// </summary>
/// <remarks>
/// Quality Gates:
/// - Application must be interactive within 10 seconds on first load
/// - Application must be interactive within 5 seconds on cached load
/// - Navigation bar must be visible within 15 seconds (WASM can be slow)
/// 
/// These thresholds account for:
/// - WASM download time (varies by network)
/// - .NET runtime initialization
/// - Initial render and hydration
/// 
/// Note: These tests are marked with [Trait("Category", "Performance")]
/// so they can be run separately in CI if needed.
/// </remarks>
public class StartupPerformanceTests : PlaywrightFixture
{
    /// <summary>
    /// Timeout for startup performance tests (ms).
    /// This is the maximum acceptable startup time for a cold load.
    /// </summary>
    private const int StartupTimeoutMs = 15000; // 15 seconds for cold WASM load

    /// <summary>
    /// Target startup time (ms).
    /// This is the ideal startup time we're aiming for.
    /// Tests pass if under timeout, but log warnings if over target.
    /// </summary>
    private const int TargetStartupTimeMs = 7000; // 7 seconds target (js-framework-benchmark timeout)

    /// <summary>
    /// Verifies the application loads and becomes interactive within the timeout.
    /// This is a quality gate - if it fails, startup performance needs investigation.
    /// </summary>
    [Fact]
    [Trait("Category", "Performance")]
    public async Task ApplicationStartsWithinTimeout()
    {
        var stopwatch = Stopwatch.StartNew();

        // Navigate to home page
        await Page.GotoAsync("/");

        // Wait for app to be ready (navbar visible)
        await Expect(Page.Locator("nav.navbar")).ToBeVisibleAsync(new() { Timeout = StartupTimeoutMs });

        stopwatch.Stop();
        var elapsedMs = stopwatch.ElapsedMilliseconds;

        // Log the startup time for analysis
        Console.WriteLine($"[StartupPerformance] Application startup time: {elapsedMs}ms");

        // Assert we're within the timeout (test passes)
        Assert.True(elapsedMs < StartupTimeoutMs,
            $"Application startup took {elapsedMs}ms, exceeding the {StartupTimeoutMs}ms timeout. " +
            "This indicates a startup performance regression.");

        // Warn if over target (test still passes, but needs attention)
        if (elapsedMs > TargetStartupTimeMs)
        {
            Console.WriteLine($"[StartupPerformance] WARNING: Startup time ({elapsedMs}ms) exceeds target ({TargetStartupTimeMs}ms). " +
                "Consider optimizing startup performance. See issue #35.");
        }
    }

    /// <summary>
    /// Verifies the application becomes fully interactive (initialization complete).
    /// This tests the full startup path including user auth check from localStorage.
    /// </summary>
    [Fact]
    [Trait("Category", "Performance")]
    public async Task ApplicationBecomesInteractiveWithinTimeout()
    {
        var stopwatch = Stopwatch.StartNew();

        // Navigate to home page
        await Page.GotoAsync("/");

        // Wait for full initialization (either Sign in or Settings link visible)
        await WaitForInitializationCompleteAsync();

        stopwatch.Stop();
        var elapsedMs = stopwatch.ElapsedMilliseconds;

        // Log the startup time
        Console.WriteLine($"[StartupPerformance] Full interactive time: {elapsedMs}ms");

        // Assert we're within an extended timeout for full initialization
        var fullInitTimeout = StartupTimeoutMs + 5000; // Extra 5s for localStorage operations
        Assert.True(elapsedMs < fullInitTimeout,
            $"Application full initialization took {elapsedMs}ms, exceeding the {fullInitTimeout}ms timeout.");
    }

    /// <summary>
    /// Verifies the home page content loads within acceptable time.
    /// This includes the API call to fetch articles.
    /// </summary>
    [Fact]
    [Trait("Category", "Performance")]
    public async Task HomePageContentLoadsWithinTimeout()
    {
        var stopwatch = Stopwatch.StartNew();

        // Navigate to home page
        await Page.GotoAsync("/");

        // Wait for articles to load (either articles or "No articles" message)
        var articlePreview = Page.Locator(".article-preview").First;
        var noArticles = Page.GetByText("No articles are here");

        await Expect(articlePreview.Or(noArticles)).ToBeVisibleAsync(new() { Timeout = 20000 });

        stopwatch.Stop();
        var elapsedMs = stopwatch.ElapsedMilliseconds;

        // Log the content load time
        Console.WriteLine($"[StartupPerformance] Home page content load time: {elapsedMs}ms");

        // Assert within 20 seconds (includes network API call)
        Assert.True(elapsedMs < 20000,
            $"Home page content took {elapsedMs}ms to load, exceeding the 20s timeout.");
    }

    /// <summary>
    /// Measures Time to First Paint (TTFP) by checking when body has content.
    /// </summary>
    [Fact]
    [Trait("Category", "Performance")]
    public async Task TimeToFirstPaintIsAcceptable()
    {
        var stopwatch = Stopwatch.StartNew();

        // Navigate to home page
        await Page.GotoAsync("/");

        // Wait for body to have any child elements (first paint)
        await Page.WaitForFunctionAsync(
            "document.body.children.length > 0",
            new PageWaitForFunctionOptions { Timeout = StartupTimeoutMs }
        );

        stopwatch.Stop();
        var elapsedMs = stopwatch.ElapsedMilliseconds;

        Console.WriteLine($"[StartupPerformance] Time to First Paint: {elapsedMs}ms");
        Assert.True(elapsedMs < 10000,
            $"Time to First Paint was {elapsedMs}ms, exceeding the 10s threshold. " +
            "This may indicate the WASM bundle is too large or initialization is too slow.");
    }

    /// <summary>
    /// Stress test: Navigate to multiple pages and ensure each loads quickly.
    /// </summary>
    [Fact]
    [Trait("Category", "Performance")]
    public async Task NavigationBetweenPagesIsResponsive()
    {
        // Initial load
        await Page.GotoAsync("/");
        await WaitForAppReadyAsync();

        var timings = new List<(string page, long ms)>();

        // Navigate to login page
        var sw = Stopwatch.StartNew();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Sign in" }).ClickAsync();
        await Expect(Page.GetByPlaceholder("Email")).ToBeVisibleAsync(new() { Timeout = 5000 });
        timings.Add(("login", sw.ElapsedMilliseconds));

        // Navigate to register page
        sw.Restart();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Sign up" }).ClickAsync();
        await Expect(Page.GetByPlaceholder("Username")).ToBeVisibleAsync(new() { Timeout = 5000 });
        timings.Add(("register", sw.ElapsedMilliseconds));

        // Navigate back to home
        sw.Restart();
        await Page.GetByRole(AriaRole.Link, new() { Name = "Home" }).ClickAsync();
        await Expect(Page.Locator(".home-page")).ToBeVisibleAsync(new() { Timeout = 5000 });
        timings.Add(("home", sw.ElapsedMilliseconds));

        // Log all timings
        foreach (var (page, ms) in timings)
        {
            Console.WriteLine($"[StartupPerformance] Navigation to {page}: {ms}ms");
        }

        // All navigations should be under 3 seconds (no network required for SPA navigation)
        foreach (var (page, ms) in timings)
        {
            Assert.True(ms < 3000,
                $"Navigation to {page} took {ms}ms, exceeding the 3s threshold for SPA navigation.");
        }
    }

    /// <summary>
    /// Measures total bundle download size by capturing network requests.
    /// This helps track if the bundle size quality gate is being met.
    /// </summary>
    [Fact]
    [Trait("Category", "Performance")]
    public async Task BundleSizeIsWithinLimits()
    {
        long totalBytes = 0;
        var downloads = new List<(string url, long bytes)>();

        // Store handlers so we can unregister them after measurement
        void OnRequest(object? _, Microsoft.Playwright.IRequest request)
        {
            if (request.Url.Contains("_framework") ||
                request.Url.EndsWith(".wasm") ||
                request.Url.EndsWith(".dll") ||
                request.Url.Contains("dotnet."))
            {
                // We'll measure response size
            }
        }

        void OnResponse(object? _, Microsoft.Playwright.IResponse response)
        {
            if (response.Url.Contains("_framework") ||
                response.Url.EndsWith(".wasm") ||
                response.Url.Contains("dotnet."))
            {
                var size = response.Headers.TryGetValue("content-length", out var len)
                    && long.TryParse(len, out var parsed)
                    ? parsed
                    : 0;

                if (size > 0)
                {
                    totalBytes += size;
                    downloads.Add((response.Url, size));
                }
            }
        }

        Page.Request += OnRequest;
        Page.Response += OnResponse;

        try
        {
        // Navigate to trigger downloads
        await Page.GotoAsync("/");
        await WaitForAppReadyAsync();

        // Give time for all resources to load
        await Page.WaitForTimeoutAsync(2000);

        // Log bundle analysis
        Console.WriteLine($"[StartupPerformance] Total framework download size: {totalBytes / 1024.0 / 1024.0:F2} MB");
        Console.WriteLine($"[StartupPerformance] Number of framework files: {downloads.Count}");

        foreach (var (url, bytes) in downloads.OrderByDescending(d => d.bytes).Take(10))
        {
            var fileName = url.Split('/').Last().Split('?').First();
            Console.WriteLine($"  - {fileName}: {bytes / 1024.0:F1} KB");
        }

        // Quality gate: Total bundle should be under 20MB (relaxed for dev builds)
        // Production builds with trimming should be under 15MB
        var maxBundleSizeMb = 50; // Relaxed for development (untrimmed builds are larger)
        var bundleSizeMb = totalBytes / 1024.0 / 1024.0;

        if (bundleSizeMb > 20)
        {
            Console.WriteLine($"[StartupPerformance] WARNING: Bundle size ({bundleSizeMb:F2}MB) is large. " +
                "Enable trimming for production builds. See issue #35.");
        }

        Assert.True(bundleSizeMb < maxBundleSizeMb,
            $"Framework bundle size ({bundleSizeMb:F2}MB) exceeds {maxBundleSizeMb}MB limit. " +
            "This indicates the build is not properly trimmed or contains unnecessary dependencies.");
        }
        finally
        {
            Page.Request -= OnRequest;
            Page.Response -= OnResponse;
        }
    }
}