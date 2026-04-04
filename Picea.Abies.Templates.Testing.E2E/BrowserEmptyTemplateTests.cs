// =============================================================================
// BrowserEmptyTemplateTests — E2E Tests for the "abies-browser-empty" Template
// =============================================================================
// Verifies that a project scaffolded from `dotnet new abies-browser-empty`
// actually works: builds, runs, and renders the expected welcome page.
//
// The empty template is the minimal starter — no counter, no messages, just:
//   - h1 "Welcome to Abies!"
//   - A "Learn more" link pointing to the GitHub repo
//   - abies.js served from wwwroot
// =============================================================================

using Microsoft.Playwright;
using Picea.Abies.Templates.Testing.E2E.Fixtures;
using Picea.Abies.Templates.Testing.E2E.Infrastructure;

namespace Picea.Abies.Templates.Testing.E2E;

/// <summary>
/// E2E tests verifying the abies-browser-empty template produces a working application.
/// </summary>
[Category("E2E")]
public class BrowserEmptyTemplateTests : IAsyncDisposable
{
    [ClassDataSource<BrowserEmptyTemplateFixture>(Shared = SharedType.PerTestSession)]
    public required BrowserEmptyTemplateFixture Fixture { get; init; }

    private IPage _page = null!;

    [Before(Test)]
    public async Task SetUp()
    {
        _page = await Fixture.CreatePageAsync();
    }

    [After(Test)]
    public async Task TearDown()
    {
        if (_page is not null)
            await _page.Context.DisposeAsync();
    }

    public async ValueTask DisposeAsync() => GC.SuppressFinalize(this);

    // ─── Content Tests ────────────────────────────────────────────────

    [Test]
    public async Task InitialLoad_ShowsWelcomeHeading()
    {
        await _page.GotoAsync("/");

        await Assertions.Expect(
            _page.GetByRole(AriaRole.Heading, new() { Name = "Welcome to Abies!" }))
            .ToBeVisibleAsync(new() { Timeout = 30_000 });
    }

    [Test]
    public async Task InitialLoad_ShowsStartBuildingText()
    {
        await _page.GotoAsync("/");

        await Assertions.Expect(
            _page.GetByText("Start building your MVU application."))
            .ToBeVisibleAsync(new() { Timeout = 30_000 });
    }

    [Test]
    public async Task LearnMoreLink_PointsToGithub()
    {
        await _page.GotoAsync("/");

        var link = _page.GetByRole(AriaRole.Link, new() { Name = "github.com/Picea/Abies" });

        await Assertions.Expect(link)
            .ToBeVisibleAsync(new() { Timeout = 30_000 });

        await Assertions.Expect(link)
            .ToHaveAttributeAsync("href", "https://github.com/Picea/Abies",
                new() { Timeout = 5_000 });
    }

    [Test]
    public async Task AbiesJs_IsServed()
    {
        using var http = new HttpClient();
        var response = await http.GetAsync($"{Fixture.BaseUrl}/abies.js");

        await Assert.That(response.StatusCode)
            .IsEqualTo(System.Net.HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task ReleasePublish_Succeeds()
    {
        var (exitCode, stdOut, stdErr) = await DotNetCli.RunAsync(
            "publish -c Release",
            workingDirectory: Fixture.ProjectDir,
            timeoutSeconds: 600);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That($"STDOUT:\n{stdOut}\nSTDERR:\n{stdErr}").DoesNotContain("error CA2252");
    }
}
