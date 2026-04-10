using Picea.Abies.DOM;
using Microsoft.Playwright;
using Picea.Abies.Subscriptions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Picea.Abies.Testing.Tests;

public sealed class TestHarnessVisualTests
{
    [Test]
    public async Task CompareVisual_WithPlaywrightPageOverload_CreatesBaselineAndMatchesSecondRun()
    {
        var harness = TestHarness<VisualProgram, VisualModel, Unit>.Create(Unit.Value);
        var testRoot = CreateTestRoot();

        await using var playwrightResources = await PlaywrightResources.CreateAsync();

        try
        {
            var baselinePath = Path.Combine(testRoot, "baselines", "page-overload.png");
            var options = new VisualComparisonOptions(
                ViewportWidth: 320,
                ViewportHeight: 180,
                FullPage: false,
                Tolerance: VisualComparisonTolerance.Strict);

            var created = await harness.CompareVisual(playwrightResources.Page, baselinePath, options);
            var compared = await harness.CompareVisual(playwrightResources.Page, baselinePath, options);

            await Assert.That(created.BaselineCreated).IsTrue();
            await Assert.That(created.IsMatch).IsTrue();
            await Assert.That(compared.BaselineCreated).IsFalse();
            await Assert.That(compared.IsMatch).IsTrue();
            await Assert.That(compared.PixelErrorCount).IsEqualTo(0);
            await Assert.That(File.Exists(baselinePath)).IsTrue();
        }
        finally
        {
            DeleteIfExists(testRoot);
        }
    }

    [Test]
    public async Task CompareVisual_CreatesBaselineOnFirstRunAndPasses()
    {
        var harness = TestHarness<VisualProgram, VisualModel, Unit>.Create(Unit.Value);
        var testRoot = CreateTestRoot();

        try
        {
            var baselinePath = Path.Combine(testRoot, "baselines", "first-run.png");
            var screenshot = CreateSolidPng(4, 4, new Rgba32(20, 40, 60, 255));

            var result = harness.CompareVisual(screenshot, baselinePath);

            await Assert.That(result.IsMatch).IsTrue();
            await Assert.That(result.BaselineCreated).IsTrue();
            await Assert.That(result.PixelErrorCount).IsEqualTo(0);
            await Assert.That(File.Exists(baselinePath)).IsTrue();
            await Assert.That(result.ActualPath).IsNull();
            await Assert.That(result.DiffPath).IsNull();
        }
        finally
        {
            DeleteIfExists(testRoot);
        }
    }

    [Test]
    public async Task CompareVisual_MismatchProducesArtifactsAndAssertThrowsInStrictMode()
    {
        var harness = TestHarness<VisualProgram, VisualModel, Unit>.Create(Unit.Value);
        var testRoot = CreateTestRoot();

        try
        {
            var baselinePath = Path.Combine(testRoot, "baselines", "strict-mismatch.png");
            var artifactDirectory = Path.Combine(testRoot, "artifacts", "visual");

            var baseline = CreateSolidPng(4, 4, new Rgba32(0, 0, 0, 255));
            var actual = CreateSolidPng(4, 4, new Rgba32(255, 255, 255, 255));
            Directory.CreateDirectory(Path.GetDirectoryName(baselinePath)!);
            File.WriteAllBytes(baselinePath, baseline);

            var result = harness.CompareVisual(
                actual,
                baselinePath,
                new VisualComparisonOptions(
                    ArtifactDirectory: artifactDirectory,
                    Tolerance: VisualComparisonTolerance.Strict));

            await Assert.That(result.IsMatch).IsFalse();
            await Assert.That(result.BaselineCreated).IsFalse();
            await Assert.That(result.PixelErrorCount).IsEqualTo(16);
            await Assert.That(result.PixelErrorPercentage).IsEqualTo(1d);
            await Assert.That(result.ActualPath).IsNotNull();
            await Assert.That(result.DiffPath).IsNotNull();
            await Assert.That(File.Exists(result.ActualPath!)).IsTrue();
            await Assert.That(File.Exists(result.DiffPath!)).IsTrue();

            var act = () => harness.AssertVisualMatch(
                actual,
                baselinePath,
                new VisualComparisonOptions(
                    ArtifactDirectory: artifactDirectory,
                    Tolerance: VisualComparisonTolerance.Strict));

            await Assert.That(act).Throws<InvalidOperationException>();
        }
        finally
        {
            DeleteIfExists(testRoot);
        }
    }

