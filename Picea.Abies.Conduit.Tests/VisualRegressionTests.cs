// =============================================================================
// Visual Regression Tests — Conduit static pages
// =============================================================================
// Pixel-snapshot regression coverage for deterministic Conduit views, driven by
// the reusable visual harness in Picea.Abies.Testing (TestHarnessVisualExtensions).
//
// These render a Conduit page model with Playwright and diff the screenshot
// against a committed baseline. Only the static, data-free pages (Login,
// Register) are covered here so the baselines stay deterministic — they route
// through Route.FromUrl with Commands.None and never fetch from the API.
//
// The CI gate (.github/workflows/visual-regression.yml) discovers these by the
// "VisualRegression_" name prefix and runs them with
// ABIES_ENABLE_PIXEL_SNAPSHOTS=1. Outside that gate (a normal `dotnet test`),
// the tests skip rather than require Playwright browsers to be installed.
// =============================================================================

using Microsoft.Playwright;
using Picea.Abies.Conduit.App;
using Picea.Abies.Testing;
using TUnit.Core.Exceptions;

namespace Picea.Abies.Conduit.Tests;

public sealed class VisualRegressionTests
{
    private const string PixelSnapshotsEnabledVariable = "ABIES_ENABLE_PIXEL_SNAPSHOTS";

    [Test]
    [Arguments("conduit-login", "/login")]
    [Arguments("conduit-register", "/register")]
    public async Task VisualRegression_StaticPage_MatchesBaseline(string snapshotName, string path)
    {
        SkipUnlessPixelSnapshotsEnabled();

        var startup = new ConduitStartup(
            ApiUrl: "https://api.conduit.local",
            Session: null,
            InitialUrl: Url.FromUri(new Uri($"https://conduit.local{path}")));

        var harness = TestHarness<ConduitProgram, Model, ConduitStartup>.Create(startup);

        await using var browser = await LaunchChromiumOrSkip();
        var page = await browser.NewPageAsync();

        var options = new VisualComparisonOptions(
            ViewportWidth: 1024,
            ViewportHeight: 768,
            FullPage: true,
            ArtifactDirectory: ArtifactsDirectory(),
            Tolerance: VisualComparisonTolerance.Strict);

        var result = await harness.CompareVisual(page, BaselinePath(snapshotName), options);

        // First run on a fresh environment writes the baseline and passes; later
        // runs diff against it. Either way a clean run is a match.
        await Assert.That(result.IsMatch).IsTrue();
    }

    private static void SkipUnlessPixelSnapshotsEnabled()
    {
        if (Environment.GetEnvironmentVariable(PixelSnapshotsEnabledVariable) != "1")
        {
            throw new SkipTestException(
                $"Visual regression tests are gated behind {PixelSnapshotsEnabledVariable}=1 " +
                "(set by the Visual Regression CI workflow).");
        }
    }

    private static async Task<IBrowser> LaunchChromiumOrSkip()
    {
        var playwright = await Playwright.CreateAsync();
        try
        {
            return await playwright.Chromium.LaunchAsync();
        }
        catch (PlaywrightException ex) when (ex.Message.Contains("Executable doesn't exist", StringComparison.OrdinalIgnoreCase))
        {
            throw new SkipTestException(
                "Playwright Chromium is not installed. Install browsers (the CI workflow runs " +
                "`playwright.ps1 install chromium`) before running visual regression tests.");
        }
    }

    private static string BaselinePath(string snapshotName) =>
        Path.Combine(SolutionDirectory(), "Picea.Abies.Conduit.Tests", "Snapshots", "visual", $"{snapshotName}.png");

    private static string ArtifactsDirectory() =>
        Path.Combine(SolutionDirectory(), "artifacts", "visual");

    private static string SolutionDirectory()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "Picea.Abies.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new DirectoryNotFoundException("Could not find solution directory (containing Picea.Abies.sln).");
    }
}
