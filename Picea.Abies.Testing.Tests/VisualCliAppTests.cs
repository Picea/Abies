using Picea.Abies.Testing.Cli;

namespace Picea.Abies.Testing.Tests;

public sealed class VisualCliAppTests
{
    [Test]
    public async Task VisualAccept_WithSnapshot_CopiesActualToBaseline()
    {
        var root = CreateTestRoot();

        try
        {
            var artifactsDirectory = Path.Combine(root, "artifacts", "visual");
            var baselinesDirectory = Path.Combine(root, "baselines", "visual");
            Directory.CreateDirectory(artifactsDirectory);

            var actualPath = Path.Combine(artifactsDirectory, "home-page.actual.png");
            await File.WriteAllBytesAsync(actualPath, [1, 2, 3, 4]);

            var output = new StringWriter();
            var exitCode = await VisualCliApp.InvokeAsync([
                "visual", "accept", "home-page.png",
                "--artifacts", artifactsDirectory,
                "--baselines", baselinesDirectory
            ], output, output);

            var baselinePath = Path.Combine(baselinesDirectory, "home-page.png");
            await Assert.That(exitCode).IsEqualTo(0);
            await Assert.That(File.Exists(baselinePath)).IsTrue();
            await Assert.That(await File.ReadAllBytesAsync(baselinePath)).IsEquivalentTo(new byte[] { 1, 2, 3, 4 });
        }
        finally
        {
            DeleteIfExists(root);
        }
    }

    [Test]
    public async Task VisualAccept_AllAndReport_ProcessesPendingSnapshots()
    {
        var root = CreateTestRoot();

        try
        {
            var artifactsDirectory = Path.Combine(root, "artifacts", "visual");
            var baselinesDirectory = Path.Combine(root, "baselines", "visual");
            var reportDirectory = Path.Combine(root, "reports");
            Directory.CreateDirectory(artifactsDirectory);

            await File.WriteAllBytesAsync(Path.Combine(artifactsDirectory, "article-list.actual.png"), [9]);
            await File.WriteAllBytesAsync(Path.Combine(artifactsDirectory, "article-list.diff.png"), [7]);

            var output = new StringWriter();
            var acceptAllExitCode = await VisualCliApp.InvokeAsync([
                "visual", "accept", "--all",
                "--artifacts", artifactsDirectory,
                "--baselines", baselinesDirectory
            ], output, output);

            var statusExitCode = await VisualCliApp.InvokeAsync([
                "visual", "status",
                "--artifacts", artifactsDirectory,
                "--baselines", baselinesDirectory
            ], output, output);

            var reportExitCode = await VisualCliApp.InvokeAsync([
                "visual", "report",
                "--output", reportDirectory,
                "--artifacts", artifactsDirectory,
                "--baselines", baselinesDirectory
            ], output, output);

            await Assert.That(acceptAllExitCode).IsEqualTo(0);
            await Assert.That(statusExitCode).IsEqualTo(0);
            await Assert.That(reportExitCode).IsEqualTo(0);
            await Assert.That(File.Exists(Path.Combine(baselinesDirectory, "article-list.png"))).IsTrue();
            await Assert.That(File.Exists(Path.Combine(reportDirectory, "visual-report.md"))).IsTrue();
        }
        finally
        {
            DeleteIfExists(root);
        }
    }

    private static string CreateTestRoot()
    {
        var path = Path.Combine(Path.GetTempPath(), "abies-visual-cli-tests", Guid.NewGuid().ToString("N"));
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
}
