# Testing Guide

Abies applications are highly testable because they separate pure logic from side effects.

## Overview

The MVU architecture enables three levels of testing:

| Level | What to Test | Speed | Dependencies |
| ----- | ------------ | ----- | ------------ |
| Unit | Transition logic, pure functions | Fast | None |
| DOM | View output, interactions | Fast | Test harness |
| E2E | Full application | Slow | Browser |

## Unit Testing

### Testing Transition Functions

Transition functions are pure and can be tested directly:

```csharp
using Xunit;

public class CounterTests
{
    [Fact]
    public void Increment_IncreasesCount()
    {
        var model = new Model(Count: 5);

        var (newModel, command) = Counter.Transition(model, new CounterMessage.Increment());

        Assert.Equal(6, newModel.Count);
        Assert.IsType<Command.None>(command);
    }

    [Fact]
    public void Decrement_DecreasesCount()
    {
        var model = new Model(Count: 5);

        var (newModel, _) = Counter.Transition(model, new CounterMessage.Decrement());

        Assert.Equal(4, newModel.Count);
    }
}
```

### Testing with Commands

Verify that correct commands are returned:

```csharp
[Fact]
public void SubmitForm_ReturnsLoginCommand()
{
    var model = new Model(
        Email: "test@example.com",
        Password: "password123"
    );

    var (newModel, command) = Login.Transition(model, new SubmitForm());

    Assert.True(newModel.IsSubmitting);
    Assert.IsType<LoginCommand>(command);

    var loginCmd = (LoginCommand)command;
    Assert.Equal("test@example.com", loginCmd.Email);
}
```

### Testing State Transitions

```csharp
public class ArticleEditorTests
{
    [Theory]
    [InlineData("", false)]
    [InlineData("Hello", true)]
    public void TitleChange_UpdatesValidity(string title, bool isValid)
    {
        var model = new EditorModel(Title: "", Body: "content");

        var (newModel, _) = Editor.Transition(model, new TitleChanged(title));

        Assert.Equal(isValid, newModel.IsValid);
    }

    [Fact]
    public void AddTag_AppendsToTagList()
    {
        var model = new EditorModel(
            Title: "Test",
            Body: "Content",
            Tags: ["existing"]
        );

        var (newModel, _) = Editor.Transition(model, new AddTag("new-tag"));

        Assert.Equal(["existing", "new-tag"], newModel.Tags);
    }
}
```

## DOM Testing

Test the virtual DOM without a browser.

### MvuDomTestHarness

A minimal test harness for DOM assertions:

```csharp
using Picea.Abies.DOM;

public static class MvuDomTestHarness
{
    public static Func<Element, bool> HasTag(string tag)
        => el => el.Tag == tag;

    public static Func<Element, bool> HasClassFragment(string fragment)
        => el => el.Attributes.Any(a =>
            a.Name == "class" && a.Value.Contains(fragment));

    public static Func<Element, bool> HasTestId(string testId)
        => el => el.Attributes.Any(a =>
            a.Name == "data-testid" && a.Value == testId);

    public static Func<Element, bool> HasDirectText(string text)
        => el => el.Children.OfType<Text>().Any(t => t.Value == text);

    public static Func<Element, bool> And(
        this Func<Element, bool> left,
        Func<Element, bool> right)
        => el => left(el) && right(el);

    public static Element FindFirstElement(Node root, Func<Element, bool> predicate)
        => EnumerateElements(root).First(predicate);

    public static IEnumerable<Element> EnumerateElements(Node root)
    {
        if (root is Element el)
        {
            yield return el;
            foreach (var child in el.Children)
                foreach (var desc in EnumerateElements(child))
                    yield return desc;
        }
    }
}
```

### Testing View Output

```csharp
[Fact]
public void View_ShowsCorrectCount()
{
    var model = new Model(Count: 42);

    var dom = Counter.View(model);

    var countElement = MvuDomTestHarness.FindFirstElement(
        dom.Body,
        MvuDomTestHarness.HasTestId("count-display")
    );

    var text = countElement.Children.OfType<Text>().First();
    Assert.Contains("42", text.Value);
}
```

## Integration Testing with the Runtime

Test complete message flows using the actual runtime:

```csharp
[Fact]
public async Task Interpreter_DispatchesFeedbackMessage()
{
    var patches = new List<IReadOnlyList<Patch>>();
    var runtime = await Runtime<MyApp, Model, Unit>.Start(
        apply: p => patches.Add(p),
        interpreter: async cmd =>
        {
            if (cmd is LoadDataCommand)
                return Result<Message[], PipelineError>.Ok(
                    [new DataLoaded(testData)]);
            return Result<Message[], PipelineError>.Ok([]);
        });

    await runtime.Dispatch(new FetchData());

    Assert.False(runtime.Model.IsLoading);
    Assert.NotEmpty(runtime.Model.Data);
}
```

### Testing Interpreters in Isolation

```csharp
[Fact]
public async Task Interpreter_HandlesHttpErrors()
{
    var fakeHandler = new FakeHttpMessageHandler();
    fakeHandler.SetupError("/api/articles", HttpStatusCode.InternalServerError);
    var httpClient = new HttpClient(fakeHandler);

    Interpreter<Command, Message> interpreter = async cmd =>
    {
        if (cmd is LoadArticles)
        {
            try
            {
                var articles = await httpClient.GetFromJsonAsync<Article[]>("/api/articles");
                return Result<Message[], PipelineError>.Ok([new ArticlesLoaded(articles!)]);
            }
            catch (Exception ex)
            {
                return Result<Message[], PipelineError>.Ok([new LoadFailed(ex.Message)]);
            }
        }
        return Result<Message[], PipelineError>.Ok([]);
    };

    var result = await interpreter(new LoadArticles());

    var messages = result.Match(msgs => msgs, _ => []);
    Assert.Single(messages);
    Assert.IsType<LoadFailed>(messages[0]);
}
```

