# Tutorial 7: Real-World App

Explore the Conduit sample — a full-featured RealWorld app that demonstrates production patterns with Abies.

**Prerequisites:** Complete tutorials 1–6

**Time:** 45 minutes (reading and exploration)

**What you'll learn:**

- Structuring a large Abies application
- Discriminated unions for page state
- Flat message and command organization
- The interpreter pattern at scale
- Route-based data loading
- Delegated view rendering

## What is Conduit?

Conduit is the [RealWorld](https://github.com/gothinkster/realworld) specification — a Medium.com clone with:

- User authentication (login, register, logout)
- Article CRUD (create, read, update, delete)
- Comments on articles
- User profiles with follow/unfollow
- Article favoriting
- Tag-based filtering
- Pagination

It demonstrates how all the concepts from previous tutorials come together in a real application.

## Running Conduit

For the canonical run matrix (AppHost, API, WASM, and test commands), see [Conduit README](../../Picea.Abies.Conduit/README.md).

```bash
# Start the API server
dotnet run --project Picea.Abies.Conduit.Api

# In another terminal, start the frontend
dotnet run --project Picea.Abies.Conduit.Wasm.Host
```

## Project Structure

The Conduit frontend is split into a platform-independent library and a WASM host:

```text
Picea.Abies.Conduit.App/        # Platform-independent MVU program
├── Conduit.cs                  # Main Program implementation
├── Model.cs                    # Application state (all record types)
├── Messages.cs                 # All message types
├── Commands.cs                 # All command types
├── Interpreter.cs              # HTTP interpreter (side effects)
├── Route.cs                    # URL → Page routing
├── ConduitJsonContext.cs       # AOT-safe JSON serialization
│
├── Pages/                      # Page-specific view functions
│   ├── Home.cs
│   ├── Article.cs
│   ├── Editor.cs
│   ├── Login.cs
│   ├── Register.cs
│   ├── Profile.cs
│   └── Settings.cs
│
└── Views/                      # Shared view components
    ├── Layout.cs
    └── ArticlePreview.cs

Picea.Abies.Conduit.Wasm.Host/  # Browser host
└── Program.cs                  # await Runtime.Run<ConduitProgram, Model, Unit>(...)
```

**Key organizational principle:** The app has no nested modules for messages, commands, or models. Everything is flat records at the namespace level. This works well in C# because record types are concise, and pattern matching provides the dispatch.

## Key Pattern 1: Discriminated Union for Pages

The `Page` type is a sealed record hierarchy that represents every possible page:

```csharp
// From Model.cs
public abstract record Page
{
    private Page() { }  // prevent external inheritance

    public sealed record Home(HomeModel Data) : Page;
    public sealed record Login(LoginModel Data) : Page;
    public sealed record Register(RegisterModel Data) : Page;
    public sealed record Article(ArticleModel Data) : Page;
    public sealed record Settings(SettingsModel Data) : Page;
    public sealed record Editor(EditorModel Data) : Page;
    public sealed record Profile(ProfileModel Data) : Page;
    public sealed record NotFound : Page;
}
```

Each page variant holds its own sub-model with page-specific state. The top-level model holds the current page:

```csharp
public sealed record Model(
    Page Page,
    Session? Session,
    string ApiUrl);
```

**Why this works:** The `Page` type makes impossible states unrepresentable. You can't be on the login page and the editor page simultaneously. Pattern matching on `Page` in the view is exhaustive — the compiler warns you if you miss a case.

## Key Pattern 2: Flat Messages

All messages are flat records implementing a marker interface:

```csharp
// From Messages.cs
public interface ConduitMessage : Message;

// Form inputs
public sealed record LoginEmailChanged(string Value) : ConduitMessage;
public sealed record LoginPasswordChanged(string Value) : ConduitMessage;
public sealed record LoginSubmitted : ConduitMessage;

// API responses
public sealed record ArticlesLoaded(
    IReadOnlyList<ArticlePreviewData> Articles,
    int ArticlesCount) : ConduitMessage;
public sealed record UserAuthenticated(Session Session) : ConduitMessage;
public sealed record ApiError(IReadOnlyList<string> Errors) : ConduitMessage;

// UI interactions
public sealed record FeedTabChanged(FeedTab Tab, string? Tag = null) : ConduitMessage;
public sealed record ToggleFavorite(string Slug, bool Favorited) : ConduitMessage;
public sealed record Logout : ConduitMessage;
```

Messages are organized by domain in the source file (form inputs, API responses, UI interactions) but are structurally flat — no nested interfaces or namespace hierarchies.

## Key Pattern 3: Flat Commands

Same pattern for commands:

```csharp
// From Commands.cs
public interface ConduitCommand : Command;

public sealed record FetchArticles(
    string ApiUrl, string? Token,
    int Limit = 10, int Offset = 0,
    string? Tag = null, string? Author = null,
    string? Favorited = null) : ConduitCommand;

public sealed record LoginUser(
    string ApiUrl, string Email, string Password) : ConduitCommand;

public sealed record FavoriteArticle(
    string ApiUrl, string Token, string Slug) : ConduitCommand;
```

Commands carry all the data needed for execution — API URL, auth token, request parameters. They are self-contained descriptions of side effects.

## Key Pattern 4: The Transition Function

The main `Transition` function is a large pattern match. It handles navigation, form inputs, UI interactions, and API responses:

```csharp
// From Conduit.cs (simplified)
public static (Model, Command) Transition(Model model, Message message) =>
    message switch
    {
        // Navigation
        UrlChanged url => HandleUrlChanged(model, url),

        // Login form
        LoginEmailChanged msg when model.Page is Page.Login login =>
            (model with {
                Page = new Page.Login(login.Data with { Email = msg.Value })
            }, Commands.None),

        LoginSubmitted when model.Page is Page.Login login =>
            (model with {
                Page = new Page.Login(login.Data with { IsSubmitting = true, Errors = [] })
            }, new LoginUser(model.ApiUrl, login.Data.Email, login.Data.Password)),

        // API responses
        UserAuthenticated msg => HandleUserAuthenticated(model, msg),
        ArticlesLoaded msg => HandleArticlesLoaded(model, msg),
        ApiError msg => HandleApiError(model, msg),

        // UI interactions
        ToggleFavorite msg when model.Session is not null =>
            (model, msg.Favorited
                ? new UnfavoriteArticle(model.ApiUrl, model.Session.Token, msg.Slug)
                : new FavoriteArticle(model.ApiUrl, model.Session.Token, msg.Slug)),

        Logout =>
            (model with { Session = null, Page = new Page.Home(...) },
             Commands.Batch(
                 new FetchArticles(model.ApiUrl, null, Constants.ArticlesPerPage, 0),
                 new FetchTags(model.ApiUrl))),

        _ => (model, Commands.None)
    };
```

**Patterns to notice:**

- **Guard clauses with `when`**: `LoginEmailChanged msg when model.Page is Page.Login login` — ensures the message is only handled when the correct page is active
- **Nested `with` expressions**: `model with { Page = new Page.Login(login.Data with { Email = msg.Value }) }` — immutably updates nested state
- **Auth guards**: `when model.Session is not null` — prevents authenticated actions when not logged in
- **`Commands.Batch`**: Combines multiple side effects (fetch articles AND fetch tags on logout)

## Key Pattern 5: Route-Based Data Loading

Route parsing is a pure function that returns both the page state and the commands needed to load data:

```csharp
// From Route.cs
public static (Page Page, Command Command) FromUrl(
    Url url, Session? session, string apiUrl) =>
    url.Path switch
    {
        [] or [""] => HomeRoute(session, apiUrl),
        ["login"]  => LoginRoute(),
        ["article", var slug] => ArticleRoute(slug, session?.Token, apiUrl),
        ["profile", var username] =>
            ProfileRoute(username, false, session?.Token, apiUrl),
        _ => (new Page.NotFound(), Commands.None)
    };

private static (Page, Command) ArticleRoute(
    string slug, string? token, string apiUrl)
{
    var model = new ArticleModel(slug, null, [], "", true);
    return (new Page.Article(model), Commands.Batch(
        new FetchArticle(apiUrl, token, slug),
        new FetchComments(apiUrl, token, slug)));
}
```

When the user navigates to `/article/hello-world`, the router:

1. Creates a `Page.Article` with a loading state (no article data yet)
2. Returns `Commands.Batch(FetchArticle, FetchComments)` to load both pieces of data
3. The interpreter executes both commands concurrently
4. The resulting `ArticleLoaded` and `CommentsLoaded` messages update the model

## Key Pattern 6: The Interpreter at Scale

The Conduit interpreter handles 15+ command types — all following the same structure:

```csharp
// From Interpreter.cs (simplified)
public static async ValueTask<Result<Message[], PipelineError>> Interpret(
    Command command)
{
    try
    {
        Message[] messages = command switch
        {
            FetchArticles cmd => await HandleFetchArticles(cmd),
            FetchArticle cmd => await HandleFetchArticle(cmd),
            LoginUser cmd => await HandleLogin(cmd),
            RegisterUser cmd => await HandleRegister(cmd),
            FavoriteArticle cmd => await HandleFavorite(cmd),
            UnfavoriteArticle cmd => await HandleUnfavorite(cmd),
            FollowUser cmd => await HandleFollow(cmd),
            AddComment cmd => await HandleAddComment(cmd),
            DeleteArticleCommand cmd => await HandleDeleteArticle(cmd),
            CreateArticle cmd => await HandleCreateArticle(cmd),
            // ... more command types
            _ => []
        };

        return Result<Message[], PipelineError>.Ok(messages);
    }
    catch (Exception ex)
    {
        return Result<Message[], PipelineError>.Ok(
            [new ApiError([$"Network error: {ex.Message}"])]);
    }
}

private static async Task<Message[]> HandleFetchArticles(FetchArticles cmd)
{
    var query = BuildArticleQuery(cmd.Limit, cmd.Offset, cmd.Tag, cmd.Author, cmd.Favorited);
    using var request = CreateRequest(HttpMethod.Get, $"{cmd.ApiUrl}/api/articles{query}", cmd.Token);
    using var response = await _http.SendAsync(request);

    if (!response.IsSuccessStatusCode)
        return [new ApiError(await ReadErrors(response))];

    var dto = await response.Content.ReadFromJsonAsync(
        ConduitJsonContext.Default.MultipleArticlesDto);
    return dto is null ? [] :
        [new ArticlesLoaded(dto.Articles.Select(MapArticlePreview).ToList(), dto.ArticlesCount)];
}
```

**Patterns to notice:**

- **DTO → Domain mapping**: The interpreter maps JSON DTOs to domain data types used by the model
- **AOT-safe JSON**: Uses source-generated `JsonSerializerContext` for trimming/AOT compatibility
- **Error normalization**: All HTTP errors are mapped to `ApiError(errors)` messages
- **Auth headers**: `CreateRequest` attaches the Bearer token when present

## Key Pattern 7: Delegated View Rendering

The main `View` function delegates to page-specific view functions:

```csharp
// From Conduit.cs
public static Document View(Model model)
{
    var content = model.Page switch
    {
        Page.Home home => Pages.Home.View(home.Data, model.Session),
        Page.Login login => Pages.Login.View(login.Data),
        Page.Register reg => Pages.Register.View(reg.Data),
        Page.Article art => Pages.Article.View(art.Data, model.Session),
        Page.Settings settings => Pages.Settings.View(settings.Data),
        Page.Editor editor => Pages.Editor.View(editor.Data),
        Page.Profile profile => Pages.Profile.View(profile.Data, model.Session),
        Page.NotFound => div([], [text("Page not found.")]),
        _ => div([], [text("Coming soon...")])
    };

    var title = model.Page switch
    {
        Page.Home => "Conduit",
        Page.Login => "Sign in — Conduit",
        Page.Article { Data.Article: not null } art =>
            $"{art.Data.Article.Title} — Conduit",
        // ...
    };

    return new Document(title,
        Views.Layout.Page(model.Page, model.Session, content));
}
```

Each page view function receives only the data it needs — its page sub-model and optionally the session for auth-dependent rendering.

## Key Pattern 8: Navigation as Subscription

URL change handling is set up as a subscription:

```csharp
public static Subscription Subscriptions(Model model) =>
    Navigation.UrlChanges(url => new UrlChanged(url));
```

And handled as a regular message in `Transition`:

```csharp
UrlChanged url => HandleUrlChanged(model, url),

private static (Model, Command) HandleUrlChanged(Model model, UrlChanged msg)
{
    var (page, command) = Route.FromUrl(msg.Url, model.Session, model.ApiUrl);
    return (model with { Page = page }, command);
}
```

Programmatic navigation uses commands:

```csharp
// After successful login: navigate to home
var (page, command) = Route.FromUrl(Url.Root, msg.Session, model.ApiUrl);
return (newModel with { Page = page },
    Commands.Batch(command, Navigation.PushUrl(Url.Root)));
```

## Architecture Summary

| Layer | File | Responsibility |
| --- | --- | --- |
| **State** | `Model.cs` | Immutable records for all application state |
| **Events** | `Messages.cs` | What happened (user actions + API responses) |
| **Effects** | `Commands.cs` | What side effects to perform |
| **Logic** | `Conduit.cs` | Pure state machine (`Transition`) |
| **Routing** | `Route.cs` | URL → (Page, Command) pure function |
| **Side Effects** | `Interpreter.cs` | HTTP execution, DTO mapping |
| **UI** | `Pages/*.cs` | Page-specific view functions |
| **UI** | `Views/*.cs` | Shared view components |

All business logic lives in the pure `Transition` function. All side effects live in the interpreter. All rendering is in the view functions. The boundaries are crisp.

## Testing Strategy

Conduit uses multiple testing levels:

### Unit Tests

Test the pure `Transition` function and route parsing:

```csharp
[Fact]
public void LoginSubmitted_SetsSubmittingState_AndReturnsLoginCommand()
{
    var model = CreateModel(page: new Page.Login(
        new LoginModel("user@test.com", "password", [], false)));

    var (newModel, command) = ConduitProgram.Transition(model, new LoginSubmitted());

    var login = Assert.IsType<Page.Login>(newModel.Page);
    Assert.True(login.Data.IsSubmitting);
    Assert.IsType<LoginUser>(command);
}

[Fact]
public void FromUrl_ArticlePath_ReturnsArticlePage_WithFetchCommands()
{
    var url = new Url(["article", "hello-world"],
        new Dictionary<string, string>(), Option<string>.None);

    var (page, command) = Route.FromUrl(url, session: null, apiUrl: "http://api");

    var article = Assert.IsType<Page.Article>(page);
    Assert.Equal("hello-world", article.Data.Slug);
    Assert.True(article.Data.IsLoading);
}
```

### Integration Tests

Test the interpreter with mocked HTTP:

```csharp
[Fact]
public async Task Interpret_LoginUser_ReturnsUserAuthenticated()
{
    var handler = SetupLoginResponse("token123");
    var command = new LoginUser("http://api", "user@test.com", "password");

    var result = await ConduitInterpreter.Interpret(command);

    var messages = result.Match(ok => ok, _ => []);
    var auth = Assert.IsType<UserAuthenticated>(Assert.Single(messages));
    Assert.Equal("token123", auth.Session.Token);
}
```

### E2E Tests

Test in a real browser with Playwright:

```csharp
[Fact]
public async Task CanRegisterAndCreateArticle()
{
    await Page.GotoAsync("/register");
    await Page.FillAsync("[name=username]", "testuser");
    await Page.FillAsync("[name=email]", "test@test.com");
    await Page.FillAsync("[name=password]", "password123");
    await Page.ClickAsync("button[type=submit]");

    await Expect(Page).ToHaveURLAsync("/");
}
```

## Explore the Code

1. **Start with `Conduit.cs`** — Read the `Transition` function to see how all messages are handled
2. **Study `Model.cs`** — See the page discriminated union and sub-models
3. **Read `Interpreter.cs`** — Understand how commands become HTTP calls
4. **Check `Route.cs`** — See how URLs map to pages with initial data loading
5. **Browse `Pages/`** — See page-specific view functions

## Exercises

1. **Add article search** — Add a search input to the home page that filters articles by title
2. **Add comment editing** — Allow users to edit their own comments
3. **Add dark mode** — Add a toggle that switches CSS classes, stored in the model
4. **Add loading skeletons** — Replace "Loading..." text with skeleton UI components

## Key Concepts

| Pattern | Purpose |
| --- | --- |
| Page discriminated union | One active page at a time, each with its own state |
| Flat messages/commands | Simple record types, organized by domain in source |
| Route-based data loading | URL → (Page, Command) pure function |
| Guard patterns in Transition | `when model.Page is Page.Login login` |
| Nested `with` expressions | Immutably update deeply nested state |
| Interpreter at scale | One function handling 15+ command types |
| `Commands.Batch` | Combine multiple side effects per transition |

## Next Steps

→ [Tutorial 8: Distributed Tracing](08-tracing.md) — Learn how to monitor and debug your application with OpenTelemetry