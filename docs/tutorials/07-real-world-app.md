# Tutorial 7: Real-World App

This tutorial walks through the Conduit sample—a full-featured RealWorld app implementation demonstrating production patterns.

**Prerequisites:** Complete tutorials 1-6

**Time:** 45 minutes (reading/exploration)

## What is Conduit?

Conduit is a "RealWorld" app implementation—a Medium.com clone with:

- User authentication (login, register, logout)
- Article CRUD (create, read, update, delete)
- Comments on articles
- User profiles with follow/unfollow
- Article favoriting
- Tag-based filtering
- Pagination

It demonstrates how all the concepts from previous tutorials come together.

## Running Conduit

```bash
# Terminal 1: Start the API server
dotnet run --project Abies.Conduit.Api

# Terminal 2: Start the frontend
dotnet run --project Abies.Conduit
```

Open `http://localhost:5209` in your browser.

## Project Structure

```text
Abies.Conduit/
├── Program.cs              # Entry point
├── Main.cs                 # Main Program implementation
├── Commands.cs             # Command handling
├── Navigation.cs           # URL handling helpers
├── Route.cs                # Route definitions
│
├── Page/                   # Page components
│   ├── Home.cs             # Home feed
│   ├── Article.cs          # Article detail
│   ├── Editor.cs           # Article editor
│   ├── Login.cs            # Login form
│   ├── Register.cs         # Registration form
│   ├── Profile.cs          # User profile
│   └── Settings.cs         # User settings
│
├── Services/               # API clients
│   ├── ApiClient.cs        # HTTP client wrapper
│   ├── ArticleService.cs   # Article operations
│   ├── AuthService.cs      # Authentication
│   └── ProfileService.cs   # Profile operations
│
└── Local/
    └── Storage.cs          # Browser storage
```

## Key Patterns

### 1. Global vs Page State

The main model owns global state; pages own their local state:

```csharp
// Main model (global state)
public record Model(
    Page Page,              // Current page component
    Route CurrentRoute,     // Current route
    User? CurrentUser       // Logged-in user
);

// Page is a sum type
public interface Page
{
    public sealed record Home(HomePage.Model Model) : Page;
    public sealed record Article(ArticlePage.Model Model) : Page;
    public sealed record Login(LoginPage.Model Model) : Page;
    // ...
}
```

### 2. Nested Messages

Messages are organized hierarchically:

```csharp
public interface Message : Abies.Message
{
    // Commands (side effects to perform)
    public interface Command : Message
    {
        public sealed record ChangeRoute(Route? Route) : Abies.Command;
        public sealed record LoadCurrentUser : Abies.Command;
    }

    // Events (things that happened)
    public interface Event : Message
    {
        public sealed record UrlChanged(Url Url) : Event;
        public sealed record LinkClicked(UrlRequest UrlRequest) : Event;
        public sealed record UserLoggedIn(User User) : Event;
        public sealed record UserLoggedOut : Event;
    }
}
```

### 3. Route-Based Initialization

Each route triggers appropriate data loading:

```csharp
private static (Model model, Command) HandleUrlChanged(Url url, Model model)
{
    var route = Route.FromUrl(url);
    
    // Create the appropriate page model
    var (nextModel, _) = GetNextModel(url, model.CurrentUser);
    
    // Get initialization command for the route
    var initCommand = GetInitCommandForRoute(nextModel);
    
    return (nextModel, initCommand);
}

private static Command GetInitCommandForRoute(Model model) =>
    model.Page switch
    {
        Page.Home => new HomePage.Command.LoadArticles(),
        Page.Article article => new ArticlePage.Command.LoadArticle(article.Model.Slug),
        Page.Profile profile => new ProfilePage.Command.LoadProfile(profile.Model.Username),
        _ => Commands.None
    };
```

### 4. Protected Routes

Some routes require authentication:

```csharp
private static bool RequiresAuth(Route route) =>
    route is Route.Settings
    || route is Route.NewArticle
    || route is Route.EditArticle;

private static (Model model, Command) HandleLinkClicked(UrlRequest request, Model model)
{
    if (request is UrlRequest.Internal @internal)
    {
        var (nextModel, _) = GetNextModel(@internal.Url, model.CurrentUser);
        
        // Redirect to login if auth required but not logged in
        if (RequiresAuth(nextModel.CurrentRoute) && nextModel.CurrentUser is null)
        {
            var loginUrl = Url.Create("/login");
            var (loginModel, _) = GetNextModel(loginUrl, null);
            return (loginModel, new Navigation.Command.PushState(loginUrl));
        }
        
        return (nextModel, new Navigation.Command.PushState(@internal.Url));
    }
    return (model, Commands.None);
}
```

### 5. Page Update Delegation

The main Update function delegates to page-specific updaters:

