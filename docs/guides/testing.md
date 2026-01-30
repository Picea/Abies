# Testing Guide

Abies applications are highly testable because they separate pure logic from side effects.

## Overview

The MVU architecture enables three levels of testing:

| Level | What to Test | Speed | Dependencies |
| ----- | ------------ | ----- | ------------ |
| Unit | Update logic, pure functions | Fast | None |
| DOM | View output, interactions | Fast | Test harness |
| E2E | Full application | Slow | Browser |

## Unit Testing

### Testing Update Functions

Update functions are pure and can be tested directly:

```csharp
using Xunit;

public class CounterTests
{
    [Fact]
    public void Increment_IncreasesCount()
    {
        // Arrange
        var model = new Model(Count: 5);
        
        // Act
        var (newModel, command) = Counter.Update(new Increment(), model);
        
        // Assert
        Assert.Equal(6, newModel.Count);
        Assert.IsType<Commands.None>(command);
    }
    
    [Fact]
    public void Decrement_DecreasesCount()
    {
        var model = new Model(Count: 5);
        
        var (newModel, _) = Counter.Update(new Decrement(), model);
        
        Assert.Equal(4, newModel.Count);
    }
    
    [Fact]
    public void Reset_SetsCountToZero()
    {
        var model = new Model(Count: 42);
        
        var (newModel, _) = Counter.Update(new Reset(), model);
        
        Assert.Equal(0, newModel.Count);
    }
}
```

### Testing with Commands

Verify that correct commands are returned:

```csharp
[Fact]
public void SubmitForm_ReturnsApiCommand()
{
    var model = new Model(
        Email: "test@example.com",
        Password: "password123"
    );
    
    var (newModel, command) = Login.Update(new SubmitForm(), model);
    
    Assert.True(newModel.IsSubmitting);
    Assert.IsType<LoginApiCommand>(command);
    
    var apiCommand = (LoginApiCommand)command;
    Assert.Equal("test@example.com", apiCommand.Email);
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
        
        var (newModel, _) = Editor.Update(new TitleChanged(title), model);
        
        Assert.Equal(isValid, newModel.IsValid);
    }
    
    [Fact]
    public void AddTag_AppendsToTagList()
    {
        var model = new EditorModel(
            Title: "Test",
            Body: "Content",
            Tags: new[] { "existing" }
        );
        
        var (newModel, _) = Editor.Update(new AddTag("new-tag"), model);
        
        Assert.Equal(new[] { "existing", "new-tag" }, newModel.Tags);
    }
}
```

## DOM Testing

Test the virtual DOM without a browser using the test harness pattern.

### MvuDomTestHarness

A minimal test harness for DOM assertions:

```csharp
using Abies.DOM;

public static class MvuDomTestHarness
{
    // Predicates for finding elements
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
    
    // Combine predicates
    public static Func<Element, bool> And(
        this Func<Element, bool> left, 
        Func<Element, bool> right) 
        => el => left(el) && right(el);
    
    // Find elements
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

### Testing Interactions

Simulate user interactions and verify state changes:

```csharp
[Fact]
public void ClickingPage2_SetsActivePageAndAriaCurrent()
{
    // Arrange: initial state with pagination
    var model = new Model(
        Articles: [],
        ArticlesCount: 20,  // 2 pages
        CurrentPage: 0
    );
    
    // Act: click the "2" button
    var (nextModel, _) = MvuDomTestHarness.DispatchClick(
        model,
        HomePage.View,
        HomePage.Update,
        elementPredicate: el => 
            el.Tag == "button" && 
            el.Children.OfType<Text>().Any(t => t.Value == "2")
    );
    
    var nextDom = HomePage.View(nextModel);
    
    // Assert: model AND DOM are correct
    Assert.Equal(1, nextModel.CurrentPage);
    
    var activeButton = MvuDomTestHarness.FindFirstElement(
        nextDom,
        el => el.Tag == "button" && 
              el.Attributes.Any(a => a.Name == "aria-current" && a.Value == "page")
    );
    
    Assert.Contains(activeButton.Children.OfType<Text>(), t => t.Value == "2");
}
```

### DispatchClick Implementation

```csharp
public static (TModel model, Command command) DispatchClick<TModel>(
    TModel model,
    Func<TModel, Document> view,
    Func<Message, TModel, (TModel, Command)> update,
    Func<Element, bool> elementPredicate)
{
    var dom = view(model);
    var (element, handler) = FindFirstHandler(dom.Body, "click", elementPredicate);
    
    if (handler.Command is not null)
        return update(handler.Command, model);
    
    throw new NotSupportedException("Handler requires event data");
}
```

### Testing Input

```csharp
[Fact]
public void TypingEmail_UpdatesModel()
{
    var model = new LoginModel(Email: "", Password: "");
    
    var (nextModel, _) = MvuDomTestHarness.DispatchInput(
        model,
        LoginPage.View,
        LoginPage.Update,
        MvuDomTestHarness.HasAttribute("name", "email"),
        value: "test@example.com"
    );
    
    Assert.Equal("test@example.com", nextModel.Email);
}
```

### Testing Form Submission

```csharp
[Fact]
public void SubmittingLoginForm_SendsLoginCommand()
{
    var model = new LoginModel(
        Email: "test@example.com",
        Password: "password"
    );
    
    var (_, command) = MvuDomTestHarness.DispatchSubmit(
        model,
        LoginPage.View,
        LoginPage.Update,
        MvuDomTestHarness.HasTag("form")
    );
    
    Assert.IsType<LoginApiCommand>(command);
}
```

## Integration Testing

Test complete user journeys with fake services.

### Fake HTTP Handler

```csharp
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> _responses = new();
    
    public void Setup(string path, object response)
    {
        _responses[path] = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(response)
        };
    }
    
    public void SetupError(string path, HttpStatusCode status)
    {
        _responses[path] = _ => new HttpResponseMessage(status);
    }
    
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? "";
        
        if (_responses.TryGetValue(path, out var handler))
            return Task.FromResult(handler(request));
        
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
```

### Using Fake APIs

```csharp
public class ArticleJourneyTests
{
    private readonly FakeHttpMessageHandler _fakeHandler = new();
    private readonly HttpClient _httpClient;
    
