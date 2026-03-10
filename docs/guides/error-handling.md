# Error Handling Guide

Patterns for handling errors gracefully in Abies applications.

## Overview

Error handling in MVU follows a predictable pattern:

1. **Model** stores error state
2. **Interpreter** executes commands that can fail
3. **Transition** handles result messages and updates model
4. **View** displays errors to users

Since the interpreter returns `Result<Message[], PipelineError>`, errors are always explicit — no hidden exceptions.

## Error State in Model

Store error information in your model:

```csharp
public record Model(
    bool IsLoading,
    IReadOnlyList<Article> Articles,
    string? Error
);
```

For complex loading states, use a discriminated union:

```csharp
public interface LoadState<T>
{
    record Loading : LoadState<T>;
    record Loaded(T Data) : LoadState<T>;
    record Failed(string Error) : LoadState<T>;
}

public record Model(
    LoadState<IReadOnlyList<Article>> ArticlesState
);
```

## Error Messages

Define message types for both success and failure:

```csharp
public interface AppMessage : Message
{
    // Success messages
    record struct ArticlesLoaded(IReadOnlyList<Article> Articles) : AppMessage;
    record struct ArticleCreated(Article Article) : AppMessage;

    // Error messages
    record struct LoadArticlesFailed(string Error) : AppMessage;
    record struct CreateArticleFailed(IReadOnlyList<string> Errors) : AppMessage;

    // User actions
    record struct DismissError : AppMessage;
    record struct RetryLoad : AppMessage;
}
```

## Handling Errors in the Interpreter

The interpreter returns `Result<Message[], PipelineError>`, making errors explicit:

```csharp
Interpreter<Command, AppMessage> interpreter = async command =>
{
    switch (command)
    {
        case LoadArticlesCommand:
            try
            {
                var response = await httpClient.GetAsync("/api/articles");

                if (response.IsSuccessStatusCode)
                {
                    var articles = await response.Content
                        .ReadFromJsonAsync<ArticlesResponse>();
                    return Ok<AppMessage>([new ArticlesLoaded(articles!.Articles)]);
                }

                var error = response.StatusCode switch
                {
                    HttpStatusCode.NotFound => "Articles not found",
                    HttpStatusCode.Unauthorized => "Please log in to view articles",
                    HttpStatusCode.Forbidden => "Access denied",
                    _ => $"Failed to load articles: {response.StatusCode}"
                };
                return Ok<AppMessage>([new LoadArticlesFailed(error)]);
            }
            catch (HttpRequestException ex)
            {
                return Ok<AppMessage>([new LoadArticlesFailed($"Network error: {ex.Message}")]);
            }

        default:
            return Ok<AppMessage>([]);
    }
};
```

**Key insight:** The interpreter catches exceptions and converts them into messages. The runtime never sees raw exceptions — all failure information flows through the message pipeline.

## Handling Errors in Transition

Process error messages and update the model:

```csharp
public static (Model, Command) Transition(Model model, Message msg)
    => msg switch
    {
        // Start loading
        LoadArticles =>
            (model with { IsLoading = true, Error = null },
             new LoadArticlesCommand()),

        // Success
        ArticlesLoaded loaded =>
            (model with { IsLoading = false, Articles = loaded.Articles },
             Commands.None),

        // Error
        LoadArticlesFailed failed =>
            (model with { IsLoading = false, Error = failed.Error },
             Commands.None),

        // Dismiss error
        DismissError =>
            (model with { Error = null }, Commands.None),

        // Retry
        RetryLoad =>
            (model with { Error = null, IsLoading = true },
             new LoadArticlesCommand()),

        _ => (model, Commands.None)
    };
```

## Displaying Errors

Show errors in the view using standard Node composition:

```csharp
using static Abies.Html.Elements;
using static Abies.Html.Attributes;

static Node ErrorBanner(string error) =>
    div([class_("error-banner"), role("alert")], [
        span([], [text(error)]),
        button([onclick(new DismissError())], [text("×")])
    ]);

public static Document View(Model model) =>
    new("App", div([], [
        model.Error is { } err ? ErrorBanner(err) : Element.Empty,
        model.IsLoading
            ? div([class_("loading")], [text("Loading...")])
            : ArticleList(model.Articles)
    ]));
```

### Form Validation Errors

Display multiple validation errors:

```csharp
static Node ValidationErrors(IReadOnlyList<string> errors) =>
    errors.Count == 0
        ? Element.Empty
        : ul([class_("error-list"), role("alert")],
            errors.Select(error =>
                li([class_("error-item")], [text(error)])
            ).ToArray());
```

## Global Error Handling

Wrap the interpreter to catch unhandled exceptions:

```csharp
static Interpreter<Command, AppMessage> WithErrorHandling(
    Interpreter<Command, AppMessage> inner) =>
    async command =>
    {
        try
        {
            return await inner(command);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unhandled error in {command.GetType().Name}: {ex}");
            return Ok<AppMessage>([new ApiError(
                "An unexpected error occurred",
                ex.Message)]);
        }
    };

// Usage:
var safeInterpreter = WithErrorHandling(interpreter);
```

This is the **Decorator pattern** applied to the interpreter delegate — it wraps the original interpreter with cross-cutting error handling behavior without modifying the original.

