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
}
