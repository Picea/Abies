# Tutorial 3: API Integration

This tutorial teaches you to work with external APIs using Commands for side effects.

**Prerequisites:** [Tutorial 2: Todo List](./02-todo-list.md)

**Time:** 30 minutes

## What You'll Build

A post viewer that:

- Fetches posts from a REST API
- Displays loading and error states
- Refreshes data on demand
- Shows post details

## The Command Pattern

In Abies, side effects (API calls, storage, timers) are expressed as **Commands**:

1. `Update` returns a Command describing *what* to do
2. `HandleCommand` executes the effect and dispatches a result Message
3. `Update` handles the result Message

This keeps `Update` pure and testable.

```text
┌──────────────────────────────────────────────────────────────┐
│                                                              │
│  User clicks     Update returns     HandleCommand runs       │
│  "Refresh" ────▶ FetchPosts ──────▶ HTTP GET /posts          │
│                                           │                  │
│                                           ▼                  │
│  Update handles  ◀──────────────── dispatch(PostsLoaded)    │
│  PostsLoaded                                                 │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

## Step 1: Define Types

Model the API response and application state:

```csharp
// API response type
public record Post(int Id, int UserId, string Title, string Body);

// Loading state
public record LoadingState
{
    public sealed record Idle : LoadingState;
    public sealed record Loading : LoadingState;
    public sealed record Failed(string Error) : LoadingState;
    public sealed record Success : LoadingState;
}

// Application model
public record Model(
    List<Post> Posts,
    LoadingState State,
    Post? SelectedPost
);
```

Using a `LoadingState` sum type makes it impossible to be "loading" and "failed" simultaneously.

## Step 2: Define Messages and Commands

```csharp
// Commands (side effects to perform)
public record FetchPosts : Command;
public record FetchPost(int Id) : Command;

// Messages (results and user actions)
public record PostsLoaded(List<Post> Posts) : Message;
public record PostLoadFailed(string Error) : Message;
public record PostSelected(Post Post) : Message;
public record RefreshClicked : Message;
public record ClearSelection : Message;
```

## Step 3: Implement Update

```csharp
public static (Model model, Command command) Update(Message message, Model model)
    => message switch
    {
        // User clicked refresh - start loading
        RefreshClicked => 
            (model with { State = new LoadingState.Loading() }, 
             new FetchPosts()),
        
        // Posts loaded successfully
        PostsLoaded loaded => 
            (model with 
            { 
                Posts = loaded.Posts,
                State = new LoadingState.Success()
            }, Commands.None),
        
        // Loading failed
        PostLoadFailed failed => 
            (model with { State = new LoadingState.Failed(failed.Error) }, 
             Commands.None),
        
        // User selected a post
        PostSelected selected => 
            (model with { SelectedPost = selected.Post }, 
             Commands.None),
        
        // Clear selection
        ClearSelection => 
            (model with { SelectedPost = null }, 
             Commands.None),
        
        _ => (model, Commands.None)
    };
```

Notice: `RefreshClicked` returns `new FetchPosts()` as the command. The actual HTTP call happens in `HandleCommand`.

## Step 4: Implement HandleCommand

This is where side effects actually happen:

```csharp
private static readonly HttpClient _httpClient = new();

