---
description: 'E2E Fixture Architecture — How Conduit E2E tests are structured and parameterized'
---

# E2E Fixture Architecture

This document explains the architecture of the Conduit E2E test suite, including how fixtures are parameterized, how rollback works, and how to add new tests.

**Audience:** Contributors building E2E tests, maintainers adding scenarios

**Test Project:** `Picea.Abies.Conduit.Testing.E2E`

**Related:** [Testing Guide](../guides/testing.md), [Playwright Documentation](https://playwright.dev/)

---

## Overview

The Conduit E2E test suite uses **Playwright** with a fixture-based architecture for:

- **Per-scenario setup/teardown** — Create test data, seed database, authenticate
- **Parameterized URLs** — Test against browser (WASM), server, and auto modes
- **Rollback** — Ensure each test runs against a clean database state
- **Page objects** — Reusable abstractions for UI interaction
- **Error screenshots** — Automatic capture on failure for debugging

## Directory Structure

```
Picea.Abies.Conduit.Testing.E2E/
├── bin/                           # Compiled test assemblies
├── Fixtures/
│   ├── ConduitFixture.cs         # Base fixture: base URL, database setup, auth
│   ├── ArticleFixture.cs         # Article CRUD operations
│   ├── UserFixture.cs            # User registration, profile management
│   └── FavoriteFixture.cs        # Favorite/unfavorite operations
├── PageObjects/
│   ├── HomePage.cs               # Home page (article feed + navigation)
│   ├── LoginPage.cs              # Login form and flow
│   ├── ArticlePage.cs            # Article detail and actions
│   ├── EditorPage.cs             # Article creation/editing
│   ├── ProfilePage.cs            # User profile
│   └── SettingsPage.cs           # Settings page
├── Tests/
│   ├── AuthenticationTests.cs    # Login, signup, logout workflows
│   ├── ArticleTests.cs           # Create, edit, delete, favorite articles
│   ├── PaginationTests.cs        # Pagination and filtering
│   ├── FavoriteTests.cs          # Favorite/unfavorite logic
│   └── ProfileTests.cs           # Profile view and editing
└── appsettings.json              # Test configuration (base URLs, timeouts)
```

## Fixture Architecture

### Base Fixture: `ConduitFixture`

All tests inherit from `ConduitFixture`, which provides:

```csharp
public class ConduitFixture : IAsyncLifetime
{
    protected IBrowser Browser { get; }
    protected IPage Page { get; }
    
    // Configuration
    protected string BaseUrl { get; }
    protected string ApiBaseUrl { get; }
    
    // Lifecycle
    public async Task InitializeAsync() { ... }  // Called before each test
    public async Task DisposeAsync() { ... }     // Called after each test
    
    // Setup methods
    protected async Task Login(string email, string password) { ... }
    protected async Task Seed<T>(T entity) { ... }
    protected async Task RollbackDatabase() { ... }
    
    // Navigation
    protected async Task NavigateTo(string path) { ... }
    protected async Task WaitForNavigation(string expectedUrl) { ... }
    
    // Assertions
    protected void AssertUrlContains(string segment) { ... }
    protected async Task AssertElementVisible(string selector) { ... }
}
```

**Initialization sequence:**
1. Launch browser (via `BrowserFixture.GetBrowser()`)
2. Create new page/context
3. Set base URL from config
4. Clear cookies and localStorage
5. Create fresh DB connection

**Disposal sequence:**
1. Close page (auto-clears cookies, localStorage)
2. Return browser to pool (via `BrowserFixture`)
3. Rollback database changes (optional, depends on test)

### Database Rollback Pattern

Each test runs in a **database transaction**:

```csharp
public class ConduitFixture : IAsyncLifetime
{
    private IDbConnection _dbConnection;
    private IDbTransaction _transaction;
    
    public async Task InitializeAsync()
    {
        _dbConnection = CreateConnection();
        _transaction = _dbConnection.BeginTransaction();
    }
    
    public async Task DisposeAsync()
    {
        // Rollback all changes made during this test
        _transaction?.Rollback();
        _dbConnection?.Dispose();
    }
}
```

**Benefits:**
- ✅ No test pollution — each test sees a clean database
- ✅ Fast — rollback is O(1), no need to reset tables
- ✅ Isolated — concurrent tests don't interfere
- ✅ Deterministic — same seed data every time

### Parameterization: Render Modes

Tests can run against **multiple render modes** using xUnit theory fixtures:

```csharp
public class ArticleTests : ConduitFixture
{
    // Theory: runs test for each value in the data source
    [Theory]
    [ClassData(typeof(RenderModeTestData))]
    public async Task CanCreateArticle(string renderMode, string baseUrl)
    {
        // This test runs 3 times:
        // - Once for Browser (WASM): http://localhost:5209
        // - Once for Server: http://localhost:5000
        // - Once for Auto: http://localhost:5000 (server-first)
        
        BaseUrl = baseUrl;
        await NavigateTo("/editor");
        await Page.FillAsync("#title", "Test Article");
        // ... rest of test
    }
}

public class RenderModeTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { "browser", "http://localhost:5209" };
        yield return new object[] { "server", "http://localhost:5000" };
        yield return new object[] { "auto", "http://localhost:5000" };
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
```

**Test output:**
```
ArticleTests.CanCreateArticle(browser) — PASSED
ArticleTests.CanCreateArticle(server) — PASSED
ArticleTests.CanCreateArticle(auto) — PASSED
```

## Page Object Pattern

Page objects encapsulate UI interaction logic to keep tests readable:

### Example: `LoginPage`

```csharp
public class LoginPage
{
    private readonly IPage _page;
    
    public LoginPage(IPage page)
    {
        _page = page;
    }
    
    // Selectors
    private string EmailInput => "#email";
    private string PasswordInput => "#password";
    private string SubmitButton => "button[type=submit]";
    private string ErrorAlert => ".alert-danger";
    
    // Actions
    public async Task EnterEmail(string email)
    {
        await _page.FillAsync(EmailInput, email);
    }
    
    public async Task EnterPassword(string password)
    {
        await _page.FillAsync(PasswordInput, password);
    }
    
    public async Task SubmitForm()
    {
        await _page.ClickAsync(SubmitButton);
    }
    
    public async Task<HomePage> Login(string email, string password)
    {
        await EnterEmail(email);
        await EnterPassword(password);
        await SubmitForm();
        await _page.WaitForURLAsync("**/");
        return new HomePage(_page);
    }
    
    // Assertions
    public async Task<string> GetErrorMessage()
    {
        return await _page.TextContentAsync(ErrorAlert);
    }
    
    public async Task AssertErrorShown()
    {
        await _page.WaitForSelectorAsync(ErrorAlert);
    }
}
```

### Using Page Objects in Tests

```csharp
[Fact]
public async Task CanLoginWithValidCredentials()
{
    var loginPage = new LoginPage(Page);
    
    // High-level, readable test
    var homePage = await loginPage.Login("user@example.com", "password");
    
    // Assertions using page objects
    await homePage.AssertArticleFeedVisible();
}
```

**Benefits:**
- ✅ Tests are readable like user journeys
- ✅ Selector changes are isolated to page objects
- ✅ Reusable across multiple tests
- ✅ Refactoring is centralized

## Test Organization

### Authentication Tests (`AuthenticationTests.cs`)

Focus on user authentication flows:

```csharp
public class AuthenticationTests : ConduitFixture
{
    [Fact]
    public async Task CanSignUp()
    {
        // Navigate to signup
        await NavigateTo("/register");
        
        // Fill signup form
        await Page.FillAsync("#username", "newuser");
        await Page.FillAsync("#email", "new@example.com");
        await Page.FillAsync("#password", "password");
        
        // Submit
        await Page.ClickAsync("button[type=submit]");
        
        // Assert: user is logged in and on home page
        await AssertElementVisible(".feed-toggle");
        var username = await Page.TextContentAsync(".navbar-text");
        Assert.Contains("newuser", username);
    }
}
```

### Article Tests (`ArticleTests.cs`)

Cover article CRUD, favorites, and interactions:

```csharp
public class ArticleTests : ConduitFixture
{
    [Fact]
    public async Task CanCreateArticle()
    {
        // Setup: authenticate
        await Login("user@example.com", "password");
        
        // Create article
        await NavigateTo("/editor");
        await Page.FillAsync("#title", "Test Article");
        await Page.FillAsync("#description", "Test description");
        await Page.FillAsync("#body", "Article body");
        await Page.ClickAsync("button:has-text('Publish')");
        
        // Assert: article is published
        var articleTitle = await Page.TextContentAsync("h1");
        Assert.Equal("Test Article", articleTitle);
    }
}
```

### Pagination Tests (`PaginationTests.cs`)

Verify pagination and filtering:

```csharp
public class PaginationTests : ConduitFixture
{
    [Fact]
    public async Task CanNavigateArticlePages()
    {
        // Setup: seed many articles (e.g., 30)
        await Seed(new Article { Title = "Article 1", ... });
        // ... seed more
        
        // Navigate to home
        await NavigateTo("/");
        
        // Assert: first page shows 10 articles
        var articles = await Page.QuerySelectorAllAsync(".article-preview");
        Assert.Equal(10, articles.Count);
        
        // Click "next" (page 2)
        await Page.ClickAsync("li.next a");
        
        // Assert: second page shows 10 different articles
        articles = await Page.QuerySelectorAllAsync(".article-preview");
        Assert.Equal(10, articles.Count);
        
        // Verify URL changed
        AssertUrlContains("page=2");
    }
}
```

### Favorite Tests (`FavoriteTests.cs`)

Test favoriting and unfavoriting:

```csharp
public class FavoriteTests : ConduitFixture
{
    [Fact]
    public async Task CanFavoriteArticle()
    {
        // Setup
        await Login("user@example.com", "password");
        var article = await Seed(new Article { ... });
        
        // Navigate to article
        await NavigateTo($"/article/{article.Slug}");
        
        // Favorite button
        var favoriteBtn = await Page.QuerySelectorAsync(".btn-primary:has-text('Favorite')");
        Assert.NotNull(favoriteBtn);
        
        // Click favorite
        await favoriteBtn.ClickAsync();
        
        // Assert: button state changed (e.g., color, text)
        var newBtn = await Page.QuerySelectorAsync(".btn-outline-primary:has-text('Favorite')");
        Assert.NotNull(newBtn);
        
        // Assert: favorite count increased
        var count = await Page.TextContentAsync(".btn-outline-primary");
        Assert.Contains("1", count);
    }
}
```

## Configuration

### `appsettings.json`

```json
{
  "E2E": {
    "BrowserUrl": "http://localhost:5209",
    "ServerUrl": "http://localhost:5000",
    "ApiUrl": "http://localhost:5179",
    "Headless": true,
    "SlowMo": 0,
    "Timeout": 30000,
    "NavigationTimeout": 30000,
    "Database": {
      "Host": "localhost",
      "Port": 5432,
      "Database": "conduit_test",
      "User": "conduit",
      "Password": "secret"
    }
  }
}
```

### Overrides via Environment Variables

```bash
# Run tests in headed mode (browser visible)
HEADED=1 dotnet test

# Run tests with slow-motion (150ms delay)
PW_SLOWMO_MS=150 dotnet test

# Use different base URL
E2E__BrowserUrl=http://staging:5209 dotnet test
```

## Running Tests

### Run All Tests

```bash
cd Picea.Abies.Conduit.Testing.E2E
dotnet test -c Debug -v minimal
```

### Run Specific Test Class

```bash
dotnet test --filter "FullyQualifiedName~FavoriteTests"
```

### Run with Headed Browser (UI Visible)

```bash
HEADED=1 dotnet test
```

### Run with Slow-Motion (150ms between actions)

```bash
PW_SLOWMO_MS=150 dotnet test
```

### Run in Debug Mode with Full Output

```bash
dotnet test -v detailed
```

### Run Single Test and Save Results

```bash
dotnet test --filter "FullyQualifiedName~FavoriteTests.CanFavoriteArticle" -l trx
```

Results are saved to `TestResults/` directory.

## Adding a New Test

### Step 1: Choose Test Class

- **Authentication flow?** → `AuthenticationTests.cs`
- **Article CRUD?** → `ArticleTests.cs`
- **Pagination/filtering?** → `PaginationTests.cs`
- **Favoriting?** → `FavoriteTests.cs`
- **Profile?** → `ProfileTests.cs`
- **New feature?** → Create new test class, e.g., `CommentTests.cs`

### Step 2: Write the Test

```csharp
[Fact]
public async Task CanUnfavoriteSigArticle()
{
    // Setup
    var user = await Seed(new User { Email = "user@example.com", Password = "password" });
    var article = await Seed(new Article { Slug = "test-article", ... });
    await Seed(new Favorite { UserId = user.Id, ArticleId = article.Id });
    
    // Navigate and authenticate
    await NavigateTo("/");
    var loginPage = new LoginPage(Page);
    await loginPage.Login(user.Email, "password");
    
    // Navigate to article
    await NavigateTo($"/article/{article.Slug}");
    
    // Unfavorite
    var favoriteBtn = await Page.QuerySelectorAsync(".btn-outline-primary:has-text('Favorite')");
    await favoriteBtn.ClickAsync();
    
    // Assert: button state changed back to primary outline
    var newBtn = await Page.QuerySelectorAsync(".btn-primary:has-text('Favorite')");
    Assert.NotNull(newBtn);
}
```

### Step 3: Add Page Objects (if needed)

Create a new page object in `PageObjects/`:

```csharp
public class FavoritesPage
{
    private readonly IPage _page;
    
    public FavoritesPage(IPage page)
    {
        _page = page;
    }
    
    public async Task ClickFavoriteButton()
    {
        await _page.ClickAsync(".btn:has-text('Favorite')");
    }
    
    public async Task AssertFavoritedState()
    {
        await _page.WaitForSelectorAsync(".btn-outline-primary:has-text('Favorite')");
    }
}
```

### Step 4: Run and Debug

```bash
# Run your test
HEADED=1 PW_SLOWMO_MS=200 dotnet test --filter "FullyQualifiedName~YourTest"

# If it fails, check:
# 1. Browser window for visual feedback (HEADED=1)
# 2. Test error output for timeout/selector issues
# 3. Screenshot if generated in TestResults/
```

## Debugging Tips

### Take a Screenshot

```csharp
await Page.ScreenshotAsync(new PageScreenshotOptions
{
    Path = "screenshot.png",
    FullPage = true
});
```

### Inspect Element

```bash
# Run in debug mode with browser inspector
PWDEBUG=1 dotnet test
```

### Wait for Selector with Timeout

```csharp
// Fail fast if element doesn't appear within 5s
await Page.WaitForSelectorAsync(".expected-element", 
    new PageWaitForSelectorOptions { Timeout = 5000 });
```

### Log Network Requests

```csharp
Page.Response += async (sender, response) =>
{
    Console.WriteLine($"{response.Request.Method} {response.Url} — {response.Status}");
};
```

### Pause Test Execution

```csharp
// Pause so you can inspect browser state
await Page.PauseAsync();
```

## Common Patterns

### Loop Through Elements

```csharp
var articles = await Page.QuerySelectorAllAsync(".article-preview");
foreach (var article in articles)
{
    var title = await article.TextContentAsync();
    Console.WriteLine(title);
}
```

### Wait for Network Idle

```csharp
await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
```

### Verify API Call

```csharp
var apiCalled = false;
Page.RequestFailed += (sender, req) =>
{
    if (req.Url.Contains("/api/articles"))
    {
        apiCalled = true;
    }
};

await Page.ClickAsync(".refresh-btn");
await Page.WaitForTimeoutAsync(1000);
Assert.True(apiCalled, "API call was not made");
```

## Troubleshooting

### Test Hangs on Navigation

**Cause:** Page didn't navigate where expected.

```csharp
// Add explicit wait
await Page.WaitForURLAsync("**/expected-path**");
```

### Selector Not Found

**Cause:** Element ID or class changed, or element is hidden.

```csharp
// Add logging
Console.WriteLine(await Page.ContentAsync());

// Use wildcard selectors
await Page.ClickAsync("[data-testid='submit']");

// Wait for element to appear
await Page.WaitForSelectorAsync(".submit-btn", new PageWaitForSelectorOptions { Timeout = 5000 });
```

### Test Works Locally but Fails in CI

**Cause:** Timing, race conditions, or URL differences.

```csharp
// Add delays for CI
if (Environment.GetEnvironmentVariable("CI") == "true")
{
    await Page.WaitForTimeoutAsync(500);
}

// Use explicit waits instead of sleeps
await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
```

## Source Files

| File | Purpose |
|------|---------|
| `ConduitFixture.cs` | Base fixture with DB setup, rollback, auth helpers |
| `PageObjects/*.cs` | Reusable page object abstractions |
| `Tests/*.cs` | Test classes organized by feature |
| `appsettings.json` | Test configuration (URLs, timeouts, DB connection) |

## See Also

- [Testing Guide](../guides/testing.md) — Broader testing strategies
- [Conduit README](../../Picea.Abies.Conduit/README.md) — Project overview and running instructions
- [Playwright API](https://playwright.dev/dotnet/docs/api/class-page) — Full Playwright documentation
- [Conduit Specification](https://docs.realworld.show/) — Feature specification for E2E scenarios

## Implementation Notes

**Tracking Issue:** [#218: E2E Fixture Architecture Guide](https://github.com/Picea/Abies/issues/218)

Last updated: 2026-04-12
