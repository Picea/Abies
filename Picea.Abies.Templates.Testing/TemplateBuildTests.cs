namespace Picea.Abies.Templates.Testing;

/// <summary>
/// Verifies that projects scaffolded from each template build successfully.
/// These are fast-feedback smoke tests that catch MSBuild/csproj issues before
/// the slower E2E tests run.
/// </summary>
[Trait("Category", "Template")]
public sealed class TemplateBuildTests
{
    [Fact]
    public async Task ServerTemplate_Builds()
    {
        await using var project = await TemplateProject.CreateAsync(
            "abies-server", "BuildTestServer");

        Assert.True(Directory.Exists(project.ProjectDir),
            "Server template project directory should exist after build.");
    }

    [Fact]
    public async Task BrowserTemplate_Builds()
    {
        await using var project = await TemplateProject.CreateAsync(
            "abies-browser", "BuildTestBrowser");

        Assert.True(Directory.Exists(project.ProjectDir),
            "Browser template project directory should exist after build.");
    }

    [Fact]
    public async Task BrowserEmptyTemplate_Builds()
    {
        await using var project = await TemplateProject.CreateAsync(
            "abies-browser-empty", "BuildTestBrowserEmpty");

        Assert.True(Directory.Exists(project.ProjectDir),
            "Browser-empty template project directory should exist after build.");
    }
}