public static async Task HandleCommand(Command command, Func<Message, ValueTuple> dispatch)
{
    switch (command)
    {
        case FetchPosts:
            try
            {
                var response = await _httpClient.GetAsync(
                    "https://jsonplaceholder.typicode.com/posts");
                response.EnsureSuccessStatusCode();
                
                var json = await response.Content.ReadAsStringAsync();
                var posts = JsonSerializer.Deserialize<List<Post>>(json, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new List<Post>();
                
                dispatch(new PostsLoaded(posts));
            }
            catch (Exception ex)
            {
                dispatch(new PostLoadFailed(ex.Message));
            }
            break;
    }
}
```

Key points:

- Use `try/catch` to handle errors
- Dispatch success or failure messages
- The runtime calls `Update` with the dispatched message

## Step 5: Initialize with a Command

Load posts immediately on startup:

```csharp
public static (Model, Command) Initialize(Url url, Arguments argument)
    => (new Model(
        Posts: new List<Post>(),
        State: new LoadingState.Loading(),
        SelectedPost: null
    ), new FetchPosts());  // Start loading immediately
```

## Step 6: Build the View

Handle each loading state:

```csharp
public static Document View(Model model)
    => new("Posts",
        div([class_("posts-app")], [
            Header(),
            model.State switch
            {
                LoadingState.Loading => LoadingView(),
                LoadingState.Failed failed => ErrorView(failed.Error),
                _ => ContentView(model)
            }
        ]));

static Node Header() =>
    header([], [
        h1([], [text("Posts")]),
        button([onclick(new RefreshClicked())], [text("Refresh")])
    ]);

static Node LoadingView() =>
    div([class_("loading")], [text("Loading posts...")]);

static Node ErrorView(string error) =>
    div([class_("error")], [
        text($"Error: {error}"),
        button([onclick(new RefreshClicked())], [text("Retry")])
    ]);

static Node ContentView(Model model) =>
    div([class_("content")], [
        model.SelectedPost is not null
            ? PostDetail(model.SelectedPost)
            : PostList(model.Posts)
    ]);

static Node PostList(List<Post> posts) =>
    ul([class_("post-list")], 
        posts.Select(post => 
            li([onclick(new PostSelected(post))], [
                text(post.Title)
            ])).ToArray());

static Node PostDetail(Post post) =>
    article([class_("post-detail")], [
        button([onclick(new ClearSelection())], [text("← Back")]),
        h2([], [text(post.Title)]),
        p([], [text(post.Body)])
    ]);
```

## Step 7: Complete Program

```csharp
using System.Net.Http;
using System.Text.Json;
using Abies;
using Abies.DOM;
using static Abies.Html.Elements;
using static Abies.Html.Attributes;
using static Abies.Html.Events;

await Runtime.Run<PostsApp, Arguments, Model>(new Arguments());

public record Arguments;

// API type
public record Post(int Id, int UserId, string Title, string Body);

// Loading state (sum type)
public record LoadingState
{
    public sealed record Idle : LoadingState;
    public sealed record Loading : LoadingState;
    public sealed record Failed(string Error) : LoadingState;
    public sealed record Success : LoadingState;
}

// Model
public record Model(List<Post> Posts, LoadingState State, Post? SelectedPost);

// Commands
public record FetchPosts : Command;

// Messages
public record PostsLoaded(List<Post> Posts) : Message;
public record PostLoadFailed(string Error) : Message;
public record PostSelected(Post Post) : Message;
public record RefreshClicked : Message;
public record ClearSelection : Message;

public class PostsApp : Program<Model, Arguments>
{
    private static readonly HttpClient _httpClient = new();

    public static (Model, Command) Initialize(Url url, Arguments argument)
        => (new Model(
            Posts: new List<Post>(),
            State: new LoadingState.Loading(),
            SelectedPost: null
        ), new FetchPosts());

    public static (Model model, Command command) Update(Message message, Model model)
        => message switch
        {
            RefreshClicked => 
                (model with { State = new LoadingState.Loading() }, new FetchPosts()),
            
            PostsLoaded loaded => 
                (model with { Posts = loaded.Posts, State = new LoadingState.Success() }, 
                 Commands.None),
            
            PostLoadFailed failed => 
                (model with { State = new LoadingState.Failed(failed.Error) }, 
                 Commands.None),
            
            PostSelected selected => 
                (model with { SelectedPost = selected.Post }, Commands.None),
            
            ClearSelection => 
                (model with { SelectedPost = null }, Commands.None),
            
            _ => (model, Commands.None)
        };

    public static Document View(Model model)
        => new("Posts",
            div([class_("posts-app")], [
                header([], [
                    h1([], [text("Posts")]),
                    button([onclick(new RefreshClicked())], [text("Refresh")])
                ]),
                model.State switch
                {
                    LoadingState.Loading => div([class_("loading")], [text("Loading...")]),
                    LoadingState.Failed failed => div([class_("error")], [
                        text($"Error: {failed.Error}"),
                        button([onclick(new RefreshClicked())], [text("Retry")])
                    ]),
                    _ => model.SelectedPost is not null
                        ? PostDetail(model.SelectedPost)
                        : PostList(model.Posts)
                }
            ]));

    static Node PostList(List<Post> posts) =>
        ul([class_("post-list")], 
            posts.Select(post => 
                li([onclick(new PostSelected(post))], [text(post.Title)])).ToArray());

    static Node PostDetail(Post post) =>
        article([], [
            button([onclick(new ClearSelection())], [text("← Back")]),
            h2([], [text(post.Title)]),
            p([], [text(post.Body)])
        ]);

    public static async Task HandleCommand(Command command, Func<Message, ValueTuple> dispatch)
    {
        switch (command)
        {
            case FetchPosts:
                try
                {
                    var response = await _httpClient.GetAsync(
                        "https://jsonplaceholder.typicode.com/posts");
                    response.EnsureSuccessStatusCode();
                    
                    var json = await response.Content.ReadAsStringAsync();
                    var posts = JsonSerializer.Deserialize<List<Post>>(json, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? new List<Post>();
                    
                    dispatch(new PostsLoaded(posts));
                }
                catch (Exception ex)
                {
                    dispatch(new PostLoadFailed(ex.Message));
                }
                break;
        }
    }

    public static Message OnUrlChanged(Url url) => new RefreshClicked();
    public static Message OnLinkClicked(UrlRequest urlRequest) => new RefreshClicked();
    public static Subscription Subscriptions(Model model) => SubscriptionModule.None;
}
```

## Testing Commands

Because `Update` is pure, testing is straightforward:

```csharp
[Fact]
public void RefreshClicked_SetsLoadingState_And_ReturnsFetchCommand()
{
    var model = new Model([], new LoadingState.Idle(), null);
    
    var (newModel, command) = PostsApp.Update(new RefreshClicked(), model);
    
    Assert.IsType<LoadingState.Loading>(newModel.State);
    Assert.IsType<FetchPosts>(command);
}

[Fact]
public void PostsLoaded_UpdatesPostsAndState()
{
    var model = new Model([], new LoadingState.Loading(), null);
    var posts = new List<Post> { new(1, 1, "Test", "Body") };
    
    var (newModel, command) = PostsApp.Update(new PostsLoaded(posts), model);
    
    Assert.Single(newModel.Posts);
    Assert.IsType<LoadingState.Success>(newModel.State);
    Assert.IsType<Command.None>(command);
}
```

## What You Learned

| Concept | Application |
| ------- | ----------- |
| Commands | Describe side effects to perform |
| HandleCommand | Execute effects and dispatch results |
| Loading states | Sum type for Idle/Loading/Failed/Success |
| Error handling | Try/catch in HandleCommand, dispatch failure |
| Initial commands | Return command from Initialize |

## Common Patterns

### POST/PUT/DELETE requests

```csharp
public record CreatePost(string Title, string Body) : Command;

case CreatePost create:
    var content = new StringContent(
        JsonSerializer.Serialize(new { create.Title, create.Body }),
        Encoding.UTF8, "application/json");
    var response = await _httpClient.PostAsync(url, content);
    // ...
    break;
```

### Debouncing requests

```csharp
// In model
public record Model(..., string SearchQuery, CancellationTokenSource? SearchCts);

// Cancel previous search, start new one
case SearchChanged changed:
    model.SearchCts?.Cancel();
    var cts = new CancellationTokenSource();
    return (model with { SearchQuery = changed.Query, SearchCts = cts },
            new SearchPosts(changed.Query, cts.Token));
```

### Batching commands

```csharp
return (model, Commands.Batch([
    new FetchPosts(),
    new FetchCategories(),
    new FetchTags()
]));
```

## Exercises

1. **Create posts**: Add a form to create new posts
2. **Delete posts**: Add delete functionality
3. **Pagination**: Load posts page by page
4. **Caching**: Don't refetch if data is recent

## Next Tutorial

→ [Tutorial 4: Routing](./04-routing.md) — Learn multi-page navigation
