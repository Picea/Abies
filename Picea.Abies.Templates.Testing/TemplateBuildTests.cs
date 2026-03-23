namespace Picea.Abies.Templates.Testing;

/// <summary>
/// Verifies that projects scaffolded from each template build successfully.
/// These are fast-feedback smoke tests that catch MSBuild/csproj issues before
/// the slower E2E tests run.
/// </summary>
[Category("Template")]
public sealed class TemplateBuildTests
{
    [Test]
    public async Task ServerTemplate_Builds()
    {
        await using var project = await TemplateProject.CreateAsync(
            "abies-server", "BuildTestServer");

        await Assert.That(Directory.Exists(project.ProjectDir)).IsTrue();
    }

    [Test]
    public async Task BrowserTemplate_Builds()
    {
        await using var project = await TemplateProject.CreateAsync(
            "abies-browser", "BuildTestBrowser");

        await Assert.That(Directory.Exists(project.ProjectDir)).IsTrue();
    }

    [Test]
    public async Task BrowserEmptyTemplate_Builds()
    {
        await using var project = await TemplateProject.CreateAsync(
            "abies-browser-empty", "BuildTestBrowserEmpty");

        await Assert.That(Directory.Exists(project.ProjectDir)).IsTrue();
    }

    [Test]
    public async Task BrowserTemplate_ReleaseAssetGate_DebuggerRuntimeMustNotShip_Issue160_Contract()
    {
        await using var project = await TemplateProject.CreateAsync(
            "abies-browser", "BuildTestBrowserReleaseAssetGate");

        await project.PublishRelease();

        var publishMarker =
            $"{Path.DirectorySeparatorChar}publish{Path.DirectorySeparatorChar}";
        var abiesJsPath = Directory
            .EnumerateFiles(project.ProjectDir, "abies.js", SearchOption.AllDirectories)
            .FirstOrDefault(path => path.Contains(publishMarker, StringComparison.OrdinalIgnoreCase));

        await Assert.That(abiesJsPath is not null).IsTrue();

        var content = await File.ReadAllTextAsync(abiesJsPath!);

        // Contract markers mapped from issue #160 debugger capabilities.
        // Release assets must remain clean from debugger UI/adapter runtime hooks.
        var forbiddenMarkers = new[]
        {
            "UseDebugger",
            "message log",
            "state scrubber",
            "step-forward",
            "step-backward",
            "model inspector",
            "ring buffer",
            "Export / Import session"
        };

        var foundMarkers = forbiddenMarkers
            .Where(marker => content.Contains(marker, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        await Assert.That(foundMarkers.Length).IsEqualTo(0);
    }
}