    public ArticleJourneyTests()
    {
        _httpClient = new HttpClient(_fakeHandler)
        {
            BaseAddress = new Uri("https://api.example.com")
        };
    }
    
    [Fact]
    public async Task ViewArticle_ShowsContent()
    {
        // Arrange
        _fakeHandler.Setup("/api/articles/test-slug", new ArticleResponse
        {
            Article = new Article
            {
                Title = "Test Article",
                Body = "Article content here"
            }
        });
        
        var model = new ArticleModel(Slug: "test-slug", Article: null);
        
        // Act: Execute the load command
        var loadCommand = new LoadArticleCommand("test-slug");
        await ExecuteCommand(loadCommand);
        
        // Assert
        Assert.NotNull(model.Article);
        Assert.Equal("Test Article", model.Article.Title);
    }
}
```

### Stateful Fake API

For complex journeys, maintain state across requests:

```csharp
public class StatefulFakeApi
{
    public List<Article> Articles { get; } = new();
    public List<User> Users { get; } = new();
    public Dictionary<string, List<Comment>> Comments { get; } = new();
    
    public HttpMessageHandler CreateHandler() => new DelegatingHandler
    {
        InnerHandler = new HttpClientHandler()
    };
    
    public void SeedArticles(params Article[] articles)
    {
        Articles.AddRange(articles);
    }
    
    // Handle requests based on current state
    public HttpResponseMessage Handle(HttpRequestMessage request)
    {
        return request.RequestUri?.AbsolutePath switch
        {
            "/api/articles" => JsonResponse(new { articles = Articles }),
            var p when p.StartsWith("/api/articles/") => HandleArticle(p),
            _ => new HttpResponseMessage(HttpStatusCode.NotFound)
        };
    }
}
```

## E2E Testing with Playwright

For full browser testing:

```csharp
using Microsoft.Playwright;
using Xunit;

public class E2ETests : IAsyncLifetime
{
    private IPlaywright _playwright;
    private IBrowser _browser;
    private IPage _page;
    
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
    public async Task HomePage_ShowsArticles()
    {
        await _page.GotoAsync("http://localhost:5000");
        
        await _page.WaitForSelectorAsync("[data-testid='article-list']");
        
        var articles = await _page.QuerySelectorAllAsync("[data-testid='article-preview']");
        Assert.True(articles.Count > 0);
    }
    
    [Fact]
    public async Task Login_NavigatesToHome()
    {
        await _page.GotoAsync("http://localhost:5000/login");
        
        await _page.FillAsync("[name='email']", "test@example.com");
        await _page.FillAsync("[name='password']", "password");
        await _page.ClickAsync("[type='submit']");
        
        await _page.WaitForURLAsync("**/");
        Assert.Contains("/", _page.Url);
    }
}
```

## Running Tests

### Unit Tests

```bash
dotnet test Abies.Tests
```

### Integration Tests

```bash
dotnet test Abies.Conduit.IntegrationTests
```

### E2E Tests

```bash
# Start the application first
dotnet run --project Abies.Conduit

# In another terminal
dotnet test Abies.Conduit.E2E
```

### All Tests

```bash
dotnet test
```

## Best Practices

### 1. Test Pure Functions First

Most bugs are in logic, not rendering:

```csharp
// High value: test Update logic
[Fact]
public void AddToCart_IncreasesTotal() { }

// Lower value: test View renders correctly
[Fact]
public void CartView_ShowsTotal() { }
```

### 2. Use Test IDs

Add `data-testid` attributes for stable selectors:

```csharp
div(
    attribute("data-testid", "article-preview"),
    h1(text(article.Title))
)
```

### 3. Keep Integration Tests Focused

One journey per test:

```csharp
// ✅ Focused
[Fact]
public void User_CanFavoriteArticle() { }

// ❌ Too broad
[Fact]
public void User_CanLoginAndFavoriteAndComment() { }
```

### 4. Test Edge Cases

```csharp
[Theory]
[InlineData(0, 0)]           // Empty cart
[InlineData(1, 10)]          // Single item
[InlineData(100, 1000)]      // Many items
public void Cart_CalculatesTotal(int items, decimal expected) { }
```

### 5. Avoid Testing Framework Internals

```csharp
// ❌ Don't test virtual DOM structure details
Assert.Equal("div", element.Tag);

// ✅ Test behavior
Assert.Contains("42", GetDisplayedCount());
```

## Test Organization

```
MyApp.Tests/
├── Unit/
│   ├── UpdateTests.cs       # Update function tests
│   ├── ValidationTests.cs   # Validation logic
│   └── RoutingTests.cs      # Route parsing
├── Integration/
│   ├── Testing/
│   │   ├── FakeHttpHandler.cs
│   │   └── MvuDomTestHarness.cs
│   ├── LoginJourneyTests.cs
│   └── ArticleJourneyTests.cs
└── E2E/
    ├── HomePageTests.cs
    └── AuthFlowTests.cs
```

## See Also

- [Concepts: Pure Functions](../concepts/pure-functions.md) — Why pure functions are testable
- [API: Update](../api/program.md#update) — Update function signature
- [Tutorial: Counter App](../tutorials/01-counter-app.md) — Simple testable example
