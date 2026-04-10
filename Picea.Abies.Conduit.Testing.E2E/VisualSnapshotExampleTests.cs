using System.Security.Cryptography;
using Microsoft.Playwright;
using Picea.Abies.Conduit.Testing.E2E.Fixtures;
using Picea.Abies.Conduit.Testing.E2E.Helpers;

namespace Picea.Abies.Conduit.Testing.E2E;

[Category("E2E")]
[ClassDataSource<ConduitAppFixture>(Shared = SharedType.Keyed, Key = "Conduit")]
[NotInParallel("Conduit")]
public sealed class VisualSnapshotExampleTests : IAsyncInitializer, IAsyncDisposable
{
    private readonly ConduitAppFixture _fixture;
    private IPage _page = null!;

    public VisualSnapshotExampleTests(ConduitAppFixture fixture)
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
    public async Task HomeBanner_ShouldMatchSnapshot()
    {
        await _page.GotoAsync("/");
        await _page.WaitForWasmReady();
        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });

        var banner = _page.Locator(".banner");
        await AssertSnapshotAsync("home-banner", banner);
    }

    [Test]
    public async Task HomeFeedToggle_ShouldMatchSnapshot()
    {
        await _page.GotoAsync("/");
        await _page.WaitForWasmReady();
        await _page.WaitForSelectorAsync(".home-page", new() { Timeout = 15000 });

        var toggle = _page.Locator(".feed-toggle");
        await AssertSnapshotAsync("home-feed-toggle", toggle);
    }

    private async Task AssertSnapshotAsync(string snapshotName, ILocator locator)
    {
        var baselinePath = GetBaselinePath(snapshotName);
        var artifactsDirectory = GetArtifactsDirectory();
        Directory.CreateDirectory(Path.GetDirectoryName(baselinePath)!);
        Directory.CreateDirectory(artifactsDirectory);

        var actualBytes = await locator.ScreenshotAsync(new LocatorScreenshotOptions
        {
            Animations = ScreenshotAnimations.Disabled
        });

        if (File.Exists(baselinePath) is false)
        {
            await File.WriteAllBytesAsync(baselinePath, actualBytes);
            return;
        }

        var baselineBytes = await File.ReadAllBytesAsync(baselinePath);
        if (baselineBytes.AsSpan().SequenceEqual(actualBytes))
        {
            return;
        }

        var actualPath = Path.Combine(artifactsDirectory, $"{snapshotName}.actual.png");
        await File.WriteAllBytesAsync(actualPath, actualBytes);

        var baselineHash = ComputeSha256(baselineBytes);
        var actualHash = ComputeSha256(actualBytes);

        throw new InvalidOperationException(
            $"Snapshot mismatch for '{snapshotName}'. Baseline: {baselinePath}. Actual: {actualPath}. " +
            $"Baseline SHA256: {baselineHash}, Actual SHA256: {actualHash}.");
    }

    private static string GetBaselinePath(string snapshotName)
    {
        var root = FindSolutionDirectory();
        return Path.Combine(root, "Picea.Abies.Conduit.Testing.E2E", "visual-baselines", "examples", $"{snapshotName}.png");
    }

    private static string GetArtifactsDirectory()
    {
        var root = FindSolutionDirectory();
        return Path.Combine(root, "artifacts", "visual", "conduit-e2e");
    }

    private static string ComputeSha256(byte[] value)
    {
        var hash = SHA256.HashData(value);
        return Convert.ToHexString(hash);
    }

    private static string FindSolutionDirectory()
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