```csharp
public static (Model model, Command command) Update(Message message, Model model)
    => message switch
    {
        // Global events
        Message.Event.UrlChanged changed => HandleUrlChanged(changed.Url, model),
        Message.Event.UserLoggedIn user => HandleUserLoggedIn(user.User, model),
        Message.Event.UserLoggedOut => HandleUserLoggedOut(model),
        
        // Page-specific messages - delegate to page updater
        HomePage.Message homeMsg when model.Page is Page.Home home =>
            UpdateHomePage(homeMsg, home.Model, model),
        
        ArticlePage.Message articleMsg when model.Page is Page.Article article =>
            UpdateArticlePage(articleMsg, article.Model, model),
        
        // ... other pages
        
        _ => (model, Commands.None)
    };

private static (Model model, Command command) UpdateHomePage(
    HomePage.Message msg, 
    HomePage.Model pageModel, 
    Model model)
{
    var (newPageModel, command) = HomePage.Update(msg, pageModel);
    return (model with { Page = new Page.Home(newPageModel) }, command);
}
```

### 6. Service Layer

API calls are organized into service classes:

```csharp
// Services/ArticleService.cs
public static class ArticleService
{
    public static async Task<ArticlesResponse> GetArticles(
        HttpClient client, 
        int offset = 0, 
        int limit = 10,
        string? tag = null,
        string? author = null,
        string? favorited = null)
    {
        var query = $"?offset={offset}&limit={limit}";
        if (tag is not null) query += $"&tag={tag}";
        if (author is not null) query += $"&author={author}";
        if (favorited is not null) query += $"&favorited={favorited}";
        
        var response = await client.GetAsync($"/api/articles{query}");
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<ArticlesResponse>()
            ?? new ArticlesResponse([], 0);
    }
}
```

### 7. Command Handling

Commands are handled centrally:

```csharp
public static async Task HandleCommand(
    Command command, 
    Func<Message, ValueTuple> dispatch)
{
    switch (command)
    {
        // Navigation
        case Navigation.Command.PushState push:
            await Interop.PushState(push.Url.ToString());
            break;
        
        // Article loading
        case ArticlePage.Command.LoadArticle load:
            try
            {
                var article = await ArticleService.GetArticle(_client, load.Slug);
                dispatch(new ArticlePage.Message.ArticleLoaded(article));
            }
            catch (Exception ex)
            {
                dispatch(new ArticlePage.Message.LoadFailed(ex.Message));
            }
            break;
        
        // ... other commands
    }
}
```

### 8. View Composition

The main view composes the layout with page content:

```csharp
public static Document View(Model model)
{
    var title = GetTitle(model);
    
    return new(title,
        div([class_("app")], [
            Header(model.CurrentUser, model.CurrentRoute),
            MainContent(model),
            Footer()
        ]));
}

static Node Header(User? user, Route route) =>
    nav([class_("navbar")], [
        a([class_("brand"), href("/")], [text("conduit")]),
        user is null
            ? GuestNav()
            : UserNav(user, route)
    ]);

static Node MainContent(Model model) =>
    model.Page switch
    {
        Page.Home home => HomePage.View(home.Model),
        Page.Article article => ArticlePage.View(article.Model),
        Page.Login login => LoginPage.View(login.Model),
        Page.Register register => RegisterPage.View(register.Model),
        Page.Profile profile => ProfilePage.View(profile.Model),
        Page.Settings settings => SettingsPage.View(settings.Model),
        Page.NewArticle editor => EditorPage.View(editor.Model),
        Page.NotFound => NotFoundView(),
        _ => NotFoundView()
    };
```

## Page Component Example: Home

Here's a simplified version of the Home page:

