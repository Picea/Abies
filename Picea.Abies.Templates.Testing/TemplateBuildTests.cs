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
    public async Task ServerTemplate_DebugRun_ServesServerDebuggerAsset()
    {
        await using var project = await TemplateProject.CreateAsync(
            "abies-server", "BuildTestServerDebuggerAsset");

        await project.RunAsync();

        using var http = new HttpClient();
        var response = await http.GetAsync($"{project.BaseUrl}/_abies/debugger.js");

        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        await Assert.That(response.Content.Headers.ContentType?.MediaType)
            .IsEqualTo("text/javascript");

        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains("mountDebugger");
    }

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
    public async Task BrowserTemplate_Defaults_EnableOtelAndIncludeHostProxy()
    {
        await using var project = await TemplateProject.CreateAsync(
            "abies-browser", "BuildTestBrowserDefaults");

        var indexPath = Path.Join(project.ProjectDir, "wwwroot", "index.html");
        var debuggerPath = Path.Join(project.ProjectDir, "wwwroot", "debugger.js");
        var hostProgramPath = Path.Join(project.ProjectDir, "BuildTestBrowserDefaults.Host", "Program.cs");

        await Assert.That(File.Exists(indexPath)).IsTrue();
        await Assert.That(File.Exists(debuggerPath)).IsTrue();
        await Assert.That(File.Exists(hostProgramPath)).IsTrue();

        var indexContent = await File.ReadAllTextAsync(indexPath);
        var hostProgramContent = await File.ReadAllTextAsync(hostProgramPath);

        await Assert.That(indexContent).Contains("<meta name=\"otel-verbosity\" content=\"user\">");
        await Assert.That(hostProgramContent).Contains("app.MapOtlpProxy();");
        await Assert.That(hostProgramContent).Contains(".AddConsoleExporter()");
    }

    [Test]
    public async Task BrowserEmptyTemplate_Defaults_EnableOtelAndIncludeHostProxy()
    {
        await using var project = await TemplateProject.CreateAsync(
            "abies-browser-empty", "BuildTestBrowserEmptyDefaults");

        var indexPath = Path.Join(project.ProjectDir, "wwwroot", "index.html");
        var debuggerPath = Path.Join(project.ProjectDir, "wwwroot", "debugger.js");
        var hostProgramPath = Path.Join(project.ProjectDir, "BuildTestBrowserEmptyDefaults.Host", "Program.cs");

        await Assert.That(File.Exists(indexPath)).IsTrue();
        await Assert.That(File.Exists(debuggerPath)).IsTrue();
        await Assert.That(File.Exists(hostProgramPath)).IsTrue();

        var indexContent = await File.ReadAllTextAsync(indexPath);
        var hostProgramContent = await File.ReadAllTextAsync(hostProgramPath);

        await Assert.That(indexContent).Contains("<meta name=\"otel-verbosity\" content=\"user\">");
        await Assert.That(hostProgramContent).Contains("app.MapOtlpProxy();");
        await Assert.That(hostProgramContent).Contains(".AddConsoleExporter()");
    }

    [Test]
    public async Task BrowserTemplate_Source_Defaults_EnableDebuggerUi_WithOptOutPath()
    {
        var repoRoot = Path.GetFullPath(Path.Join(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var programPath = Path.Join(
            repoRoot,
            "Picea.Abies.Templates",
            "templates",
            "abies-browser",
            "Program.cs");

        await Assert.That(File.Exists(programPath)).IsTrue();

        var content = await File.ReadAllTextAsync(programPath);

        await Assert.That(content).Contains("ConfigureAbiesDebugger();");
        await Assert.That(content).Contains("ABIES_DEBUG_UI");
    }

    [Test]
    public async Task BrowserTemplate_Generated_Defaults_EnableDebuggerUi_WithOptOutPath()
    {
        await using var project = await TemplateProject.CreateAsync(
            "abies-browser", "BuildTestBrowserGeneratedDebuggerDefaults");

        var generatedProgramPath = Path.Join(project.ProjectDir, "Program.cs");

        await Assert.That(File.Exists(generatedProgramPath)).IsTrue();

        var content = await File.ReadAllTextAsync(generatedProgramPath);

        await Assert.That(content).Contains("ConfigureAbiesDebugger();");
        await Assert.That(content).Contains("ABIES_DEBUG_UI");
    }

    [Test]
    public async Task BrowserEmptyTemplate_Source_Defaults_EnableDebuggerUi_WithOptOutPath()
    {
        var repoRoot = Path.GetFullPath(Path.Join(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var programPath = Path.Join(
            repoRoot,
            "Picea.Abies.Templates",
            "templates",
            "abies-browser-empty",
            "Program.cs");

        await Assert.That(File.Exists(programPath)).IsTrue();

        var content = await File.ReadAllTextAsync(programPath);

        await Assert.That(content).Contains("ConfigureAbiesDebugger();");
        await Assert.That(content).Contains("ABIES_DEBUG_UI");
    }

    [Test]
    public async Task BrowserEmptyTemplate_Generated_Defaults_EnableDebuggerUi_WithOptOutPath()
    {
        await using var project = await TemplateProject.CreateAsync(
            "abies-browser-empty", "BuildTestBrowserEmptyGeneratedDebuggerDefaults");

        var generatedProgramPath = Path.Join(project.ProjectDir, "Program.cs");

        await Assert.That(File.Exists(generatedProgramPath)).IsTrue();

        var content = await File.ReadAllTextAsync(generatedProgramPath);

        await Assert.That(content).Contains("ConfigureAbiesDebugger();");
        await Assert.That(content).Contains("ABIES_DEBUG_UI");
    }

    [Test]
    public async Task ServerTemplate_Generated_Defaults_EnableDebuggerUi_WithOptOutPath()
    {
        await using var project = await TemplateProject.CreateAsync(
            "abies-server", "BuildTestServerGeneratedDebuggerDefaults");

        var generatedProgramPath = Path.Join(project.ProjectDir, "Program.cs");

        await Assert.That(File.Exists(generatedProgramPath)).IsTrue();

        var content = await File.ReadAllTextAsync(generatedProgramPath);

        await Assert.That(content).Contains("ConfigureAbiesDebugger();");
        await Assert.That(content).Contains("ABIES_DEBUG_UI");
    }

    [Test]
    public async Task ServerTemplate_Defaults_EnableOtelProxyWithConsoleExporter()
    {
        await using var project = await TemplateProject.CreateAsync(
            "abies-server", "BuildTestServerDefaults");

        var programPath = Path.Join(project.ProjectDir, "Program.cs");

        await Assert.That(File.Exists(programPath)).IsTrue();

        var content = await File.ReadAllTextAsync(programPath);

        await Assert.That(content).Contains("app.MapOtlpProxy();");
        await Assert.That(content).Contains(".AddConsoleExporter()");
        await Assert.That(content).Contains("ConfigureAbiesDebugger();");
        await Assert.That(content).Contains("ABIES_DEBUG_UI");
    }

    [Test]
    public async Task ServerTemplate_Source_Defaults_EnableDebuggerUi_WithOptOutPath()
    {
        var repoRoot = Path.GetFullPath(Path.Join(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var programPath = Path.Join(
            repoRoot,
            "Picea.Abies.Templates",
            "templates",
            "abies-server",
            "Program.cs");

        await Assert.That(File.Exists(programPath)).IsTrue();

        var content = await File.ReadAllTextAsync(programPath);

        await Assert.That(content).Contains("ConfigureAbiesDebugger();");
        await Assert.That(content).Contains("ABIES_DEBUG_UI");
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