    [Test]
    public async Task CompareVisual_ToleranceAllowsControlledMismatch()
    {
        var harness = TestHarness<VisualProgram, VisualModel, Unit>.Create(Unit.Value);
        var testRoot = CreateTestRoot();

        try
        {
            var baselinePath = Path.Combine(testRoot, "baselines", "tolerance.png");
            Directory.CreateDirectory(Path.GetDirectoryName(baselinePath)!);
            File.WriteAllBytes(baselinePath, CreateSolidPng(4, 4, new Rgba32(0, 0, 0, 255)));

            var actual = CreateSolidPng(4, 4, new Rgba32(10, 10, 10, 255));
            var strict = harness.CompareVisual(actual, baselinePath);
            var tolerant = harness.CompareVisual(
                actual,
                baselinePath,
                new VisualComparisonOptions(
                    Tolerance: new VisualComparisonTolerance(
                        MaxPixelErrorCount: 16,
                        MaxPixelErrorPercentage: 1,
                        MaxMeanError: 0.05,
                        MaxAbsoluteError: 0.05,
                        PerChannelThreshold: 16)));

            await Assert.That(strict.IsMatch).IsFalse();
            await Assert.That(tolerant.IsMatch).IsTrue();
        }
        finally
        {
            DeleteIfExists(testRoot);
        }
    }

    [Test]
    public async Task CompareVisual_WhenImageDimensionsDiffer_Throws()
    {
        var harness = TestHarness<VisualProgram, VisualModel, Unit>.Create(Unit.Value);
        var testRoot = CreateTestRoot();

        try
        {
            var baselinePath = Path.Combine(testRoot, "baselines", "dimension-mismatch.png");
            Directory.CreateDirectory(Path.GetDirectoryName(baselinePath)!);
            File.WriteAllBytes(baselinePath, CreateSolidPng(4, 4, new Rgba32(0, 0, 0, 255)));
            var actual = CreateSolidPng(5, 4, new Rgba32(0, 0, 0, 255));

            var act = () => harness.CompareVisual(actual, baselinePath);
            await Assert.That(act).Throws<InvalidOperationException>();
        }
        finally
        {
            DeleteIfExists(testRoot);
        }
    }

    private static string CreateTestRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "abies-visual-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private static byte[] CreateSolidPng(int width, int height, Rgba32 color)
    {
        using var image = new Image<Rgba32>(width, height);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image[x, y] = color;
            }
        }

        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }

    private sealed record VisualModel(int Value);

    private sealed class VisualProgram : Program<VisualModel, Unit>
    {
        public static (VisualModel, Command) Initialize(Unit argument) =>
            (new VisualModel(0), Commands.None);

        public static (VisualModel, Command) Transition(VisualModel model, Message message) =>
            (model, Commands.None);

        public static Result<Message[], Message> Decide(VisualModel state, Message command) =>
            Result<Message[], Message>.Ok([command]);

        public static bool IsTerminal(VisualModel state) => false;

        public static Document View(VisualModel model) =>
            new("Visual Test", Html.Elements.div([], [Html.Elements.text($"value:{model.Value}")]));

        public static Subscription Subscriptions(VisualModel model) =>
            new Subscription.None();
    }

    private sealed class PlaywrightResources : IAsyncDisposable
    {
        private readonly IPlaywright _playwright;
        private readonly IBrowser _browser;
        private readonly IBrowserContext _context;

        public IPage Page { get; }

        private PlaywrightResources(IPlaywright playwright, IBrowser browser, IBrowserContext context, IPage page)
        {
            _playwright = playwright;
            _browser = browser;
            _context = context;
            Page = page;
        }

        public static async Task<PlaywrightResources> CreateAsync()
        {
            var playwright = await Playwright.CreateAsync();
            var browser = await LaunchChromiumWithInstallFallback(playwright);
            var context = await browser.NewContextAsync();
            var page = await context.NewPageAsync();

            return new PlaywrightResources(playwright, browser, context, page);
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
            await _browser.DisposeAsync();
            _playwright.Dispose();
        }

        private static async Task<IBrowser> LaunchChromiumWithInstallFallback(IPlaywright playwright)
        {
            try
            {
                return await LaunchChromium(playwright);
            }
            catch (PlaywrightException ex) when (ex.Message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase))
            {
                var installExitCode = global::Microsoft.Playwright.Program.Main(["install", "chromium"]);
                if (installExitCode != 0)
                {
                    throw new InvalidOperationException($"Playwright browser install failed with exit code {installExitCode}.", ex);
                }

                return await LaunchChromium(playwright);
            }
        }

        private static Task<IBrowser> LaunchChromium(IPlaywright playwright) =>
            playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
    }
}