## E2E Testing with Playwright

```csharp
using Microsoft.Playwright;
using Xunit;

public class E2ETests : IAsyncLifetime
{
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IPage _page = null!;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync();
        _page = await _browser.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }

    [Fact]
    public async Task Counter_IncrementsOnClick()
    {
        await _page.GotoAsync("http://localhost:5000");

        await _page.ClickAsync("text=+");
        await _page.ClickAsync("text=+");

        var count = await _page.TextContentAsync("[data-testid='count']");
        Assert.Equal("2", count);
    }
}
```

## Visual Regression Testing

`Picea.Abies.Testing` ships a screenshot-based visual harness (`TestHarnessVisualExtensions`) plus an
`abies` baseline-management CLI (`Picea.Abies.Cli`). It renders a model with Playwright, captures a PNG,
and compares it against a stored baseline.

On the **first run** the baseline is created automatically (`BaselineCreated == true`) and the comparison
passes. On later runs the screenshot is diffed against that baseline; on mismatch, `.actual.png` and
`.diff.png` artifacts are written next to the baseline.

```csharp
using Picea.Abies.Testing;

var harness = TestHarness<MyProgram, MyModel, Unit>.Create(Unit.Value);

var options = new VisualComparisonOptions(
    ViewportWidth: 1280,
    ViewportHeight: 720,
    FullPage: true,
    Tolerance: VisualComparisonTolerance.Strict); // every pixel channel must match exactly

// Render + screenshot via Playwright, then compare (creates the baseline on first run):
var result = await harness.CompareVisual(page, "baselines/home.png", options);
await Assert.That(result.IsMatch).IsTrue();

// Or assert directly (throws on mismatch in strict mode):
await harness.AssertVisualMatch(page, "baselines/home.png", options);
```

Use `VisualComparisonTolerance` to allow controlled drift (`MaxPixelErrorCount`, `MaxPixelErrorPercentage`,
`MaxMeanError`, `MaxAbsoluteError`, `PerChannelThreshold`). A `byte[]` overload of `CompareVisual` exists for
comparing a screenshot you already captured.

> Visual tests require Playwright browsers. Install them before running (the
> [`visual-regression.yml`](../../.github/workflows/visual-regression.yml) workflow does this in CI); without
> them the Playwright-backed tests fail with an "install Playwright browsers" message.

### Managing baselines with the `abies` CLI

When a change is intentional, promote the pending `*.actual.png` artifacts to baselines instead of editing
images by hand:

```bash
# Accept one pending snapshot
dotnet run --project Picea.Abies.Cli -- visual accept home-page.png \
  --artifacts artifacts/visual --baselines baselines/visual

# Accept every pending snapshot
dotnet run --project Picea.Abies.Cli -- visual accept --all \
  --artifacts artifacts/visual --baselines baselines/visual

# List pending mismatches
dotnet run --project Picea.Abies.Cli -- visual status \
  --artifacts artifacts/visual --baselines baselines/visual

# Write a markdown mismatch report (visual-report.md)
dotnet run --project Picea.Abies.Cli -- visual report --output reports \
  --artifacts artifacts/visual --baselines baselines/visual
```

The CLI is also packed as a .NET tool (`ToolCommandName` = `abies`), so once installed the commands are
available as `abies visual accept|status|report`.

> **Seeding baselines.** Screenshots are environment-sensitive (fonts, anti-aliasing), so baselines must be
> generated on the same OS as CI. The `VisualRegression_*` tests in `Picea.Abies.Conduit.Tests` create a
> baseline on first run and pass; the Visual Regression workflow uploads the generated PNGs as the
> `visual-regression-*` artifact. Download that artifact, commit the baselines under
> `Picea.Abies.Conduit.Tests/Snapshots/visual/`, and subsequent runs diff against them. Do **not** commit
> baselines captured on a developer machine — they will mismatch the CI runner.

## Running Tests

```bash
# Unit tests
dotnet test MyApp.Tests

# E2E tests (requires app running)
dotnet test MyApp.Testing.E2E

# Visual regression tests (requires Playwright browsers installed)
dotnet test Picea.Abies.Testing.Tests

# All tests
dotnet test
```

## Best Practices

1. **Test Transition first** — Most bugs are in logic, not rendering
2. **Use `data-testid`** for stable DOM selectors
3. **One journey per E2E test** — Keep focused
4. **Test edge cases** with `[Theory]` and `[InlineData]`
5. **Avoid testing framework internals** — Test behavior, not structure

## See Also

- [Pure Functions](../concepts/pure-functions.md) — Why pure functions are testable
- [Commands and Effects](../concepts/commands-effects.md) — Testing interpreters
- [Conduit E2E Fixture Architecture](./conduit-e2e-fixture-architecture.md) — Real project fixture, seeding, and user-journey coverage patterns
- [`visual-regression.yml`](../../.github/workflows/visual-regression.yml) — CI workflow for the visual harness
