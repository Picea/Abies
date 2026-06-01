# Testing Guide

Abies applications are highly testable because they separate pure logic from side effects.

## Overview

The MVU architecture enables three levels of testing:

| Level | What to Test | Speed | Dependencies |
| ----- | ------------ | ----- | ------------ |
| Unit | Transition logic, pure functions | Fast | None |
| DOM | View output, interactions | Fast | Test harness |
| E2E | Full application | Slow | Browser |

Abies' own test suites use [**TUnit**](https://tunit.dev/) on the Microsoft.Testing.Platform, and the
examples below follow the same conventions: `[Test]` methods are `async Task`, parameterized cases use
repeated `[Arguments(...)]`, and assertions use the awaitable `await Assert.That(x).IsEqualTo(y)` fluent
API. TUnit registers its global usings automatically, so an explicit `using TUnit.Core;` is only needed
when `ImplicitUsings` is disabled.

## Unit Testing

### Testing Transition Functions

Transition functions are pure and can be tested directly:

```csharp
using TUnit.Core;

public class CounterTests
{
    [Test]
    public async Task Increment_IncreasesCount()
    {
        var model = new Model(Count: 5);

        var (newModel, command) = Counter.Transition(model, new CounterMessage.Increment());

        await Assert.That(newModel.Count).IsEqualTo(6);
        await Assert.That(command).IsTypeOf<Command.None>();
    }

    [Test]
    public async Task Decrement_DecreasesCount()
    {
        var model = new Model(Count: 5);

        var (newModel, _) = Counter.Transition(model, new CounterMessage.Decrement());

        await Assert.That(newModel.Count).IsEqualTo(4);
    }
}
```

### Testing with Commands

Verify that correct commands are returned:

```csharp
[Test]
public async Task SubmitForm_ReturnsLoginCommand()
{
    var model = new Model(
        Email: "test@example.com",
        Password: "password123"
    );

    var (newModel, command) = Login.Transition(model, new SubmitForm());

    await Assert.That(newModel.IsSubmitting).IsTrue();
    await Assert.That(command).IsTypeOf<LoginCommand>();

    var loginCmd = (LoginCommand)command;
    await Assert.That(loginCmd.Email).IsEqualTo("test@example.com");
}
```

### Testing State Transitions

```csharp
public class ArticleEditorTests
{
    [Test]
    [Arguments("", false)]
    [Arguments("Hello", true)]
    public async Task TitleChange_UpdatesValidity(string title, bool isValid)
    {
        var model = new EditorModel(Title: "", Body: "content");

        var (newModel, _) = Editor.Transition(model, new TitleChanged(title));

        await Assert.That(newModel.IsValid).IsEqualTo(isValid);
    }

    [Test]
    public async Task AddTag_AppendsToTagList()
    {
        var model = new EditorModel(
            Title: "Test",
            Body: "Content",
            Tags: ["existing"]
        );

        var (newModel, _) = Editor.Transition(model, new AddTag("new-tag"));

        await Assert.That(newModel.Tags).IsEquivalentTo(new[] { "existing", "new-tag" });
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
[Test]
public async Task View_ShowsCorrectCount()
{
    var model = new Model(Count: 42);

    var dom = Counter.View(model);

    var countElement = MvuDomTestHarness.FindFirstElement(
        dom.Body,
        MvuDomTestHarness.HasTestId("count-display")
    );

    var text = countElement.Children.OfType<Text>().First();
    await Assert.That(text.Value).Contains("42");
}
```

## Integration Testing with the Runtime

Test complete message flows using the actual runtime:

```csharp
[Test]
public async Task Interpreter_DispatchesFeedbackMessage()
{
    var patches = new List<IReadOnlyList<Patch>>();
    using var runtime = await Runtime<MyApp, Model, Unit>.Start(
        apply: p => patches.Add(p),
        interpreter: async cmd =>
        {
            if (cmd is LoadDataCommand)
                return Result<Message[], PipelineError>.Ok(
                    [new DataLoaded(testData)]);
            return Result<Message[], PipelineError>.Ok([]);
        });

    await runtime.Dispatch(new FetchData());

    await Assert.That(runtime.Model.IsLoading).IsFalse();
    await Assert.That(runtime.Model.Data).IsNotEmpty();
}
```

### Testing Interpreters in Isolation

```csharp
[Test]
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

    var messages = result switch
    {
        Ok<Message[], PipelineError>(var msgs) => msgs,
        _ => []
    };
    await Assert.That(messages).Count().IsEqualTo(1);
    await Assert.That(messages[0]).IsTypeOf<LoadFailed>();
}
```

## E2E Testing with Playwright

TUnit uses `[Before(Test)]`/`[After(Test)]` for per-test setup and teardown:

```csharp
using Microsoft.Playwright;
using TUnit.Core;

public class E2ETests : IAsyncDisposable
{
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IPage _page = null!;

    [Before(Test)]
    public async Task SetUp()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync();
        _page = await _browser.NewPageAsync();
    }

    [After(Test)]
    public async Task TearDown()
    {
        if (_browser is not null)
            await _browser.DisposeAsync();
        _playwright?.Dispose();
    }

    public async ValueTask DisposeAsync() => GC.SuppressFinalize(this);

    [Test]
    public async Task Counter_IncrementsOnClick()
    {
        await _page.GotoAsync("http://localhost:5000");

        await _page.ClickAsync("text=+");
        await _page.ClickAsync("text=+");

        var count = await _page.TextContentAsync("[data-testid='count']");
        await Assert.That(count).IsEqualTo("2");
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
4. **Test edge cases** with `[Test]` plus repeated `[Arguments(...)]` (TUnit's data-driven tests)
5. **Avoid testing framework internals** — Test behavior, not structure

## See Also

- [Pure Functions](../concepts/pure-functions.md) — Why pure functions are testable
- [Commands and Effects](../concepts/commands-effects.md) — Testing interpreters
- [Conduit E2E Fixture Architecture](./conduit-e2e-fixture-architecture.md) — Real project fixture, seeding, and user-journey coverage patterns
- [`visual-regression.yml`](../../.github/workflows/visual-regression.yml) — CI workflow for the visual harness