## Retry with Exponential Backoff

Implement retry for transient failures:

```csharp
public record Model(
    int RetryCount,
    int MaxRetries,
    string? Error,
    IReadOnlyList<Article> Articles
);

public static (Model, Command) Transition(Model model, Message msg)
    => msg switch
    {
        // Retry if under limit
        LoadArticlesFailed failed when model.RetryCount < model.MaxRetries =>
            (model with { RetryCount = model.RetryCount + 1 },
             new DelayedRetry(
                 TimeSpan.FromSeconds(Math.Pow(2, model.RetryCount)),
                 new LoadArticlesCommand())),

        // Give up after max retries
        LoadArticlesFailed failed =>
            (model with
             {
                 Error = $"Failed after {model.MaxRetries} retries: {failed.Error}"
             },
             Commands.None),

        // Reset on success
        ArticlesLoaded loaded =>
            (model with { RetryCount = 0, Articles = loaded.Articles },
             Commands.None),

        _ => (model, Commands.None)
    };
```

## Optimistic Updates with Rollback

Show changes immediately and roll back on failure:

```csharp
public record Model(
    IReadOnlyList<Article> Articles,
    Article? PendingDelete,
    string? Error
);

public static (Model, Command) Transition(Model model, Message msg)
    => msg switch
    {
        // Optimistically remove article
        DeleteArticle delete =>
        {
            var article = model.Articles.First(a => a.Slug == delete.Slug);
            var remaining = model.Articles
                .Where(a => a.Slug != delete.Slug).ToList();
            return (
                model with { Articles = remaining, PendingDelete = article },
                new DeleteArticleCommand(delete.Slug));
        },

        // Confirm deletion
        ArticleDeleted =>
            (model with { PendingDelete = null }, Commands.None),

        // Rollback on failure
        DeleteArticleFailed failed when model.PendingDelete is not null =>
        {
            var restored = model.Articles
                .Append(model.PendingDelete).ToList();
            return (
                model with
                {
                    Articles = restored,
                    PendingDelete = null,
                    Error = failed.Error
                },
                Commands.None);
        },

        _ => (model, Commands.None)
    };
```

## HTTP Status Code Mapping

Map status codes to user-friendly messages:

```csharp
static string GetErrorMessage(HttpStatusCode status, string? serverMessage = null)
    => status switch
    {
        HttpStatusCode.BadRequest => serverMessage ?? "Invalid request",
        HttpStatusCode.Unauthorized => "Please log in to continue",
        HttpStatusCode.Forbidden => "You don't have permission for this action",
        HttpStatusCode.NotFound => "The requested item was not found",
        HttpStatusCode.Conflict => serverMessage ?? "This conflicts with existing data",
        HttpStatusCode.TooManyRequests => "Too many requests. Please wait and try again.",
        HttpStatusCode.InternalServerError => "Server error. Please try again later.",
        HttpStatusCode.ServiceUnavailable => "Service is temporarily unavailable",
        _ => serverMessage ?? $"Request failed: {status}"
    };
```

## Best Practices

### 1. Convert Exceptions to Messages

```csharp
// ❌ Exception escapes interpreter
case LoadArticlesCommand:
    var articles = await httpClient.GetFromJsonAsync<List<Article>>("/api/articles");
    return Ok([new ArticlesLoaded(articles!)]);

// ✅ Exception caught and converted to message
case LoadArticlesCommand:
    try
    {
        var articles = await httpClient.GetFromJsonAsync<List<Article>>("/api/articles");
        return Ok([new ArticlesLoaded(articles!)]);
    }
    catch (Exception ex)
    {
        return Ok([new LoadArticlesFailed(ex.Message)]);
    }
```

### 2. Be Specific with Error Messages

```csharp
// ❌ Vague
return Ok([new LoadArticlesFailed("Something went wrong")]);

// ✅ Specific
return Ok([new LoadArticlesFailed("Network timeout after 30 seconds")]);
```

### 3. Always Allow Recovery

```csharp
// ❌ No way to recover
static Node ErrorView(string error) =>
    div([], [text(error)]);

// ✅ User can retry
static Node ErrorView(string error) =>
    div([], [
        text(error),
        button([onclick(new RetryLoad())], [text("Try Again")])
    ]);
```

### 4. Clear Errors on New Actions

```csharp
public static (Model, Command) Transition(Model model, Message msg)
    => msg switch
    {
        // Clear error when user takes new action
        LoadArticles =>
            (model with { Error = null, IsLoading = true },
             new LoadArticlesCommand()),

        // Clear error on navigation
        UrlChanged changed =>
            (model with { Error = null, Page = ParseRoute(changed.Url) },
             Commands.None),

        _ => (model, Commands.None)
    };
```

### 5. Log for Debugging

```csharp
// In the interpreter
catch (Exception ex)
{
    Console.WriteLine($"Error in {command.GetType().Name}: {ex}");
    return Ok([new LoadArticlesFailed(GetErrorMessage(ex))]);
}
```

## See Also

- [Commands & Effects](../concepts/commands-effects.md) — How interpreters work
- [Debugging](./debugging.md) — Troubleshooting errors
- [Testing](./testing.md) — Testing error paths