```csharp
// Page/Home.cs
namespace Abies.Conduit.Page;

public static class HomePage
{
    public record Model(
        List<Article> Articles,
        int ArticlesCount,
        List<string> Tags,
        FeedTab ActiveTab,
        string? SelectedTag,
        bool IsLoading,
        int CurrentPage,
        User? CurrentUser
    );

    public enum FeedTab { Personal, Global, Tag }

    public interface Message : Abies.Message
    {
        public sealed record ArticlesLoaded(List<Article> Articles, int Count) : Message;
        public sealed record TagsLoaded(List<string> Tags) : Message;
        public sealed record TabChanged(FeedTab Tab) : Message;
        public sealed record TagSelected(string Tag) : Message;
        public sealed record PageChanged(int Page) : Message;
        public sealed record ToggleFavorite(string Slug, bool Favorited) : Message;
    }

    public interface Command : Abies.Command
    {
        public sealed record LoadArticles(FeedTab Tab, string? Tag, int Page) : Command;
        public sealed record LoadTags : Command;
        public sealed record Favorite(string Slug) : Command;
        public sealed record Unfavorite(string Slug) : Command;
    }

    public static (Model model, Abies.Command command) Update(Message msg, Model model)
        => msg switch
        {
            Message.ArticlesLoaded loaded =>
                (model with 
                { 
                    Articles = loaded.Articles,
                    ArticlesCount = loaded.Count,
                    IsLoading = false
                }, Commands.None),
            
            Message.TabChanged tab =>
                (model with { ActiveTab = tab.Tab, CurrentPage = 0, IsLoading = true },
                 new Command.LoadArticles(tab.Tab, null, 0)),
            
            Message.TagSelected tag =>
                (model with { ActiveTab = FeedTab.Tag, SelectedTag = tag.Tag, CurrentPage = 0, IsLoading = true },
                 new Command.LoadArticles(FeedTab.Tag, tag.Tag, 0)),
            
            Message.PageChanged page =>
                (model with { CurrentPage = page.Page, IsLoading = true },
                 new Command.LoadArticles(model.ActiveTab, model.SelectedTag, page.Page)),
            
            _ => (model, Commands.None)
        };

    public static Node View(Model model) =>
        div([class_("home-page")], [
            Banner(),
            div([class_("container")], [
                div([class_("row")], [
                    div([class_("col-md-9")], [
                        FeedToggle(model),
                        model.IsLoading
                            ? LoadingSpinner()
                            : ArticleList(model.Articles),
                        Pagination(model.CurrentPage, model.ArticlesCount)
                    ]),
                    div([class_("col-md-3")], [
                        TagsSidebar(model.Tags)
                    ])
                ])
            ])
        ]);

    static Node FeedToggle(Model model) =>
        div([class_("feed-toggle")], [
            ul([class_("nav")], [
                model.CurrentUser is not null
                    ? TabLink("Your Feed", FeedTab.Personal, model.ActiveTab)
                    : text(""),
                TabLink("Global Feed", FeedTab.Global, model.ActiveTab),
                model.ActiveTab == FeedTab.Tag
                    ? li([class_("nav-item")], [
                        a([class_("nav-link active")], [text($"#{model.SelectedTag}")])
                      ])
                    : text("")
            ])
        ]);

    static Node TabLink(string label, FeedTab tab, FeedTab active) =>
        li([class_("nav-item")], [
            a([
                class_(tab == active ? "nav-link active" : "nav-link"),
                onclick(new Message.TabChanged(tab))
            ], [text(label)])
        ]);
}
```

## Testing Strategy

Conduit uses multiple testing levels:

### Unit Tests (Abies.Tests)

Test pure functions in isolation:

```csharp
[Fact]
public void TabChanged_SetsActiveTab_AndReturnsLoadCommand()
{
    var model = new HomePage.Model(/* ... */);
    
    var (newModel, command) = HomePage.Update(
        new HomePage.Message.TabChanged(FeedTab.Personal), 
        model);
    
    Assert.Equal(FeedTab.Personal, newModel.ActiveTab);
    Assert.True(newModel.IsLoading);
    Assert.IsType<HomePage.Command.LoadArticles>(command);
}
```

### Integration Tests (Abies.Conduit.IntegrationTests)

Test complete user journeys with fake HTTP:

```csharp
[Fact]
public async Task User_CanLogin_AndSeePersonalFeed()
{
    // Arrange
    var fakeHttp = new FakeHttpHandler();
    fakeHttp.SetupLogin("user@test.com", "password", validToken);
    
    // Act
    var model = await SimulateLogin("user@test.com", "password");
    
    // Assert
    Assert.NotNull(model.CurrentUser);
    Assert.Equal("user", model.CurrentUser.Username);
}
```

### E2E Tests (Abies.Conduit.E2E)

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
    
    await Page.ClickAsync("text=New Article");
    // ...
}
```

## Key Takeaways

| Pattern | Purpose |
| ------- | ------- |
| Global + Page state | Separate concerns, share user/route |
| Nested messages | Organize by feature |
| Route-based init | Load data when route changes |
| Protected routes | Redirect unauthenticated users |
| Delegated updates | Each page handles its messages |
| Service layer | Encapsulate API calls |
| Central command handler | One place for side effects |

## Explore the Code

1. **Start with `Main.cs`** — See how routing and delegation work
2. **Study `Page/Home.cs`** — Understand page component structure
3. **Read `Commands.cs`** — See how effects are handled
4. **Check `Services/`** — Understand API abstraction

## Exercises

1. **Add article editing** — Modify the editor for updates
2. **Add comment deletion** — Implement the missing feature
3. **Add article search** — Add a search box to the home page
4. **Add error boundaries** — Handle and display errors gracefully

## Conclusion

You've completed the Abies tutorial series! You now understand:

- ✅ MVU architecture
- ✅ Pure functional state management
- ✅ Virtual DOM rendering
- ✅ Commands for side effects
- ✅ Subscriptions for events
- ✅ Routing and navigation
- ✅ Form handling
- ✅ Real-world application structure

Continue exploring:

- [API Reference](../api/program.md) — Detailed API documentation
- [Concepts](../concepts/mvu-architecture.md) — Deep dives
- [ADRs](../adr/README.md) — Design decisions
